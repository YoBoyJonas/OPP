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
/// <param name="Obstacles">The themed obstacle variants this level uses (one family per theme).</param>
public sealed record LevelLayoutResponse(
    Guid SessionId,
    int LevelIndex,
    LevelTheme Theme,
    int Seed,
    int Width,
    int Height,
    string[] Rows,
    Dictionary<string, string> Legend,
    LevelObstacleDto[] Obstacles);

/// <summary>An obstacle variant used in a level, e.g. Crypt's poison water.</summary>
/// <param name="Legend">Map legend character of its terrain.</param>
/// <param name="Id">Variant id, e.g. <c>poison-water</c>.</param>
/// <param name="Name">Display name.</param>
/// <param name="Variant">Class name of the themed variant, e.g. <c>PoisonWater</c>.</param>
/// <param name="RequiredPower">Power needed to enter it; <c>None</c> for walls.</param>
/// <param name="MoveSpeedMultiplier">Speed factor while crossing it.</param>
public sealed record LevelObstacleDto(string Legend, string Id, string Name, string Variant, PowerType RequiredPower, double MoveSpeedMultiplier);

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
    HudPlayerDto[] Players);

/// <summary>A player's row in the HUD table.</summary>
public sealed record HudPlayerDto(
    Guid PlayerId,
    int Slot,
    string Name,
    string CharacterId,
    int Lives,
    int MaxLives,
    int Score,
    Realtime.ActivePowerDto[] Powers,
    PowerType[] Combos,
    Dictionary<string, int> Stats);
