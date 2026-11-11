namespace AlgoristSupport.Chat;

/// <summary>The fictional service policy (Markdown, pt-BR): the assistant's only knowledge source.</summary>
public sealed record PolicyDocument(string Markdown);

/// <summary>Why the policy could not be loaded.</summary>
public enum PolicyLoadProblem
{
    Missing,
    Unreadable,
    Empty,
}

/// <summary>
/// Outcome of loading the policy at startup: either a <see cref="Document"/> or a <see cref="Problem"/>.
/// Registered as a singleton; consumed by <see cref="ChatAvailability"/> and the policy page.
/// </summary>
public sealed record PolicyLoadResult(string Path, PolicyDocument? Document, PolicyLoadProblem? Problem)
{
    public bool IsLoaded => Document is not null;

    public static PolicyLoadResult Loaded (string path, PolicyDocument document) => new(path, document, null);

    public static PolicyLoadResult Failed (string path, PolicyLoadProblem problem) => new(path, null, problem);
}

/// <summary>Reads the policy file. Never throws for a missing, unreadable or empty file: the app must still start.</summary>
public static class PolicyDocumentLoader
{
    /// <summary>
    /// Location of the policy in the app output (<c>Policy/policy.md</c>, linked from docs/policy.md by the csproj).
    /// </summary>
    public static string DefaultPath => System.IO.Path.Combine(AppContext.BaseDirectory, "Policy", "policy.md");

    public static PolicyLoadResult Load (string path)
    {
        if (!File.Exists(path))
        {
            return PolicyLoadResult.Failed(path, PolicyLoadProblem.Missing);
        }

        string markdown;
        try
        {
            markdown = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return PolicyLoadResult.Failed(path, PolicyLoadProblem.Unreadable);
        }

        return string.IsNullOrWhiteSpace(markdown)
            ? PolicyLoadResult.Failed(path, PolicyLoadProblem.Empty)
            : PolicyLoadResult.Loaded(path, new PolicyDocument(markdown.Trim()));
    }
}
