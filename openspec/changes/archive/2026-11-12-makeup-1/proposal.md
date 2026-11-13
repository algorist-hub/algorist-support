# Proposal

## Why

The support chat is a portfolio piece for Algorist, but it still uses the default Blazor/Bootstrap look (blue buttons, Helvetica, no logo). A visitor coming from algorist.com.br sees an app that does not look like the company's. Matching the company site's visual identity makes the demo read as an Algorist product and shows front-end polish alongside the back-end work.

## What Changes

- Add a site-style **brand top bar** on every page (chat and `/politica`): a black, slightly translucent bar with the gold Algorist logo. The logo links to `https://algorist.com.br` and opens in a new tab, so an active conversation is never lost.
- Adopt the site's **typography**: Raleway for headings, Roboto for body text, Poppins for the brand bar and buttons, Roboto Mono for code. The fonts are self-hosted, so the app makes no third-party font requests.
- Adopt the site's **color palette** as shared design tokens: gold accent `#ffc451`, heading `#151515`, body text `#444444`, light content background, `#252525`/black for dark surfaces. Bootstrap's default blue is replaced in buttons, focus rings, links and form controls.
- Restyle the chat to match: gold-accented primary buttons, user and assistant message bubbles, suggested-question chips, composer, generating indicator, demo notice, maintenance and error states, and policy-citation links.
- Restyle the policy page: site-style headings, tables, blockquote (fictional-document notice) and the `:target` section highlight.
- Restyle the leftover template chrome (reconnect modal, unhandled-error bar) so no default Blazor blue remains.
- Keep text accessible: where the site uses gold text on white (contrast too low for WCAG AA), the app uses a darker gold for text and keeps the bright gold for backgrounds, borders and focus rings on dark surfaces.

Out of scope: a dark theme or theme switcher, a site-style footer, the site's navigation menu, animations (AOS) and hero imagery. Chat behavior, texts and layout order do not change.

## Capabilities

### New Capabilities
- `brand-appearance`: the visual identity shared by all pages: the brand top bar and logo link, typography, color palette, and the contrast and focus-visibility rules the styling must meet.

### Modified Capabilities
None. The existing `support-chat` and `policy-viewer` requirements (demo notice always visible, policy link behavior, rendering safety, pt-BR texts) stay valid without changes. The new top bar must not break them, and the `brand-appearance` scenarios cover that.

## Impact

- **Code**: `Components/App.razor` (font and stylesheet links), `Components/Layout/MainLayout.razor(.css)` (new brand bar, layout), a new `Components/Layout/BrandBar.razor(.css)`, `wwwroot/app.css` (design tokens, Bootstrap overrides, `@font-face`), and the scoped CSS of the chat components, the policy page and `ReconnectModal`.
- **Assets**: new self-hosted font files under `wwwroot/fonts/` (SIL Open Font License). The existing `wwwroot/brand/logo.png` is reused.
- **Tests**: new bUnit tests for the brand bar (logo, alt text, link target and accessible name). Existing component tests must still pass. A manual visual and contrast check is added.
- **Dependencies/APIs**: no new packages, no API or configuration changes. Deployment is unchanged.
