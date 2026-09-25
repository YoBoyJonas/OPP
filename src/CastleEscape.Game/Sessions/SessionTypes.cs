using CastleEscape.Contracts;
using CastleEscape.Contracts.Gameplay;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;

namespace CastleEscape.Game.Sessions;

/// <summary>What a player asked for. Queued by the hub/REST, applied at the start of the next tick.</summary>
public enum PlayerInputKind
{
    SetDirection,
    RequestRestart,
}

/// <summary>A queued player input. <see cref="Sequence"/> fixes the order inputs are applied in.</summary>
public sealed record PlayerInput(long Sequence, Guid PlayerId, PlayerInputKind Kind, Direction Direction = Direction.None);

/// <summary>Lives and score of each player when the level started; restart goes back to these (D8).</summary>
public sealed record LevelCheckpoint(IReadOnlyDictionary<Guid, (int Lives, int Score)> Players);

/// <summary>A message for every client of the session: which client method to call, with what payload.</summary>
public sealed record OutgoingMessage(string Method, object Payload);

/// <summary>
/// Everything REST reads, published by the session after each tick. Immutable, so readers never lock the world.
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
