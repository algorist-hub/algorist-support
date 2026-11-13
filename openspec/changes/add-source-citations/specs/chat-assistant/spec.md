# Spec Delta

## ADDED Requirements

### Requirement: Inline policy section citations
The system instructions SHALL tell the assistant to cite, inline, the policy section(s) an answer is based on, in the format `(Seção N)`, where N is the section number as numbered in the policy. The instructions MUST tell the assistant not to cite sections when the policy does not cover the question or when declining an off-topic question.

#### Scenario: Citation rule included
- **WHEN** a request is sent to the provider
- **THEN** its system message contains the directive to cite the policy sections used, in the format `(Seção N)`
- **AND** the directive says not to cite sections for uncovered or off-topic questions

#### Scenario: Covered answer cites its section (manual evaluation)
- **WHEN** a user asks "Qual é o prazo de garantia?"
- **THEN** the reply states the 90-day warranty and cites `Seção 12`

#### Scenario: Cross-section answer cites each section (manual evaluation)
- **WHEN** a user asks "Durante a garantia, em quanto tempo vocês respondem a um problema crítico?"
- **THEN** the reply cites both `Seção 12` and `Seção 13`

#### Scenario: Uncovered answer has no citation (manual evaluation)
- **WHEN** a user asks "Qual é o valor da hora de um desenvolvedor sênior?"
- **THEN** the reply says the policy does not define that value and contains no `Seção` citation
