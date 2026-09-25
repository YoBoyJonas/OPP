namespace CastleEscape.Contracts.Gameplay;

/// <summary>Static layout of the current level (<c>GET /api/sessions/{id}/level</c>).</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="LevelIndex">1-10.</param>
/// <param name="Theme">Dungeon or Crypt.</param>
/// <param name="Seed">Generation seed (same seed, same layout).</param>
/// <param name="Width">Width in tiles.</param>
/// <param name="Height">Height in tiles.</param>
/// <param name="Rows">One string per row, map legend characters.</param>
/// <param name="Legend">Meaning of every legend character.</param>
public sealed record LevelLayoutResponse(
    Guid SessionId,
    int LevelIndex,
    LevelTheme Theme,
    int Seed,
    int Width,
    int Height,
    IReadOnlyList<string> Rows,
    IReadOnlyDictionary<string, string> Legend);

/// <summary>The "View game state" table (view-game-state activity diagram).</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Phase">Current phase.</param>
/// <param name="LevelIndex">Current level.</param>
/// <param name="MaxLevel">Levels in the run.</param>
/// <param name="Tick">Server tick.</param>
/// <param name="LevelElapsedSeconds">Time spent on the current level.</param>
/// <param name="LeversActive">Levers active right now.</param>
/// <param name="LeversTotal">Levers in the level.</param>
/// <param name="DoorOpen">Whether the exit door is open.</param>
/// <param name="ItemsRemaining">Items still on the map.</param>
/// <param name="Players">One row per player.</param>
public sealed record HudResponse(
    Guid SessionId,
    SessionPhase Phase,
    int LevelIndex,
    int MaxLevel,
    long Tick,
    double LevelElapsedSeconds,
    int LeversActive,
    int LeversTotal,
    bool DoorOpen,
    int ItemsRemaining,
    IReadOnlyList<HudPlayerDto> Players);

/// <summary>A player's row in the HUD table.</summary>
public sealed record HudPlayerDto(
    Guid PlayerId,
    int Slot,
    string Name,
    string CharacterId,
    int Lives,
    int MaxLives,
    int Score,
    IReadOnlyList<Realtime.ActivePowerDto> Powers,
    IReadOnlyList<PowerType> Combos,
    IReadOnlyDictionary<string, int> Stats);
