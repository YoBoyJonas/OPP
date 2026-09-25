using CastleEscape.Game.Content;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Builds a level step by step. <see cref="LevelDirector"/> calls the steps in a fixed order; each
/// builder decides how to do them (random generation, or a hand-made map).
/// A step that can't be done throws <see cref="LevelBuildException"/>.
/// </summary>
[DesignPattern("Builder", "Builder")]
public interface ILevelBuilder
{
    /// <summary>True if the result depends on the seed, so a failed attempt is worth retrying with another one.</summary>
    bool IsRandom { get; }

    /// <summary>Starts a new level. The theme factory makes the walls, water, pits and zombies (Abstract Factory).</summary>
    void Reset(LevelDefinition definition, int seed, IThemeFactory theme);

    /// <summary>Grid, outer walls and inner walls.</summary>
    void BuildTerrain();

    /// <summary>The exit tiles and the door in front of them.</summary>
    void PlaceExitAndDoor();

    /// <summary>The two distinct start tiles (LVL-3).</summary>
    void PlaceStartTiles();

    /// <summary>At least two levers (GEN-3).</summary>
    void PlaceLevers();

    /// <summary>Water and pits that need a power (GEN-3), never on the critical path (D5).</summary>
    void PlacePowerObstacles();

    /// <summary>Items, through the item spawners (Factory Method).</summary>
    void PlaceItems();

    /// <summary>Zombies, away from the start tiles.</summary>
    void PlaceZombies();

    LevelState GetResult();
}

/// <summary>A build step could not be completed (e.g. no room for the levers).</summary>
public sealed class LevelBuildException(string message) : Exception(message);
