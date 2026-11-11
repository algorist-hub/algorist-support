using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace AlgoristSupport.Chat;

/// <summary>
/// Conversation state and reply lifecycle for one Blazor circuit (registered as a scoped service).
/// History lives only in memory and is discarded with the circuit.
/// </summary>
/// <remarks>
/// Blazor Server serializes UI events per circuit, so the <see cref="IsGenerating"/> flag is enough to
/// reject concurrent sends; no locking is needed.
/// Logs never contain message text (user, assistant or system): only metadata.
/// </remarks>
public sealed class ChatSession : IDisposable
{
    private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(1);

    private readonly IChatClient chatClient;
    private readonly ChatAvailability availability;
    private readonly IOptions<SupportChatOptions> options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ChatSession> logger;
    private readonly SlidingWindowCounter? rateLimiter;
    private readonly string? systemPrompt;
    private readonly List<ChatEntry> entries = [];
    private CancellationTokenSource? currentGeneration;

    public ChatSession (
        IChatClient chatClient,
        ChatAvailability availability,
        IOptions<SupportChatOptions> options,
        TimeProvider timeProvider,
        ILogger<ChatSession> logger)
    {
        this.chatClient = chatClient;
        this.availability = availability;
        this.options = options;
        this.timeProvider = timeProvider;
        this.logger = logger;

        if (availability.Settings is { } settings)
        {
            rateLimiter = new SlidingWindowCounter(settings.MaxMessagesPerMinute, RateWindow, timeProvider);
        }

        // Built once per circuit from the policy loaded at startup; null only in maintenance mode (nothing is sent).
        if (availability.Policy is { } policy)
        {
            systemPrompt = SupportAssistantPrompt.Build(policy);
        }
    }

    /// <summary>Raised whenever entries, the generating state or the notice change.</summary>
    public event Action? Changed;

    public IReadOnlyList<ChatEntry> Entries => entries;

    public bool IsAvailable => availability.IsAvailable;

    public bool IsGenerating => currentGeneration is not null;

    /// <summary>pt-BR feedback for a rejected attempt (e.g. the per-minute limit); cleared on the next accepted one.</summary>
    public string? Notice { get; private set; }

    public int MaxMessageLength => options.Value.MaxMessageLength;

    public MessageValidationResult Validate (string? input) => ChatMessageValidator.Validate(input, MaxMessageLength);

    public bool CanSend (string? input) => IsAvailable && !IsGenerating && Validate(input).IsValid;

    /// <summary>True when the last entry is a failed reply that can be regenerated now.</summary>
    public bool CanRetry => IsAvailable && !IsGenerating && HasRetryableFailure;

    private bool HasRetryableFailure =>
        entries.Count >= 2
        && entries[^1] is { IsUser: false, Status: ChatEntryStatus.Failed }
        && entries[^2].IsUser;

    /// <summary>
    /// Starts sending a user message. All checks run synchronously, so <see cref="SendAttempt.Outcome"/> is known
    /// before the reply streams; a rejected attempt changes no entries.
    /// </summary>
    public SendAttempt Send (string? input)
    {
        if (Reject(Validate(input).IsValid) is { } rejection)
        {
            return rejection;
        }

        var text = input!.Trim();
        var history = ChatHistoryBuilder.Build(systemPrompt!, entries, text, availability.Settings!.MaxHistoryExchanges);
        entries.Add(new ChatEntry(ChatRole.User, text, ChatEntryStatus.Complete));

        return new SendAttempt(SendOutcome.Accepted, GenerateAsync(history));
    }

    /// <summary>Sends a user message and waits for the reply to finish.</summary>
    public async Task<SendOutcome> SendAsync (string? input)
    {
        var attempt = Send(input);
        await attempt.Completion;
        return attempt.Outcome;
    }

    /// <summary>
    /// Regenerates the last failed reply for the same user message (no duplicate user entry). Subject to the same
    /// checks as <see cref="Send"/>; when rejected the failed reply stays.
    /// </summary>
    public SendAttempt Retry ()
    {
        if (Reject(HasRetryableFailure) is { } rejection)
        {
            return rejection;
        }

        entries.RemoveAt(entries.Count - 1);
        var userIndex = entries.Count - 1;
        var history = ChatHistoryBuilder.Build(systemPrompt!, entries.Take(userIndex), entries[userIndex].Text, availability.Settings!.MaxHistoryExchanges);

        return new SendAttempt(SendOutcome.Accepted, GenerateAsync(history));
    }

    public async Task<SendOutcome> RetryAsync ()
    {
        var attempt = Retry();
        await attempt.Completion;
        return attempt.Outcome;
    }

    /// <summary>Stops the reply in progress, keeping any text already streamed.</summary>
    public void Stop ()
    {
        var generation = currentGeneration;
        if (generation is null)
        {
            return;
        }

        currentGeneration = null;
        generation.Cancel();

        // The streaming loop may not observe cancellation until its next await; mark the entry now so the UI
        // reflects the stop immediately.
        if (entries.Count > 0 && entries[^1] is { IsUser: false, Status: ChatEntryStatus.Streaming } reply)
        {
            reply.Status = ChatEntryStatus.Interrupted;
        }

        RaiseChanged();
    }

    /// <summary>Stops any reply in progress and clears the conversation.</summary>
    public void Reset ()
    {
        Stop();
        entries.Clear();
        Notice = null;
        RaiseChanged();
    }

    public void Dispose ()
    {
        var generation = currentGeneration;
        currentGeneration = null;
        generation?.Cancel();
        Changed = null;
    }

    /// <summary>Common acceptance checks; returns a rejection, or <c>null</c> when accepted (rate slot taken).</summary>
    private SendAttempt? Reject (bool requestIsValid)
    {
        if (!IsAvailable)
        {
            return SendAttempt.Rejected(SendOutcome.Unavailable);
        }

        if (IsGenerating)
        {
            return SendAttempt.Rejected(SendOutcome.Busy);
        }

        if (!requestIsValid)
        {
            return SendAttempt.Rejected(SendOutcome.Invalid);
        }

        if (!rateLimiter!.TryAcquire(out var retryAfter))
        {
            var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            Notice = $"Você atingiu o limite de {rateLimiter.Limit} mensagens por minuto. Aguarde cerca de {seconds} segundos e tente novamente.";
            RaiseChanged();
            return SendAttempt.Rejected(SendOutcome.RateLimited);
        }

        Notice = null;
        return null;
    }

    private async Task GenerateAsync (List<ChatMessage> history)
    {
        var reply = new ChatEntry(ChatRole.Assistant, string.Empty, ChatEntryStatus.Streaming);
        entries.Add(reply);

        // Mark as generating synchronously, before the first await.
        var generation = new CancellationTokenSource();
        currentGeneration = generation;
        var timeoutSeconds = options.Value.ReplyTimeoutSeconds;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds), timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(generation.Token, timeout.Token);
        var token = linked.Token;
        var started = timeProvider.GetTimestamp();
        UsageDetails? usage = null;
        ChatFinishReason? finishReason = null;
        RaiseChanged();

        try
        {
            await foreach (var update in chatClient.GetStreamingResponseAsync(history, cancellationToken: token).WithCancellation(token))
            {
                token.ThrowIfCancellationRequested();
                if (!string.IsNullOrEmpty(update.Text))
                {
                    reply.Append(update.Text);
                    RaiseChanged();
                }

                foreach (var content in update.Contents.OfType<UsageContent>())
                {
                    usage = content.Details;
                }

                finishReason = update.FinishReason ?? finishReason;
            }

            // A reply with no text (e.g. the whole token limit spent on internal reasoning) is a failure, not an
            // empty "completed" bubble.
            if (reply.Length == 0)
            {
                reply.Status = ChatEntryStatus.Failed;
                logger.LogWarning(
                    "Chat reply ended without text after {ElapsedMs} ms; finish reason {FinishReason}; {MessageCount} messages sent; input tokens {InputTokens}; output tokens {OutputTokens}.",
                    ElapsedMs(started), finishReason?.Value, history.Count, usage?.InputTokenCount, usage?.OutputTokenCount);
                return;
            }

            reply.Status = ChatEntryStatus.Complete;
            logger.LogInformation(
                "Chat reply completed in {ElapsedMs} ms; finish reason {FinishReason}; {MessageCount} messages sent; reply length {ReplyLength}; input tokens {InputTokens}; output tokens {OutputTokens}.",
                ElapsedMs(started), finishReason?.Value, history.Count, reply.Length, usage?.InputTokenCount, usage?.OutputTokenCount);
        }
        catch (OperationCanceledException) when (generation.IsCancellationRequested)
        {
            reply.Status = ChatEntryStatus.Interrupted;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            reply.Status = ChatEntryStatus.Failed;
            logger.LogWarning(
                "Chat reply timed out after {TimeoutSeconds} s; {MessageCount} messages sent; partial reply length {ReplyLength}.",
                timeoutSeconds, history.Count, reply.Length);
        }
        catch (Exception ex)
        {
            reply.Status = ChatEntryStatus.Failed;

            // The exception message is deliberately not logged: provider error bodies may echo request content.
            logger.LogError(
                "Chat reply failed after {ElapsedMs} ms: {ExceptionType} (inner {InnerExceptionType}), HTTP status {HttpStatus}; {MessageCount} messages sent.",
                ElapsedMs(started), ex.GetType().FullName, ex.InnerException?.GetType().FullName, HttpStatusOf(ex), history.Count);
            logger.LogDebug("Chat reply failure stack trace ({ExceptionType}):\n{StackTrace}", ex.GetType().FullName, ex.StackTrace);
        }
        finally
        {
            // Stop/Reset may already have released this generation (and a new one may have started).
            if (ReferenceEquals(currentGeneration, generation))
            {
                currentGeneration = null;
            }

            generation.Dispose();
            RaiseChanged();
        }
    }

    private long ElapsedMs (long startedTimestamp) =>
        (long)timeProvider.GetElapsedTime(startedTimestamp).TotalMilliseconds;

    private static int? HttpStatusOf (Exception ex) => ex switch
    {
        ClientResultException { Status: > 0 } client => client.Status,
        HttpRequestException { StatusCode: { } status } => (int)status,
        _ => ex.InnerException is { } inner ? HttpStatusOf(inner) : null,
    };

    private void RaiseChanged () => Changed?.Invoke();
}
