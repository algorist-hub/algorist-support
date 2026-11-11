namespace AlgoristSupport.Chat;

/// <summary>
/// Whether the AI provider is configured. When it is not, the app still runs and the chat shows a maintenance
/// message instead of accepting messages. Registered as a singleton, computed once at startup.
/// </summary>
public sealed class ChatAvailability
{
    public ChatAvailability (ChatProviderLoadResult loadResult)
    {
        Settings = loadResult.Settings;
        InvalidSettings = loadResult.InvalidSettings;
    }

    public ChatProviderSettings? Settings { get; }

    /// <summary>Names (never values) of the missing or invalid settings.</summary>
    public IReadOnlyList<string> InvalidSettings { get; }

    public bool IsAvailable => Settings is not null;

    public static ChatAvailability FromConfiguration (IConfiguration configuration) =>
        new(ChatProviderSettings.Load(configuration));

    /// <summary>Logs a startup warning naming the offending settings. Values are never logged.</summary>
    public void LogStatus (ILogger logger)
    {
        if (IsAvailable)
        {
            return;
        }

        logger.LogWarning(
            "Support chat is in maintenance mode: missing or invalid settings: {InvalidSettings}.",
            string.Join(", ", InvalidSettings));
    }
}
