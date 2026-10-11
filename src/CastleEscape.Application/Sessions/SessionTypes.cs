using CastleEscape.Contracts;
using CastleEscape.Contracts.Gameplay;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;

namespace CastleEscape.Application.Sessions;

/// <summary>A message for every client of the session: which client method to call, with what payload.</summary>
public sealed record OutgoingMessage(string Method, object Payload);

/// <summary>
/// Everything REST reads, published by the session after each tick. Treated as read-only, so snapshot readers do not lock the world.
/// </summary>
public sealed record SessionSnapshot(
    SessionDto Session,
    LevelStartedMessage? LevelStarted,
    TickStateMessage? State,
    LevelLayoutResponse? Layout,
    HudResponse Hud);

/// <summary>A lobby or gameplay rule was broken; <see cref="Code"/> maps to an HTTP status and hub error.</summary>
public class GameException(GameErrorCode code, string message) : Exception(message)
{
    public GameErrorCode Code { get; } = code;
}
