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
/// </remarks>
public sealed class ChatSession (
    IChatClient chatClient,
    IOptions<SupportChatOptions> options,
    ILogger<ChatSession> logger) : IDisposable
{
    private readonly List<ChatEntry> entries = [];
    private CancellationTokenSource? currentGeneration;

    /// <summary>Raised whenever entries or the generating state change.</summary>
    public event Action? Changed;

    public IReadOnlyList<ChatEntry> Entries => entries;

    public bool IsGenerating => currentGeneration is not null;

    public int MaxMessageLength => options.Value.MaxMessageLength;

    public MessageValidationResult Validate (string? input) => ChatMessageValidator.Validate(input, MaxMessageLength);

    public bool CanSend (string? input) => !IsGenerating && Validate(input).IsValid;

    /// <summary>
    /// Sends a user message and streams the assistant reply. Returns <c>false</c> (and changes nothing) when the
    /// input is invalid or a reply is already in progress.
    /// </summary>
    public async Task<bool> SendAsync (string? input)
    {
        if (!CanSend(input))
        {
            return false;
        }

        entries.Add(new ChatEntry(ChatRole.User, input!.Trim(), ChatEntryStatus.Complete));
        var history = BuildHistory();

        var reply = new ChatEntry(ChatRole.Assistant, string.Empty, ChatEntryStatus.Streaming);
        entries.Add(reply);

        // Mark as generating synchronously, before the first await.
        var generation = new CancellationTokenSource();
        currentGeneration = generation;
        var token = generation.Token;
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
            }

            reply.Status = ChatEntryStatus.Complete;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            reply.Status = ChatEntryStatus.Interrupted;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate chat reply.");
            reply.Status = ChatEntryStatus.Failed;
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

        return true;
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
        RaiseChanged();
    }

    public void Dispose ()
    {
        var generation = currentGeneration;
        currentGeneration = null;
        generation?.Cancel();
        Changed = null;
    }

    private List<ChatMessage> BuildHistory () =>
        entries
            .Where(e => e.IsUser || (e.Status is ChatEntryStatus.Complete or ChatEntryStatus.Interrupted && e.Length > 0))
            .Select(e => new ChatMessage(e.Role, e.Text))
            .ToList();

    private void RaiseChanged () => Changed?.Invoke();
}
