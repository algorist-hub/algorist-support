using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using OpenAI;

namespace AlgoristSupport.Chat;

/// <summary>
/// Builds the <see cref="IChatClient"/> used by the support chat: the OpenAI SDK pointed at the configured
/// OpenAI-compatible endpoint (Maritaca AI), with temperature and max output tokens applied to every request.
/// </summary>
/// <remarks>
/// SDK retries are disabled: they would silently multiply provider calls (and quota use) within the reply timeout,
/// and the UI offers an explicit "Tentar novamente". No logging middleware is added, because it can log message
/// content.
/// </remarks>
public static class SupportChatClientFactory
{
    public static IServiceCollection AddSupportChatClient (this IServiceCollection services, ChatAvailability availability)
    {
        if (availability.Settings is { } settings)
        {
            services.AddSingleton(_ => Create(settings));
        }
        else
        {
            services.AddSingleton<IChatClient, UnavailableChatClient>();
        }

        return services;
    }

    public static IChatClient Create (ChatProviderSettings settings)
    {
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = settings.BaseUrl,
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
        };

        var provider = new OpenAIClient(new ApiKeyCredential(settings.ApiKey), clientOptions)
            .GetChatClient(settings.Model)
            .AsIChatClient();

        return Configure(provider, settings);
    }

    /// <summary>Wraps <paramref name="inner"/> so each request carries the configured temperature and token limit.</summary>
    public static IChatClient Configure (IChatClient inner, ChatProviderSettings settings) =>
        new ChatClientBuilder(inner)
            .ConfigureOptions(options =>
            {
                options.Temperature = settings.Temperature;
                options.MaxOutputTokens = settings.MaxOutputTokens;
            })
            .Build();
}

/// <summary>
/// Registered when the provider is not configured. Never reached in practice: <see cref="ChatSession"/> rejects
/// sends in maintenance mode before calling the client.
/// </summary>
public sealed class UnavailableChatClient : IChatClient
{
    private const string Message = "The AI provider is not configured (maintenance mode).";

    public Task<ChatResponse> GetResponseAsync (IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync (IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public object? GetService (Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose () { }
}
