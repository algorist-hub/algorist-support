using System.Text;
using Microsoft.Extensions.AI;

namespace AlgoristSupport.Chat;

public enum ChatEntryStatus
{
    Complete,
    Streaming,
    Interrupted,
    Failed,
}

/// <summary>
/// A message shown in the chat UI. Assistant entries grow while streaming.
/// </summary>
public sealed class ChatEntry
{
    private readonly StringBuilder text;

    public ChatEntry (ChatRole role, string text, ChatEntryStatus status)
    {
        Role = role;
        this.text = new StringBuilder(text);
        Status = status;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public ChatRole Role { get; }

    public string Text => text.ToString();

    public int Length => text.Length;

    public ChatEntryStatus Status { get; internal set; }

    public bool IsUser => Role == ChatRole.User;

    internal void Append (string chunk) => text.Append(chunk);
}
