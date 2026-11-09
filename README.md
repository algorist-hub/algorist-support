# algorist-support
Algorist Support Demo App

A pt-BR support chat demo (Blazor Web App, .NET 10, Interactive Server). The assistant will eventually answer
only from the fictional policy in [`docs/policy.md`](docs/policy.md). In the current version replies are
**simulated** (streamed canned text, no AI calls).

## Run

```bash
dotnet run --project AlgoristSupport
```

Then open the URL printed in the console. Run the tests with:

```bash
dotnet test AlgoristSupport.slnx
```

## Chat features

- Header with the assistant name and a permanent "demo / fictional policy" notice.
- Message list with auto-scroll, streamed replies, a "Gerando resposta…" indicator and a **Parar** button.
- **Enter** sends, **Shift+Enter** inserts a newline; empty/whitespace-only messages are not sent.
- Character counter with a configurable limit (default 500); sending is blocked while a reply is in progress.
- **Nova conversa** clears the history; suggested-question chips appear while the conversation is empty.
- History is kept in memory per browser tab session (Blazor circuit) only: no database, no sign-in. Reloading starts fresh.

## Configuration

`AlgoristSupport/appsettings.json`, section `Chat`:

| Key | Default | Description |
| --- | --- | --- |
| `AssistantName` | `Assistente Algorist` | Name shown in the header. |
| `MaxMessageLength` | `500` | Maximum characters per message (1–4000). |
| `SuggestedQuestions` | 4 policy questions | Chips shown on an empty conversation (empty list hides them). |
| `Simulation:InitialDelayMs` | `400` | Delay before the simulated reply starts streaming. |
| `Simulation:ChunkDelayMs` | `60` | Delay between streamed chunks. |

Invalid values fail at startup. Any key can be overridden with environment variables, e.g. `Chat__MaxMessageLength=300`.
