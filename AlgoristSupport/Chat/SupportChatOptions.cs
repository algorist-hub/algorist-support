using System.ComponentModel.DataAnnotations;

namespace AlgoristSupport.Chat;

/// <summary>
/// Settings for the support chat, bound from the <c>Chat</c> configuration section.
/// </summary>
/// <remarks>
/// Named <c>SupportChatOptions</c> to avoid clashing with <see cref="Microsoft.Extensions.AI.ChatOptions"/>.
/// </remarks>
public sealed class SupportChatOptions
{
    public const string SectionName = "Chat";

    [Required(AllowEmptyStrings = false)]
    public string AssistantName { get; set; } = "Assistente Algorist";

    [Range(1, 4000)]
    public int MaxMessageLength { get; set; } = 500;

    public string[] SuggestedQuestions { get; set; } = [];

    /// <summary>A reply not completed within this time is cancelled and shown as failed.</summary>
    [Range(1, 600)]
    public int ReplyTimeoutSeconds { get; set; } = 30;
}
