# Tasks

## 1. Setup

- [x] 1.1 Add the `Microsoft.Extensions.AI.Abstractions` package (latest stable) to `AlgoristSupport.csproj`; verify `dotnet build AlgoristSupport.slnx` succeeds
- [x] 1.2 Create the `AlgoristSupport.Tests` xUnit project (net10.0) referencing `AlgoristSupport`, with `bunit` added, and register it in `AlgoristSupport.slnx`; verify `dotnet test AlgoristSupport.slnx` runs (0 tests, success)

## 2. Options and validation

- [x] 2.1 Add `ChatOptions` (`AssistantName`, `MaxMessageLength` [1–4000, default 500], `SuggestedQuestions`, `Simulation.InitialDelayMs`/`ChunkDelayMs`) under `Chat/`, bind the `Chat` section in `Program.cs` with DataAnnotations + `ValidateOnStart()`, and add the `Chat` section with pt-BR defaults (design D5) to `appsettings.json`; verify the app starts and fails fast when `MaxMessageLength` is set to 0
- [x] 2.2 Implement message validation (empty/whitespace rejected, raw length > max rejected, pt-BR error text) and add unit tests for empty, whitespace/newlines-only, exactly-max, max+1, and a custom limit of 200; verify the tests pass

## 3. Simulated reply source

- [x] 3.1 Implement `SimulatedChatClient : IChatClient` (design D1): streams a pt-BR reply marked as simulated and echoing the last user message in multiple chunks with configured delays, honors cancellation, `GetResponseAsync` returns the aggregated text; register it as singleton `IChatClient`. Add tests (zero delays) asserting >1 update, the simulated marker is present, and cancellation stops the stream; verify the tests pass

## 4. Chat session state

- [x] 4.1 Add `ChatEntry`/`ChatEntryStatus` (design D3) and the scoped `ChatSession` service (design D2/D4): `Entries`, `IsGenerating`, `Changed`, `Validate`, `SendAsync`, `Stop`, `Reset`, `IDisposable`; register as scoped in `Program.cs`; verify `dotnet build` succeeds
- [x] 4.2 Add `ChatSession` unit tests with a fake `IChatClient` covering: trimmed user entry appended; invalid/over-limit input rejected server-side; second send while generating rejected; streamed chunks accumulate and end as `Complete`; `Stop` keeps partial text and marks `Interrupted`; failure marks `Failed`, keeps earlier entries, and re-enables sending; `Reset` during generation leaves an empty list that stays empty; `Dispose` cancels in-flight generation; history passed to the client excludes failed entries. Verify all tests pass

## 5. Chat UI

- [x] 5.1 Set `<html lang="pt-BR">` in `App.razor` and adjust `MainLayout`/`app.css` so the page fills the viewport; verify the rendered page source has `lang="pt-BR"` and no page-level scrollbar on an empty chat
- [x] 5.2 Build `ChatHeader` (assistant name, permanent non-dismissible pt-BR demo/fictional-policy notice, "Nova conversa" button calling `Reset`) outside the scroll area; verify via bUnit test that name and notice render and no close control exists
- [x] 5.3 Build `ChatMessageList` (`role="log"`, user/assistant styling plus visually hidden author label, `white-space: pre-wrap`, HTML-encoded text, "Resposta interrompida" marker for `Interrupted`, generic pt-BR error for `Failed`) and the `role="status"` "Gerando resposta…" indicator; verify via bUnit tests that `<b>olá</b>` renders literally, authors are labeled, and the indicator appears only while generating
- [x] 5.4 Build `SuggestedQuestions` chips (shown only when there are no entries, disabled while generating, click sends via `ChatSession.SendAsync`); verify via bUnit tests that chips render on empty state, clicking one adds the user message and hides the chips, and nothing renders when the list is empty
- [x] 5.5 Build `ChatComposer` (textarea with `@bind:event="oninput"`, live `<n>/<max>` counter with error state, pt-BR over-limit feedback, Enviar button disabled when invalid or generating, Parar button visible only while generating calling `Stop`, form `@onsubmit` sharing the send path, pt-BR placeholder); verify via bUnit tests the counter text (`42/500`), error state at 501, Send disabled for empty/whitespace/over-limit/generating, and Stop visibility
- [x] 5.6 Compose `ChatPanel` in `Home.razor` (`/`): subscribe to `ChatSession.Changed` with `InvokeAsync(StateHasChanged)`, unsubscribe on dispose; verify via bUnit test that sending a message through the fake client renders the streamed assistant reply
- [x] 5.7 Add the collocated `ChatPanel.razor.js` module (design D7): `attachComposer` (Enter submits via `requestSubmit`, Shift+Enter/IME composition untouched) and `scrollToBottom`, invoked from `OnAfterRenderAsync` when entry count or last-entry length changes; verify manually that Enter sends, Shift+Enter adds a newline, and the list follows a streaming reply

## 6. Integration verification

- [x] 6.1 Run `dotnet build` and `dotnet test` for `AlgoristSupport.slnx` with zero warnings-as-errors failures and all tests green
- [x] 6.2 Manual end-to-end check with `dotnet run` against the spec scenarios: notice always visible in a long conversation; auto-scroll; Enter/Shift+Enter; whitespace-only not sent; counter and 500-char feedback; generating indicator; Stop mid-reply keeps partial text; sending blocked while generating; "Nova conversa" during generation clears for good; chips only on empty state; two tabs independent; reload starts fresh; no network calls to AI providers (browser dev tools)
- [x] 6.3 Update `README.md` with a short description of the chat demo, how to run it, and the `Chat` configuration section; verify the documented run command works as written
- [x] 6.4 Run `openspec validate add-basic-chat --strict` and verify it passes

## Workflow follow-up

- Archive the change after review, then start `add-llm-integration`.
