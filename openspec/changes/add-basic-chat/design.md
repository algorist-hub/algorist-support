# Design

## Context

- Single project `AlgoristSupport` (.NET 10, `net10.0`, nullable + implicit usings), Blazor Web App with **global** Interactive Server: `App.razor` renders `<Routes @rendermode="InteractiveServer" />` (prerendering on by default). `Program.cs` uses a classic `namespace` + `Main` style; keep it.
- `Home.razor` (`/`) is a placeholder `<h1>`. `MainLayout` wraps `@Body` in `article.content.px-4`. Bootstrap 5 is bundled under `wwwroot/lib`.
- No test project, no OpenSpec specs yet. `docs/policy.md` (pt-BR, fictional) is the future grounding source; this change only uses it to pick sensible suggested questions.
- Roadmap: the next change (`add-llm-integration`) swaps in a Maritaca-backed `IChatClient`. This design must make that a registration change, not a UI rewrite. Requirements are in `specs/support-chat/spec.md`.

## Goals / Non-Goals

**Goals:**
- All chat behavior (validation, streaming lifecycle, stop, reset, history) lives in a plain C# scoped service that is unit-testable without a browser.
- The reply source is an `IChatClient` (`Microsoft.Extensions.AI`), so the simulated implementation and the future Maritaca client are interchangeable.
- Minimal JS: only what Blazor Server cannot do server-side (conditional Enter handling, scrolling).

**Non-Goals:**
- Streaming render throttling/batching, token counting, rate limiting (next change).
- Markdown rendering of replies (plain text only for now; revisit with citations).
- Resilience across circuit loss (history is intentionally lost on new circuit).
- Picking/verifying the Maritaca Sabiá model name (belongs to `add-llm-integration`).

## Decisions

### D1. Simulated reply as an `IChatClient` implementation
`SimulatedChatClient : IChatClient` (package `Microsoft.Extensions.AI.Abstractions`) implements `GetStreamingResponseAsync` by yielding `ChatResponseUpdate`s (a few words per chunk) with a configurable delay, honoring the `CancellationToken`; `GetResponseAsync` aggregates the same text. The canned pt-BR reply states it is a simulated demo reply and echoes the user's question, e.g. *"(Resposta simulada) Você perguntou: “…”. Nesta versão de demonstração ainda não há IA conectada; em breve as respostas serão baseadas na política da Algorist."* Registered as a singleton `IChatClient`.
- *Alternative:* a custom `IReplyGenerator` interface. Rejected: the next change would need an adapter anyway; `IChatClient` is already the target contract.
- Only the Abstractions package is needed now; `Microsoft.Extensions.AI` (builder/middleware) and the OpenAI connector arrive with the next change. Use the latest stable version available at implementation time.

### D2. Per-circuit `ChatSession` scoped service owns state and logic
In Blazor Server, DI scope == circuit, so a **scoped** `ChatSession` satisfies "in-memory history per circuit" with no extra infrastructure. It exposes:
- `IReadOnlyList<ChatEntry> Entries`, `bool IsGenerating`, `event Action? Changed`
- `ValidationResult Validate(string? input)` (empty/whitespace, max length)
- `Task SendAsync(string input)` — validates again (server-side enforcement independent of UI), rejects if `IsGenerating`, appends the trimmed user entry and a streaming assistant entry, sets `IsGenerating` **synchronously before the first `await`**, then consumes `IChatClient.GetStreamingResponseAsync` appending text and raising `Changed` per update.
- `void Stop()` — cancels the current `CancellationTokenSource`.
- `void Reset()` — `Stop()`, then clears entries.
- `IDisposable` — cancels any in-flight generation when the circuit is disposed.

Blazor's per-circuit synchronization context serializes UI events, so the `IsGenerating` flag is a sufficient guard against double sends; no locks.
- *Alternative:* keep state in the page component. Rejected: harder to test, and logic would leak into markup.
- Prerender creates a throwaway scope; that's fine because the initial state is empty either way.

### D3. UI model `ChatEntry` separate from `ChatMessage`
`ChatEntry { Id, ChatRole Role, string Text (mutable while streaming), ChatEntryStatus Status }` with `Status ∈ { Complete, Streaming, Interrupted, Failed }`. The history sent to `IChatClient` is mapped from entries (user + `Complete`/`Interrupted` assistant text; failed entries are skipped).
- *Alternative:* store `ChatMessage` and put status in `AdditionalProperties`. Rejected: stringly-typed and awkward in Razor.

### D4. Cancellation and race safety
Each send creates a new `CancellationTokenSource` and captures its `ChatEntry`. Outcomes:
- normal end → `Complete`;
- `OperationCanceledException` from our token → `Interrupted` (partial text kept; pt-BR marker "Resposta interrompida" rendered by the UI);
- any other exception → `Failed`, generic pt-BR message shown, exception logged via `ILogger` (never displayed).
`finally` clears `IsGenerating`, disposes the CTS, raises `Changed`. Because `Reset()` clears the list and the loop only mutates its captured entry (which is no longer in the list), a stopped stream can't repopulate a cleared conversation. The loop also checks the token before each append.

### D5. Validation via options
`ChatOptions` bound to the `Chat` section of `appsettings.json` with DataAnnotations + `ValidateOnStart()`:
```json
"Chat": {
  "AssistantName": "Assistente Algorist",
  "MaxMessageLength": 500,
  "SuggestedQuestions": [
    "Quais serviços a Algorist oferece?",
    "Como funciona a garantia?",
    "Quais são os níveis de suporte (SLA)?",
    "Como posso cancelar um contrato?"
  ],
  "Simulation": { "InitialDelayMs": 400, "ChunkDelayMs": 60 }
}
```
`MaxMessageLength` range 1–4000. Length = `string.Length` of the raw input (UTF-16 units; an emoji may count as 2 — acceptable for a demo). Empty check uses `string.IsNullOrWhiteSpace`. The same `Validate` method drives the UI (counter error state, disabled Send, pt-BR feedback) and `SendAsync`.
- *Alternative:* HTML `maxlength`. Rejected: silently truncates pastes, giving no feedback (spec requires feedback); kept only server-side/UI validation.

### D6. Components and layout
- `Components/Pages/Home.razor` (`/`) hosts `<ChatPanel />`.
- `Components/Chat/ChatPanel.razor` — injects `ChatSession`, `IOptions<ChatOptions>`; subscribes to `Changed` and calls `InvokeAsync(StateHasChanged)`; unsubscribes in `Dispose`. Subcomponents: `ChatHeader` (name + permanent notice + "Nova conversa"), `ChatMessageList`, `SuggestedQuestions`, `ChatComposer` (textarea, counter, feedback, Enviar/Parar).
- Layout: full-height flex column — header (outside the scroll area, so the notice is always visible), scrollable message list (`flex: 1; overflow-y: auto`), composer footer. Component-scoped CSS (`*.razor.css`); `MainLayout`/`app.css` adjusted so the page fills the viewport.
- Text rendered via normal Razor `@text` (HTML-encoded) with `white-space: pre-wrap` for line breaks.
- Accessibility: message list `role="log"`; each entry has a visually hidden author label ("Você"/assistant name); generating indicator in `role="status"` ("Gerando resposta…"); buttons have pt-BR text/`aria-label`.
- `App.razor`: `<html lang="pt-BR">`.

### D7. Minimal JS module for Enter and auto-scroll
Collocated module `Components/Chat/ChatPanel.razor.js`, imported in `OnAfterRenderAsync(firstRender)` via `IJSRuntime` (`import`):
- `attachComposer(textarea, form)`: on `keydown`, if `key === "Enter" && !shiftKey && !isComposing` → `preventDefault()` and `form.requestSubmit()`. Shift+Enter falls through to native newline. The form's `@onsubmit` calls the same send path as the button, so all validation/generating guards apply.
- `scrollToBottom(element)`: called from `OnAfterRenderAsync` when entry count or the last entry's text length changed.
Textarea uses `@bind="draft" @bind:event="oninput"` so the counter is live; SignalR preserves event order, so the latest `oninput` is processed before the submit.
- *Alternative:* `@onkeydown:preventDefault` — Blazor can't make it conditional on Shift per event; rejected.

### D8. Tests
New `AlgoristSupport.Tests` (xUnit) added to `AlgoristSupport.slnx`:
- `ChatSession` unit tests with a fake `IChatClient` (controllable chunks, throw, block until cancelled) covering validation, trimming, concurrent-send rejection, stop/partial text, failure, reset during generation, dispose cancellation.
- `SimulatedChatClient` tests with zero delays (streams multiple updates, contains the simulated marker, honors cancellation).
- bUnit render tests for `ChatPanel`: header notice, chips visible only when empty, counter text/error state, Send disabled states, Stop visible only while generating. JS interop set to loose mode.
Enter/Shift+Enter and scroll behavior are verified manually (checklist in tasks).

## Risks / Trade-offs

- [Circuit loss drops history, e.g. B1 restart or long disconnect] → Accepted for demo; the reconnect modal already exists. Documented in the spec as session-scoped.
- [Per-keystroke `oninput` round-trips over SignalR] → Small payloads, single user per circuit; acceptable. Revisit with debounce only if latency is noticeable.
- [`StateHasChanged` per streamed chunk could be heavy with a real LLM] → Fine for the simulator's pacing; the next change may add render throttling.
- [Over-limit check uses UTF-16 length] → Minor mismatch with perceived characters for emoji; acceptable, documented.
- [JS module fails to load] → Send button and native textarea still work; only Enter-to-send and auto-scroll degrade.

## Migration Plan

Pure addition replacing the placeholder home page; no data or config migration. Deploy as usual to App Service; rollback = redeploy previous build.
