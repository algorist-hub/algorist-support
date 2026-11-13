# Design

## Context

See proposal.md (Why) and the delta specs for `support-chat` and `brand-appearance`.

Current state:
- `ChatHeader` renders `<h1 class="chat-title">@AssistantName</h1>`, the demo notice (with the `/politica` link and a visually hidden " (abre em nova janela)") and the "Nova conversa" button. It receives `AssistantName` and `OnNewConversation` from `ChatPanel`. `ChatPanel` reads `IOptions<SupportChatOptions>` and owns `NewConversation()`, which calls `Session.Reset()` and clears the draft.
- `ChatComposer` is a `<form>` whose footer has `.chat-composer-info` (counter and validation message) on the left and `.chat-composer-actions` (Parar, Enviar) pushed right with `margin-left: auto`. The footer already wraps (`flex-wrap: wrap`). The composer is not rendered in maintenance mode.
- `BrandBar` (in `MainLayout`, so on every page) is static: a logo link only.
- `SupportChatOptions` is registered globally with `AddOptions<SupportChatOptions>().Bind(...)`, so the layout can inject it on any page. `AssistantName` is `[Required]`.
- `Routes.razor` uses `<FocusOnNavigate Selector="h1">`, so each page needs an `h1`.

## Goals / Non-Goals

**Goals:**
- Keep the assistant name in one setting (`Chat:AssistantName`) used by the bar, the hidden heading and the screen-reader author labels.
- Keep `ChatSession` and the new-conversation behavior untouched. Only the place where the button is rendered changes.

**Non-Goals:**
- No change to the system prompt's self-introduction (see proposal, out of scope).
- No configuration option for the repository URL. It is a fixed fact about this project, like the company site URL in `BrandBar`.

## Decisions

### D1. The brand bar reads the name from options
`BrandBar` injects `IOptions<SupportChatOptions>` and renders, after the logo link, `<span class="brand-name">@Options.AssistantName</span>`. The name is outside the link, so the link's accessible name stays "Algorist (abre em nova janela)" as the existing spec requires. The span is plain text, not a heading: the bar is on every page, and the policy page has its own `h1`.
Styling: Poppins (`--font-ui`) 500, white text on the dark bar (contrast above 15:1), separated from the logo by a 1px light divider and padding, like the site's header spacing. Under 576px the font shrinks to 0.95rem, and the 32px logo plus the name fit in 360px (about 110px + 170px). `white-space: nowrap` with `overflow: hidden; text-overflow: ellipsis` protects against a long configured name.
*Alternative:* a fixed string in `BrandBar`. Rejected because the user chose to keep it configurable.

### D2. Default name changes in two places
`SupportChatOptions.AssistantName` default and `appsettings.json` `Chat:AssistantName` become "Assistente de Suporte". Environment overrides (`Chat__AssistantName`) keep working.

### D3. Hidden `h1` in `ChatHeader`
`ChatHeader` keeps `<h1 class="visually-hidden">@AssistantName</h1>`, which drops the `chat-title` styling. Screen-reader users still get a page heading, and `FocusOnNavigate` still finds an `h1` on the chat page. It does not duplicate what sighted users see, because the visible name is in the bar.
*Alternative:* make the bar's name the `h1`. Rejected because the policy page would then have two `h1` elements.

### D4. Repository link inside the notice
After "…criada apenas para fins ilustrativos.", append ` Repositório do projeto: <a class="chat-repo-link" href="https://github.com/algorist-hub/algorist-support" target="_blank" rel="noopener noreferrer">https://github.com/algorist-hub/algorist-support<span class="visually-hidden"> (abre em nova janela)</span></a>`. This follows the existing policy-link pattern, and both links share the same style (`--brand-gold-text`, underlined, 6.6:1 on the tint). The notice gets `overflow-wrap: anywhere` so the long URL wraps on narrow screens instead of causing horizontal scroll. The URL is a `const` in `ChatHeader`.

### D5. "Nova conversa" moves into `ChatComposer`
`ChatComposer` gets a new `[Parameter] EventCallback OnNewConversation`. The footer becomes:

```
[Nova conversa] [counter] [validation message] ............ [Parar] [Enviar]
```

The button is `<button type="button" class="btn btn-outline-secondary chat-new">Nova conversa</button>`, the first child of `.chat-composer-info` or a new left group. `type="button"` is required because it sits inside the `<form>` and must not submit. It stays enabled while generating, since the spec allows clearing during generation. It uses the normal button size, so it lines up with Enviar. `ChatPanel` passes `OnNewConversation="NewConversation"` to the composer. `ChatHeader` loses its button and the `OnNewConversation` parameter, so the header becomes notice-only and spans the full width.
The `.chat-new` class name is kept, so existing tests that click it keep working, and only their parent assertions change.
Narrow screens: the footer already wraps. With Parar shown at 360px the actions group wraps to a second line, still right-aligned, and the button order is kept.
*Alternative:* keep the button in `ChatPanel` and position it with CSS next to the composer. Rejected because CSS cannot reorder it into the composer's flex row cleanly, and the composer is the component that owns that row.

## Risks / Trade-offs

- [The composer row is crowded on phones (Nova conversa, counter, a long validation message, Parar, Enviar)] → existing wrap behavior. The validation message is allowed to take its own line, and the 360px check is a manual task.
- [The assistant names itself "Assistente Algorist" in replies while the UI says "Assistente de Suporte"] → accepted as out of scope for this change, and flagged in the proposal. A follow-up can make the prompt use the setting.
- [A long configured name overflows the bar] → ellipsis on the name span (D1).

## Migration Plan

UI and default-setting change only. It ships with the normal deployment. Deployments that set `Chat__AssistantName` keep their value. Rollback is a revert.
