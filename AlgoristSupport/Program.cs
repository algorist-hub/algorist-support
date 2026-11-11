using AlgoristSupport.Chat;
using AlgoristSupport.Components;

namespace AlgoristSupport
{
    public class Program
    {
        public static void Main (string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Support chat UI options (section "Chat").
            builder.Services.AddOptions<SupportChatOptions>()
                .Bind(builder.Configuration.GetSection(SupportChatOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // AI provider settings come from flat keys (user secrets / environment variables); when any is missing or
            // invalid the app still starts and the chat shows a maintenance message.
            var availability = ChatAvailability.FromConfiguration(builder.Configuration);
            builder.Services.AddSingleton(availability);
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSupportChatClient(availability);
            builder.Services.AddScoped<ChatSession>();

            var app = builder.Build();

            availability.LogStatus(app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AlgoristSupport.Chat.Configuration"));

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
