# Design

## Context

- `docs/policy.md` has 21 top-level sections written as `## N. Title`. Clauses are paragraphs that start with `N.M.`, and the FAQ already cites sections as `(Seção 12)` / `(Seção 8.6)`. The policy text also cross-references sections, for example "conforme a Seção 14".
- `SafeMarkdown.ToHtml(markdown, headingIds)` parses with Markdig and then walks the AST. It removes any link whose URL is not an absolute http/https/mailto URL, so a relative `politica#...` link written by the model is dropped today. With `headingIds: true`, which only the policy page uses, headings get GitHub-style auto IDs that depend on the title (`12-garantia`).
- `ChatMessageList` renders assistant entries with `SafeMarkdown.ToHtml(entry.Text)` on every streamed chunk. `ChatEntry.Status` tells streaming entries apart from finished ones (`Complete`/`Interrupted`/`Failed`).
- `PolicyLoadResult` is a singleton loaded once in `Program.cs`. When the policy is missing, the chat is in maintenance mode, so no reply is ever rendered.
- The app prerenders (global `InteractiveServer`), so `/politica` HTML, including heading IDs, is in the first response, and the browser scrolls to `#fragment` natively.

See proposal.md for motivation and the three spec deltas for the required behavior.

## Goals / Non-Goals

**Goals:**
- One source of truth for "which sections exist and what their anchor is", shared by the chat linker and the policy page.
- Citation detection and section indexing as pure, unit-testable code in the `Chat` layer. Markdig stays in `Components/Shared`.
- Keep the existing URL safety filter intact for anything the model writes.

**Non-Goals:**
- Checking that a cited section actually supports the claim (manual evaluation only).
- Logging or metrics for unresolved citations.
- Any change to streaming, history or the provider request apart from one prompt rule.

## Decisions

### D1. Model writes plain-text citations; the app builds the links
The prompt asks for `(Seção N)`, the format the policy FAQ already shows the model in every request. The app finds these citations after rendering and turns them into links.
*Alternatives:*
- Let the model write Markdown links: they get dropped by the URL filter, and the URL would come from model output.
- A structured "Fontes:" footer or tool/JSON output: harder with streaming, and it does not match the FAQ style.
Plain text also degrades well: if linking fails, the reader still sees "Seção 12".

### D2. `PolicySectionIndex` in `Chat/` (pure)
- `PolicySectionIndex.Build(PolicyDocument)` scans lines that match `^## (\d+)\.\s+(.+)$` and keeps `Number`, `Title` and `Anchor` for each.
- `static string AnchorFor(int number) => $"secao-{number}"` is the single definition of the anchor format.
- `TryGet(int number, out PolicySection section)` looks up a section.
- `PolicySectionIndex.Empty` is used when the policy is not loaded.

`Program.cs` builds it once from `PolicyLoadResult` and registers it as a singleton, next to the existing policy registration.
*Alternative:* derive sections from the Markdig AST in `Components`. This was rejected because it would tie domain knowledge (what a "section" is) to the renderer. A line regex is enough for the authored format and is trivially testable.

### D3. `PolicyCitationFinder` in `Chat/` (pure)
`Find(string text)` returns the citation numbers it finds as `(Start, Length, SectionNumber)` spans over digit runs.

The pattern is case-insensitive:
- `Seção|Secao` followed by `N` or `N.M`, which maps to N;
- the plural `Seções|Secoes` followed by `N(.M)?` items separated by `,` / `e`.

Each number becomes its own span, so `Seções 12 e 13` produces two links. In the singular case, the span covers `Seção 12.1` as a whole for a larger click target. Word boundaries keep `Seção` inside a longer word, and digits not preceded by the keyword, from matching. The finder does not check whether the section exists. The index decides that (SRP).

### D4. Linking runs on the Markdig AST, opt-in
`SafeMarkdown.ToHtml` gains an optional `PolicySectionIndex? citations` argument. When it is set, after the existing URL-filter pass:
- each `LiteralInline` that is not inside a `LinkInline` (code spans are separate `CodeInline` nodes and are never visited) is split into literal pieces and new `LinkInline`s for spans whose number exists in the index;
- each link gets `href="politica#secao-N"`, `target="_blank"`, `rel="noopener noreferrer"` and `aria-label="Seção N — <Title> (abre em nova janela)"`, plus `class="policy-citation"` for styling.

Because these links are created after the filter and only from the index, the relative-URL exception covers app-built links only. Model-written links still go through the filter unchanged (see the support-chat scenario).
*Alternative:* a regex over the rendered HTML string. Rejected: fragile with entities and tags, and it could link inside `<code>` or existing links.

### D5. Link only when the entry is not streaming
`ChatMessageList` injects `PolicySectionIndex` and passes it only when `entry.Status != Streaming`. This avoids a link to section 1 showing up while `12` is still arriving, and it keeps the per-chunk render cost the same as today. When streaming ends, the final re-render shows the links. User entries never go through Markdown, so they are never linked.

### D6. Numeric anchors on the policy page
With `headingIds: true`, `SafeMarkdown` overrides the auto ID of every level-2 heading whose text matches `^\d+\.` with `PolicySectionIndex.AnchorFor(n)`. Markdig sets auto IDs during parsing, so the override happens after `Parse` and before rendering. Other headings, such as the document title, keep their GitHub auto IDs.

`Policy.razor.css` adds `scroll-margin-top` to `h2` and a `:target` highlight (background tint plus outline, with contrast checked in light and dark themes).
*Alternative:* keep the title-based IDs and compute matching anchors. Rejected: renaming a title would silently break links, and the index would have to copy Markdig's slug rules.

### D7. Prompt rule
`SupportAssistantPrompt` adds `CiteSections` as rule 10, after `PolicyIsData`, roughly: "Ao usar a política, cite a(s) seção(ões) em que a resposta se baseia no formato (Seção N), como no FAQ da política. Não cite seções quando a informação não constar na política nem ao recusar perguntas fora do tema." The existing tests that check rule order and delimiters are extended, not rewritten.

## Risks / Trade-offs

- [The model cites a wrong but existing section] → It still gets a link. This is mitigated by the prompt and the manual evaluation script, which now checks citation correctness. The reader can verify with one click, which is the point of the feature.
- [The model omits citations or uses another format (e.g. "item 12", "§12")] → Plain text is still correct. The format list stays narrow on purpose to avoid false links. Widen it later if evaluation shows a common variant.
- [A false positive on `Seção N` that is not a policy reference] → Only numbers present in the index are linked, and only in assistant replies grounded in this policy.
- [The relative-link exception weakens the safety filter] → Links are created only from index entries with a fixed `politica#secao-<int>` format. No model text reaches `href`. A test asserts that a model-written relative link is still removed.
- [The policy heading format changes (e.g. `### 12 -`)] → The index finds no sections, citations stay plain text, and nothing breaks. A unit test over the real `docs/policy.md` asserts 21 sections, so a format change fails CI.
- [Hash scroll without prerendering] → The app prerenders today. If that changes, deep-link scrolling would need a small JS `scrollIntoView` and could be revisited then.

## Migration Plan

No data or configuration changes. Deploy through GitHub Actions as usual. Roll back by redeploying the previous build. Old links to `/politica#12-garantia` (title-based) stop pointing at the section, which is acceptable because nothing published them.
