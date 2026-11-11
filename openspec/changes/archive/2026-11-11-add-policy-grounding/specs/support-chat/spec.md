# Spec Delta

## MODIFIED Requirements

### Requirement: Chat screen header and demo notice
The chat screen SHALL display a header containing the configured assistant name and a permanent notice, in pt-BR, stating that the assistant is an automated demo and that it answers about a fictional policy. The notice MUST remain visible at all times, regardless of conversation length or state, and MUST NOT be dismissible. The notice's reference to the fictional policy MUST be a link to the policy page (`/politica`) that opens in a new browser tab or window, keeping the chat and its conversation intact, and whose accessible name indicates that it opens in a new window.

#### Scenario: Header shown on first load
- **WHEN** a visitor opens the chat screen
- **THEN** the header shows the configured assistant name
- **AND** the header shows a notice that this is an automated demo assistant and the policy is fictional

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
