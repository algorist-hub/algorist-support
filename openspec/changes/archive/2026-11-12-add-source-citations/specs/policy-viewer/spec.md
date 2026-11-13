# Spec Delta

## ADDED Requirements

### Requirement: Stable section anchors
Each numbered top-level section heading of the policy page (a heading whose text starts with a number followed by a period, such as `12. Garantia`) SHALL have the anchor `secao-N`, where N is that number. The anchor MUST NOT depend on the section title. Opening `/politica#secao-N` MUST scroll to that section and visually highlight its heading.

#### Scenario: Section heading has a numeric anchor
- **WHEN** a visitor opens `/politica`
- **THEN** the heading "12. Garantia" has the anchor `secao-12`

#### Scenario: Title change keeps the anchor
- **WHEN** the section 12 title in the policy document is changed and the application is redeployed
- **THEN** that section's anchor is still `secao-12`

#### Scenario: Deep link scrolls to and highlights the section
- **WHEN** a visitor opens `/politica#secao-13`
- **THEN** the page scrolls so that the heading "13. Suporte e níveis de serviço (SLA)" is visible and not hidden under the page header
- **AND** that heading is visually highlighted
