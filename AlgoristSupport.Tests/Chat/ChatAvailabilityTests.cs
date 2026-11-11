using AlgoristSupport.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlgoristSupport.Tests.Chat;

public class ChatAvailabilityTests
{
    private static ChatAvailability FromValues (Dictionary<string, string?> values, PolicyLoadResult? policy = null) =>
        ChatAvailability.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection(values).Build(), policy ?? TestChat.Policy());

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

    [Fact]
    public void Missing_policy_makes_chat_unavailable_and_logs_path_and_problem ()
    {
        var availability = FromValues(ChatProviderSettingsTests.ValidValues(), TestChat.MissingPolicy());
        var logger = new CapturingLogger<ChatAvailabilityTests>();

        availability.LogStatus(logger);

        Assert.False(availability.IsAvailable);
        Assert.NotNull(availability.Settings);
        Assert.Null(availability.Policy);
        Assert.Equal(PolicyLoadProblem.Missing, availability.PolicyProblem);
        var record = Assert.Single(logger.Records);
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Contains("Policy/policy.md", record.Message);
        Assert.Contains("missing", record.Message);
    }

    [Fact]
    public void Empty_policy_is_logged_as_empty ()
    {
        var availability = FromValues(ChatProviderSettingsTests.ValidValues(), PolicyLoadResult.Failed("p.md", PolicyLoadProblem.Empty));
        var logger = new CapturingLogger<ChatAvailabilityTests>();

        availability.LogStatus(logger);

        Assert.False(availability.IsAvailable);
        Assert.Contains("empty", Assert.Single(logger.Records).Message);
    }

    [Fact]
    public void Invalid_settings_and_missing_policy_log_both_warnings ()
    {
        var values = ChatProviderSettingsTests.ValidValues();
        values.Remove(ChatProviderSettings.ApiKeyKey);
        var availability = FromValues(values, TestChat.MissingPolicy());
        var logger = new CapturingLogger<ChatAvailabilityTests>();

        availability.LogStatus(logger);

        Assert.False(availability.IsAvailable);
        Assert.Equal(2, logger.Records.Count);
        Assert.Contains(logger.Records, r => r.Message.Contains(ChatProviderSettings.ApiKeyKey));
        Assert.Contains(logger.Records, r => r.Message.Contains("Policy/policy.md") && r.Message.Contains("missing"));
    }

    [Fact]
    public void Loaded_policy_content_is_never_logged ()
    {
        var values = ChatProviderSettingsTests.ValidValues();
        values.Remove(ChatProviderSettings.ApiKeyKey);
        var logger = new CapturingLogger<ChatAvailabilityTests>();

        FromValues(values).LogStatus(logger);

        Assert.DoesNotContain("garantia", logger.AllText);
        Assert.DoesNotContain("Política de teste", logger.AllText);
    }
}
