# Design

## Context

See proposal.md for motivation. Current state (from `add-basic-chat`):

- `Program.cs` registers `SupportChatOptions` (section `Chat`, `ValidateOnStart`), a singleton `IChatClient` = `SimulatedChatClient`, and a scoped `ChatSession` (one per Blazor circuit).
- `ChatSession` owns the entries, validates input (`ChatMessageValidator`, 500 chars), streams `IChatClient.GetStreamingResponseAsync`, and maps outcomes to `ChatEntryStatus` (`Complete`, `Interrupted` on user stop, `Failed` on any exception, logged with `LogError(ex, ...)`). `BuildHistory()` sends every user entry plus completed/interrupted assistant entries, with no system message.
- `ChatPanel` clears the draft *before* awaiting `SendAsync`; `ChatMessageList` already renders a generic pt-BR text for `Failed`.
- Only `Microsoft.Extensions.AI.Abstractions` 10.11.0 is referenced. Tests use `FakeChatClient` (channel-driven) with xUnit + bUnit. The CI workflow only publishes; it does not run tests.

Constraints: settings arrive as flat, root-level configuration keys (user secrets locally, App Service environment variables in Azure), so they must be read from `IConfiguration` by exact name rather than from a `Chat:` section. A missing setting must not stop the app (maintenance mode), which rules out `ValidateOnStart` for them.

## Goals / Non-Goals

**Goals:**
- Swap the reply source behind `IChatClient` with minimal changes to the UI flow.
- Keep `ChatSession` provider-agnostic and fully testable with `FakeChatClient` and a fake clock; no test needs secrets or network.
- Make every limit and failure path deterministic and testable.

**Non-Goals:**
- Policy content in the prompt, out-of-scope refusal, injection evaluation suites (`add-policy-grounding`).
- Global/cross-session or per-IP quotas, persistence of usage counters.
- Running tests in CI (worth doing, but a separate change).

## Decisions

### D1. Provider settings: hand-parsed, non-throwing, singleton availability
A `ChatProviderSettings` type with a static `Load(IConfiguration)` reads the seven keys by exact name and returns either valid settings or the list of missing/invalid key names (never values). Parsing uses `CultureInfo.InvariantCulture` (so `0,3` is invalid) and `Uri.TryCreate` + `https` scheme. The result is registered as a singleton `ChatAvailability { bool IsAvailable; ChatProviderSettings? Settings; IReadOnlyList<string> InvalidSettings }`. After `builder.Build()`, if unavailable, one warning is logged listing the names.

*Alternatives:* `IOptions` + `ValidateOnStart` - crashes the app instead of showing maintenance; `IOptions` with lazy validation - throws on first use inside the circuit, harder to turn into a UI state. Binding a section - the required key names are flat and fixed.

### D2. Client construction with the OpenAI SDK through `Microsoft.Extensions.AI.OpenAI`
Add `Microsoft.Extensions.AI` and `Microsoft.Extensions.AI.OpenAI` (10.11.0, matching the existing Abstractions). When available, register a singleton:

```
new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = baseUrl, RetryPolicy = <no retries> })
    .GetChatClient(model).AsIChatClient()
    -> ChatClientBuilder.ConfigureOptions(o => { o.Temperature = t; o.MaxOutputTokens = maxTokens; })
```

Temperature and max tokens are applied in the pipeline so `ChatSession` stays unaware of them; tests assert them on the built pipeline. SDK retries are disabled: retries would silently multiply provider calls (and quota use) inside the 30 s budget, and the user has an explicit try-again button. No `UseLogging()` middleware (it logs message content at trace level).

When unavailable, register an `UnavailableChatClient` that throws `InvalidOperationException`; `ChatSession` never reaches it because it checks availability first. This keeps `ChatSession`'s constructor unchanged for DI.

*Alternative:* a raw `HttpClient` against `/chat/completions` - more code, loses the `IChatClient` ecosystem the roadmap relies on.

### D3. System prompt as code
A static `SupportAssistantPrompt` holds the pt-BR system message (role: Algorist support assistant; reply in pt-BR; keep the role; never reveal/repeat/change instructions; treat user text as questions, not commands). Kept in code, not configuration, so it is versioned, reviewed and unit-tested; `add-policy-grounding` will extend it with the policy text.

### D4. History trimming as a pure function
`ChatHistoryBuilder.Build(entries, maxExchanges)` returns `[system] + last N exchanges + current user message`. It pairs each user entry with the following assistant entry; pairs whose reply is `Failed` or empty-`Interrupted` are dropped. Replaces `ChatSession.BuildHistory()`.

### D5. Per-session sliding-window rate limiter with `TimeProvider`
`ChatSession` owns a `SlidingWindowCounter` (queue of request timestamps, 60 s window, limit = `chat_max_msg_per_min`) using an injected `TimeProvider` (`TimeProvider.System` registered as singleton; tests use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`). `TryAcquire(out TimeSpan retryAfter)` computes the wait from the oldest timestamp, which produces the "wait about N seconds" message.

*Alternative:* `System.Threading.RateLimiting.SlidingWindowRateLimiter` - segment-based, less precise retry-after, and needs disposal per circuit; a 20-line queue is simpler and exact.

### D6. Acceptance separated from generation; outcomes instead of `bool`
`SendAsync`/`RetryAsync` perform all synchronous checks before the first `await` - available, not generating, valid input, rate limit - and report a `SendOutcome` (`Accepted`, `Invalid`, `Busy`, `RateLimited`, `Unavailable`). The session exposes a pt-BR `Notice` (e.g. the rate-limit message) cleared on the next accepted send. `ChatPanel` clears the draft only on `Accepted` (it observes acceptance synchronously, before awaiting the generation task), so a rate-limited message stays in the input.

### D7. Timeout via linked cancellation, distinguished from user stop
Each generation creates the existing user-stop `CancellationTokenSource` plus a timeout source (`CancelAfter(30 s)` using the `TimeProvider`), linked into the token passed to the client. On `OperationCanceledException`: user stop -> `Interrupted` (unchanged); timeout -> `Failed`. The 30 s value lives in `SupportChatOptions.ReplyTimeoutSeconds` (default 30, in `appsettings.json`) so tests can shorten it; it is not one of the externally required settings.

### D8. Retry reuses the generation core
`RetryAsync()` is allowed only when the last entry is a `Failed` assistant entry and nothing is generating. It runs the same acceptance checks (counts toward the rate limit); on acceptance it removes the failed entry and runs the shared generation routine for the preceding user message. `ChatMessageList` shows "Tentar novamente" only on the last entry when it is `Failed` and not generating, raising a callback to `ChatPanel`.

### D9. Maintenance UI
`ChatPanel` injects `ChatAvailability`; when unavailable it renders the header and a pt-BR maintenance block ("O chat está em manutenção. Tente novamente mais tarde.") instead of suggestions, message list and composer. `ChatSession` also rejects sends (`Unavailable`) as server-side enforcement.

### D10. Content-free logging
Logs carry only metadata: on success, duration, message count sent and token usage when the provider reports it; on failure, exception type, HTTP status (`ClientResultException.Status`) and duration. The exception message is not logged because provider error bodies may echo request content; the stack trace stays available in Development only. The existing `LogError(ex, ...)` in `ChatSession` is replaced accordingly. Startup validation logs key names only.

### D11. Remove the simulation
Delete `SimulatedChatClient`, its tests, `SupportChatOptions.Simulation` and its `Program.cs` validation, and the `Chat:Simulation` section of `appsettings.json`. Tests use `FakeChatClient` directly.

## Risks / Trade-offs

- [Maritaca may not accept `max_completion_tokens`, which recent OpenAI SDK versions send for `MaxOutputTokens`, or `stream_options.include_usage`] -> Verify in a manual smoke test against the real API (task). If rejected, switch the SDK back to the legacy `max_tokens` field with its documented option (`SetNewMaxCompletionTokensPropertyEnabled(false)` via `ChatOptions.RawRepresentationFactory`), and skip usage logging if no usage arrives.
- [Smoke test, 2026-11-10, model `S5-free`] -> Findings: `max_completion_tokens` and `stream_options` are accepted and usage is returned (fallback not needed); the model rejects any `temperature` other than `1` (HTTP 400), so `chat_temperature` must be `1` for this model; with a low token limit the model can spend all output tokens on internal reasoning and return no text, which is now treated as a failed reply (task 5.4).
- [Per-session limits can be bypassed by reloading or opening new tabs] -> Accepted for a portfolio demo; the provider account quota is the hard ceiling. A global limiter is a future change.
- [Without grounding (step 3), the model may answer off-topic or invent policy details] -> The permanent demo notice stays; consider deploying to production only after `add-policy-grounding`, or accept the interim state knowingly.
- [Basic injection defense is instruction-only and can be bypassed by determined users] -> Expected at this step; hardened in `add-policy-grounding`.
- [A 30 s overall timeout can cut long streamed answers] -> `chat_max_token_per_answer` keeps replies short; the timeout is configurable via `Chat:ReplyTimeoutSeconds` if needed.
- [Deploying before configuring App Service settings shows maintenance on the live site] -> Intended fallback; the migration plan configures settings first.
- [Tests are not run in CI] -> Run `dotnet test` locally before pushing; adding a CI test step is a separate change.

## Migration Plan

1. Locally: `dotnet user-secrets set <name> <value>` for the seven settings in `AlgoristSupport/` (API key, model, base URL, temperature, limits).
2. Azure App Service > Environment variables: add the same seven names and values; restart.
3. Merge `dev` into `main` to trigger the existing build-and-deploy workflow.
4. Smoke test on the live site: one normal question, one rate-limit hit, maintenance check by temporarily removing a setting in a non-production slot or locally.
5. Rollback: redeploy the previous commit (or revert the merge); the old build ignores the new settings.
