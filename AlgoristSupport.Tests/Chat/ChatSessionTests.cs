using AlgoristSupport.Chat;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AlgoristSupport.Tests.Chat;

public class ChatSessionTests
{
    private readonly FakeChatClient client = new();

    private ChatSession CreateSession (int maxLength = 500) =>
        new(client, Options.Create(new SupportChatOptions { MaxMessageLength = maxLength }), NullLogger<ChatSession>.Instance);

    private static async Task WaitUntil (Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Condition not met in time.");
            }

            await Task.Delay(5);
        }
    }

    [Fact]
    public async Task Send_appends_trimmed_user_entry_and_completed_reply ()
    {
        client.AutoReply = ["Olá, ", "tudo bem?"];
        var session = CreateSession();

        var accepted = await session.SendAsync("   Olá   ");

        Assert.True(accepted);
        Assert.Collection(session.Entries,
            user =>
            {
                Assert.True(user.IsUser);
                Assert.Equal("Olá", user.Text);
            },
            reply =>
            {
                Assert.Equal(ChatRole.Assistant, reply.Role);
                Assert.Equal("Olá, tudo bem?", reply.Text);
                Assert.Equal(ChatEntryStatus.Complete, reply.Status);
            });
        Assert.False(session.IsGenerating);
        Assert.Equal("Olá", Assert.Single(client.Requests.Single()).Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \n\t ")]
    public async Task Empty_or_whitespace_input_is_rejected (string input)
    {
        var session = CreateSession();

        Assert.False(await session.SendAsync(input));
        Assert.Empty(session.Entries);
        Assert.Empty(client.Requests);
    }

    [Fact]
    public async Task Over_limit_input_is_rejected_server_side ()
    {
        var session = CreateSession(maxLength: 10);

        Assert.False(await session.SendAsync(new string('a', 11)));
        Assert.Empty(session.Entries);
        Assert.Empty(client.Requests);
    }

    [Fact]
    public async Task Second_send_while_generating_is_rejected ()
    {
        var session = CreateSession();
        var first = session.SendAsync("Primeira");
        Assert.True(session.IsGenerating);

        Assert.False(session.CanSend("Segunda"));
        Assert.False(await session.SendAsync("Segunda"));
        Assert.Equal(2, session.Entries.Count);

        client.Complete();
        await first;
        Assert.Single(client.Requests);
    }

    [Fact]
    public async Task Streamed_chunks_accumulate_until_complete ()
    {
        var session = CreateSession();
        var changes = 0;
        session.Changed += () => Interlocked.Increment(ref changes);

        var send = session.SendAsync("Pergunta");
        var reply = session.Entries[^1];
        Assert.Equal(ChatEntryStatus.Streaming, reply.Status);

        client.Push("Parte 1. ");
        await WaitUntil(() => reply.Text == "Parte 1. ");
        client.Push("Parte 2.");
        await WaitUntil(() => reply.Text == "Parte 1. Parte 2.");
        client.Complete();
        await send;

        Assert.Equal(ChatEntryStatus.Complete, reply.Status);
        Assert.False(session.IsGenerating);
        Assert.True(changes >= 4);
    }

    [Fact]
    public async Task Stop_keeps_partial_text_and_marks_interrupted ()
    {
        var session = CreateSession();
        var send = session.SendAsync("Pergunta");
        var reply = session.Entries[^1];
        client.Push("Parcial");
        await WaitUntil(() => reply.Text == "Parcial");

        session.Stop();

        Assert.False(session.IsGenerating);
        Assert.Equal(ChatEntryStatus.Interrupted, reply.Status);
        await send;
        client.Push("não deve aparecer");
        Assert.Equal("Parcial", reply.Text);
        Assert.Equal(ChatEntryStatus.Interrupted, reply.Status);
        Assert.True(session.CanSend("Outra"));
    }

    [Fact]
    public async Task Stop_before_any_text_leaves_empty_interrupted_reply ()
    {
        var session = CreateSession();
        var send = session.SendAsync("Pergunta");

        session.Stop();
        await send;

        var reply = session.Entries[^1];
        Assert.Equal(string.Empty, reply.Text);
        Assert.Equal(ChatEntryStatus.Interrupted, reply.Status);
    }

    [Fact]
    public async Task Failure_marks_failed_keeps_history_and_reenables_sending ()
    {
        var session = CreateSession();
        client.AutoReply = ["Resposta 1"];
        await session.SendAsync("Pergunta 1");
        client.AutoReply = null;

        var send = session.SendAsync("Pergunta 2");
        client.Push("meio");
        client.Fail(new InvalidOperationException("boom"));
        await send;

        Assert.Equal(4, session.Entries.Count);
        Assert.Equal(ChatEntryStatus.Failed, session.Entries[^1].Status);
        Assert.Equal("Resposta 1", session.Entries[1].Text);
        Assert.False(session.IsGenerating);

        client.AutoReply = ["ok"];
        Assert.True(await session.SendAsync("Pergunta 3"));

        // The failed reply is not sent back to the model.
        var history = client.Requests[^1];
        Assert.Equal(["Pergunta 1", "Resposta 1", "Pergunta 2", "Pergunta 3"], history.Select(m => m.Text));
    }

    [Fact]
    public async Task Reset_during_generation_clears_and_stays_empty ()
    {
        var session = CreateSession();
        var send = session.SendAsync("Pergunta");
        client.Push("Parcial");
        await WaitUntil(() => session.Entries[^1].Text == "Parcial");

        session.Reset();

        Assert.Empty(session.Entries);
        Assert.False(session.IsGenerating);
        client.Push("mais texto");
        await send;
        Assert.Empty(session.Entries);
    }

    [Fact]
    public async Task Reset_when_idle_clears_history ()
    {
        client.AutoReply = ["ok"];
        var session = CreateSession();
        await session.SendAsync("Pergunta");

        session.Reset();

        Assert.Empty(session.Entries);
    }

    [Fact]
    public async Task Dispose_cancels_in_flight_generation ()
    {
        var session = CreateSession();
        var send = session.SendAsync("Pergunta");

        session.Dispose();
        await send.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(ChatEntryStatus.Interrupted, session.Entries[^1].Status);
        Assert.False(session.IsGenerating);
    }

    [Fact]
    public async Task Interrupted_partial_reply_is_included_in_next_history ()
    {
        var session = CreateSession();
        var send = session.SendAsync("Pergunta 1");
        client.Push("Parcial");
        await WaitUntil(() => session.Entries[^1].Text == "Parcial");
        session.Stop();
        await send;

        client.AutoReply = ["ok"];
        await session.SendAsync("Pergunta 2");

        Assert.Equal(["Pergunta 1", "Parcial", "Pergunta 2"], client.Requests[^1].Select(m => m.Text));
    }
}
