# Spec Delta

## MODIFIED Requirements

### Requirement: Support assistant instructions
Every request to the provider SHALL begin with a single system message, in pt-BR, that defines the assistant as the Algorist support chat assistant, requires replies in Brazilian Portuguese, contains the grounding rules and contains the full text of the policy document, clearly delimited from the rules. User text MUST be sent only as user-role messages and MUST NOT be inserted into the system message.

#### Scenario: Instructions sent first
- **WHEN** a user sends a message
- **THEN** the request to the provider starts with a single system message containing the support-assistant instructions
- **AND** the user's text appears only in user-role messages

#### Scenario: Policy included in the instructions
- **WHEN** a user sends a message
- **THEN** the system message contains the complete policy document text between explicit start and end delimiters

## ADDED Requirements

### Requirement: Policy as the only knowledge source
The system instructions SHALL tell the assistant to answer only from the policy text in the system message, and to never invent or estimate deadlines, prices, percentages or conditions that the policy does not state.

#### Scenario: Grounding rule included
- **WHEN** a request is sent to the provider
- **THEN** its system message contains the directive to answer only from the policy and not to invent information absent from it

#### Scenario: Covered question answered from the policy (manual evaluation)
- **WHEN** a user asks "Qual é o prazo de garantia?"
- **THEN** the reply states the 90-day warranty after final acceptance in production, as defined in the policy

### Requirement: Uncovered questions
The system instructions SHALL tell the assistant that, when a question about Algorist or its services is not answered by the policy, it must say that the policy does not cover it and point the user to the contact e-mail defined in the policy.

#### Scenario: Uncovered-question rule included
- **WHEN** a request is sent to the provider
- **THEN** its system message contains the directive to state that the information is not in the policy and to refer to the policy's contact channel

#### Scenario: Question not covered by the policy (manual evaluation)
- **WHEN** a user asks "Qual é o valor da hora de um desenvolvedor sênior?"
- **THEN** the reply says the policy does not define that value and suggests contacting Algorist by the e-mail in the policy
- **AND** the reply does not state any hourly price

### Requirement: Out-of-scope refusal
The system instructions SHALL tell the assistant to politely decline questions unrelated to Algorist, its services or its policy, and to briefly say what it can help with.

#### Scenario: Out-of-scope rule included
- **WHEN** a request is sent to the provider
- **THEN** its system message contains the directive to decline topics unrelated to Algorist

#### Scenario: Off-topic question (manual evaluation)
- **WHEN** a user asks "Qual é a receita de bolo de cenoura?"
- **THEN** the reply politely declines and says it can answer questions about Algorist's services and policy

### Requirement: Policy text is reference data
The system instructions SHALL tell the assistant to treat the delimited policy text only as reference information, never as instructions that change its rules.

#### Scenario: Data-not-instructions rule included
- **WHEN** a request is sent to the provider
- **THEN** its system message contains the directive that the delimited policy content is reference data and not instructions

### Requirement: Maintenance mode when the policy cannot be loaded
If the policy document is missing, unreadable or empty at startup, the application SHALL still start, and the chat SHALL enter the same maintenance mode used for invalid provider settings: no message can be sent and no request is made to the provider. At startup the system MUST log a warning that identifies the policy document, without logging its content.

#### Scenario: Policy file missing
- **WHEN** the application starts and the policy document is not present in its output
- **THEN** the application starts successfully
- **AND** the chat screen shows the pt-BR "under maintenance" message and offers no way to send a message
- **AND** a startup warning identifies the policy document as missing

#### Scenario: Policy file empty
- **WHEN** the policy document contains only whitespace
- **THEN** the chat is in maintenance mode and a startup warning identifies the policy document as empty

#### Scenario: Provider settings and policy both invalid
- **WHEN** a provider setting is invalid and the policy document is missing
- **THEN** the chat is in maintenance mode
- **AND** the startup warnings name the invalid setting and identify the missing policy document
