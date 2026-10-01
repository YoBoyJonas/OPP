using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Content;
using CastleEscape.Game.Messaging;
using CastleEscape.Server.OpenApi;

namespace CastleEscape.Server.Endpoints;

/// <summary>
/// The SignalR protocol, described for frontend developers (OpenAPI can't describe hubs), with a real example of
/// every message.
/// </summary>
public static class RealtimeEndpoints
{
    public const string HubPath = "/hubs/game";

    public static IEndpointRouteBuilder MapRealtimeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/realtime").WithTags(ApiTags.Realtime);

        group.MapGet("/protocol", () => TypedResults.Ok(Protocol))
            .WithName("GetRealtimeProtocol")
            .WithSummary("SignalR hub protocol")
            .WithDescription("Hub URL and query parameters, every server-to-client and client-to-server method with its payload "
                             + "and when it happens, every event type and error code, and the ordering rules.");

        group.MapGet("/examples", (ContentCatalog catalog) => TypedResults.Ok(
                RealtimeExamples.Capture(catalog).ToDictionary(kv => kv.Key, kv => (object)kv.Value)))
            .WithName("GetRealtimeExamples")
            .WithSummary("Example payload of every server message")
            .WithDescription("Captured from a short real session on a small map, keyed by client method name. This is exactly "
                             + "the JSON SignalR sends.");

        return app;
    }

    public static readonly RealtimeProtocolResponse Protocol = new(
        HubPath,
        [
            new("sessionId", "The session id from create/join."),
            new("playerToken", "The player token from create/join. A wrong token gets an Error message and the connection is closed."),
        ],
        [
            new(ClientMethods.SessionUpdated, nameof(SessionUpdatedMessage),
                "On connect, and whenever the lobby changes (join, character, connect, leave) or the phase changes."),
            new(ClientMethods.LevelStarted, nameof(LevelStartedMessage),
                "On connect while a level runs, at every level start and restart: the static layout plus the first state."),
            new(ClientMethods.StateUpdated, nameof(TickStateMessage),
                "Every tick (20 Hz) while playing: players, zombies, items, levers, door. Interpolate x/y for smooth movement."),
            new(ClientMethods.GameEvent, nameof(GameEventMessage),
                "Whenever something happens (see eventTypes). SoundCue events name a sound to play."),
            new(ClientMethods.Error, nameof(ErrorMessage), "A rejected connection, or a session stopped by a server error."),
            new(ClientMethods.Pong, nameof(PongMessage), "The answer to Ping, to the caller only."),
        ],
        [
            new(HubMethods.SetDirection, "direction: None | Up | Down | Left | Right",
                "On key down send the direction, on key up send None. Applied at the start of the next tick."),
            new(HubMethods.RequestRestart, "(none)", "Restart the current level (D8). Ask the player to confirm first."),
            new(HubMethods.Ping, "(none)", "Round-trip check; answered with Pong."),
        ],
        typeof(GameEventTypes).GetFields().Select(f => (string)f.GetValue(null)!).ToArray(),
        Enum.GetNames<GameErrorCode>(),
        [
            "Every server message has sessionId, seq and tick. seq increases by one per message within a session; use it to order and de-duplicate.",
            "On (re)connect the server sends SessionUpdated, then LevelStarted and StateUpdated if a level is running, so a client can join at any time.",
            "Clients only send input; the server decides everything (positions, collisions, items). Never move a player locally without a StateUpdated.",
            "Without SignalR, poll GET /api/sessions/{id}/messages?afterSeq={last seq seen}: the same messages, StateUpdated thinned out (Realtime:PollingStateEveryNthTick).",
            "Enums are sent as strings; property names are camelCase.",
        ]);
}
