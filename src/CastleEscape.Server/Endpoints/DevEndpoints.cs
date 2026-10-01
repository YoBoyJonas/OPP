using CastleEscape.Contracts.Dev;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CastleEscape.Server.Endpoints;

/// <summary>
/// Developer and defence shortcuts under <c>/api/dev</c>. Mapped only when <c>DevTools:Enabled</c> is true
/// (Development by default), so they don't exist at all on a public deployment.
/// </summary>
public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dev").WithTags(ApiTags.Dev);

        group.MapPost("/quickstart", Quickstart)
            .WithName("DevQuickstart")
            .WithSummary("Two players, characters picked, level 1 starting")
            .WithDescription("Skips the lobby: creates a session with Ana and Ben, picks their characters and returns both "
                             + "player tokens and hub URLs. Open each hub URL (or the playground) in its own tab.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        var session = group.MapGroup("/sessions/{sessionId:guid}");

        session.MapPost("/undo", (Guid sessionId, Guid? playerId, DevActions dev) => TypedResults.Ok(dev.UndoLastCommand(sessionId, playerId)))
            .WithName("DevUndoLastCommand")
            .WithSummary("Undo the last command")
            .WithDescription("Undoes the most recent command of the session, or of `playerId` (Command pattern). Undoing a "
                             + "restart brings back the level as it was before it.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        session.MapPut("/zombies/strategy", (Guid sessionId, ZombieStrategyRequest request, DevActions dev) =>
            {
                dev.SetZombieStrategy(sessionId, request.Strategy);
                return TypedResults.Accepted((string?)null);
            })
            .WithName("DevSetZombieStrategy")
            .WithSummary("Switch every zombie's chase strategy")
            .WithDescription("Greedy, Bfs, AStar or Predictive, from the next tick (Strategy pattern). It is a command, so "
                             + "`/undo` switches the zombies back. The tick state shows each zombie's `strategy`.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        session.MapPost("/players/{playerId:guid}/powers", (Guid sessionId, Guid playerId, GivePowerRequest request, DevActions dev) =>
            {
                dev.GivePower(sessionId, playerId, request.Power, request.DurationSeconds);
                return TypedResults.Accepted((string?)null);
            })
            .WithName("DevGivePower")
            .WithSummary("Give a player a power")
            .WithDescription("Jump, Sprint or Swim, as if an item had been picked up: stacks and starts combos (Decorator "
                             + "pattern). Undoable.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        session.MapPost("/skip-level", (Guid sessionId, DevActions dev) =>
            {
                dev.SkipLevel(sessionId);
                return TypedResults.NoContent();
            })
            .WithName("DevSkipLevel")
            .WithSummary("Complete the current level")
            .WithDescription("Ends the level as if both players had reached the exit; the next level loads after the usual pause.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/patterns/prototype-clone-mode", (DevActions dev) => TypedResults.Ok(dev.GetCloneMode()))
            .WithName("DevGetCloneMode")
            .WithSummary("Current Prototype clone mode");

        group.MapPut("/patterns/prototype-clone-mode", (CloneModeDto request, DevActions dev) => TypedResults.Ok(dev.SetCloneMode(request.Mode)))
            .WithName("DevSetCloneMode")
            .WithSummary("Switch deep/shallow level copies at runtime")
            .WithDescription("`Deep` or `Shallow` (Prototype pattern). Takes effect on the next level start or restart: with "
                             + "Shallow, collected items stay gone after a restart.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static Ok<DevQuickstartResponse> Quickstart(DevQuickstartRequest? request, HttpRequest http, DevActions dev)
    {
        var baseUrl = $"{http.Scheme}://{http.Host}";
        return TypedResults.Ok(dev.Quickstart(request ?? new DevQuickstartRequest(),
            (sessionId, token) => $"{baseUrl}/hubs/game?sessionId={sessionId}&playerToken={token}"));
    }
}
