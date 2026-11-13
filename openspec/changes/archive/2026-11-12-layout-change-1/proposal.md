# Proposal

## Why

After the brand restyle the chat screen shows its identity twice: the Algorist logo in the top bar and an "Assistente Algorist" title right below it. The "Nova conversa" button also sits far from the controls it relates to. Moving the assistant name into the top bar and the button into the composer row gives the conversation more vertical space and a cleaner layout. Adding the project repository link to the demo notice lets portfolio visitors go straight to the source code.

## What Changes

- Remove the visible "Assistente Algorist" title from the chat header.
- Show the assistant name as text beside the Algorist logo in the top bar, on every page (chat, `/politica`, not-found, error). The name still comes from the `AssistantName` setting, whose default and `appsettings.json` value change from "Assistente Algorist" to **"Assistente de Suporte"**. The request's spelling "Assitente" is taken as a typo for "Assistente".
- Keep a heading for the chat screen for screen readers and focus-on-navigate, but hide it visually.
- Move the "Nova conversa" button to the composer row, at the far left, before the character counter. The row then reads: [Nova conversa] [counter / validation message] … [Parar] [Enviar]. Behavior is unchanged: it stops any reply in progress and clears the history and the input.
- End the demo notice with the sentence "Repositório do projeto: https://github.com/algorist-hub/algorist-support". The URL is a link that opens in a new tab, like the notice's policy link.
- Screen-reader author labels follow the setting ("Assistente de Suporte disse:").

Out of scope: the system prompt's self-introduction ("Você é o Assistente Algorist…") is not changed. Neither is the logo link, the colors, or any chat behavior.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `support-chat`: "Chat screen header and demo notice": the assistant name is no longer a visible header title, and the notice gains the repository sentence and link. "New conversation": the button's position in the composer row is specified, and it is not shown in maintenance mode, where the composer is hidden.
- `brand-appearance`: "Brand top bar": the bar shows the configured assistant name beside the logo on every page.

## Impact

- **Code**: `Components/Layout/BrandBar.razor(.css)` (assistant name from `IOptions<SupportChatOptions>`), `Components/Chat/ChatHeader.razor(.css)` (visually hidden heading, no button, repository link), `Components/Chat/ChatComposer.razor(.css)` (new `OnNewConversation` callback and button), `Components/Chat/ChatPanel.razor` (wiring), `Chat/SupportChatOptions.cs` and `appsettings.json` (default name).
- **Tests**: update `ChatComponentTests` (header, composer, new-conversation flow) and `BrandBarTests`/`MainLayout` test, which now need the options registered.
- **Docs**: `README.md` UI configuration table (`AssistantName` default and description).
- **Dependencies/APIs**: none. Deployment is unchanged. Deployments that override `Chat__AssistantName` keep their value.
