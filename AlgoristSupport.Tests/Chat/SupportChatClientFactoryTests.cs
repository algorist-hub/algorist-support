using AlgoristSupport.Chat;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlgoristSupport.Tests.Chat;

/// <summary>Pipeline construction tests. None of them sends a request, so no network or secret is needed.</summary>
public class SupportChatClientFactoryTests
{
    [Fact]
    public async Task Configured_pipeline_applies_temperature_and_max_output_tokens_to_every_request ()
    {
        var fake = new FakeChatClient { AutoReply = ["ok"] };
        var client = SupportChatClientFactory.Configure(fake, TestChat.Settings(maxTokens: 800, temperature: 0.3f));

        await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "Oi")])) { }
        await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "Oi")], new ChatOptions { Temperature = 1.5f })) { }

        Assert.Equal(2, fake.RequestOptions.Count);
        Assert.All(fake.RequestOptions, options =>
        {
            Assert.NotNull(options);
            Assert.Equal(0.3f, options!.Temperature);
            Assert.Equal(800, options.MaxOutputTokens);
        });
    }

    [Fact]
    public void Available_configuration_registers_provider_client_for_configured_model_and_endpoint ()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(ChatProviderSettingsTests.ValidValues()).Build();
        var services = new ServiceCollection().AddSupportChatClient(ChatAvailability.FromConfiguration(configuration, TestChat.Policy()));

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IChatClient>();

        Assert.IsNotType<UnavailableChatClient>(client);
        var metadata = client.GetService<ChatClientMetadata>();
        Assert.NotNull(metadata);
        Assert.Equal("modelo-teste", metadata!.DefaultModelId);
        Assert.Equal("provider.example", metadata.ProviderUri?.Host);
        Assert.StartsWith("https://provider.example/api", metadata.ProviderUri!.AbsoluteUri);
    }

    [Fact]
    public async Task Unavailable_configuration_registers_client_that_never_calls_out ()
    {
        var services = new ServiceCollection().AddSupportChatClient(TestChat.Unavailable());

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IChatClient>();

        Assert.IsType<UnavailableChatClient>(client);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetResponseAsync("Oi"));
    }
}
