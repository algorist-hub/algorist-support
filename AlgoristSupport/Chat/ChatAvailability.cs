namespace AlgoristSupport.Chat;

/// <summary>
/// Whether the chat can answer: the AI provider must be configured and the policy (its only knowledge source) must
/// be loaded. When either is missing, the app still runs and the chat shows a maintenance message instead of
/// accepting messages. Registered as a singleton, computed once at startup.
/// </summary>
public sealed class ChatAvailability
{
    private readonly PolicyLoadResult policyResult;

    public ChatAvailability (ChatProviderLoadResult loadResult, PolicyLoadResult policyResult)
    {
        Settings = loadResult.Settings;
        InvalidSettings = loadResult.InvalidSettings;
        this.policyResult = policyResult;
    }

    public ChatProviderSettings? Settings { get; }

    /// <summary>Names (never values) of the missing or invalid settings.</summary>
    public IReadOnlyList<string> InvalidSettings { get; }

    public PolicyDocument? Policy => policyResult.Document;

    public PolicyLoadProblem? PolicyProblem => policyResult.Problem;

    public bool IsAvailable => Settings is not null && Policy is not null;

    public static ChatAvailability FromConfiguration (IConfiguration configuration, PolicyLoadResult policyResult) =>
        new(ChatProviderSettings.Load(configuration), policyResult);

    /// <summary>
    /// Logs a startup warning naming the offending settings and another one identifying the policy problem.
    /// Setting values and policy content are never logged.
    /// </summary>
    public void LogStatus (ILogger logger)
    {
        if (InvalidSettings.Count > 0)
        {
            logger.LogWarning(
                "Support chat is in maintenance mode: missing or invalid settings: {InvalidSettings}.",
                string.Join(", ", InvalidSettings));
        }

        if (PolicyProblem is { } problem)
        {
            logger.LogWarning(
                "Support chat is in maintenance mode: policy document {PolicyPath} is {PolicyProblem}.",
                policyResult.Path,
                problem.ToString().ToLowerInvariant());
        }
    }
}
