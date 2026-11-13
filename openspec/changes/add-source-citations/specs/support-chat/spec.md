# Spec Delta

## MODIFIED Requirements

### Requirement: Message list rendering
The chat screen SHALL display the conversation as an ordered list of messages, oldest first, where each message is visually and semantically identified as either a user message or an assistant message. User message text MUST be rendered as plain text (no HTML or Markdown interpretation) and MUST preserve line breaks entered by the user. Assistant message text MUST be rendered as Markdown formatting (the Markdown markers themselves are not shown), with raw HTML in the reply escaped rather than interpreted and links kept only for `http`, `https` and `mailto` URLs. The only exception is links to the policy page that the application itself generates from policy section citations.

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

#### Scenario: Model-written relative links are still removed
- **WHEN** an assistant reply contains the Markdown link `[veja](politica#secao-12)`
- **THEN** the text `veja` is shown without a link

#### Scenario: Multi-line message preserves line breaks
- **WHEN** a user sends a message containing two lines
- **THEN** the message is displayed on two lines

## ADDED Requirements

### Requirement: Linked policy section citations
In a finished assistant reply, each citation of an existing policy section (`Seção N`, `Seção N.M` or `Seções N e M`, case-insensitive) SHALL become a link to that section on the policy page, opening in a new tab or window. `N.M` links to section N. The cited text MUST stay as written. Each link's accessible name MUST include the section title and say it opens in a new window.

#### Scenario: Cited section becomes a link
- **WHEN** a finished assistant reply contains `(Seção 12)` and the policy has section 12 "Garantia"
- **THEN** the text `Seção 12` is a link to the section 12 anchor of `/politica` that opens in a new tab or window
- **AND** the link's accessible name includes "Garantia" and indicates that it opens in a new window

#### Scenario: Clause reference links to its section
- **WHEN** a finished assistant reply contains `Seção 8.6`
- **THEN** the text `Seção 8.6` is shown unchanged and links to the section 8 anchor

#### Scenario: Several sections in one citation
- **WHEN** a finished assistant reply contains `Seções 12 e 13`
- **THEN** `12` links to the section 12 anchor and `13` links to the section 13 anchor

### Requirement: Unlinked citations
A citation SHALL NOT become a link when the section number does not exist in the loaded policy, when it appears in a user message, inside code formatting or inside an existing link, or while the reply is still streaming. In those cases the text MUST be shown unchanged.

#### Scenario: Unknown section stays plain text
- **WHEN** a finished assistant reply contains `Seção 99` and the policy has no section 99
- **THEN** `Seção 99` is shown as plain text without a link

#### Scenario: User message is not linked
- **WHEN** a user sends the message `O que diz a Seção 12?`
- **THEN** the message is shown as plain text without a link

#### Scenario: Code formatting is not linked
- **WHEN** a finished assistant reply contains `` `Seção 12` ``
- **THEN** it is shown as code without a link

#### Scenario: Links appear when streaming ends
- **WHEN** an assistant reply containing `(Seção 12)` is still streaming
- **THEN** `Seção 12` is shown as plain text
- **AND** once the reply is complete or stopped, `Seção 12` becomes a link
