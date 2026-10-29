using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Builder. Builds one level step by step; <see cref="LevelDirector"/> calls the steps in a fixed order.
/// Each concrete builder decides how to do each step (random generation, or reading a hand-made map).
/// A builder makes one level: create a new one for every level.
/// A step that can't be done throws <see cref="LevelBuildException"/>.
/// </summary>
public interface ILevelBuilder
{
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

    /// <summary>The finished level.</summary>
    LevelState GetResult();
}

/// <summary>A build step could not be completed (e.g. no room for the levers).</summary>
public sealed class LevelBuildException(string message) : Exception(message);
