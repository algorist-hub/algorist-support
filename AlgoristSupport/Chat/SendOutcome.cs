namespace AlgoristSupport.Chat;

public enum SendOutcome
{
    /// <summary>The message (or retry) was accepted and a reply is being generated.</summary>
    Accepted,

    /// <summary>Invalid input, or nothing to retry.</summary>
    Invalid,

    /// <summary>A reply is already in progress.</summary>
    Busy,

    /// <summary>The per-minute limit was reached; see <see cref="ChatSession.Notice"/>.</summary>
    RateLimited,

    /// <summary>The AI provider is not configured (maintenance mode).</summary>
    Unavailable,
}

/// <summary>
/// Result of starting a send or retry. <see cref="Outcome"/> is known synchronously, so callers can react to
/// acceptance (e.g. clear the input) before awaiting <see cref="Completion"/>, which ends with the reply.
/// </summary>
public readonly record struct SendAttempt(SendOutcome Outcome, Task Completion)
{
    public bool IsAccepted => Outcome == SendOutcome.Accepted;

    public static SendAttempt Rejected (SendOutcome outcome) => new(outcome, Task.CompletedTask);
}
