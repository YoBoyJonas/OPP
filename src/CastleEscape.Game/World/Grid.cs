using CastleEscape.Game.Content;

namespace CastleEscape.Game.World;

/// <summary>
/// The level's tiles. All tile access goes through these methods; the array never leaves this class
/// (seam for P2 Flyweight and Iterator).
/// </summary>
public class Grid
{
    private readonly Tile[,] _tiles;

    public Grid(int width, int height)
    {
        if (width < 1 || height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Grid must be at least 1x1.");
        }

        Width = width;
        Height = height;
        _tiles = new Tile[width, height];
        foreach (var pos in Positions())
        {
            _tiles[pos.X, pos.Y] = new Tile(TerrainKind.Floor);
        }
    }

    public int Width { get; }
    public int Height { get; }

    public bool InBounds(GridPos pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height;

    /// <summary>The tile at a position; outside the grid everything is wall.</summary>
    public Tile GetTile(GridPos pos) => InBounds(pos) ? _tiles[pos.X, pos.Y] : OutOfBounds;

    public TerrainKind GetTerrain(GridPos pos) => GetTile(pos).Terrain;

    public void SetTile(GridPos pos, TerrainKind terrain, Obstacle? obstacle = null)
    {
        if (!InBounds(pos))
        {
            throw new ArgumentOutOfRangeException(nameof(pos), $"{pos} is outside the {Width}x{Height} grid.");
        }
        _tiles[pos.X, pos.Y] = new Tile(terrain, obstacle);
    }

    /// <summary>Every position, row by row (y outer, x inner).</summary>
    public IEnumerable<GridPos> Positions()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                yield return new GridPos(x, y);
            }
        }
    }

    /// <summary>The in-bounds neighbours of a tile, in <see cref="DirectionExtensions.Cardinal"/> order.</summary>
    public IEnumerable<GridPos> Neighbours(GridPos pos) =>
        DirectionExtensions.Cardinal.Select(pos.Step).Where(InBounds);

    private static readonly Tile OutOfBounds = new(TerrainKind.Wall);
}
