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

    public SimulationOptions Simulation { get; set; } = new();

    public sealed class SimulationOptions
    {
        [Range(0, 10_000)]
        public int InitialDelayMs { get; set; } = 400;

        [Range(0, 5_000)]
        public int ChunkDelayMs { get; set; } = 60;
    }
}
