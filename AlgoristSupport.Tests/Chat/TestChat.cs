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

    public const string PolicyMarkdown = "# Política de teste\n\nA garantia é de **90 dias corridos**.";

    public static PolicyLoadResult Policy () =>
        PolicyLoadResult.Loaded("Policy/policy.md", new PolicyDocument(PolicyMarkdown));

    public static PolicyLoadResult MissingPolicy () =>
        PolicyLoadResult.Failed("Policy/policy.md", PolicyLoadProblem.Missing);

    /// <summary>Section index with sections 8 ("Preços"), 12 ("Garantia") and 13 ("Suporte (SLA)").</summary>
    public static PolicySectionIndex Sections () =>
        PolicySectionIndex.Build(new PolicyDocument("# Política\n\n## 8. Preços\n\n## 12. Garantia\n\n## 13. Suporte (SLA)\n"));

    public static ChatAvailability Available (int maxPerMinute = 100, int maxHistory = 10) =>
        new(new ChatProviderLoadResult(Settings(maxPerMinute, maxHistory), []), Policy());

    public static ChatAvailability Unavailable () =>
        new(new ChatProviderLoadResult(null, [ChatProviderSettings.ApiKeyKey]), Policy());

    public static ChatAvailability WithoutPolicy () =>
        new(new ChatProviderLoadResult(Settings(), []), MissingPolicy());
}
