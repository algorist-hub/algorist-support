using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.AI;

namespace AlgoristSupport.Tests.Chat;

/// <summary>
/// Test <see cref="IChatClient"/> whose stream is driven by the test: push chunks with <see cref="Push"/>,
/// finish with <see cref="Complete"/>, or fail with <see cref="Fail"/>. Honors cancellation.
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
    private Channel<string> channel = Channel.CreateUnbounded<string>();

    public List<IReadOnlyList<ChatMessage>> Requests { get; } = [];

    public List<ChatOptions?> RequestOptions { get; } = [];

    /// <summary>Optional updates (e.g. usage) yielded after the text chunks of an auto reply.</summary>
    public List<ChatResponseUpdate> AutoReplyExtras { get; } = [];

    /// <summary>When set, each request immediately streams these chunks and completes.</summary>
    public string[]? AutoReply { get; set; }

    public void Push (string chunk) => channel.Writer.TryWrite(chunk);

    public void Complete () => channel.Writer.TryComplete();

    public void Fail (Exception exception) => channel.Writer.TryComplete(exception);

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync (
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Requests.Add(messages.ToList());
        RequestOptions.Add(options);

        if (AutoReply is not null)
        {
            foreach (var chunk in AutoReply)
            {
                await Task.Yield();
                yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
            }

            foreach (var extra in AutoReplyExtras)
            {
                yield return extra;
            }

            yield break;
        }

        try
        {
            await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
            }
        }
        finally
        {
            // The next request gets a fresh channel.
            channel = Channel.CreateUnbounded<string>();
        }
    }

    public Task<ChatResponse> GetResponseAsync (
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService (Type serviceType, object? serviceKey = null) => null;

    public void Dispose () { }
}
