using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace AlgoristSupport.Chat;

/// <summary>
/// Placeholder <see cref="IChatClient"/> that streams a canned pt-BR reply without calling any AI provider.
/// Replaced by a real client in a later change.
/// </summary>
public sealed class SimulatedChatClient (IOptions<SupportChatOptions> options) : IChatClient
{
    public const string SimulatedMarker = "(Resposta simulada)";

    private const int WordsPerChunk = 2;

    public async Task<ChatResponse> GetResponseAsync (
        IEnumerable<ChatMessage> messages,
        ChatOptions? chatOptions = null,
        CancellationToken cancellationToken = default)
    {
        var text = new StringBuilder();
        await foreach (var update in GetStreamingResponseAsync(messages, chatOptions, cancellationToken))
        {
            text.Append(update.Text);
        }

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, text.ToString()));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync (
        IEnumerable<ChatMessage> messages,
        ChatOptions? chatOptions = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var simulation = options.Value.Simulation;
        var question = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text?.Trim() ?? string.Empty;

        await DelayAsync(simulation.InitialDelayMs, cancellationToken);

        foreach (var chunk in Chunk(BuildReply(question)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
            await DelayAsync(simulation.ChunkDelayMs, cancellationToken);
        }
    }

    public object? GetService (Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose () { }

    internal static string BuildReply (string question) =>
        $"{SimulatedMarker} Você perguntou: “{question}”. " +
        "Nesta versão de demonstração ainda não há inteligência artificial conectada; " +
        "em breve as respostas serão geradas com base na política (fictícia) de prestação de serviços da Algorist.";

    private static IEnumerable<string> Chunk (string text)
    {
        var words = text.Split(' ');
        for (var i = 0; i < words.Length; i += WordsPerChunk)
        {
            var chunk = string.Join(' ', words.Skip(i).Take(WordsPerChunk));
            // Re-insert the separating space so the concatenated chunks equal the original text.
            yield return i + WordsPerChunk < words.Length ? chunk + " " : chunk;
        }
    }

    private static Task DelayAsync (int milliseconds, CancellationToken cancellationToken) =>
        milliseconds > 0 ? Task.Delay(milliseconds, cancellationToken) : Task.CompletedTask;
}
