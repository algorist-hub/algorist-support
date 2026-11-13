# Spec Delta

## MODIFIED Requirements

### Requirement: Chat screen header and demo notice
The chat screen SHALL display a header containing a permanent notice, in pt-BR, stating that the assistant is an automated demo and that it answers about a fictional policy. The configured assistant name MUST NOT appear as a visible title in the header, because it is shown in the brand top bar. The chat screen MUST still have a level-1 heading with the configured assistant name for assistive technology. The notice MUST remain visible at all times, regardless of conversation length or state, and MUST NOT be dismissible. The notice's reference to the fictional policy MUST be a link to the policy page (`/politica`) that opens in a new browser tab or window, keeping the chat and its conversation intact, and whose accessible name indicates that it opens in a new window.

#### Scenario: Header shown on first load
- **WHEN** a visitor opens the chat screen
- **THEN** the header shows a notice that this is an automated demo assistant and the policy is fictional
- **AND** no visible title with the assistant name is shown below the top bar

#### Scenario: Screen-reader heading kept
- **WHEN** a screen-reader user lists the headings of the chat screen
- **THEN** a level-1 heading with the configured assistant name is present

#### Scenario: Notice stays visible in a long conversation
- **WHEN** the conversation has enough messages to scroll the message list
- **THEN** the demo notice remains visible without scrolling

#### Scenario: Notice cannot be dismissed
- **WHEN** a visitor inspects the notice
- **THEN** no control is offered to hide or close it

#### Scenario: Policy link opens in a new window
- **WHEN** a visitor activates the policy link in the notice
- **THEN** the policy page `/politica` opens in a new browser tab or window
- **AND** the chat tab keeps its current conversation

#### Scenario: Policy link is accessible
- **WHEN** a screen-reader user reaches the policy link
- **THEN** its accessible name identifies the policy and states, in pt-BR, that it opens in a new window

#### Scenario: Policy link available in maintenance mode
- **WHEN** the chat is in maintenance mode
- **THEN** the notice and its policy link are still displayed

### Requirement: New conversation
The chat screen SHALL provide a "New conversation" button that clears the entire conversation history for the current session and returns the screen to its empty state (showing suggested questions). If a reply is in progress, it MUST be stopped before clearing. The input content MUST be cleared as well. The button MUST be in the composer row, at its start, before the character counter, on the same line as the Send button when the width allows. It is not shown in maintenance mode, where the composer is hidden.

#### Scenario: Clear history
- **WHEN** a user with several messages clicks "New conversation"
- **THEN** the message list becomes empty
- **AND** the suggested-question chips are shown again

#### Scenario: Clear during generation
- **WHEN** a user clicks "New conversation" while a reply is in progress
- **THEN** generation stops, the history is cleared, and no text from the stopped reply reappears afterwards

#### Scenario: Button placed in the composer row
- **WHEN** a visitor views the chat screen on a desktop-width viewport
- **THEN** the "Nova conversa" button is on the same line as the "Enviar" button, to the left of the character counter
- **AND** it is no longer shown in the header

## ADDED Requirements

### Requirement: Project repository link in the demo notice
The demo notice SHALL end with the sentence "Repositório do projeto: https://github.com/algorist-hub/algorist-support", where the URL is a link to that repository. The link MUST open in a new browser tab or window, keeping the chat and its conversation intact, and its accessible name MUST state, in pt-BR, that it opens in a new window.

#### Scenario: Repository sentence shown
- **WHEN** a visitor opens the chat screen
- **THEN** the demo notice ends with "Repositório do projeto: https://github.com/algorist-hub/algorist-support"

#### Scenario: Repository link opens in a new window
- **WHEN** a visitor activates the repository link
- **THEN** `https://github.com/algorist-hub/algorist-support` opens in a new browser tab or window
- **AND** the chat tab keeps its current conversation

#### Scenario: Repository link is accessible
- **WHEN** a screen-reader user reaches the repository link
- **THEN** its accessible name includes the repository address and states, in pt-BR, that it opens in a new window

#### Scenario: Repository link available in maintenance mode
- **WHEN** the chat is in maintenance mode
- **THEN** the notice still ends with the repository sentence and link
