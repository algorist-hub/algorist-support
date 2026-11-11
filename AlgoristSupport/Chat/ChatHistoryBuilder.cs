using Microsoft.Extensions.AI;

namespace AlgoristSupport.Chat;

/// <summary>
/// Builds the messages sent to the provider: the system instructions, the last N complete previous exchanges and
/// the current user message.
/// </summary>
public static class ChatHistoryBuilder
{
    /// <param name="systemPrompt">The full system instructions (see <see cref="SupportAssistantPrompt.Build"/>).</param>
    /// <param name="previousEntries">Conversation entries before the current user message.</param>
    /// <param name="currentUserMessage">The message being answered.</param>
    /// <param name="maxExchanges">Maximum number of previous exchanges to include (0 = none).</param>
    public static List<ChatMessage> Build (string systemPrompt, IEnumerable<ChatEntry> previousEntries, string currentUserMessage, int maxExchanges)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);
        ArgumentOutOfRangeException.ThrowIfNegative(maxExchanges);

        var exchanges = Pair(previousEntries.ToList());

        var messages = new List<ChatMessage> { SupportAssistantPrompt.CreateSystemMessage(systemPrompt) };
        foreach (var (user, assistant) in exchanges.Skip(Math.Max(0, exchanges.Count - maxExchanges)))
        {
            messages.Add(new ChatMessage(ChatRole.User, user.Text));
            messages.Add(new ChatMessage(ChatRole.Assistant, assistant.Text));
        }

        messages.Add(new ChatMessage(ChatRole.User, currentUserMessage));
        return messages;
    }

    /// <summary>
    /// A user entry immediately followed by an assistant reply that completed, or was interrupted with some text,
    /// forms an exchange. User entries whose reply failed (or has no text) are left out.
    /// </summary>
    private static List<(ChatEntry User, ChatEntry Assistant)> Pair (List<ChatEntry> entries)
    {
        var exchanges = new List<(ChatEntry, ChatEntry)>();
        for (var i = 0; i < entries.Count - 1; i++)
        {
            var user = entries[i];
            var reply = entries[i + 1];
            if (user.IsUser && !reply.IsUser && IsUsableReply(reply))
            {
                exchanges.Add((user, reply));
                i++;
            }
        }

        return exchanges;
    }

    private static bool IsUsableReply (ChatEntry reply) =>
        reply.Length > 0 && reply.Status is ChatEntryStatus.Complete or ChatEntryStatus.Interrupted;
}
