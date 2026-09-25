using System.Reflection;
using CastleEscape.Contracts.Diagnostics;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.OpenApi;
using CastleEscape.Server.Realtime;
using Microsoft.Extensions.Options;

namespace CastleEscape.Server.Endpoints;

public static class DiagnosticsEndpoints
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;

    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags(ApiTags.Diagnostics);

        group.MapGet("/health", GetHealth)
            .WithName("GetHealth")
            .WithSummary("Server health")
            .WithDescription("Liveness check. Returns 200 with version and uptime while the server is running.");

        app.MapGet("/api/diagnostics/loop", GetLoop)
            .WithTags(ApiTags.Diagnostics)
            .WithName("GetLoopDiagnostics")
            .WithSummary("Game loop timings")
            .WithDescription("Tick rate, session counts, and the average and slowest time to tick all sessions once.");

        return app;
    }

    private static LoopDiagnosticsResponse GetLoop(SessionRegistry registry, GameLoopStats stats, IOptions<GameOptions> options)
    {
        var sessions = registry.All;
        return new LoopDiagnosticsResponse(
            options.Value.TickRate,
            sessions.Count,
            sessions.Count(s => !s.IsFinished),
            stats.Iterations,
            Math.Round(stats.AverageMilliseconds, 3),
            Math.Round(stats.MaxMilliseconds, 3),
            stats.FailedSessionTicks);
    }

    private static HealthResponse GetHealth(IHostEnvironment environment)
    {
        var version = typeof(DiagnosticsEndpoints).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        return new HealthResponse(
            "Healthy",
            version,
            environment.EnvironmentName,
            StartedAt,
            Math.Round((DateTimeOffset.UtcNow - StartedAt).TotalSeconds, 1));
    }
}
