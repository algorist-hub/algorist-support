// Enter sends the message; Shift+Enter (and IME composition) keep the native newline/composition behavior.
// Blazor cannot conditionally preventDefault per key on the server, hence this small handler.
export function attachComposer(textarea, form) {
    if (!textarea || !form) {
        return;
    }

    const onKeyDown = (event) => {
        if (event.key === "Enter" && !event.shiftKey && !event.isComposing && event.keyCode !== 229) {
            event.preventDefault();
            form.requestSubmit();
        }
    };

    textarea.addEventListener("keydown", onKeyDown);
    textarea._chatOnKeyDown = onKeyDown;
}

export function detachComposer(textarea) {
    if (textarea && textarea._chatOnKeyDown) {
        textarea.removeEventListener("keydown", textarea._chatOnKeyDown);
        delete textarea._chatOnKeyDown;
    }
}
