# Proposal

## Why

The portfolio app currently renders an empty home page. Before wiring in a real LLM (Maritaca AI) and grounding it in `docs/policy.md`, we need a complete, testable chat experience so that later roadmap steps (`add-llm-integration`, `add-policy-grounding`, `add-source-citations`) only swap the reply source instead of also building UI, validation, and session state.

## What Changes

- Replace the placeholder home page (`/`) with a pt-BR support chat screen:
  - Header with the assistant name and a permanent notice that this is an automated demo assistant answering about a fictional policy.
  - Message list distinguishing user and assistant messages, auto-scrolling to the latest message.
  - Multi-line input with a Send button; Enter sends, Shift+Enter inserts a newline.
  - "Generating" indicator while a reply is in progress, and a Stop button that cancels the in-progress reply.
  - "New conversation" button that clears the session history.
  - Clickable suggested-question chips shown only while the conversation is empty.
- Input validation: empty/whitespace-only messages are not sent; configurable max length (default 500 characters) with a live counter and feedback; sending is blocked while a reply is in progress.
- A **simulated** reply generator that streams a canned pt-BR reply token-by-token, behind an abstraction that the next change will replace with an `IChatClient`-backed implementation. No real AI calls in this change.
- Conversation history kept in memory, scoped to the Blazor circuit. No database, no authentication, no persistence across reloads.
- Chat options (assistant name, max message length, suggested questions, simulated delay) bound from `appsettings.json`.

## Capabilities

### New Capabilities
- `support-chat`: The end-user support chat experience — screen layout and notices, message composition and validation, reply streaming lifecycle (generating, stop), conversation history scoped to the user's session, and conversation reset/suggestions.

### Modified Capabilities
- None (no existing specs).

## Impact

- **Code**: `AlgoristSupport/Components/Pages/Home.razor` (replaced by the chat page), new chat components under `Components/Chat/`, new services/models under `Chat/`, `Program.cs` (DI and options registration), `appsettings.json` (new `Chat` section), `wwwroot/app.css` or component-scoped CSS, a small JS module for auto-scroll and Enter/Shift+Enter handling. `App.razor` `lang` changes to `pt-BR`.
- **Dependencies**: Add `Microsoft.Extensions.AI.Abstractions` to reuse `ChatMessage`/`ChatRole`/`ChatResponseUpdate` types so the next change can drop in `IChatClient` without reshaping the history model. No HTTP/AI provider packages yet.
- **Tests**: Introduces a test project (none exists today) for validation, session state, and streaming/cancellation logic.
- **Hosting**: No change; still runs on Azure App Service B1. Per-circuit in-memory state means history is lost on reconnect-to-new-circuit or app restart — acceptable for the demo.
- **Out of scope**: Real LLM calls, Maritaca configuration/secrets, usage limits, policy grounding, citations, persistence, auth.
