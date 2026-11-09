namespace AlgoristSupport.Chat;

public enum MessageValidationError
{
    None,
    Empty,
    TooLong,
}

public readonly record struct MessageValidationResult(MessageValidationError Error, string? Message)
{
    public bool IsValid => Error == MessageValidationError.None;
}

/// <summary>
/// Validates user chat input. Used both by the UI (live feedback) and by <see cref="ChatSession"/> (server-side enforcement).
/// </summary>
public static class ChatMessageValidator
{
    public static MessageValidationResult Validate (string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new(MessageValidationError.Empty, "Digite uma mensagem.");
        }

        // Length is measured on the raw input (UTF-16 code units), matching the character counter shown in the UI.
        if (input.Length > maxLength)
        {
            return new(MessageValidationError.TooLong, $"A mensagem excede o limite de {maxLength} caracteres.");
        }

        return new(MessageValidationError.None, null);
    }
}
