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

        var sessions = app.MapGroup("/api/diagnostics/sessions/{sessionId:guid}").WithTags(ApiTags.Diagnostics);

        sessions.MapGet("/commands", (Guid sessionId, GameFacade game) => game.GetCommandHistory(sessionId))
            .WithName("GetCommandHistory")
            .WithSummary("Command history of a session")
            .WithDescription("The last 100 commands (inputs, steps, restarts, given powers), oldest first, with the tick each ran "
                             + "in and whether it was undone (Command pattern). An undone StartStep is a D7 tile conflict.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        sessions.MapGet("/events", (Guid sessionId, long? afterTick, GameFacade game) => game.GetEventLog(sessionId, afterTick ?? -1))
            .WithName("GetEventLog")
            .WithSummary("Event log of a session")
            .WithDescription("The last 200 game events, oldest first, as recorded by the EventLogObserver (Observer pattern). "
                             + "Pass `afterTick` to get only newer ones.")
            .ProducesProblem(StatusCodes.Status404NotFound);

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
