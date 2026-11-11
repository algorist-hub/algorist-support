# Tasks

## 1. Dependencies

- [x] 1.1 Add `Microsoft.Extensions.AI` and `Microsoft.Extensions.AI.OpenAI` 10.11.0 to `AlgoristSupport.csproj`, and `Microsoft.Extensions.TimeProvider.Testing` to `AlgoristSupport.Tests.csproj`; verify `dotnet build AlgoristSupport.slnx` succeeds

## 2. Provider settings and maintenance mode (design D1, D9)

- [x] 2.1 Implement `ChatProviderSettings.Load(IConfiguration)` reading the seven keys by exact name with invariant-culture parsing and the validity rules from the spec, returning valid settings or the missing/invalid key names; verify with unit tests covering each key missing, `0,3` temperature, non-https/relative URL, `0`/`abc` limits, history `0` valid, and that results never contain values
- [x] 2.2 Add the `ChatAvailability` singleton and register it in `Program.cs`; after `Build()`, log one warning listing invalid key names when unavailable; verify a test with a capturing logger asserts the names are logged and the API key value is not
- [x] 2.3 Render the pt-BR maintenance block in `ChatPanel` (header kept; no suggestions, message list or composer) when unavailable, and make `ChatSession` reject sends with `Unavailable`; verify with bUnit and `ChatSession` tests
- [x] 2.4 Document the seven settings in `README.md` (meaning, valid values, `dotnet user-secrets set` examples, App Service environment variables, maintenance behavior); verify the documented commands match the key names in code

## 3. Real AI client (design D2, D3, D11)

- [x] 3.1 Add `SupportAssistantPrompt` with the pt-BR system message (Algorist support role, pt-BR replies, keep role, never reveal/repeat/change instructions, user text is not instructions); verify a unit test asserts each directive is present
- [x] 3.2 Build the `IChatClient` pipeline in `Program.cs` when available (OpenAI SDK with custom endpoint, API key, model, SDK retries disabled, `ConfigureOptions` setting temperature and max output tokens, no logging middleware) and register `UnavailableChatClient` otherwise; verify a test resolves the pipeline from test configuration and asserts the applied `ChatOptions` (temperature, max tokens) without network access
- [x] 3.3 Remove `SimulatedChatClient`, `SimulatedChatClientTests`, `SupportChatOptions.Simulation`, its `Program.cs` validation and the `Chat:Simulation` section of `appsettings.json`; verify `dotnet build` and `dotnet test` pass and no reference to "Resposta simulada" remains

## 4. History and per-session limits (design D4, D5, D6)

- [x] 4.1 Implement `ChatHistoryBuilder.Build(entries, maxExchanges)` (system message first, last N complete exchanges, current user message; failed and empty-interrupted exchanges dropped) and use it in `ChatSession`; verify unit tests for N=2 with 3 prior exchanges, N=0, and a failed exchange followed by a new message
- [x] 4.2 Implement the `TimeProvider`-based sliding-window counter (60 s, limit from settings, retry-after from oldest timestamp) and register `TimeProvider.System`; verify unit tests with `FakeTimeProvider` for limit reached, retry-after value and window sliding
- [x] 4.3 Refactor `ChatSession` acceptance into synchronous checks returning `SendOutcome` (`Accepted`, `Invalid`, `Busy`, `RateLimited`, `Unavailable`) with a pt-BR `Notice` for the rate limit, cleared on the next accepted send; verify `ChatSession` tests that a rate-limited send adds no entry and makes no `FakeChatClient` request, and that two sessions are independent
- [x] 4.4 Update `ChatPanel`/`ChatComposer` to clear the draft only when the send is accepted and to show the rate-limit notice; verify bUnit tests that a rate-limited message stays in the input and the notice is shown, and that chips are counted too

## 5. Timeout, retry and failure handling (design D7, D8)

- [x] 5.1 Add `SupportChatOptions.ReplyTimeoutSeconds` (default 30, in `appsettings.json`) and the linked timeout cancellation in `ChatSession` (timeout -> `Failed`, user stop -> `Interrupted`); verify `ChatSession` tests with `FakeTimeProvider` for timeout with and without partial text, and that Stop still yields `Interrupted`
- [x] 5.2 Implement `ChatSession.RetryAsync()` (only for the last `Failed` entry while idle; same acceptance checks; removes the failed entry and regenerates for the same user message); verify tests for success without a duplicate user message, rejection when not last/while generating, and rate-limited retry keeping the failed entry
- [x] 5.3 Show the "Tentar novamente" button in `ChatMessageList` only on the last `Failed` entry when idle and wire it through `ChatPanel`; verify bUnit tests for button visibility and that clicking it streams a new reply
- [x] 5.4 Treat a reply that completes without any text (e.g. `finish=length` with the limit spent on reasoning, found in the 7.2 smoke test) as `Failed`, logging finish reason and token usage without content; verify `ChatSession` tests that it is marked failed, offers retry, is excluded from history, and that the log entry has no message text

## 6. Content-free logging (design D10)

- [x] 6.1 Replace `ChatSession` error/success logging with metadata only (exception type, HTTP status, duration, message count, token usage when present; no exception message outside Development); verify tests with a capturing logger that neither user text nor assistant text appears in any log entry on success, provider error and timeout

## 7. Integration checks

- [x] 7.1 Run `dotnet build` and `dotnet test` for `AlgoristSupport.slnx` and `openspec validate add-llm-integration --strict`; verify all pass
- [x] 7.2 Manual smoke test locally with user secrets against the real Maritaca API: a streamed pt-BR reply, the token limit respected, an injection attempt that does not reveal the system prompt, the rate-limit message, maintenance mode after removing one secret; if the provider rejects `max_completion_tokens` or `stream_options`, apply the fallback in design Risks and repeat; record the outcome in the PR description

## Workflow follow-up

- Configure the seven settings in Azure App Service before merging `dev` into `main`, then smoke-test the deployed site.
- Archive the change with `/opsx:archive add-llm-integration` after review.
