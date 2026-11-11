using AlgoristSupport.Chat;
using Microsoft.Extensions.AI;

namespace AlgoristSupport.Tests.Chat;

public class ChatHistoryBuilderTests
{
    private static readonly string Prompt = SupportAssistantPrompt.Build(new PolicyDocument(TestChat.PolicyMarkdown));

    private static ChatEntry User (string text) => new(ChatRole.User, text, ChatEntryStatus.Complete);

    private static ChatEntry Reply (string text, ChatEntryStatus status = ChatEntryStatus.Complete) =>
        new(ChatRole.Assistant, text, status);

    private static readonly ChatEntry[] ThreeExchanges =
        [User("P1"), Reply("R1"), User("P2"), Reply("R2"), User("P3"), Reply("R3")];

    [Fact]
    public void System_message_first_then_last_N_exchanges_then_current ()
    {
        var messages = ChatHistoryBuilder.Build(Prompt, ThreeExchanges, "P4", maxExchanges: 2);

        Assert.Equal(ChatRole.System, messages[0].Role);
        Assert.Equal(Prompt, messages[0].Text);
        Assert.DoesNotContain("P4", messages[0].Text);
        Assert.Equal(["P2", "R2", "P3", "R3", "P4"], messages.Skip(1).Select(m => m.Text));
        Assert.Equal(
            [ChatRole.User, ChatRole.Assistant, ChatRole.User, ChatRole.Assistant, ChatRole.User],
            messages.Skip(1).Select(m => m.Role));
    }

    [Fact]
    public void Zero_exchanges_sends_only_system_and_current ()
    {
        var messages = ChatHistoryBuilder.Build(Prompt, ThreeExchanges, "P4", maxExchanges: 0);

        Assert.Equal([ChatRole.System, ChatRole.User], messages.Select(m => m.Role));
        Assert.Equal("P4", messages[1].Text);
    }

    [Fact]
    public void N_larger_than_history_sends_everything ()
    {
        var messages = ChatHistoryBuilder.Build(Prompt, ThreeExchanges, "P4", maxExchanges: 10);

        Assert.Equal(["P1", "R1", "P2", "R2", "P3", "R3", "P4"], messages.Skip(1).Select(m => m.Text));
    }

    [Fact]
    public void Failed_and_empty_interrupted_exchanges_are_dropped ()
    {
        ChatEntry[] entries =
        [
            User("P1"), Reply("R1"),
            User("P2"), Reply("meio", ChatEntryStatus.Failed),
            User("P3"), Reply(string.Empty, ChatEntryStatus.Interrupted),
            User("P4"), Reply("Parcial", ChatEntryStatus.Interrupted),
        ];

        var messages = ChatHistoryBuilder.Build(Prompt, entries, "P5", maxExchanges: 10);

        Assert.Equal(["P1", "R1", "P4", "Parcial", "P5"], messages.Skip(1).Select(m => m.Text));
    }

    [Fact]
    public void Dropped_exchanges_do_not_count_towards_N ()
    {
        ChatEntry[] entries =
        [
            User("P1"), Reply("R1"),
            User("P2"), Reply(string.Empty, ChatEntryStatus.Failed),
        ];

        var messages = ChatHistoryBuilder.Build(Prompt, entries, "P3", maxExchanges: 1);

        Assert.Equal(["P1", "R1", "P3"], messages.Skip(1).Select(m => m.Text));
    }
}
