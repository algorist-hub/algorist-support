using AlgoristSupport.Chat;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace AlgoristSupport.Tests.Chat;

public class SimulatedChatClientTests
{
    private static SimulatedChatClient CreateClient (int chunkDelayMs = 0) =>
        new(Options.Create(new SupportChatOptions
        {
            Simulation = new() { InitialDelayMs = 0, ChunkDelayMs = chunkDelayMs },
        }));

    private static List<ChatMessage> Question (string text) => [new(ChatRole.User, text)];

    [Fact]
    public async Task Streams_multiple_updates_with_simulated_marker ()
    {
        var client = CreateClient();
        var updates = new List<ChatResponseUpdate>();

        await foreach (var update in client.GetStreamingResponseAsync(Question("Como funciona a garantia?")))
        {
            updates.Add(update);
        }

        Assert.True(updates.Count > 1);
        Assert.All(updates, u => Assert.Equal(ChatRole.Assistant, u.Role));

        var text = string.Concat(updates.Select(u => u.Text));
        Assert.StartsWith(SimulatedChatClient.SimulatedMarker, text);
        Assert.Contains("Como funciona a garantia?", text);
        Assert.Equal(SimulatedChatClient.BuildReply("Como funciona a garantia?"), text);
    }

    [Fact]
    public async Task GetResponseAsync_returns_aggregated_text ()
    {
        var client = CreateClient();

        var response = await client.GetResponseAsync(Question("Olá"));

        Assert.Equal(SimulatedChatClient.BuildReply("Olá"), response.Text);
    }

    [Fact]
    public async Task Cancellation_stops_the_stream ()
    {
        var client = CreateClient(chunkDelayMs: 50);
        using var cts = new CancellationTokenSource();
        var received = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in client.GetStreamingResponseAsync(Question("Olá"), cancellationToken: cts.Token))
            {
                received++;
                cts.Cancel();
            }
        });

        Assert.Equal(1, received);
    }
}
