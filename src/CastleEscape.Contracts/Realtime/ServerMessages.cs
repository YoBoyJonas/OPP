using System.Runtime.Serialization;
using CastleEscape.Contracts.Sessions;

namespace CastleEscape.Contracts.Realtime;

// Every message the server sends carries SessionId, Seq (monotonic per session) and Tick.
// [DataContract]/[DataMember] and concrete collection types (arrays, Dictionary) make the messages
// serializable to XML as well as JSON (NET-2; see the Adapter pattern: XmlMessageSerializerAdapter).

/// <summary>Sent when the lobby changes: a player joins, picks a character, connects, leaves; or the phase changes.</summary>
[DataContract(Namespace = Xml.Namespace)]
public sealed record SessionUpdatedMessage(
    [property: DataMember] Guid SessionId,
    [property: DataMember] long Seq,
    [property: DataMember] long Tick,
    [property: DataMember] SessionDto Session);

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
[DataContract(Namespace = Xml.Namespace)]
public sealed record LevelStartedMessage(
    [property: DataMember] Guid SessionId,
    [property: DataMember] long Seq,
    [property: DataMember] long Tick,
    [property: DataMember] int LevelIndex,
    [property: DataMember] LevelTheme Theme,
    [property: DataMember] int Width,
    [property: DataMember] int Height,
    [property: DataMember] string[] Rows,
    [property: DataMember] TickStateMessage State);

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
[DataContract(Namespace = Xml.Namespace)]
public sealed record TickStateMessage(
    [property: DataMember] Guid SessionId,
    [property: DataMember] long Seq,
    [property: DataMember] long Tick,
    [property: DataMember] SessionPhase Phase,
    [property: DataMember] int LevelIndex,
    [property: DataMember] bool DoorOpen,
    [property: DataMember] PlayerStateDto[] Players,
    [property: DataMember] ZombieStateDto[] Zombies,
    [property: DataMember] ItemStateDto[] Items,
    [property: DataMember] LeverStateDto[] Levers);

/// <summary>A player in the tick state. <c>X</c>/<c>Y</c> are interpolated for smooth rendering.</summary>
[DataContract(Namespace = Xml.Namespace)]
public sealed record PlayerStateDto(
    [property: DataMember] Guid PlayerId,
    [property: DataMember] int Slot,
    [property: DataMember] string Name,
    [property: DataMember] string CharacterId,
    [property: DataMember] int TileX,
    [property: DataMember] int TileY,
    [property: DataMember] double X,
    [property: DataMember] double Y,
    [property: DataMember] Direction Facing,
    [property: DataMember] bool IsMoving,
    [property: DataMember] int Lives,
    [property: DataMember] int MaxLives,
    [property: DataMember] int Score,
    [property: DataMember] ActivePowerDto[] Powers,
    [property: DataMember] PowerType[] Combos);

/// <summary>A power a player has right now.</summary>
/// <param name="Power">Jump, Sprint or Swim.</param>
/// <param name="RemainingSeconds">Time left.</param>
/// <param name="Level">Stack level: 1 for one pickup, +1 for each repeat pickup (PWR-3).</param>
[DataContract(Namespace = Xml.Namespace)]
public sealed record ActivePowerDto(
    [property: DataMember] PowerType Power,
    [property: DataMember] double RemainingSeconds,
    [property: DataMember] int Level);

/// <summary>A zombie in the tick state. <c>Strategy</c> is how it chases right now (it can be swapped at runtime).</summary>
[DataContract(Namespace = Xml.Namespace)]
public sealed record ZombieStateDto(
    [property: DataMember] string Id,
    [property: DataMember] string TypeId,
    [property: DataMember] int TileX,
    [property: DataMember] int TileY,
    [property: DataMember] double X,
    [property: DataMember] double Y,
    [property: DataMember] Direction Facing,
    [property: DataMember] MovementStrategyKind Strategy);

/// <summary>An item on the map. <c>Power</c> is set for Power items.</summary>
[DataContract(Namespace = Xml.Namespace)]
public sealed record ItemStateDto(
    [property: DataMember] string Id,
    [property: DataMember] string ConsumableId,
    [property: DataMember] ItemKind Kind,
    [property: DataMember] PowerType? Power,
    [property: DataMember] int X,
    [property: DataMember] int Y);

[DataContract(Namespace = Xml.Namespace)]
public sealed record LeverStateDto(
    [property: DataMember] string Id,
    [property: DataMember] int X,
    [property: DataMember] int Y,
    [property: DataMember] bool IsActive);

/// <summary>A gameplay fact (item collected, life lost, door opened…). See <c>GameEventTypes</c>.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Seq">Message sequence number.</param>
/// <param name="Tick">Server tick.</param>
/// <param name="Type">Event type, e.g. "LifeLost".</param>
/// <param name="PlayerId">The player concerned, if any.</param>
/// <param name="Message">Human-readable text.</param>
/// <param name="Data">Extra values as strings (e.g. "lives": "2").</param>
[DataContract(Namespace = Xml.Namespace)]
public sealed record GameEventMessage(
    [property: DataMember] Guid SessionId,
    [property: DataMember] long Seq,
    [property: DataMember] long Tick,
    [property: DataMember] string Type,
    [property: DataMember] Guid? PlayerId,
    [property: DataMember] string Message,
    [property: DataMember] Dictionary<string, string> Data);

/// <summary>Something went wrong for this client or session.</summary>
[DataContract(Namespace = Xml.Namespace)]
public sealed record ErrorMessage(
    [property: DataMember] Guid SessionId,
    [property: DataMember] long Seq,
    [property: DataMember] long Tick,
    [property: DataMember] GameErrorCode Code,
    [property: DataMember] string Message);

/// <summary>Reply to <c>Ping()</c>.</summary>
[DataContract(Namespace = Xml.Namespace)]
public sealed record PongMessage(
    [property: DataMember] Guid SessionId,
    [property: DataMember] long Seq,
    [property: DataMember] long Tick,
    [property: DataMember] DateTimeOffset ServerTimeUtc);

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
    public const string CommandUndone = "CommandUndone";
}

/// <summary>XML settings shared by the data contracts.</summary>
public static class Xml
{
    /// <summary>Namespace of every Castle Escape XML element.</summary>
    public const string Namespace = "urn:castle-escape";
}
