# Design

## Context

See proposal.md (Why) for motivation and `specs/brand-appearance/spec.md` for the requirements.

Current state:
- `Components/App.razor` loads Bootstrap 5.3.3 (`lib/bootstrap`), `app.css` and the scoped-CSS bundle. Every routed page, including `NotFound` and `Error`, renders inside `Layout/MainLayout`.
- `MainLayout` is a full-height flex column (`100dvh`) holding `<main><article>`. The chat scrolls inside `.chat-scroll`. The policy page is taller than the viewport, so it scrolls the document.
- Colors are hard-coded hex values in `app.css` and in each scoped `*.razor.css` (blue `#1b6ec2`/`#006bb7`/`#0b5394`/`#258cfb`, Bootstrap yellow notices, grays). Components use the Bootstrap classes `btn-primary`, `btn-outline-primary`, `btn-outline-secondary`, `btn-outline-danger` and `form-control`.
- `wwwroot/brand/logo.png` (680×200, gold on transparent, 128 KB) already exists.

Observed style of algorist.com.br (from its published CSS): accent `#ffc451`, headings `#151515`, body `#444444`, white and `#f9f9f9` sections, dark sections `#060606`/`#252525`. The header is `rgba(0,0,0,0.8)` with a 40–50px logo. Fonts are Raleway (headings), Roboto (body), Poppins (nav/buttons) and Roboto Mono (code), loaded from Google Fonts. Focus rings use a white inner ring and a gold outer ring.

## Goals / Non-Goals

**Goals:**
- One set of design tokens (CSS custom properties) that every stylesheet uses, so the palette is defined in one place.
- Theme Bootstrap through its CSS variables instead of forking or rebuilding it.
- Keep component markup and behavior unchanged apart from the new brand bar.

**Non-Goals:**
- No Sass build or Bootstrap recompile, and no CSS framework change.
- No configuration option for the company URL or logo. They are brand constants.
- No pixel-for-pixel copy of the site. The goal is the same visual language (palette, fonts, header style), not the same layout.

## Decisions

### D1. Design tokens as CSS custom properties in `app.css`
Define these tokens on `:root` in `wwwroot/app.css`. The values have been checked for contrast (ratios against the backgrounds named):

| Token | Value | Use / contrast |
|---|---|---|
| `--brand-gold` | `#ffc451` | button and bubble backgrounds, borders, focus ring, top-bar accents (13.3:1 on black, **1.6:1 on white: never as text on light**) |
| `--brand-gold-text` | `#7a5000` | gold-family text and links on light backgrounds (7.1:1 on white, 6.6:1 on `--brand-gold-tint`) |
| `--brand-gold-tint` | `#fff6e0` | demo notice, policy blockquote, `:target` highlight |
| `--heading-color` | `#151515` | headings, text on gold (11.5:1 on `--brand-gold`) |
| `--text-color` | `#444444` | body text (9.7:1 on white, 8.6:1 on `#f1f1f1`) |
| `--muted-color` | `#666666` | counter, status, generating indicator (≥5.4:1 on white/`#f9f9f9`) |
| `--surface-color` | `#f1f1f1` | assistant bubble, table header |
| `--page-background` | `#ffffff` | page |
| `--border-color` | `#dddddd` | separators (decorative only) |
| `--danger-color` | `#b02a37` | errors (6.5:1 on white, 5.8:1 on `#f1f1f1`) |
| `--brand-bar-background` | `rgba(0,0,0,0.85)` | top bar |
| `--brand-bar-height` | `4rem` | top bar height and the policy `scroll-margin-top` |
| font tokens | `--font-body`, `--font-heading`, `--font-ui`, `--font-mono` | Roboto, Raleway, Poppins, Roboto Mono, each with a system fallback stack |

Scoped `*.razor.css` files replace their hex values with `var(--…)`. Custom properties cascade through Blazor CSS isolation, so no `::deep` is needed for tokens.
*Alternative:* copy the site's variable names (`--primary-color`, `--default-color`). Rejected because names like "default" are vague, and the app needs extra tokens (gold text, tint) that the site does not have.

### D2. Theme Bootstrap through its variables
In `app.css`, after the tokens, set `--bs-body-font-family`, `--bs-body-color`, `--bs-body-bg`, `--bs-link-color(-rgb)`, `--bs-link-hover-color(-rgb)`, `--bs-primary(-rgb)`, `--bs-focus-ring-color` and `--bs-border-radius` on `:root`. Then override the component variables:
- `.btn-primary`: `--bs-btn-bg/border-color: var(--brand-gold)`, `--bs-btn-color: var(--heading-color)`, hover/active a slightly darker gold, disabled at reduced opacity but still with dark text.
- `.btn-outline-primary` and `.btn-outline-secondary` (chips, retry, "Nova conversa"): text and border `--brand-gold-text`, hover background `--brand-gold` with `--heading-color` text.
- `.btn-outline-danger` (Parar) keeps red, which is not blue and has enough contrast.
- `.btn`, `.form-control` and `a`: the focus ring is a **dark inner ring plus a gold outer ring** (`0 0 0 .125rem var(--heading-color), 0 0 0 .3rem var(--brand-gold)`). Gold alone on white is only 1.6:1, below the 3:1 needed for focus indicators. The dark ring carries the contrast and the gold carries the brand, mirroring the site's double ring. `.form-control:focus` border uses `--brand-gold-text`.
- `body` declares `color-scheme: light` so native controls stay light (dark theme is out of scope).

*Alternative:* recompile Bootstrap from Sass with a new `$primary`. Rejected because it adds a Node/Sass toolchain to a .NET-only build for a few variables.

### D3. Self-hosted fonts
Download the latin-subset WOFF2 files from Fontsource (the same Google Fonts files, packaged under the SIL OFL) into `wwwroot/fonts/`, with `OFL.txt` next to them. Declare `@font-face` rules with `font-display: swap` at the top of `app.css`. Load only the weights in use: Roboto 400/400-italic/500/700, Raleway 600/700, Poppins 500/600, Roboto Mono 400. `MapStaticAssets` already fingerprints and caches them.
*Alternative:* the Google Fonts `<link>` the site uses. Rejected because it sends every visitor's IP address to a third party on each page load, which goes against the app's privacy stance (no message content in logs) and the spec's "no third-party font requests" rule. It also adds an external runtime dependency.

### D4. `BrandBar` component inside `MainLayout`
Add `Components/Layout/BrandBar.razor` (+ `.razor.css`). It renders `<header class="brand-bar">` containing a link to `https://algorist.com.br` (a `const` in the component) with `target="_blank" rel="noopener noreferrer"`. The link wraps `<img src="brand/logo.png" alt="Algorist" width="136" height="40">` followed by `<span class="visually-hidden"> (abre em nova janela)</span>`, so the accessible name is "Algorist (abre em nova janela)". This follows the pattern of the existing policy link in `ChatHeader`. `MainLayout` renders `<BrandBar />` as the first child of `.page`.

The bar is the first flex item of the `100dvh` `.page` column, and `<main>` takes the rest of the height with `overflow-y: auto`:
- On the chat screen the bar takes its height first, and the chat header, notice, list and composer share the rest, so nothing is covered (spec: "Top bar does not hide the demo notice").
- Taller pages (the policy) scroll inside `<main>`, below the bar, so the bar is always visible and never covers a heading. `/politica#secao-N` scrolls `<main>`, and the existing `scroll-margin-top: 1.5rem` is enough.

*Revised during implementation:* the first plan was `position: sticky; top: 0` with the policy page scrolling the document under a translucent bar. That fails because a sticky element cannot leave its containing block: inside the `100dvh` `.page`, the bar would scroll away once the policy scrolled past one viewport. Making `<main>` the scroll container fixes this. The cost is that content never passes under the bar, so its 0.85 translucency shows as a solid near-black.

Because the brand bar's `<header>` sits outside `<main>`, it becomes the page's only `banner` landmark. `ChatHeader`'s `<header>` is nested in `<main>`/`<article>`, so it is not a banner, and there are no duplicate landmarks.
*Alternatives:* `position: fixed` with padding on `main`. Rejected because it needs the bar height duplicated as padding and risks covering the notice on resize. Putting the logo inside `ChatHeader`: rejected because the policy, not-found and error pages would not get it.

### D5. Component restyle mapping
- **Chat bubbles:** user `--brand-gold` background with `--heading-color` text. Assistant `--surface-color` background with `--text-color` text. Markdown `code`/`pre` use `--font-mono` on a translucent dark tint. Code text uses the bubble's text color, not gold.
- **Policy-citation links and the notice's policy link:** `--brand-gold-text`, underline kept (dotted for citations, solid on hover/focus).
- **Demo notice and maintenance box:** `--brand-gold-tint` background, 4px `--brand-gold` left border, `--text-color` text. Replaces Bootstrap's alert yellow.
- **Headings** (chat title, policy `h1`/`h2`): `--font-heading`, `--heading-color`. Policy `h2` gets a short gold underline accent like the site's section titles (decorative, not relied on for meaning).
- **Policy `:target`:** `--brand-gold-tint` background with a 2px `--brand-gold-text` outline. A darker outline keeps 3:1 on white.
- **ReconnectModal and `#blazor-error-ui`:** the blue button becomes `.btn-primary` colors, and the error bar uses the tint and danger tokens.

### D6. Guard test against leftover default blue
Add a test in `AlgoristSupport.Tests` that reads `wwwroot/app.css` and every `Components/**/*.razor.css` from the source tree and fails if it finds the known template blues (`#1b6ec2`, `#1861ac`, `#006bb7`, `#0b5394`, `#258cfb`, `#0087ff`, `#6b9ed2`, `#3b6ea2`). This is cheap protection for "No default blue remains". Bootstrap's own file is excluded because D2 overrides it through variables. The visual part is checked manually.

## Risks / Trade-offs

- [Bootstrap blue leaks from a state not overridden (active, disabled or validation focus)] → D2 overrides the root `--bs-primary`/link/focus variables as well as the per-button ones. The manual check walks every control state.
- [Text flashes in the fallback font while fonts load (FOUT)] → `font-display: swap`, latin-only subsets (each file is about 20 KB), and fallback stacks with similar metrics (Segoe UI/Helvetica, Arial).
- [The 128 KB logo is larger than a 40px bar needs] → acceptable for now: it is cached by fingerprinting and appears once. A resized copy can come later without spec changes.
- [The sticky bar takes 4rem of height on small phones, leaving less room for messages] → the bar is compact on narrow viewports (`3.25rem`, logo 32px), and the spec's 360px scenario is checked manually.
- [Gold-on-gold or gold-on-white contrast regressions when tokens change later] → the contrast table in D1 is the reference, and the manual contrast check is part of the tasks.

## Migration Plan

Front-end assets only. They deploy with the normal pipeline, with no configuration or data changes. Rollback is a revert of the commit.
