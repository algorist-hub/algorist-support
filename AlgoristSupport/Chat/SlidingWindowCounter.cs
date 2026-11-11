namespace AlgoristSupport.Chat;

/// <summary>
/// Allows at most <c>limit</c> acquisitions within any sliding <c>window</c>. Not thread-safe: used by one
/// <see cref="ChatSession"/>, whose UI events are serialized by the Blazor circuit.
/// </summary>
public sealed class SlidingWindowCounter
{
    private readonly Queue<DateTimeOffset> timestamps = new();
    private readonly int limit;
    private readonly TimeSpan window;
    private readonly TimeProvider timeProvider;

    public SlidingWindowCounter (int limit, TimeSpan window, TimeProvider timeProvider)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);
        this.limit = limit;
        this.window = window;
        this.timeProvider = timeProvider;
    }

    public int Limit => limit;

    /// <summary>
    /// Records an acquisition when under the limit. Otherwise returns <c>false</c> and how long until the oldest
    /// counted acquisition leaves the window.
    /// </summary>
    public bool TryAcquire (out TimeSpan retryAfter)
    {
        var now = timeProvider.GetUtcNow();
        while (timestamps.Count > 0 && now - timestamps.Peek() >= window)
        {
            timestamps.Dequeue();
        }

        if (timestamps.Count < limit)
        {
            timestamps.Enqueue(now);
            retryAfter = TimeSpan.Zero;
            return true;
        }

        retryAfter = timestamps.Peek() + window - now;
        return false;
    }
}
