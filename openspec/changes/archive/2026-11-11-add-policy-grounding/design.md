# Design

## Context

- Every reply request is built by `ChatHistoryBuilder.Build`. It starts with `SupportAssistantPrompt.CreateSystemMessage()`, a static system prompt with role, pt-BR and prompt-injection rules and no policy knowledge.
- `ChatAvailability`, a singleton computed once at startup, decides whether the chat is usable (maintenance mode otherwise) and logs the names of invalid settings. `ChatSession` (scoped per circuit) and `ChatPanel` already react to it.
- `docs/policy.md` (about 17.6 KB, pt-BR Markdown) lives outside the web project. CI publishes only `AlgoristSupport.csproj`, so the file is not in the deployed output today.
- Routing is globally interactive (`<Routes @rendermode="InteractiveServer" />`). A new tab is a separate circuit and does not affect the chat tab.
- `Components/Chat/ChatMarkdown.cs` already renders Markdown safely: raw HTML is escaped and only http/https/mailto links are kept.

See proposal.md for motivation and the specs for the required behavior.

## Goals / Non-Goals

**Goals:**
- One policy file feeds both the assistant and the `/politica` page, read once at startup.
- Grounding logic (policy loading, prompt composition) lives in the `Chat` layer as plain, unit-testable code with no UI or I/O in prompt composition.
- Policy problems reuse the existing maintenance path rather than adding a second failure UI.

**Non-Goals:**
- Hot reload of the policy at runtime (a redeploy is required).
- Section anchors, citations, or retrieval (later roadmap steps).
- Automated LLM-quality evaluation. Grounding quality is checked with a manual script.

## Decisions

### D1. Send the whole policy in the system message (no RAG)
The policy is about 6k tokens, which fits easily in Maritaca's context window. Sending it whole gives the model full context for cross-section questions (for example, the warranty plus SLA), with no embeddings, vector store or chunking.
*Alternative:* RAG with embeddings. It was rejected for now: extra infrastructure and failure modes, plus retrieval misses, for a single small document. It can be revisited if the knowledge base grows.

### D2. Ship `docs/policy.md` as linked content
`AlgoristSupport.csproj` adds `<Content Include="..\docs\policy.md" Link="Policy\policy.md" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />`. The file stays in `docs/` (single source of truth) and lands at `<AppContext.BaseDirectory>/Policy/policy.md` in both `dotnet run` and `dotnet publish`.
*Alternatives:*
- Embedded resource: not editable after publish, and harder to inspect.
- Moving the file into `wwwroot`: duplicates the source and serves raw Markdown publicly.
- Configurable path: unnecessary for now. The loader takes the path as a parameter, so it can be added later without changing behavior.

### D3. `PolicyDocument` plus a loader with an explicit result
In `Chat/`:
- `PolicyDocument` is a sealed record holding `Markdown`.
- `PolicyDocumentLoader.Load(string path)` returns `PolicyLoadResult(PolicyDocument? Document, string? Problem)`. Problem is `"missing"`, `"unreadable"` or `"empty"`. The loader catches only I/O exceptions and never throws for these cases.

`Program.cs` loads the policy once and registers the result as a singleton. It is consumed by `ChatAvailability` and by the policy page.
*Rationale:* this mirrors the existing `ChatProviderSettings.Load` → `ChatProviderLoadResult` pattern (consistency), keeps startup non-fatal, and isolates file I/O behind one small, testable unit.

### D4. `ChatAvailability` gains the policy
- The constructor takes both load results.
- New members: `Policy` (`PolicyDocument?`) and `PolicyProblem` (`string?`).
- `IsAvailable` becomes `Settings is not null && Policy is not null`.
- `LogStatus` logs one warning for invalid settings (unchanged) and one naming the policy file path and the problem. It never logs content.

`ChatSession` and `ChatPanel` need no new branching, because they already rely on `IsAvailable`.
*Alternative:* a separate `PolicyAvailability` service. It was rejected because two availability sources would force every consumer to combine them.

### D5. Prompt composition becomes a pure function of the policy
`SupportAssistantPrompt` keeps its rule constants and adds the grounding rules:
- `AnswerOnlyFromPolicy`
- `UncoveredQuestion` (refer to the contact e-mail)
- `DeclineOffTopic`
- `PolicyIsData`

The static `Text` is replaced by `Build(PolicyDocument policy)`. Its layout is: role, the numbered rules, then the policy between `<politica>` and `</politica>` lines.

`ChatHistoryBuilder.Build` receives the system prompt text as a parameter instead of reaching for a static. `ChatSession` builds that text once in its constructor from `availability.Policy` (cheap string concatenation, once per circuit).
*Rationale:*
- The prompt is deterministic and testable (contains rules, contains the exact policy, delimiters in order).
- The history builder stays focused on trimming (SRP).
- No user text ever enters the system message.

Rules come before the policy so the instructions are not buried after 17 KB of data. The `PolicyIsData` rule plus delimiters reduces the chance that policy wording is read as instructions.

### D6. Shared safe Markdown renderer
`Components/Chat/ChatMarkdown.cs` moves to `Components/Shared/SafeMarkdown.cs`, with the same pipeline and URL filtering. It gains an optional flag for heading IDs (Markdig `UseAutoIdentifiers`), used only by the policy page. These IDs are harmless now and give `add-source-citations` stable anchors. `ChatMessageList` switches to the new name, and its existing tests keep passing unchanged.

### D7. `/politica` page
- `Components/Pages/Policy.razor` (`@page "/politica"`) injects `PolicyLoadResult` and sets `<PageTitle>Política (fictícia) — Algorist</PageTitle>`.
- It renders `SafeMarkdown.ToHtml(document.Markdown)` inside a readable, max-width article with scoped CSS for tables and headings. When the policy is missing, it shows a pt-BR unavailable message.
- It uses the existing `MainLayout`. The global interactive render mode is acceptable: the content is static and a new tab gets its own circuit.

### D8. Header link
In `ChatHeader.razor`, the words "política fictícia" become:

```html
<a href="politica" target="_blank" rel="noopener noreferrer">política fictícia<span class="visually-hidden"> (abre em nova janela)</span></a>
```

The relative `href` respects `<base href="/">`. The visually-hidden suffix gives the pt-BR accessible name. Link color must keep contrast inside the notice's styling.

## Risks / Trade-offs

- [The model still answers beyond the policy or invents numbers] → Explicit rules plus delimiters, a low configured temperature, and a manual evaluation script (covered, uncovered, off-topic and injection questions) run before archiving. Citations in a later step further constrain answers.
- [About 6k extra input tokens per request raise cost and latency] → Bounded by `chat_max_msg_per_min` and the history limit. Acceptable for a demo; revisit with RAG or provider prompt caching if needed.
- [The policy file is missing from the publish output because of a csproj mistake] → Maintenance mode plus a startup warning make it visible immediately. A unit test asserts the file exists in the test output (the test project references the web project, so linked content is copied).
- [Large system prompt plus history exceeds the model context] → 6k (policy) + N exchanges × 500-char messages + 800 output tokens stays far below Maritaca's context window. No extra guard is needed now.
- [The policy text contains instruction-like wording] → It is authored in-repo (trusted), delimited, and covered by the `PolicyIsData` rule.

## Migration Plan

No data migration. Deploy as usual through GitHub Actions. If grounding misbehaves in production, roll back by redeploying the previous build. No configuration changes are required: the existing provider settings are reused unchanged.
