# brand-appearance Specification

## Purpose

Gives every page of the support app the visual identity of the Algorist company website (algorist.com.br), so the demo is recognizable as an Algorist product, while keeping the interface readable and accessible.

## Requirements

### Requirement: Brand top bar
Every page (the chat screen, `/politica` and the not-found and error pages) SHALL show a brand top bar at the top of the viewport: a black, slightly translucent bar with the Algorist logo, matching the header of algorist.com.br. Beside the logo, the bar MUST show the configured assistant name (default "Assistente de Suporte") as text. The logo image MUST have the alternative text "Algorist". The bar MUST stay visible when the page content scrolls.

#### Scenario: Top bar on the chat screen
- **WHEN** a visitor opens the chat screen
- **THEN** a dark top bar with the Algorist logo is shown above the chat header
- **AND** the text "Assistente de Suporte" is shown beside the logo

#### Scenario: Top bar on the policy page
- **WHEN** a visitor opens `/politica`
- **THEN** the same dark top bar with the Algorist logo and the text "Assistente de Suporte" is shown above the policy text

#### Scenario: Configured name shown
- **WHEN** the assistant name setting is changed to another value and the application is restarted
- **THEN** the top bar shows that value beside the logo

#### Scenario: Top bar shown in maintenance mode
- **WHEN** the chat is in maintenance mode
- **THEN** the top bar with the Algorist logo and the assistant name is still shown

#### Scenario: Logo has alternative text
- **WHEN** a screen-reader user reaches the logo
- **THEN** it is announced as "Algorist"

#### Scenario: Name fits on a narrow viewport
- **WHEN** a visitor opens the chat screen on a 360px-wide viewport
- **THEN** the logo and the assistant name are both fully visible in the top bar, without horizontal scroll

### Requirement: Logo links to the company website
The logo in the brand top bar SHALL be a link to `https://algorist.com.br` that opens in a new browser tab or window, so the current page and any conversation stay intact. The link's accessible name MUST identify Algorist and state, in pt-BR, that it opens in a new window.

#### Scenario: Logo opens the company site in a new tab
- **WHEN** a visitor activates the logo link during a conversation
- **THEN** `https://algorist.com.br` opens in a new browser tab or window
- **AND** the chat tab keeps its current conversation

#### Scenario: Logo link is accessible
- **WHEN** a screen-reader user reaches the logo link
- **THEN** its accessible name identifies Algorist and says, in pt-BR, that it opens in a new window

### Requirement: Top bar does not hide the demo notice
The brand top bar SHALL NOT cover any page content. On the chat screen the chat header and its demo notice MUST remain fully visible below the top bar, at any conversation length and on narrow (mobile) viewports.

#### Scenario: Notice visible below the top bar in a long conversation
- **WHEN** the conversation has enough messages to scroll the message list
- **THEN** the top bar, the chat header and the demo notice are all visible without scrolling the page
- **AND** none of them overlaps another

#### Scenario: Narrow viewport
- **WHEN** a visitor opens the chat screen on a 360px-wide viewport
- **THEN** the top bar, the demo notice and the message composer are visible and the page has no horizontal scroll

### Requirement: Company typography
Text SHALL use the typefaces of algorist.com.br: Raleway for headings, Roboto for body text, Poppins for the top bar and buttons, and Roboto Mono for code. The font files MUST be served by the application itself, so pages make no requests to third-party font services.

#### Scenario: Headings and body use the company fonts
- **WHEN** a visitor opens `/politica`
- **THEN** the section headings are rendered in Raleway and the paragraphs in Roboto

#### Scenario: Code uses the monospace font
- **WHEN** an assistant reply contains inline code
- **THEN** the code is rendered in Roboto Mono

#### Scenario: No third-party font requests
- **WHEN** a visitor loads any page
- **THEN** no request is made to a host other than the application's own for fonts

### Requirement: Company color palette
The interface SHALL use the color palette of algorist.com.br instead of the default Bootstrap blue: the gold accent `#ffc451` for primary buttons, focus indicators and highlights, near-black headings, dark-gray body text and light content backgrounds. Controls and links on every page MUST NOT use the default Bootstrap blue.

#### Scenario: Primary button uses the brand accent
- **WHEN** a visitor views the chat composer
- **THEN** the Send button has a gold background with dark text

#### Scenario: Focus uses the brand accent
- **WHEN** a keyboard user moves focus to the message input or a button
- **THEN** a visible gold focus indicator is shown

#### Scenario: No default blue remains
- **WHEN** a visitor views the chat screen with messages, suggested questions and a policy citation, and the policy page
- **THEN** no button, link, message bubble or focus ring uses the default Bootstrap blue

### Requirement: Accessible contrast
All text and meaningful non-text elements styled with the brand palette SHALL meet WCAG 2.1 AA contrast: at least 4.5:1 for normal text, and 3:1 for large text, focus indicators, and control borders that identify a control. Where the brand gold is too light on a light background, a darker gold MUST be used for text.

#### Scenario: Links on light backgrounds are readable
- **WHEN** a policy-citation link or the notice's policy link is shown on a light background
- **THEN** the link text has a contrast ratio of at least 4.5:1 against its background

#### Scenario: Message bubbles are readable
- **WHEN** a user message and an assistant reply are shown
- **THEN** the text in each bubble has a contrast ratio of at least 4.5:1 against the bubble background

#### Scenario: Status and error texts are readable
- **WHEN** the generating indicator, the character counter, a validation message or a reply error is shown
- **THEN** its text has a contrast ratio of at least 4.5:1 against its background
