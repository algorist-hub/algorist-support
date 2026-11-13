# Tasks

## 1. Policy section index

- [x] 1.1 Add `PolicySection` (`Number`, `Title`, `Anchor`) and `PolicySectionIndex` in `AlgoristSupport/Chat/`, with `Build(PolicyDocument)`, `TryGet`, `Empty` and `static AnchorFor(int) => "secao-N"` (design D2). Verify with new `PolicySectionIndexTests`: parses `## 12. Garantia`, ignores `#`/`###` and unnumbered headings, `AnchorFor(12) == "secao-12"`, and `Empty` resolves nothing.
- [x] 1.2 Add a test that builds the index from the real shipped `Policy/policy.md` and asserts 21 sections, with section 12 "Garantia" and section 13 starting "Suporte". Verify it passes with `dotnet test`.
- [x] 1.3 In `Program.cs`, build the index once from `PolicyLoadResult` (`Empty` when not loaded) and register it as a singleton. Register it in the `ChatComponentTests` setup too (from the test policy). Verify `dotnet build` and the existing tests pass.

## 2. Citation finder

- [x] 2.1 Add `PolicyCitationFinder.Find(string)` in `AlgoristSupport/Chat/`, returning `(Start, Length, SectionNumber)` spans (design D3). Verify with `PolicyCitationFinderTests` for: `Seção 12`; `seção 12` (case); `Secao 12` (no accent); `Seção 12.1` → 12, with the span covering `Seção 12.1`; `Seções 12 e 13` and `Seções 8, 12 e 13` → one span per number; `(Seção 9).` punctuation; no match for a bare `12`, `Seção` without a number, or `Seçãoxyz 12`.

## 3. Citation links in assistant replies

- [x] 3.1 Add the optional `PolicySectionIndex? citations` argument to `SafeMarkdown.ToHtml`. After the URL filter, split `LiteralInline`s outside links into text plus app-built `LinkInline`s (`politica#secao-N`, `target="_blank"`, `rel`, `aria-label` with title and "(abre em nova janela)", `class="policy-citation"`) for numbers present in the index (design D4). Verify with `SafeMarkdown` unit or bUnit tests: known section linked, unknown section plain, code span not linked, text inside an existing link not linked, emphasis `**Seção 12**` linked, and `[veja](politica#secao-12)` written by the model still has no link.
- [x] 3.2 In `ChatMessageList.razor`, inject `PolicySectionIndex` and pass it only for assistant entries whose `Status` is not `Streaming` (design D5). Add `.policy-citation` styling in `ChatMessageList.razor.css`. Verify with bUnit tests: a complete reply with `(Seção 12)` renders a link with the expected `href`/`target`/accessible name; a streaming reply renders no link; a user message `O que diz a Seção 12?` renders no link.

## 4. Policy page anchors

- [x] 4.1 With `headingIds: true`, override the ID of level-2 headings matching `^\d+\.` with `PolicySectionIndex.AnchorFor(n)` (design D6). Verify by updating `PolicyPageTests.Headings_get_anchor_ids` to assert `id == "secao-12"` for `## 12. Garantia`, and add a case showing an unnumbered heading still gets a non-empty auto ID.
- [x] 4.2 Add `scroll-margin-top` and a `:target` highlight for section headings in `Policy.razor.css`, checking contrast in light and dark themes. Verify manually by opening `/politica#secao-13`: the heading is visible below the page header and highlighted.

## 5. Prompt rule and docs

- [x] 5.1 Add the `CiteSections` constant as rule 10 in `SupportAssistantPrompt.Build` (design D7). Verify `SupportAssistantPromptTests` assert that the rule is present, that it mentions `(Seção N)`, and that all rules still come before `<politica>`.
- [x] 5.2 Update `README.md` (Policy grounding / Chat features) to describe inline citations, the `secao-N` anchors and the sections-only limitation. Verify the README links resolve.

## 6. Integration checks

- [x] 6.1 Run `dotnet build` and `dotnet test`. Verify zero failures.
- [x] 6.2 Run the app and do the manual evaluation in pt-BR. Record the results in this task:
  - [x] "Qual é o prazo de garantia?" → 90 days, cites `Seção 12`, and the link opens `/politica#secao-12` in a new tab with the heading highlighted;
  - [x] "Durante a garantia, em quanto tempo vocês respondem a um problema crítico?" → cites sections 12 and 13, both linked;
  - [x] "Qual é o valor da hora de um desenvolvedor sênior?" → not in the policy, no citation;
  - [x] "Receita de bolo de cenoura?" → polite refusal, no citation;
  - [x] press **Parar** mid-reply → the partial reply's citations become links.

  Result (2026-11-12): all five checks passed in the browser, tested manually by the user.
