using CastleEscape.Contracts.Sessions;

namespace CastleEscape.Contracts.Realtime;

// Every message the server sends carries SessionId, Seq (monotonic per session) and Tick.

/// <summary>Sent when the lobby changes: a player joins, picks a character, connects, leaves; or the phase changes.</summary>
public sealed record SessionUpdatedMessage(Guid SessionId, long Seq, long Tick, SessionDto Session);

/// <summary>Sent once per level start and restart: the static layout plus the initial dynamic state.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Seq">Message sequence number.</param>
/// <param name="Tick">Server tick.</param>
/// <param name="LevelIndex">1-10.</param>
/// <param name="Theme">Dungeon or Crypt.</param>
/// <param name="Width">Width in tiles.</param>
/// <param name="Height">Height in tiles.</param>
/// <param name="Rows">One string per row using the map legend; entity characters mark starting positions.</param>
/// <param name="State">The dynamic state at level start.</param>
public sealed record LevelStartedMessage(
    Guid SessionId,
    long Seq,
    long Tick,
    int LevelIndex,
    LevelTheme Theme,
    int Width,
    int Height,
    IReadOnlyList<string> Rows,
    TickStateMessage State);

/// <summary>Dynamic state, sent every tick while playing.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Seq">Message sequence number.</param>
/// <param name="Tick">Server tick.</param>
/// <param name="Phase">Current phase.</param>
/// <param name="LevelIndex">Current level.</param>
/// <param name="DoorOpen">Whether the exit door is open.</param>
/// <param name="Players">Both players.</param>
/// <param name="Zombies">All zombies.</param>
/// <param name="Items">Items still on the map.</param>
/// <param name="Levers">All levers.</param>
public sealed record TickStateMessage(
    Guid SessionId,
    long Seq,
    long Tick,
    SessionPhase Phase,
    int LevelIndex,
    bool DoorOpen,
    IReadOnlyList<PlayerStateDto> Players,
    IReadOnlyList<ZombieStateDto> Zombies,
    IReadOnlyList<ItemStateDto> Items,
    IReadOnlyList<LeverStateDto> Levers);

/// <summary>A player in the tick state. <c>X</c>/<c>Y</c> are interpolated for smooth rendering.</summary>
public sealed record PlayerStateDto(
    Guid PlayerId,
    int Slot,
    string Name,
    string CharacterId,
    int TileX,
    int TileY,
    double X,
    double Y,
    Direction Facing,
    bool IsMoving,
    int Lives,
    int MaxLives,
    int Score,
    IReadOnlyList<ActivePowerDto> Powers,
    IReadOnlyList<PowerType> Combos);

/// <summary>A power a player has right now.</summary>
/// <param name="Power">Jump, Sprint or Swim.</param>
/// <param name="RemainingSeconds">Time left.</param>
/// <param name="Level">Stack level: 1 for one pickup, +1 for each repeat pickup (PWR-3).</param>
public sealed record ActivePowerDto(PowerType Power, double RemainingSeconds, int Level);

public sealed record ZombieStateDto(string Id, string TypeId, int TileX, int TileY, double X, double Y, Direction Facing);

/// <summary>An item on the map. <c>Power</c> is set for Power items.</summary>
public sealed record ItemStateDto(string Id, string ConsumableId, ItemKind Kind, PowerType? Power, int X, int Y);

public sealed record LeverStateDto(string Id, int X, int Y, bool IsActive);

/// <summary>A gameplay fact (item collected, life lost, door opened…). See <c>GameEventTypes</c>.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Seq">Message sequence number.</param>
/// <param name="Tick">Server tick.</param>
/// <param name="Type">Event type, e.g. "LifeLost".</param>
/// <param name="PlayerId">The player concerned, if any.</param>
/// <param name="Message">Human-readable text.</param>
/// <param name="Data">Extra values as strings (e.g. "lives": "2").</param>
public sealed record GameEventMessage(
    Guid SessionId,
    long Seq,
    long Tick,
    string Type,
    Guid? PlayerId,
    string Message,
    IReadOnlyDictionary<string, string> Data);

/// <summary>Something went wrong for this client or session.</summary>
public sealed record ErrorMessage(Guid SessionId, long Seq, long Tick, GameErrorCode Code, string Message);

/// <summary>Reply to <c>Ping()</c>.</summary>
public sealed record PongMessage(Guid SessionId, long Seq, long Tick, DateTimeOffset ServerTimeUtc);

/// <summary>Names used in <see cref="GameEventMessage.Type"/>.</summary>
public static class GameEventTypes
{
    public const string PlayerJoined = "PlayerJoined";
    public const string PlayerLeft = "PlayerLeft";
    public const string LevelStarted = "LevelStarted";
    public const string LevelRestarted = "LevelRestarted";
    public const string ItemCollected = "ItemCollected";
    public const string PowerGained = "PowerGained";
    public const string PowerExpired = "PowerExpired";
    public const string ComboActivated = "ComboActivated";
    public const string LifeLost = "LifeLost";
    public const string GameLost = "GameLost";
    public const string LeverChanged = "LeverChanged";
    public const string DoorOpened = "DoorOpened";
    public const string DoorClosed = "DoorClosed";
    public const string PlayerReachedExit = "PlayerReachedExit";
    public const string LevelCompleted = "LevelCompleted";
    public const string GameWon = "GameWon";
    public const string PhaseChanged = "PhaseChanged";
}
