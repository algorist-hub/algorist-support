using AlgoristSupport.Chat;
using AlgoristSupport.Components.Chat;
using AlgoristSupport.Tests.Chat;
using Bunit;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace AlgoristSupport.Tests.Components;

public class ChatComponentTests : BunitContext
{
    private static readonly string[] Suggestions = ["Como funciona a garantia?", "Quais serviços a Algorist oferece?"];

    private readonly FakeChatClient client = new();
    private readonly FakeTimeProvider time = new();

    public ChatComponentTests ()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        Services.AddSingleton<IChatClient>(client);
        Services.AddSingleton<TimeProvider>(time);
        Services.AddSingleton(TestChat.Available());
        Services.AddSingleton(TestChat.Sections());
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

        // The visible name lives in the brand bar; the header keeps it only as a screen-reader heading.
        var heading = cut.Find("h1");
        Assert.Equal("Assistente Teste", heading.TextContent);
        Assert.Contains("visually-hidden", heading.ClassList);
        var notice = cut.Find(".chat-notice").TextContent;
        Assert.Contains("Assistente automatizado de demonstração", notice);
        Assert.Contains("política fictícia", notice);

        // No buttons at all: nothing dismisses the notice, and "Nova conversa" moved to the composer.
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Header_notice_ends_with_repository_link_in_a_new_window ()
    {
        var cut = Render<ChatHeader>(p => p.Add(x => x.AssistantName, "Assistente Teste"));

        var link = cut.Find(".chat-notice a.chat-repo-link");
        Assert.Equal("https://github.com/algorist-hub/algorist-support", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
        Assert.Equal("https://github.com/algorist-hub/algorist-support (abre em nova janela)", link.TextContent);
        Assert.Equal(" (abre em nova janela)", link.QuerySelector(".visually-hidden")!.TextContent);

        // The repository sentence is the end of the notice (ignoring the screen-reader-only hint).
        var visibleText = cut.Find(".chat-notice").TextContent.Replace(" (abre em nova janela)", string.Empty);
        Assert.EndsWith(
            "Repositório do projeto: https://github.com/algorist-hub/algorist-support",
            System.Text.RegularExpressions.Regex.Replace(visibleText, @"\s+", " ").Trim());
    }

    [Fact]
    public void Header_notice_links_to_policy_page_in_a_new_window ()
    {
        var cut = Render<ChatHeader>(p => p.Add(x => x.AssistantName, "Assistente Teste"));

        var link = cut.Find(".chat-notice a.chat-policy-link");
        Assert.Equal("politica", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
        Assert.Equal("política fictícia (abre em nova janela)", link.TextContent);
        Assert.Equal(" (abre em nova janela)", link.QuerySelector(".visually-hidden")!.TextContent);
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
    public void Message_list_renders_assistant_markdown_as_formatting ()
    {
        IReadOnlyList<ChatEntry> entries =
        [
            new(ChatRole.User, "**literal**", ChatEntryStatus.Complete),
            new(ChatRole.Assistant, "A **Algorist** oferece:\n\n- *suporte*\n- `garantia`", ChatEntryStatus.Complete),
        ];

        var cut = Render<ChatMessageList>(p => p
            .Add(x => x.Entries, entries)
            .Add(x => x.AssistantName, "Assistente Teste"));

        // User text stays literal.
        Assert.Equal("**literal**", cut.Find(".chat-message-user .chat-text").TextContent);

        var reply = cut.Find(".chat-message-assistant .chat-text");
        Assert.Equal("Algorist", reply.QuerySelector("strong")!.TextContent);
        Assert.Equal("suporte", reply.QuerySelector("li em")!.TextContent);
        Assert.Equal("garantia", reply.QuerySelector("li code")!.TextContent);
        Assert.DoesNotContain("*", reply.TextContent);
    }

    [Fact]
    public void Message_list_escapes_html_and_drops_unsafe_links_in_assistant_markdown ()
    {
        IReadOnlyList<ChatEntry> entries =
        [
            new(ChatRole.Assistant,
                "<script>alert(1)</script> [ruim](javascript:alert(1)) [site](https://example.com)",
                ChatEntryStatus.Complete),
        ];

        var cut = Render<ChatMessageList>(p => p
            .Add(x => x.Entries, entries)
            .Add(x => x.AssistantName, "Assistente Teste"));

        var reply = cut.Find(".chat-message-assistant .chat-text");
        Assert.Empty(reply.QuerySelectorAll("script"));
        Assert.Contains("<script>alert(1)</script>", reply.TextContent);

        var links = reply.QuerySelectorAll("a");
        Assert.DoesNotContain(links, a => (a.GetAttribute("href") ?? "").Contains("javascript", StringComparison.OrdinalIgnoreCase));

        var safe = Assert.Single(links, a => a.GetAttribute("href") == "https://example.com");
        Assert.Equal("_blank", safe.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", safe.GetAttribute("rel"));
    }

    [Fact]
    public void Message_list_links_section_citations_in_finished_replies ()
    {
        IReadOnlyList<ChatEntry> entries =
        [
            new(ChatRole.Assistant, "A garantia é de 90 dias (Seção 12).", ChatEntryStatus.Complete),
            new(ChatRole.Assistant, "Parcial (Seção 13)", ChatEntryStatus.Interrupted),
        ];

        var cut = Render<ChatMessageList>(p => p
            .Add(x => x.Entries, entries)
            .Add(x => x.AssistantName, "Assistente Teste"));

        var links = cut.FindAll(".chat-message-assistant a.policy-citation");
        Assert.Equal(["politica#secao-12", "politica#secao-13"], links.Select(a => a.GetAttribute("href")));
        Assert.Equal("_blank", links[0].GetAttribute("target"));
        Assert.Equal("Seção 12 — Garantia (abre em nova janela)", links[0].GetAttribute("aria-label"));
    }

    [Fact]
    public void Message_list_does_not_link_citations_while_streaming_or_in_user_messages ()
    {
        IReadOnlyList<ChatEntry> entries =
        [
            new(ChatRole.User, "O que diz a Seção 12?", ChatEntryStatus.Complete),
            new(ChatRole.Assistant, "A garantia (Seção 12)", ChatEntryStatus.Streaming),
        ];

        var cut = Render<ChatMessageList>(p => p
            .Add(x => x.Entries, entries)
            .Add(x => x.AssistantName, "Assistente Teste"));

        Assert.Empty(cut.FindAll("a"));
        Assert.Equal("O que diz a Seção 12?", cut.Find(".chat-message-user .chat-text").TextContent);
        Assert.Contains("Seção 12", cut.Find(".chat-message-assistant .chat-text").TextContent);
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
    public void New_conversation_button_is_in_the_composer_row_before_the_counter ()
    {
        var cut = RenderComposer("Pergunta válida");

        var footerChildren = cut.Find(".chat-composer-footer").QuerySelectorAll("button, #chat-counter").ToList();
        Assert.Equal("Limpar", footerChildren[0].TextContent.Trim());
        Assert.Equal("chat-counter", footerChildren[1].Id);
        Assert.Equal("button", cut.Find(".chat-new").GetAttribute("type"));
    }

    [Fact]
    public void New_conversation_button_invokes_callback_without_submitting ()
    {
        var cleared = 0;
        var sent = 0;
        var cut = Render<ChatComposer>(p => p
            .Add(x => x.Value, "Pergunta válida")
            .Add(x => x.MaxLength, 500)
            .Add(x => x.OnSend, () => sent++)
            .Add(x => x.OnNewConversation, () => cleared++));

        cut.Find(".chat-new").Click();

        Assert.Equal(1, cleared);
        Assert.Equal(0, sent);
    }

    [Fact]
    public void New_conversation_button_enabled_while_generating ()
    {
        var cut = RenderComposer("Pergunta válida", generating: true);

        Assert.False(cut.Find(".chat-new").HasAttribute("disabled"));
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
        client.Push("gerada.");
        cut.WaitForAssertion(() =>
            Assert.Equal("Resposta gerada.", cut.Find(".chat-message-assistant .chat-text").TextContent));

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

    [Fact]
    public void New_conversation_during_generation_stops_reply_and_clears_history ()
    {
        var cut = Render<ChatPanel>();
        cut.Find("textarea").Input("Pergunta");
        cut.Find("form").Submit();
        client.Push("Parcial");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".chat-stop")));

        cut.Find(".chat-composer .chat-new").Click();
        client.Push(" texto tardio");

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".chat-message"));
            Assert.Empty(cut.FindAll(".chat-stop"));
            Assert.Equal(Suggestions.Length, cut.FindAll(".chat-chip").Count);
        });
    }

    [Fact]
    public void Header_has_no_new_conversation_button ()
    {
        var cut = Render<ChatPanel>();

        Assert.Empty(cut.FindAll(".chat-header button"));
        Assert.Single(cut.FindAll(".chat-composer .chat-new"));
    }

    // ---- Maintenance mode ------------------------------------------------------------------------------------

    [Fact]
    public void Missing_policy_puts_chat_in_maintenance_with_policy_link_still_shown ()
    {
        Services.AddSingleton(TestChat.WithoutPolicy());

        var cut = Render<ChatPanel>();

        Assert.Contains("O chat está em manutenção", cut.Find(".chat-maintenance").TextContent);
        Assert.Empty(cut.FindAll("textarea"));
        Assert.Equal("politica", cut.Find(".chat-notice a.chat-policy-link").GetAttribute("href"));
    }

    [Fact]
    public void Maintenance_mode_shows_message_and_no_way_to_send ()
    {
        Services.AddSingleton(TestChat.Unavailable());

        var cut = Render<ChatPanel>();

        Assert.Contains("O chat está em manutenção", cut.Find(".chat-maintenance").TextContent);
        Assert.Equal("alert", cut.Find(".chat-maintenance").GetAttribute("role"));
        Assert.Equal("Assistente Teste", cut.Find("h1").TextContent);
        Assert.Contains("demonstração", cut.Find(".chat-notice").TextContent);
        Assert.Equal("politica", cut.Find(".chat-notice a.chat-policy-link").GetAttribute("href"));
        Assert.Equal("https://github.com/algorist-hub/algorist-support", cut.Find(".chat-notice a.chat-repo-link").GetAttribute("href"));
        Assert.Empty(cut.FindAll("textarea"));
        Assert.Empty(cut.FindAll(".chat-send"));
        Assert.Empty(cut.FindAll(".chat-new"));
        Assert.Empty(cut.FindAll(".chat-chip"));
        Assert.Empty(client.Requests);
    }

    /// <summary>Waits until the reply finished and the panel re-rendered, so element handles are not stale.</summary>
    private static void WaitUntilIdleWithMessages (IRenderedComponent<ChatPanel> cut, int count) =>
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(count, cut.FindAll(".chat-message").Count);
            Assert.Empty(cut.FindAll(".chat-stop"));
            Assert.DoesNotContain("Gerando resposta", cut.Find("[role=status]").TextContent);
        });

    /// <summary>
    /// Types and submits, retrying if a trailing render (after the previous send's handler completed) made the
    /// element handles stale.
    /// </summary>
    private static void TypeAndSubmit (IRenderedComponent<ChatPanel> cut, string text) =>
        cut.WaitForAssertion(() =>
        {
            cut.Find("textarea").Input(text);
            cut.Find("form").Submit();
        });

    // ---- Per-minute limit ------------------------------------------------------------------------------------

    [Fact]
    public void Rate_limited_message_stays_in_input_and_notice_is_shown ()
    {
        Services.AddSingleton(TestChat.Available(maxPerMinute: 1));
        client.AutoReply = ["ok"];
        var cut = Render<ChatPanel>();
        cut.Find("textarea").Input("Primeira");
        cut.Find("form").Submit();
        WaitUntilIdleWithMessages(cut, 2);

        TypeAndSubmit(cut, "Segunda");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("limite de 1 mensagens por minuto", cut.Find(".chat-limit-notice").TextContent);
            Assert.Equal("Segunda", cut.Find("textarea").GetAttribute("value"));
            Assert.Equal(2, cut.FindAll(".chat-message").Count);
        });
        Assert.Single(client.Requests);
    }

    [Fact]
    public void Suggested_question_counts_towards_the_limit ()
    {
        Services.AddSingleton(TestChat.Available(maxPerMinute: 1));
        client.AutoReply = ["ok"];
        var cut = Render<ChatPanel>();
        cut.FindAll(".chat-chip")[0].Click();
        WaitUntilIdleWithMessages(cut, 2);

        TypeAndSubmit(cut, "Outra pergunta");

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".chat-limit-notice")));
        Assert.Single(client.Requests);
    }

    // ---- Try again -------------------------------------------------------------------------------------------

    [Fact]
    public void Retry_button_shown_only_on_last_failed_entry_when_allowed ()
    {
        IReadOnlyList<ChatEntry> entries =
        [
            new(ChatRole.User, "P1", ChatEntryStatus.Complete),
            new(ChatRole.Assistant, string.Empty, ChatEntryStatus.Failed),
            new(ChatRole.User, "P2", ChatEntryStatus.Complete),
            new(ChatRole.Assistant, string.Empty, ChatEntryStatus.Failed),
        ];

        var allowed = Render<ChatMessageList>(p => p
            .Add(x => x.Entries, entries)
            .Add(x => x.AssistantName, "Assistente Teste")
            .Add(x => x.CanRetry, true));
        var notAllowed = Render<ChatMessageList>(p => p
            .Add(x => x.Entries, entries)
            .Add(x => x.AssistantName, "Assistente Teste")
            .Add(x => x.CanRetry, false));

        var button = Assert.Single(allowed.FindAll(".chat-retry"));
        Assert.Equal("Tentar novamente", button.TextContent);
        Assert.Same(allowed.FindAll(".chat-message")[3], button.Closest(".chat-message"));
        Assert.Empty(notAllowed.FindAll(".chat-retry"));
    }

    [Fact]
    public void Clicking_retry_streams_a_new_reply_for_the_same_question ()
    {
        var cut = Render<ChatPanel>();
        cut.Find("textarea").Input("Pergunta");
        cut.Find("form").Submit();
        client.Fail(new InvalidOperationException("boom"));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".chat-retry")));

        client.AutoReply = ["Nova resposta"];
        cut.Find(".chat-retry").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Nova resposta", cut.Find(".chat-message-assistant .chat-text").TextContent);
            Assert.Single(cut.FindAll(".chat-message-user"));
            Assert.Empty(cut.FindAll(".chat-retry"));
            Assert.Empty(cut.FindAll(".chat-status-error"));
        });
        Assert.Equal(2, client.Requests.Count);
    }
}
