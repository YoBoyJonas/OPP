using CastleEscape.Game.Content;

namespace CastleEscape.Game.World;

/// <summary>What a grid tile is made of.</summary>
public enum TerrainKind
{
    Floor,
    Wall,
    Water,
    Pit,

    /// <summary>The exit door; passable only while the level's <see cref="ExitDoor"/> is open.</summary>
    Door,

    /// <summary>Floor tile that counts as the exit (DOOR-2).</summary>
    Exit,
}

/// <summary>
/// One grid cell. P1 keeps a separate object per cell; P2's Flyweight will share these.
/// </summary>
public class Tile(TerrainKind terrain, Obstacle? obstacle = null)
{
    public TerrainKind Terrain { get; } = terrain;

    /// <summary>The obstacle definition for Wall, Water and Pit tiles (traversal rules, speed).</summary>
    public Obstacle? Obstacle { get; } = obstacle;
}
