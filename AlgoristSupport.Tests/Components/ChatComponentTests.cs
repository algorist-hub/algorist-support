using AlgoristSupport.Chat;
using AlgoristSupport.Components.Chat;
using AlgoristSupport.Tests.Chat;
using Bunit;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AlgoristSupport.Tests.Components;

public class ChatComponentTests : BunitContext
{
    private static readonly string[] Suggestions = ["Como funciona a garantia?", "Quais serviços a Algorist oferece?"];

    private readonly FakeChatClient client = new();

    public ChatComponentTests ()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        Services.AddSingleton<IChatClient>(client);
        Services.AddSingleton(Options.Create(new SupportChatOptions
        {
            AssistantName = "Assistente Teste",
            MaxMessageLength = 500,
            SuggestedQuestions = Suggestions,
        }));
        Services.AddScoped<ChatSession>();
    }

    // ---- Header ----------------------------------------------------------------------------------------------

    [Fact]
    public void Header_shows_name_and_permanent_notice_without_close_control ()
    {
        var cut = Render<ChatHeader>(p => p.Add(x => x.AssistantName, "Assistente Teste"));

        Assert.Equal("Assistente Teste", cut.Find("h1").TextContent);
        var notice = cut.Find(".chat-notice").TextContent;
        Assert.Contains("assistente automatizado de demonstração", notice);
        Assert.Contains("política fictícia", notice);

        // The only button is "Nova conversa"; nothing dismisses the notice.
        var button = Assert.Single(cut.FindAll("button"));
        Assert.Contains("Nova conversa", button.TextContent);
    }

    // ---- Message list ----------------------------------------------------------------------------------------

    [Fact]
    public void Message_list_renders_markup_literally_and_labels_authors ()
    {
        IReadOnlyList<ChatEntry> entries =
        [
            new(ChatRole.User, "<b>olá</b>", ChatEntryStatus.Complete),
            new(ChatRole.Assistant, "Resposta", ChatEntryStatus.Complete),
        ];

        var cut = Render<ChatMessageList>(p => p
            .Add(x => x.Entries, entries)
            .Add(x => x.AssistantName, "Assistente Teste"));

        Assert.Equal("log", cut.Find("ol").GetAttribute("role"));
        Assert.Empty(cut.FindAll(".chat-text b"));
        Assert.Equal("<b>olá</b>", cut.FindAll(".chat-text")[0].TextContent);

        var labels = cut.FindAll(".visually-hidden").Select(e => e.TextContent).ToList();
        Assert.Equal(["Você disse:", "Assistente Teste disse:"], labels);
        Assert.Single(cut.FindAll(".chat-message-user"));
        Assert.Single(cut.FindAll(".chat-message-assistant"));
    }

    [Fact]
    public void Message_list_shows_interrupted_and_failed_markers ()
    {
        IReadOnlyList<ChatEntry> entries =
        [
            new(ChatRole.Assistant, "Parcial", ChatEntryStatus.Interrupted),
            new(ChatRole.Assistant, string.Empty, ChatEntryStatus.Failed),
        ];

        var cut = Render<ChatMessageList>(p => p
            .Add(x => x.Entries, entries)
            .Add(x => x.AssistantName, "Assistente Teste"));

        var bubbles = cut.FindAll(".chat-bubble");
        Assert.Contains("Parcial", bubbles[0].TextContent);
        Assert.Contains("Resposta interrompida", bubbles[0].TextContent);
        Assert.Contains("não foi possível gerar a resposta", bubbles[1].TextContent);
    }

    // ---- Suggested questions ---------------------------------------------------------------------------------

    [Fact]
    public void Suggestions_render_nothing_when_list_is_empty ()
    {
        var cut = Render<SuggestedQuestions>(p => p.Add(x => x.Questions, Array.Empty<string>()));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Panel_shows_chips_on_empty_conversation ()
    {
        var cut = Render<ChatPanel>();

        var chips = cut.FindAll(".chat-chip").Select(c => c.TextContent.Trim());
        Assert.Equal(Suggestions, chips);
    }

    [Fact]
    public void Clicking_chip_sends_question_and_hides_chips ()
    {
        var cut = Render<ChatPanel>();

        cut.FindAll(".chat-chip")[0].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Como funciona a garantia?", cut.Find(".chat-message-user .chat-text").TextContent);
            Assert.Empty(cut.FindAll(".chat-chip"));
        });
        client.Complete();
    }

    // ---- Composer --------------------------------------------------------------------------------------------

    private IRenderedComponent<ChatComposer> RenderComposer (string value, bool generating = false, int max = 500) =>
        Render<ChatComposer>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.MaxLength, max)
            .Add(x => x.IsGenerating, generating));

    [Fact]
    public void Counter_shows_current_and_max_length ()
    {
        var cut = RenderComposer(new string('a', 42));

        Assert.Equal("42/500", cut.Find("#chat-counter").TextContent);
        Assert.False(cut.Find(".chat-send").HasAttribute("disabled"));
    }

    [Fact]
    public void Over_limit_shows_error_and_disables_send ()
    {
        var cut = RenderComposer(new string('a', 501));

        Assert.Equal("501/500", cut.Find("#chat-counter").TextContent);
        Assert.Contains("chat-counter-error", cut.Find("#chat-counter").ClassList);
        Assert.Equal("A mensagem excede o limite de 500 caracteres.", cut.Find("#chat-feedback").TextContent);
        Assert.True(cut.Find(".chat-send").HasAttribute("disabled"));
    }

    [Fact]
    public void Exactly_at_limit_allows_send ()
    {
        var cut = RenderComposer(new string('a', 500));

        Assert.False(cut.Find(".chat-send").HasAttribute("disabled"));
        Assert.Empty(cut.Find("#chat-feedback").TextContent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \n\t ")]
    public void Send_disabled_for_empty_or_whitespace (string value)
    {
        var cut = RenderComposer(value);

        Assert.True(cut.Find(".chat-send").HasAttribute("disabled"));
    }

    [Fact]
    public void Generating_disables_send_and_shows_stop ()
    {
        var cut = RenderComposer("Pergunta válida", generating: true);

        Assert.True(cut.Find(".chat-send").HasAttribute("disabled"));
        Assert.Single(cut.FindAll(".chat-stop"));
    }

    [Fact]
    public void Stop_hidden_when_idle ()
    {
        var cut = RenderComposer("Pergunta válida");

        Assert.Empty(cut.FindAll(".chat-stop"));
    }

    [Fact]
    public void Submit_with_invalid_input_does_not_invoke_send ()
    {
        var sent = false;
        var cut = Render<ChatComposer>(p => p
            .Add(x => x.Value, "   ")
            .Add(x => x.MaxLength, 500)
            .Add(x => x.OnSend, () => sent = true));

        cut.Find("form").Submit();

        Assert.False(sent);
    }

    // ---- Panel integration -----------------------------------------------------------------------------------

    [Fact]
    public void Sending_streams_reply_with_generating_indicator_then_completes ()
    {
        var cut = Render<ChatPanel>();

        cut.Find("textarea").Input("  Qual o prazo de garantia?  ");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Qual o prazo de garantia?", cut.Find(".chat-message-user .chat-text").TextContent);
            Assert.Contains("Gerando resposta", cut.Find("[role=status]").TextContent);
            Assert.Single(cut.FindAll(".chat-stop"));
            Assert.Equal(string.Empty, cut.Find("textarea").GetAttribute("value") ?? string.Empty);
        });

        client.Push("Resposta ");
        client.Push("simulada.");
        cut.WaitForAssertion(() =>
            Assert.Equal("Resposta simulada.", cut.Find(".chat-message-assistant .chat-text").TextContent));

        client.Complete();
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Gerando resposta", cut.Find("[role=status]").TextContent);
            Assert.Empty(cut.FindAll(".chat-stop"));
        });
    }

    [Fact]
    public void Stop_button_interrupts_reply ()
    {
        var cut = Render<ChatPanel>();
        cut.Find("textarea").Input("Pergunta");
        cut.Find("form").Submit();
        client.Push("Parcial");
        cut.WaitForAssertion(() => Assert.Equal("Parcial", cut.Find(".chat-message-assistant .chat-text").TextContent));

        cut.Find(".chat-stop").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Resposta interrompida", cut.Find(".chat-message-assistant").TextContent);
            Assert.Empty(cut.FindAll(".chat-stop"));
        });
    }

    [Fact]
    public void New_conversation_clears_messages_and_restores_chips ()
    {
        client.AutoReply = ["ok"];
        var cut = Render<ChatPanel>();
        cut.Find("textarea").Input("Pergunta");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".chat-message")));

        cut.Find(".chat-new").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".chat-message"));
            Assert.Equal(Suggestions.Length, cut.FindAll(".chat-chip").Count);
        });
    }
}
