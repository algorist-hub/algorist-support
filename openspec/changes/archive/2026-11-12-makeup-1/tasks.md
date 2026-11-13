# Tasks

## 1. Design tokens, fonts and Bootstrap theme

- [x] 1.1 Download the latin WOFF2 files from Fontsource for Roboto 400/400-italic/500/700, Raleway 600/700, Poppins 500/600 and Roboto Mono 400 into `AlgoristSupport/wwwroot/fonts/`, and add `OFL.txt` (design D3). Verify the 9 `.woff2` files and `OFL.txt` are present and each font is under 40 KB.
- [x] 1.2 At the top of `wwwroot/app.css`, add the `@font-face` rules (`font-display: swap`, relative `fonts/…` URLs) and the `:root` design tokens from design D1, including the font tokens with fallback stacks. Verify `dotnet build` passes and the browser DevTools Network tab shows the fonts loading only from the app's own host.
- [x] 1.3 In `app.css`, theme Bootstrap through its variables (design D2): root `--bs-*` body, link, primary and focus variables; `.btn-primary`, `.btn-outline-primary` and `.btn-outline-secondary` button variables; the dark-plus-gold focus ring on `.btn`, `.form-control` and `a`; `color-scheme: light`; headings in `--font-heading`, buttons in `--font-ui`, `code` in `--font-mono`. Remove the old blue rules. Verify in the running app that the Send button is gold with dark text and that Tab focus shows the double ring.
- [x] 1.4 Add the guard test from design D6 (for example `AlgoristSupport.Tests/Components/BrandStyleTests.cs`) that scans `wwwroot/app.css` and `Components/**/*.razor.css` for the listed template blues. Verify it fails before groups 3 and 4 remove the remaining blues in the scoped CSS: record the failing files, then let it go green at task 4.3.

## 2. Brand top bar

- [x] 2.1 Create `Components/Layout/BrandBar.razor` with the logo link (`https://algorist.com.br`, `target="_blank"`, `rel="noopener noreferrer"`, `<img alt="Algorist" width="136" height="40">` and the visually hidden " (abre em nova janela)") (design D4). Verify with new bUnit `BrandBarTests`: `href`, `target`, `rel`, image `alt`, and an accessible name containing "Algorist" and "abre em nova janela".
- [x] 2.2 Add `BrandBar.razor.css`: `--brand-bar-background`, `--brand-bar-height`, kept at the top of the layout column (design D4), compact size under 576px (3.25rem, 32px logo), and a gold focus ring on the dark bar. Verify visually that the bar matches the site header (black translucent bar, gold logo).
- [x] 2.3 Render `<BrandBar />` first in `MainLayout.razor` and clean `MainLayout.razor.css`: remove the unused template `.sidebar`/`.top-row` rules and keep the `100dvh` column with `<main>` as the scroll container, so the chat fills the space below the bar and taller pages scroll below it. Verify with a bUnit test that rendering `MainLayout` with a body includes the brand bar link. Verify manually that on the chat screen the bar, chat header and demo notice are all visible in a long conversation and do not overlap.

## 3. Chat restyle

- [x] 3.1 Restyle `ChatHeader.razor.css`: title in `--font-heading`/`--heading-color`, notice with `--brand-gold-tint` background, gold left border and `--text-color` text, policy link in `--brand-gold-text` (design D5). Verify the existing `ChatComponentTests` still pass and the notice link reads at least 4.5:1 in a contrast checker.
- [x] 3.2 Restyle `ChatMessageList.razor.css`: user bubble gold with `--heading-color` text, assistant bubble `--surface-color` with `--text-color`, Markdown code in `--font-mono`, blockquote and table borders from tokens, `a.policy-citation` in `--brand-gold-text`, status and error colors from tokens. Verify the existing citation and message-list tests still pass and a reply with a `Seção 12` citation looks correct in the browser.
- [x] 3.3 Restyle `ChatPanel.razor.css`, `ChatComposer.razor.css` and `SuggestedQuestions.razor.css`: welcome text, generating indicator, maintenance box (tint and gold border), limit notice, counter, feedback, and chips (outline gold-text style from 1.3). Verify `dotnet test` passes and that the empty state, the over-limit counter and maintenance mode (blank API key) display in the new palette.

## 4. Policy page and template chrome

- [x] 4.1 Restyle `Policy.razor.css`: text and heading tokens, `h2` gold accent underline, `scroll-margin-top` kept at 1.5rem since `<main>` scrolls below the bar (design D4), a `:target` highlight with tint and a `--brand-gold-text` outline, blockquote with tint and gold border, and table header on `--surface-color` (design D5). Verify `PolicyPageTests` pass and that opening `/politica#secao-13` shows the highlighted heading below the sticky bar, not under it.
- [x] 4.2 Restyle `ReconnectModal.razor.css` and the `#blazor-error-ui` rules: brand button colors in place of the blue, and the error bar with the tint and danger tokens. Verify `dotnet build` passes. Check the reconnect modal by stopping the server with the page open.
- [x] 4.3 Run the guard test from 1.4. Verify it now passes with no template blue left in the app or scoped CSS.
- [x] 4.4 Update `README.md` with a short "Look and feel" note: the algorist.com.br visual identity, the self-hosted fonts (OFL) and the tokens in `wwwroot/app.css`. Verify the README links resolve.

## 5. Integration checks

- [x] 5.1 Run `dotnet build` and `dotnet test`. Verify zero failures.
- [x] 5.2 Do the manual visual and accessibility check and record the results in this task:
  - chat screen and `/politica` side by side with algorist.com.br: same top bar, gold accent and fonts;
  - logo link opens algorist.com.br in a new tab, and the chat keeps its conversation;
  - 360px viewport: bar, notice and composer visible, no horizontal scroll;
  - keyboard Tab through every control: visible focus ring on light backgrounds and on the dark bar;
  - contrast checker on the bubbles, links, notice, counter and error text: all at least 4.5:1;
  - DevTools Network: no requests to `fonts.googleapis.com`, `fonts.gstatic.com` or any other third-party host.

  Result (2026-11-12, headless Chrome 1280×800 and 360×740 driven over the DevTools protocol, with screenshots reviewed):
  - top bar, gold accent and Raleway/Roboto/Poppins match the algorist.com.br stylesheets. Chat, `/politica#secao-13` (highlighted below the bar), maintenance mode, the over-limit counter and the reconnect modal all render in the new palette;
  - logo link has `href="https://algorist.com.br"`, `target="_blank"` and `rel="noopener noreferrer"` (bUnit `BrandBarTests`). A physical click was not performed headless;
  - 360px: `scrollWidth == clientWidth == 360`, and the bar, notice and composer are visible;
  - Tab focus shows the dark inner and gold outer ring on light backgrounds; the dark bar's link uses a gold outline;
  - measured contrast: notice 9.05, notice link 6.56, counter 5.74, Send 11.54, Nova conversa 7.06, user bubble 11.54, assistant bubble 8.62, citation 6.25, counter/feedback error red ≥ 5.7;
  - network: only `localhost:5026` (plus a local antivirus injection, not from the app); no Google Fonts or other third-party hosts.
