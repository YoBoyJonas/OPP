using CastleEscape.Contracts;
using CastleEscape.Contracts.Gameplay;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CastleEscape.Server.Endpoints;

/// <summary>
/// REST mirror of the hub, so everything can be tried from Scalar: send input, restart, and read
/// the state, layout and HUD. Reads use the session's latest snapshot and never lock the world.
/// </summary>
public static class GameplayEndpoints
{
    public static IEndpointRouteBuilder MapGameplayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions/{sessionId:guid}").WithTags(ApiTags.Gameplay);

        group.MapPost("/input", SubmitInput)
            .WithName("SubmitDirection")
            .WithSummary("Set the held direction")
            .WithDescription("Same as the hub's SetDirection. Send a direction on key down and None on key up. "
                             + "Applied at the start of the next tick.")
            .RequirePlayerToken()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/restart", Restart)
            .WithName("RestartLevel")
            .WithSummary("Restart the current level")
            .WithDescription("D8: the level returns to its generated layout; both players' lives and score return to the level start.")
            .RequirePlayerToken()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/state", GetState)
            .WithName("GetGameState")
            .WithSummary("Latest game state")
            .WithDescription("The latest per-tick state: players, zombies, items, levers, door.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/level", GetLevel)
            .WithName("GetLevelLayout")
            .WithSummary("Static layout of the current level")
            .WithDescription("Rows of map legend characters, plus the legend itself.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/hud", GetHud)
            .WithName("GetHud")
            .WithSummary("View game state (HUD table)")
            .WithDescription("Per player: lives, score, active powers with time left, combos, statistics. "
                             + "Plus level, levers and door status.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Accepted SubmitInput(Guid sessionId, DirectionRequest request, HttpContext http, SessionRegistry registry)
    {
        var (session, player) = registry.Authenticate(sessionId, http.PlayerToken());
        session.SubmitDirection(player.PlayerId, request.Direction);
        return TypedResults.Accepted((string?)null);
    }

    private static Accepted Restart(Guid sessionId, HttpContext http, SessionRegistry registry)
    {
        var (session, player) = registry.Authenticate(sessionId, http.PlayerToken());
        if (session.Phase != SessionPhase.Playing)
        {
            throw new GameException(GameErrorCode.WrongPhase, $"A level can only be restarted while playing (now {session.Phase}).");
        }
        session.RequestRestart(player.PlayerId);
        return TypedResults.Accepted((string?)null);
    }

    private static Ok<TickStateMessage> GetState(Guid sessionId, SessionRegistry registry) =>
        TypedResults.Ok(registry.Get(sessionId).Snapshot.State ?? throw NoLevel());

    private static Ok<LevelLayoutResponse> GetLevel(Guid sessionId, SessionRegistry registry) =>
        TypedResults.Ok(registry.Get(sessionId).Snapshot.Layout ?? throw NoLevel());

    private static Ok<HudResponse> GetHud(Guid sessionId, SessionRegistry registry) =>
        TypedResults.Ok(registry.Get(sessionId).Snapshot.Hud);

    private static GameException NoLevel() =>
        new(GameErrorCode.NoLevelLoaded, "No level has been loaded yet; both players must join and pick a character.");
}
