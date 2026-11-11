# support-chat Specification

## Purpose

Provides the end-user support chat experience in pt-BR: a chat screen where a visitor composes questions, watches a streamed assistant reply, can stop or reset the conversation, and keeps history only for the duration of their session.

## Requirements

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

### Requirement: Message list rendering
The chat screen SHALL display the conversation as an ordered list of messages, oldest first, where each message is visually and semantically identified as either a user message or an assistant message. User message text MUST be rendered as plain text (no HTML or Markdown interpretation) and MUST preserve line breaks entered by the user. Assistant message text MUST be rendered as Markdown formatting (the Markdown markers themselves are not shown), with raw HTML in the reply escaped rather than interpreted and links kept only for `http`, `https` and `mailto` URLs.

#### Scenario: User and assistant messages are distinguishable
- **WHEN** the conversation contains a user message followed by an assistant reply
- **THEN** both messages appear in that order
- **AND** each message is labeled or styled so its author (user or assistant) is identifiable, including by assistive technology

#### Scenario: Markup in a message is shown literally
- **WHEN** a user sends the message `<b>olá</b>`
- **THEN** the message list shows the literal text `<b>olá</b>` and no bold formatting is applied

#### Scenario: Assistant Markdown is rendered as formatting
- **WHEN** an assistant reply contains `**Assistente Algorist**`
- **THEN** the message list shows `Assistente Algorist` in bold and no `*` characters are displayed

#### Scenario: Unsafe content in an assistant reply is neutralized
- **WHEN** an assistant reply contains `<script>` markup or a `javascript:` link
- **THEN** the markup is shown as literal text and the link has no `javascript:` target

#### Scenario: Multi-line message preserves line breaks
- **WHEN** a user sends a message containing two lines
- **THEN** the message is displayed on two lines

### Requirement: Auto-scroll to latest message
The message list SHALL keep the latest message in view: it MUST scroll to the bottom when a new message is added and while an assistant reply grows during streaming.

#### Scenario: Scroll on new user message
- **WHEN** a user sends a message in a conversation that already overflows the visible area
- **THEN** the message list scrolls so the new message is visible

#### Scenario: Scroll follows streaming reply
- **WHEN** an assistant reply grows beyond the visible area while streaming
- **THEN** the message list keeps scrolling so the latest streamed text stays visible

### Requirement: Message composition and sending
The chat screen SHALL provide a multi-line text input and a Send button. Pressing Enter in the input MUST send the message; pressing Shift+Enter MUST insert a newline without sending. Clicking Send MUST send the message. On a successful send, the message MUST be appended to the conversation as a user message (with leading and trailing whitespace trimmed) and the input MUST be cleared.

#### Scenario: Send with button
- **WHEN** a user types "Qual o prazo de garantia?" and clicks Send
- **THEN** the message appears in the list as a user message
- **AND** the input is cleared

#### Scenario: Send with Enter
- **WHEN** a user types a valid message and presses Enter
- **THEN** the message is sent and no newline is added to the input

#### Scenario: Shift+Enter inserts newline
- **WHEN** a user presses Shift+Enter in the input
- **THEN** a newline is inserted in the input
- **AND** no message is sent

#### Scenario: Surrounding whitespace is trimmed
- **WHEN** a user sends "   Olá   "
- **THEN** the user message is stored and displayed as "Olá"

### Requirement: Empty message rejection
The system MUST NOT send a message that is empty or consists only of whitespace (spaces, tabs, newlines). The Send button MUST be disabled while the input has no non-whitespace content, and pressing Enter in that state MUST do nothing.

#### Scenario: Empty input
- **WHEN** the input is empty
- **THEN** the Send button is disabled
- **AND** pressing Enter sends nothing and adds nothing to the conversation

#### Scenario: Whitespace-only input
- **WHEN** the input contains only spaces and newlines and the user presses Enter or attempts to click Send
- **THEN** no message is added to the conversation and no reply is generated

### Requirement: Maximum message length
The system SHALL enforce a configurable maximum message length, defaulting to 500 characters. The chat screen MUST display a live character counter in the form `<current>/<max>` and MUST show a pt-BR feedback message and disable sending while the input exceeds the maximum. The limit MUST also be enforced server-side, independent of the UI state.

#### Scenario: Counter updates while typing
- **WHEN** a user types 42 characters with the default configuration
- **THEN** the counter shows `42/500`

#### Scenario: Over-limit input blocks sending
- **WHEN** the input contains 501 characters with the default configuration
- **THEN** the counter is shown in an error state
- **AND** a pt-BR message explains the 500-character limit
- **AND** the Send button is disabled and pressing Enter sends nothing

#### Scenario: Exactly at the limit is allowed
- **WHEN** the input contains exactly 500 characters with the default configuration
- **THEN** the message can be sent

#### Scenario: Configured limit is respected
- **WHEN** the maximum length is configured to 200
- **THEN** the counter shows `<current>/200` and messages over 200 characters cannot be sent

#### Scenario: Server-side enforcement
- **WHEN** a send request reaches the server with a message longer than the configured maximum
- **THEN** the message is rejected and not added to the conversation

### Requirement: Streamed AI-generated reply
After a user message is sent, the system SHALL produce an assistant reply that appears in the message list incrementally (streamed) until complete. The reply MUST be generated by the configured AI provider (see the chat-assistant capability) and streamed to the screen as the provider sends it; the simulated reply is no longer produced.

#### Scenario: Reply streams incrementally
- **WHEN** a user sends a valid message
- **THEN** an assistant message appears and its text grows in several visible increments until the reply is complete

#### Scenario: Reply comes from the AI provider
- **WHEN** an assistant reply is generated
- **THEN** its text is the content streamed by the configured AI provider
- **AND** it contains no "(Resposta simulada)" marker

### Requirement: Generating indicator
While an assistant reply is in progress, the chat screen SHALL display a "generating" indicator in pt-BR, exposed to assistive technology, and MUST remove it when the reply completes, is stopped, or fails.

#### Scenario: Indicator visible during generation
- **WHEN** a reply is in progress
- **THEN** a pt-BR "generating" indicator is visible and announced to screen readers

#### Scenario: Indicator removed on completion
- **WHEN** the reply completes
- **THEN** the indicator is no longer shown

### Requirement: Sending blocked during reply
While an assistant reply is in progress, the system MUST NOT accept a new message: the Send button and suggested-question chips MUST be disabled and pressing Enter MUST NOT send. The user MAY continue typing in the input. Sending MUST be re-enabled once the reply completes, is stopped, or fails.

#### Scenario: Send disabled while generating
- **WHEN** a reply is in progress and the input contains a valid message
- **THEN** the Send button is disabled and pressing Enter does not send

#### Scenario: Typing allowed while generating
- **WHEN** a reply is in progress
- **THEN** the user can type in the input and the counter updates

#### Scenario: Sending re-enabled after completion
- **WHEN** the reply completes and the input contains a valid message
- **THEN** the Send button is enabled

#### Scenario: Concurrent send rejected server-side
- **WHEN** a second send reaches the server while a reply is still in progress
- **THEN** the second message is rejected and not added to the conversation

### Requirement: Stop reply in progress
While an assistant reply is in progress, the chat screen SHALL show a Stop button. Activating it MUST end generation promptly, keep any text already streamed in the assistant message, mark that message as interrupted in pt-BR, remove the generating indicator, and re-enable sending. The Stop button MUST NOT be shown when no reply is in progress.

#### Scenario: Stop keeps partial text
- **WHEN** a user clicks Stop after part of the reply has streamed
- **THEN** no further text is appended to the assistant message
- **AND** the already-streamed text remains visible with a pt-BR "interrupted" marker
- **AND** sending is re-enabled

#### Scenario: Stop before any text
- **WHEN** a user clicks Stop before any reply text has streamed
- **THEN** the assistant message shows only the pt-BR "interrupted" marker

#### Scenario: Stop hidden when idle
- **WHEN** no reply is in progress
- **THEN** the Stop button is not shown

### Requirement: Reply failure handling
If generating an assistant reply fails (provider error, network error, the 30-second timeout, or a reply that ends without any text), the system SHALL show a generic pt-BR error message in place of (or appended to) the assistant message, remove the generating indicator, keep the conversation history intact, and re-enable sending. Internal error details MUST NOT be shown to the user. The failed reply, when it is the most recent message and no reply is in progress, SHALL offer a "Tentar novamente" (try again) button; activating it MUST remove the failed reply and generate a new reply for the same user message without adding a duplicate user message. The try-again action is subject to the same rules as sending (blocked while a reply is in progress, counted in the per-minute limit, unavailable in maintenance mode); if it is rejected by the per-minute limit, the failed reply and its button remain.

#### Scenario: Generation error
- **WHEN** reply generation throws an error
- **THEN** a generic pt-BR error message is shown for that reply
- **AND** earlier messages remain in the list
- **AND** the user can send a new message

#### Scenario: Try again regenerates the reply
- **WHEN** the last reply failed and the user clicks "Tentar novamente"
- **THEN** the failed reply is removed and a new reply streams for the same user message
- **AND** the user message appears only once in the conversation

#### Scenario: Try again only on the latest failed reply
- **WHEN** a failed reply is followed by newer messages
- **THEN** that failed reply shows no try-again button

#### Scenario: Try again blocked by the per-minute limit
- **WHEN** the user clicks "Tentar novamente" after reaching the per-minute limit
- **THEN** no provider request is made, the failed reply stays with its button, and the pt-BR limit message is shown

#### Scenario: Timeout shown as failure
- **WHEN** a reply exceeds the 30-second timeout
- **THEN** it is shown with the generic pt-BR error message and the try-again button

### Requirement: New conversation
The chat screen SHALL provide a "New conversation" button that clears the entire conversation history for the current session and returns the screen to its empty state (showing suggested questions). If a reply is in progress, it MUST be stopped before clearing. The input content MUST be cleared as well.

#### Scenario: Clear history
- **WHEN** a user with several messages clicks "New conversation"
- **THEN** the message list becomes empty
- **AND** the suggested-question chips are shown again

#### Scenario: Clear during generation
- **WHEN** a user clicks "New conversation" while a reply is in progress
- **THEN** generation stops, the history is cleared, and no text from the stopped reply reappears afterwards

### Requirement: Suggested questions
When the conversation is empty, the chat screen SHALL display clickable chips with configured suggested questions in pt-BR about the policy. Clicking a chip MUST send that question as a user message, subject to the same rules as typed messages. The chips MUST be hidden once the conversation has at least one message.

#### Scenario: Chips shown on empty conversation
- **WHEN** a visitor opens the chat screen with no messages
- **THEN** the configured suggested questions are displayed as clickable chips

#### Scenario: Clicking a chip sends the question
- **WHEN** a user clicks the chip "Como funciona a garantia?"
- **THEN** "Como funciona a garantia?" is added as a user message and a reply starts streaming
- **AND** the chips are hidden

#### Scenario: No chips configured
- **WHEN** no suggested questions are configured
- **THEN** no chips are displayed and the chat remains usable

### Requirement: Session-scoped in-memory history
Conversation history SHALL be kept only in server memory and scoped to the visitor's interactive session (one per browser tab connection). History MUST NOT be shared between sessions, MUST NOT be persisted to any storage, and is discarded when the session ends (for example, page reload). No sign-in is required to use the chat.

#### Scenario: Separate tabs are independent
- **WHEN** a visitor opens the chat in two browser tabs and sends a message in the first
- **THEN** the second tab's conversation is unaffected

#### Scenario: Reload starts fresh
- **WHEN** a visitor reloads the page after chatting
- **THEN** the chat screen shows an empty conversation with suggested questions

#### Scenario: No authentication required
- **WHEN** an anonymous visitor opens the chat screen
- **THEN** they can send messages without signing in

### Requirement: Portuguese (pt-BR) interface
All user-facing chat text (labels, buttons, placeholders, notices, indicators, validation, rate-limit, maintenance and error messages, and suggested questions) SHALL be in Brazilian Portuguese, assistant replies are requested in Brazilian Portuguese, and the page MUST declare its language as `pt-BR`.

#### Scenario: Page language declared
- **WHEN** the chat page is rendered
- **THEN** the document language is `pt-BR`

#### Scenario: Controls labeled in pt-BR
- **WHEN** a visitor views the chat screen
- **THEN** the Send, Stop, New conversation and Try again controls and the input placeholder are labeled in pt-BR
