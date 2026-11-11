using AlgoristSupport.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlgoristSupport.Tests.Chat;

public class ChatAvailabilityTests
{
    private static ChatAvailability FromValues (Dictionary<string, string?> values) =>
        ChatAvailability.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection(values).Build());

    [Fact]
    public void Valid_configuration_is_available_and_logs_nothing ()
    {
        var availability = FromValues(ChatProviderSettingsTests.ValidValues());
        var logger = new CapturingLogger<ChatAvailabilityTests>();

        availability.LogStatus(logger);

        Assert.True(availability.IsAvailable);
        Assert.NotNull(availability.Settings);
        Assert.Empty(logger.Records);
    }

    [Fact]
    public void Missing_api_key_logs_warning_with_names_only ()
    {
        var values = ChatProviderSettingsTests.ValidValues();
        values.Remove(ChatProviderSettings.ApiKeyKey);
        values[ChatProviderSettings.MaxMessagesPerMinuteKey] = "abc";
        var availability = FromValues(values);
        var logger = new CapturingLogger<ChatAvailabilityTests>();

        availability.LogStatus(logger);

        Assert.False(availability.IsAvailable);
        var record = Assert.Single(logger.Records);
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Contains(ChatProviderSettings.ApiKeyKey, record.Message);
        Assert.Contains(ChatProviderSettings.MaxMessagesPerMinuteKey, record.Message);
        Assert.DoesNotContain("abc", logger.AllText);
    }

    [Fact]
    public void Invalid_setting_never_logs_any_configured_value ()
    {
        var values = ChatProviderSettingsTests.ValidValues();
        values[ChatProviderSettings.BaseUrlKey] = "http://insecure.example";
        var logger = new CapturingLogger<ChatAvailabilityTests>();

        FromValues(values).LogStatus(logger);

        foreach (var value in values.Values)
        {
            Assert.DoesNotContain(value!, logger.AllText);
        }
    }
}
