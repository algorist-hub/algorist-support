using System.Globalization;

namespace AlgoristSupport.Chat;

/// <summary>
/// AI provider settings, read from flat root-level configuration keys (user secrets locally, App Service
/// environment variables in Azure). There are no defaults: every key is required.
/// </summary>
public sealed record ChatProviderSettings(
    string ApiKey,
    string Model,
    Uri BaseUrl,
    float Temperature,
    int MaxMessagesPerMinute,
    int MaxOutputTokens,
    int MaxHistoryExchanges)
{
    public const string ApiKeyKey = "chave_algorist_demo_apps";
    public const string ModelKey = "chat_model";
    public const string BaseUrlKey = "chat_base_url";
    public const string TemperatureKey = "chat_temperature";
    public const string MaxMessagesPerMinuteKey = "chat_max_msg_per_min";
    public const string MaxOutputTokensKey = "chat_max_token_per_answer";
    public const string MaxHistoryExchangesKey = "chat_max_history_sent";

    public static IReadOnlyList<string> AllKeys { get; } =
    [
        ApiKeyKey,
        ModelKey,
        BaseUrlKey,
        TemperatureKey,
        MaxMessagesPerMinuteKey,
        MaxOutputTokensKey,
        MaxHistoryExchangesKey,
    ];

    /// <summary>
    /// Reads and validates the settings. Never throws; the result lists the names (never the values) of missing
    /// or invalid keys.
    /// </summary>
    public static ChatProviderLoadResult Load (IConfiguration configuration)
    {
        var invalid = new List<string>();

        var apiKey = ReadText(configuration, ApiKeyKey, invalid);
        var model = ReadText(configuration, ModelKey, invalid);
        var baseUrl = ReadHttpsUrl(configuration, BaseUrlKey, invalid);
        var temperature = ReadTemperature(configuration, TemperatureKey, invalid);
        var maxPerMinute = ReadInt(configuration, MaxMessagesPerMinuteKey, minimum: 1, invalid);
        var maxTokens = ReadInt(configuration, MaxOutputTokensKey, minimum: 1, invalid);
        var maxHistory = ReadInt(configuration, MaxHistoryExchangesKey, minimum: 0, invalid);

        if (invalid.Count > 0)
        {
            return new ChatProviderLoadResult(null, invalid);
        }

        return new ChatProviderLoadResult(
            new ChatProviderSettings(apiKey!, model!, baseUrl!, temperature, maxPerMinute, maxTokens, maxHistory),
            []);
    }

    private static string? Raw (IConfiguration configuration, string key) => configuration[key]?.Trim();

    private static string? ReadText (IConfiguration configuration, string key, List<string> invalid)
    {
        var value = Raw(configuration, key);
        if (string.IsNullOrEmpty(value))
        {
            invalid.Add(key);
            return null;
        }

        return value;
    }

    private static Uri? ReadHttpsUrl (IConfiguration configuration, string key, List<string> invalid)
    {
        if (Uri.TryCreate(Raw(configuration, key), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            return uri;
        }

        invalid.Add(key);
        return null;
    }

    private static float ReadTemperature (IConfiguration configuration, string key, List<string> invalid)
    {
        // NumberStyles.Float with the invariant culture accepts only '.' as decimal separator ("0,3" is rejected).
        if (float.TryParse(Raw(configuration, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && value is >= 0f and <= 2f)
        {
            return value;
        }

        invalid.Add(key);
        return 0f;
    }

    private static int ReadInt (IConfiguration configuration, string key, int minimum, List<string> invalid)
    {
        if (int.TryParse(Raw(configuration, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            && value >= minimum)
        {
            return value;
        }

        invalid.Add(key);
        return 0;
    }
}

public sealed record ChatProviderLoadResult(ChatProviderSettings? Settings, IReadOnlyList<string> InvalidSettings)
{
    public bool IsValid => Settings is not null;
}
