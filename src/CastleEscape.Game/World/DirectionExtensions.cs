using CastleEscape.Contracts;

namespace CastleEscape.Game.World;

public static class DirectionExtensions
{
    /// <summary>The four movement directions, in a fixed order (used for deterministic searches).</summary>
    public static readonly IReadOnlyList<Direction> Cardinal = [Direction.Up, Direction.Down, Direction.Left, Direction.Right];

    public static (int Dx, int Dy) ToOffset(this Direction direction) => direction switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        Direction.Right => (1, 0),
        _ => (0, 0),
    };

    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => Direction.None,
    };
}
