using System.Reflection;
using CastleEscape.Contracts.Diagnostics;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.OpenApi;
using CastleEscape.Server.Realtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CastleEscape.Server.Controllers;

[ApiController]
[Tags(ApiTags.Diagnostics)]
public sealed class DiagnosticsController(
    GameFacade game,
    SessionRegistry registry,
    GameLoopStats stats,
    IOptions<GameOptions> options,
    IHostEnvironment environment) : ControllerBase
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;

    [HttpGet("/health", Name = "GetHealth")]
    public ActionResult<HealthResponse> GetHealth()
    {
        var version = typeof(DiagnosticsController).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        return Ok(new HealthResponse(
            "Healthy",
            version,
            environment.EnvironmentName,
            StartedAt,
            Math.Round((DateTimeOffset.UtcNow - StartedAt).TotalSeconds, 1)));
    }

    [HttpGet("/api/diagnostics/loop", Name = "GetLoopDiagnostics")]
    public ActionResult<LoopDiagnosticsResponse> GetLoop()
    {
        var sessions = registry.All;
        return Ok(new LoopDiagnosticsResponse(
            options.Value.TickRate,
            sessions.Count,
            sessions.Count(s => !s.IsFinished),
            stats.Iterations,
            Math.Round(stats.AverageMilliseconds, 3),
            Math.Round(stats.MaxMilliseconds, 3),
            stats.FailedSessionTicks));
    }

    [HttpGet("/api/diagnostics/sessions/{sessionId:guid}/commands", Name = "GetCommandHistory")]
    public ActionResult<IReadOnlyList<CommandRecordDto>> GetCommandHistory([FromRoute] Guid sessionId)
    {
        var commands = game.GetCommandHistory(sessionId);
        return Ok(commands);
    }

    [HttpGet("/api/diagnostics/sessions/{sessionId:guid}/events", Name = "GetEventLog")]
    public ActionResult<IReadOnlyList<EventLogEntryDto>> GetEventLog([FromRoute] Guid sessionId, [FromQuery] long? afterTick)
    {
        var events = game.GetEventLog(sessionId, afterTick ?? -1);
        return Ok(events);
    }
}
