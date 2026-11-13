# Spec Delta

## MODIFIED Requirements

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
