using CastleEscape.Application.Development;
using CastleEscape.Application.Content;
using CastleEscape.Infrastructure.Sessions;
using CastleEscape.Infrastructure.Messaging;
using CastleEscape.Application.Configuration;
using CastleEscape.Domain.Configuration;
using CastleEscape.Domain.Content;
using CastleEscape.Infrastructure.Content;
using CastleEscape.Application.Generation;
using CastleEscape.Domain.Generation;
using CastleEscape.Application.Sessions;
using CastleEscape.Domain.Sessions;
using CastleEscape.Application.Messaging;
using CastleEscape.Infrastructure.Serialization;
using CastleEscape.Server.Endpoints;
using CastleEscape.Server.Hubs;
using CastleEscape.Server.OpenApi;
using CastleEscape.Server.Configuration;
using CastleEscape.Server.Realtime;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace CastleEscape.Server.Configuration;

/// <summary>Composition root: chooses implementations for application and domain ports.</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddCastleEscape(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration, validated at startup so a bad appsettings value fails fast.
        AddValidatedOptions<GameOptions>(services, GameOptions.SectionName);
        AddValidatedOptions<GenerationOptions>(services, GenerationOptions.SectionName);
        AddValidatedOptions<RealtimeOptions>(services, RealtimeOptions.SectionName);
        AddValidatedOptions<CorsSettings>(services, CorsSettings.SectionName);
        AddValidatedOptions<DevToolsOptions>(services, DevToolsOptions.SectionName);

        services.ConfigureHttpJsonOptions(options => JsonDefaults.Apply(options.SerializerOptions));
        services.AddSignalR().AddJsonProtocol(options => JsonDefaults.Apply(options.PayloadSerializerOptions));
        services.AddProblemDetails();
        services.AddExceptionHandler<GameExceptionHandler>();
        services.AddCastleEscapeOpenApi();

        var corsOrigins = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()?.AllowedOrigins ?? [];
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials())); // SignalR sends credentials, which rules out AllowAnyOrigin.

        // The Singleton owns its own lifetime; DI just hands out the same instance.
        services.AddSingleton<IContentCatalog>(_ => ContentCatalog.Instance);
        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        services.AddSingleton<IPresetMapSource, FilePresetMapSource>();
        services.AddSingleton<ILevelProvider>(sp => new LevelProvider(
            sp.GetRequiredService<IContentCatalog>(),
            sp.GetRequiredService<IOptions<GameOptions>>().Value,
            sp.GetRequiredService<IOptions<GenerationOptions>>().Value,
            sp.GetRequiredService<IPresetMapSource>()));
        services.AddSingleton(sp => new SessionRegistry(
            sp.GetRequiredService<IContentCatalog>(),
            sp.GetRequiredService<ILevelProvider>(),
            sp.GetRequiredService<IOptions<GameOptions>>().Value,
            sp.GetRequiredService<ISessionStore>()));
        services.AddSingleton<GameLoopStats>();
        services.AddSingleton(sp =>
        {
            var scheduler = new GameLoopScheduler(sp.GetRequiredService<SessionRegistry>());
            var stats = sp.GetRequiredService<GameLoopStats>();
            var logger = sp.GetRequiredService<ILogger<GameLoopScheduler>>();
            scheduler.SessionFailed += (session, ex) =>
            {
                stats.RecordFailure();
                logger.LogError(ex, "Tick failed for session {SessionId}; stopping it", session.Id);
            };
            return scheduler;
        });
        services.AddSingleton<GameFacade>();
        services.AddSingleton(sp => new ContentQueries(sp.GetRequiredService<IContentCatalog>(), sp.GetRequiredService<IOptions<GameOptions>>().Value));
        services.AddSingleton(sp => new LevelPreviewer(
            sp.GetRequiredService<IContentCatalog>(),
            sp.GetRequiredService<IOptions<GameOptions>>().Value,
            sp.GetRequiredService<IOptions<GenerationOptions>>().Value,
            sp.GetRequiredService<IPresetMapSource>()));
        services.AddSingleton(sp => new DevActions(
            sp.GetRequiredService<SessionRegistry>(),
            sp.GetRequiredService<IContentCatalog>()));

        // Bridge: notifiers (what to send) over channels (how), both picked from Realtime settings.
        services.AddSingleton(sp => new PollingBufferChannel(sp.GetRequiredService<IOptions<RealtimeOptions>>().Value.PollingBufferSize));
        services.AddSingleton<SignalRClientChannel>();
        services.AddSingleton<IReadOnlyList<ClientNotifier>>(sp =>
        {
            var realtime = sp.GetRequiredService<IOptions<RealtimeOptions>>().Value;
            var channels = realtime.EnabledChannels.Distinct().Select(c => c switch
            {
                RealtimeChannel.SignalR => (IClientChannel)sp.GetRequiredService<SignalRClientChannel>(),
                _ => sp.GetRequiredService<PollingBufferChannel>(),
            });
            return ClientNotifiers.For(channels, new Dictionary<string, int> { ["Polling"] = realtime.PollingStateEveryNthTick });
        });
        services.AddHostedService<GameLoopService>();
        return services;
    }

    private static void AddValidatedOptions<TOptions>(IServiceCollection services, string section) where TOptions : class =>
        services.AddOptions<TOptions>()
            .BindConfiguration(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
