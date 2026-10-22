using CastleEscape.Contracts;

namespace CastleEscape.Game.World;

/// <summary>
/// A tile coordinate. Origin is the top-left corner; x grows to the right, y grows down.
/// </summary>
public readonly record struct GridPos(int X, int Y)
{
    /// <summary>The neighbouring tile in a direction (<c>None</c> returns the same tile).</summary>
    public GridPos Step(Direction direction)
    {
        var (dx, dy) = direction.ToOffset();
        return new GridPos(X + dx, Y + dy);
    }

    public int ManhattanTo(GridPos other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    /// <summary>The direction of a neighbouring tile, or <c>None</c> if it is not adjacent.</summary>
    public Direction DirectionTo(GridPos neighbour) => (neighbour.X - X, neighbour.Y - Y) switch
    {
        (0, -1) => Direction.Up,
        (0, 1) => Direction.Down,
        (-1, 0) => Direction.Left,
        (1, 0) => Direction.Right,
        _ => Direction.None,
    };

    public override string ToString() => $"({X},{Y})";
}
