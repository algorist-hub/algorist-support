# Spec Delta

## Purpose

Lets visitors read the fictional service policy that the support assistant answers from, so they can check the assistant's replies against the source.

## ADDED Requirements

### Requirement: Policy page
The system SHALL serve a public, read-only page at `/politica` that shows the full text of the same policy document the assistant uses, rendered as formatted text: headings, lists, emphasis and tables appear as formatting, and Markdown markers are not shown. The page MUST NOT require authentication.

#### Scenario: Visitor opens the policy page
- **WHEN** a visitor navigates to `/politica`
- **THEN** the page shows the policy title "Política de Prestação de Serviços de Desenvolvimento de Software — Algorist"
- **AND** section headings are rendered as headings and the SLA table is rendered as a table
- **AND** no Markdown markers such as `**` or `##` are visible

#### Scenario: Page matches the assistant's source
- **WHEN** the policy document is changed and the application is redeployed
- **THEN** the policy page shows the changed text

### Requirement: Policy page is clearly fictional
The policy page SHALL be in pt-BR and SHALL keep visible the policy's own notice that the document is a fictional example created for demonstration.

#### Scenario: Fictional notice visible
- **WHEN** a visitor opens `/politica`
- **THEN** the notice stating that the document is a fictional example is visible near the top of the page

### Requirement: Safe policy rendering
Raw HTML in the policy document SHALL be shown as literal text and never interpreted. Links SHALL be kept only for `http`, `https` and `mailto` URLs.

#### Scenario: HTML in the policy is not executed
- **WHEN** the policy document contains `<script>` markup
- **THEN** the page shows the markup as text and no script runs

### Requirement: Policy unavailable
If the policy document could not be loaded at startup, the policy page SHALL show a pt-BR message stating that the policy is temporarily unavailable, instead of an error page.

#### Scenario: Missing policy file
- **WHEN** the application started without the policy document
- **THEN** `/politica` shows a pt-BR "policy unavailable" message
- **AND** no error details are shown
