using AlgoristSupport.Chat;

namespace AlgoristSupport.Tests.Chat;

/// <summary>Builders for provider settings / availability used across tests (no secrets, no network).</summary>
internal static class TestChat
{
    public static ChatProviderSettings Settings (
        int maxPerMinute = 100,
        int maxHistory = 10,
        int maxTokens = 800,
        float temperature = 0.3f) =>
        new("test-key-not-secret", "modelo-teste", new Uri("https://provider.example/api"), temperature, maxPerMinute, maxTokens, maxHistory);

    public static ChatAvailability Available (int maxPerMinute = 100, int maxHistory = 10) =>
        new(new ChatProviderLoadResult(Settings(maxPerMinute, maxHistory), []));

    public static ChatAvailability Unavailable () =>
        new(new ChatProviderLoadResult(null, [ChatProviderSettings.ApiKeyKey]));
}
