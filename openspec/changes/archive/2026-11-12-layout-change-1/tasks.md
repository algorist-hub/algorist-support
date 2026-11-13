# Tasks

## 1. Assistant name in the top bar

- [x] 1.1 Change the `AssistantName` default in `Chat/SupportChatOptions.cs` and `Chat:AssistantName` in `appsettings.json` to "Assistente de Suporte" (design D2), and update the `AssistantName` row of the README "UI configuration" table (new default; "Name shown in the top bar next to the logo and in screen-reader labels"). Verify `dotnet build` passes and the README row matches the code.
- [x] 1.2 In `BrandBar.razor`, inject `IOptions<SupportChatOptions>` and render `<span class="brand-name">` with the name after the logo link, outside it. Style it in `BrandBar.razor.css` (Poppins 500, white, light divider, 0.95rem under 576px, nowrap with ellipsis) (design D1). Update `BrandBarTests`: register the options, and assert the name text is shown, is outside the `a.brand-link`, and that the link's accessible name is still "Algorist (abre em nova janela)". Verify the `MainLayout` test passes with options registered.

## 2. Header and demo notice

- [x] 2.1 In `ChatHeader.razor`, replace the visible title with `<h1 class="visually-hidden">@AssistantName</h1>`, remove the "Nova conversa" button and the `OnNewConversation` parameter, and remove the now-unused `.chat-title`/`.chat-new` rules from `ChatHeader.razor.css` (design D3). Update `Header_shows_name_and_permanent_notice_without_close_control` to assert a visually hidden `h1` with the name and no buttons in the header. Verify the tests pass.
- [x] 2.2 Append the sentence "Repositório do projeto: https://github.com/algorist-hub/algorist-support" to the notice, with the URL as a `chat-repo-link` anchor (`target="_blank"`, `rel="noopener noreferrer"`, visually hidden " (abre em nova janela)"), styled like the policy link, plus `overflow-wrap: anywhere` on the notice (design D4). Change the policy-link test selector to `.chat-policy-link`. Add a test asserting the repository link's `href`, `target`, `rel`, text, and that the notice text ends with the sentence. Verify the tests pass, including the maintenance-mode test, which should also see the repository link.

## 3. "Nova conversa" in the composer row

- [x] 3.1 Add `[Parameter] EventCallback OnNewConversation` to `ChatComposer` and render `<button type="button" class="btn btn-outline-secondary chat-new">Nova conversa</button>` first in the footer, before the counter, enabled while generating. Adjust `ChatComposer.razor.css` so the left group holds the button, counter and message, and actions stay right with wrapping (design D5). Add bUnit tests: the button is in `.chat-composer-footer` before `#chat-counter`, clicking it invokes the callback and does not submit the form, and it is enabled while `IsGenerating`. Verify the tests pass.
- [x] 3.2 In `ChatPanel.razor`, stop passing `OnNewConversation` to `ChatHeader` and pass it to `ChatComposer`. Verify `New_conversation_clears_messages_and_restores_chips` passes unchanged. Add or extend a test for clearing during generation through the composer button (stops generation, clears history), and verify it passes.

## 4. Integration checks

- [x] 4.1 Run `dotnet build` and `dotnet test`. Verify zero failures and that the `BrandStyleTests` guard still passes.
- [x] 4.2 Run the app and check manually, recording the results in this task:
  - chat screen: no title below the bar; "Assistente de Suporte" beside the logo; the same on `/politica`;
  - composer row on desktop: [Nova conversa] [0/500] … [Enviar] on one line; "Nova conversa" clears the conversation, including during a reply;
  - notice ends with the repository sentence; the link opens GitHub in a new tab and the chat keeps its conversation;
  - 360px viewport: logo and name fully visible, the notice URL wraps, no horizontal scroll, and the composer row wraps cleanly with Parar shown;
  - maintenance mode: bar with name, notice with both links, no composer and no "Nova conversa".

  Result (2026-11-12, headless Chrome at 1280×800 and 360×740 driven over the DevTools protocol, with screenshots reviewed):
  - chat: no visible title; `h1` "Assistente de Suporte" is visually hidden; the bar shows "Assistente de Suporte" beside the logo. `/politica` shows the same bar and keeps a single `h1`;
  - desktop composer row: Nova conversa (x 196), counter 0/500 (x 355), Parar (x 939) and Enviar (x 1017) all at y 746, on one line. Clicking Nova conversa during a reply cleared the messages (0), restored the 4 chips and removed Parar;
  - notice ends with "Repositório do projeto: https://github.com/algorist-hub/algorist-support". The link has `target="_blank"` and `rel="noopener noreferrer"`. A physical click to GitHub was not performed headless;
  - 360px: scrollWidth 360/360; logo (16–125px) and name (137–316px) fully visible, not truncated; the URL wraps inside the notice; during a reply Nova conversa stays on the first row and Parar/Enviar wrap to a right-aligned second row;
  - maintenance mode: the bar shows the name, the notice has both links, and there is no composer and no Nova conversa.
