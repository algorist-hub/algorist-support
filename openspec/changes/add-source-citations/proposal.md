# Proposal

## Why

The assistant now answers only from the fictional policy, but a visitor cannot easily check where an answer came from. Inline section citations that link to the matching section of `/politica` make answers verifiable at a glance. They also push the model to stay close to the text it cites. This is the roadmap step after `add-policy-grounding`.

## What Changes

- New grounding rule in the system prompt: when an answer uses the policy, cite the section(s) used inline as `(Seção N)`, following the style the policy's own FAQ already uses. Do not cite when the policy does not cover the question or when declining off-topic requests.
- Assistant replies turn citations such as `Seção 12`, `Seção 12.1` and `Seções 12 e 13` into links to the matching section anchor on `/politica`. Links open in a new tab, so the conversation is kept.
- Links point to **sections only**. A clause reference like `Seção 12.1` links to section 12. Clause-level anchors are out of scope.
- A citation is linked only when the section number exists in the loaded policy. Invented numbers stay plain text.
- Citations are linked once a reply stops streaming, so no link briefly points to the wrong section while digits are still arriving.
- On `/politica`, each numbered section heading (`## N. Title`) gets a stable anchor `secao-N` that does not depend on the title, replacing its title-based auto ID. Unnumbered headings keep their current IDs.

Out of scope: clause-level anchors (`12.1`), a separate "Fontes" list under replies, linking cross-references inside the policy page itself, and automated checks that a citation is correct (the manual evaluation script is extended instead).

## Capabilities

### New Capabilities
- (none)

### Modified Capabilities
- `chat-assistant`: new requirement that the system instructions tell the assistant to cite policy sections inline in a fixed format.
- `support-chat`: "Message list rendering" gains linked section citations in assistant replies, along with the rules for invalid numbers, streaming and user messages.
- `policy-viewer`: new requirement for stable, title-independent section anchors on `/politica`.

## Impact

- **Code**:
  - `AlgoristSupport/Chat/`: a new pure policy section index (section number, title and anchor, built from the policy Markdown) and a pure citation finder. `SupportAssistantPrompt` adds the citation rule.
  - `Components/Shared/SafeMarkdown.cs`: an optional citation-linking step, and section anchors for numbered headings.
  - `ChatMessageList.razor`: enables linking for assistant replies that are not streaming.
  - `Policy.razor`/CSS: section anchors, plus `:target` highlight and scroll margin.
  - `Program.cs`: registers the section index as a singleton.
- **Dependencies**: none new (Markdig is already used).
- **Cost/latency**: one more short prompt rule. Linking is a local text pass per render.
- **Tests**: unit tests for the index, the finder and the prompt; bUnit tests for reply links and policy anchors; the manual evaluation script checks that cited sections are correct.
- **Docs**: README updated.
