using AlgoristSupport.Chat;
using AlgoristSupport.Components;
using Microsoft.Extensions.AI;

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

            // Support chat: options, reply source (simulated for now) and per-circuit session state.
            builder.Services.AddOptions<SupportChatOptions>()
                .Bind(builder.Configuration.GetSection(SupportChatOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(o => o.Simulation is { InitialDelayMs: >= 0, ChunkDelayMs: >= 0 },
                    "Chat:Simulation delays must be zero or positive.")
                .ValidateOnStart();
            builder.Services.AddSingleton<IChatClient, SimulatedChatClient>();
            builder.Services.AddScoped<ChatSession>();

            var app = builder.Build();

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
