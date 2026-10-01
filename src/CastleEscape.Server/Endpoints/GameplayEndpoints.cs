using CastleEscape.Contracts;
using CastleEscape.Contracts.Gameplay;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Messaging;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

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
            .WithDescription("The latest per-tick state: players, zombies, items, levers, door. "
                             + "JSON by default; `?format=xml` or `Accept: application/xml` for XML (NET-2, Adapter pattern).")
            .Produces<TickStateMessage>(StatusCodes.Status200OK, "application/json", "application/xml")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/messages", GetMessages)
            .WithTags(ApiTags.Realtime)
            .WithName("PollMessages")
            .WithSummary("Read buffered messages (polling channel)")
            .WithDescription("The same messages SignalR pushes, kept in a per-session buffer for clients without a hub "
                             + "connection (Bridge pattern: the polling channel). Pass the last `seq` you saw as `afterSeq`. "
                             + "Each `body` is serialized as `format=json|xml`. StateUpdated is buffered every "
                             + "`Realtime:PollingStateEveryNthTick` ticks; events are never skipped.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

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

    /// <summary>JSON or XML through the Adapter (IMessageSerializer): <c>?format=</c> wins over the Accept header.</summary>
    private static IResult GetState(Guid sessionId, string? format, HttpRequest request, SessionRegistry registry)
    {
        var state = registry.Get(sessionId).Snapshot.State ?? throw NoLevel();
        var serializer = ChooseSerializer(format, request);
        return Results.Text(serializer.Serialize(state), serializer.ContentType, System.Text.Encoding.UTF8);
    }

    private static Ok<PolledMessagesResponse> GetMessages(Guid sessionId, long? afterSeq, string? format, HttpRequest request,
        SessionRegistry registry, PollingBufferChannel polling, IOptions<RealtimeOptions> realtime)
    {
        registry.Get(sessionId);
        if (!realtime.Value.EnabledChannels.Contains(RealtimeChannel.Polling))
        {
            throw new GameException(GameErrorCode.InvalidRequest, "The polling channel is disabled (Realtime:EnabledChannels).");
        }
        var serializer = ChooseSerializer(format, request);
        var after = afterSeq ?? 0;
        var messages = polling.Read(sessionId, after)
            .Select(m => new PolledMessageDto(m.Seq, m.Method, serializer.Serialize(m.Message)))
            .ToArray();
        return TypedResults.Ok(new PolledMessagesResponse(sessionId, serializer.ContentType,
            messages.Length > 0 ? messages[^1].Seq : after, messages));
    }

    private static IMessageSerializer ChooseSerializer(string? format, HttpRequest request)
    {
        try
        {
            return MessageSerializers.Choose(format, request.Headers.Accept);
        }
        catch (ArgumentException ex)
        {
            throw new GameException(GameErrorCode.InvalidRequest, ex.Message);
        }
    }

    private static Ok<LevelLayoutResponse> GetLevel(Guid sessionId, SessionRegistry registry) =>
        TypedResults.Ok(registry.Get(sessionId).Snapshot.Layout ?? throw NoLevel());

    private static Ok<HudResponse> GetHud(Guid sessionId, SessionRegistry registry) =>
        TypedResults.Ok(registry.Get(sessionId).Snapshot.Hud);

    private static GameException NoLevel() =>
        new(GameErrorCode.NoLevelLoaded, "No level has been loaded yet; both players must join and pick a character.");
}
