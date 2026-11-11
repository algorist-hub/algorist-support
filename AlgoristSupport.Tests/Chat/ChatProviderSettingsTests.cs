using AlgoristSupport.Chat;
using Microsoft.Extensions.Configuration;

namespace AlgoristSupport.Tests.Chat;

public class ChatProviderSettingsTests
{
    internal static Dictionary<string, string?> ValidValues () => new()
    {
        [ChatProviderSettings.ApiKeyKey] = "secret-key-123",
        [ChatProviderSettings.ModelKey] = "modelo-teste",
        [ChatProviderSettings.BaseUrlKey] = "https://provider.example/api",
        [ChatProviderSettings.TemperatureKey] = "0.3",
        [ChatProviderSettings.MaxMessagesPerMinuteKey] = "5",
        [ChatProviderSettings.MaxOutputTokensKey] = "800",
        [ChatProviderSettings.MaxHistoryExchangesKey] = "10",
    };

    private static ChatProviderLoadResult Load (Dictionary<string, string?> values) =>
        ChatProviderSettings.Load(new ConfigurationBuilder().AddInMemoryCollection(values).Build());

    [Fact]
    public void Valid_configuration_is_parsed ()
    {
        var result = Load(ValidValues());

        Assert.True(result.IsValid);
        Assert.Empty(result.InvalidSettings);
        var s = result.Settings!;
        Assert.Equal("secret-key-123", s.ApiKey);
        Assert.Equal("modelo-teste", s.Model);
        Assert.Equal(new Uri("https://provider.example/api"), s.BaseUrl);
        Assert.Equal(0.3f, s.Temperature);
        Assert.Equal(5, s.MaxMessagesPerMinute);
        Assert.Equal(800, s.MaxOutputTokens);
        Assert.Equal(10, s.MaxHistoryExchanges);
    }

    public static TheoryData<string> AllKeys () => new(ChatProviderSettings.AllKeys);

    [Theory]
    [MemberData(nameof(AllKeys))]
    public void Each_missing_key_is_reported (string key)
    {
        var values = ValidValues();
        values.Remove(key);

        var result = Load(values);

        Assert.False(result.IsValid);
        Assert.Null(result.Settings);
        Assert.Equal([key], result.InvalidSettings);
    }

    [Theory]
    [MemberData(nameof(AllKeys))]
    public void Each_blank_key_is_reported (string key)
    {
        var values = ValidValues();
        values[key] = "   ";

        Assert.Equal([key], Load(values).InvalidSettings);
    }

    [Theory]
    [InlineData(ChatProviderSettings.TemperatureKey, "0,3")]
    [InlineData(ChatProviderSettings.TemperatureKey, "-0.1")]
    [InlineData(ChatProviderSettings.TemperatureKey, "2.5")]
    [InlineData(ChatProviderSettings.TemperatureKey, "NaN")]
    [InlineData(ChatProviderSettings.BaseUrlKey, "http://example.com/api")]
    [InlineData(ChatProviderSettings.BaseUrlKey, "not a url")]
    [InlineData(ChatProviderSettings.BaseUrlKey, "/api")]
    [InlineData(ChatProviderSettings.MaxMessagesPerMinuteKey, "0")]
    [InlineData(ChatProviderSettings.MaxMessagesPerMinuteKey, "abc")]
    [InlineData(ChatProviderSettings.MaxOutputTokensKey, "0")]
    [InlineData(ChatProviderSettings.MaxOutputTokensKey, "1.5")]
    [InlineData(ChatProviderSettings.MaxHistoryExchangesKey, "-1")]
    public void Invalid_values_are_reported (string key, string value)
    {
        var values = ValidValues();
        values[key] = value;

        Assert.Equal([key], Load(values).InvalidSettings);
    }

    [Theory]
    [InlineData(ChatProviderSettings.TemperatureKey, "0")]
    [InlineData(ChatProviderSettings.TemperatureKey, "2")]
    [InlineData(ChatProviderSettings.MaxHistoryExchangesKey, "0")]
    [InlineData(ChatProviderSettings.MaxMessagesPerMinuteKey, "1")]
    public void Boundary_values_are_valid (string key, string value)
    {
        var values = ValidValues();
        values[key] = value;

        Assert.True(Load(values).IsValid);
    }

    [Fact]
    public void Several_invalid_keys_are_all_reported_without_values ()
    {
        var values = ValidValues();
        values.Remove(ChatProviderSettings.ApiKeyKey);
        values[ChatProviderSettings.TemperatureKey] = "quente";

        var result = Load(values);

        Assert.Equal([ChatProviderSettings.ApiKeyKey, ChatProviderSettings.TemperatureKey], result.InvalidSettings);
        Assert.DoesNotContain(result.InvalidSettings, name => name.Contains("quente"));
    }
}
