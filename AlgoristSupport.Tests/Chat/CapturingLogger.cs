using Microsoft.Extensions.Logging;

namespace AlgoristSupport.Tests.Chat;

/// <summary>Logger that records every entry (formatted message, raw state and exception) for assertions.</summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<LogRecord> Records { get; } = [];

    public IDisposable? BeginScope<TState> (TState state) where TState : notnull => null;

    public bool IsEnabled (LogLevel logLevel) => true;

    public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var values = state is IEnumerable<KeyValuePair<string, object?>> pairs
            ? string.Join(" | ", pairs.Select(p => $"{p.Key}={p.Value}"))
            : string.Empty;
        Records.Add(new LogRecord(logLevel, formatter(state, exception), values, exception));
    }

    /// <summary>Everything a log sink could persist for all records, concatenated.</summary>
    public string AllText => string.Join('\n', Records.Select(r => $"{r.Message}\n{r.StateValues}\n{r.Exception}"));
}

internal sealed record LogRecord(LogLevel Level, string Message, string StateValues, Exception? Exception);
