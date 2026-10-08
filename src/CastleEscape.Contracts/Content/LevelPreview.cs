using CastleEscape.Contracts.Gameplay;

namespace CastleEscape.Contracts.Content;

/// <summary>A level built without a session, for previewing (<c>GET /api/levels/{index}/preview</c>).</summary>
/// <param name="LevelIndex">1-10.</param>
/// <param name="Theme">Dungeon or Crypt (picks the Abstract Factory).</param>
/// <param name="Builder">The concrete Builder used: ProceduralLevelBuilder or PresetLevelBuilder.</param>
/// <param name="RequestedSeed">The seed asked for.</param>
/// <param name="Seed">The seed of the attempt that produced this level (a retry adds 7919 per attempt).</param>
/// <param name="Width">Width in tiles.</param>
/// <param name="Height">Height in tiles.</param>
/// <param name="Rows">Map legend rows.</param>
/// <param name="Obstacles">The themed obstacle variants used.</param>
/// <param name="Zombies">Zombie class and chase strategy per zombie.</param>
/// <param name="Valid">True when <paramref name="ValidationErrors"/> is empty.</param>
/// <param name="ValidationErrors">Broken generation rules (GEN-1..3, LVL-3, D5); empty for a valid level.</param>
public sealed record LevelPreviewResponse(
    int LevelIndex,
    LevelTheme Theme,
    string Builder,
    int RequestedSeed,
    int Seed,
    int Width,
    int Height,
    string[] Rows,
    LevelObstacleDto[] Obstacles,
    PreviewZombieDto[] Zombies,
    bool Valid,
    string[] ValidationErrors);

/// <summary>A zombie in a preview.</summary>
public sealed record PreviewZombieDto(string Id, string TypeId, string Variant, MovementStrategyKind Strategy, int X, int Y);
