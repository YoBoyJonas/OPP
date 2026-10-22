using CastleEscape.Contracts;

namespace CastleEscape.Game.World;

/// <summary>
/// An entity that moves tile by tile. A step goes from <see cref="Entity.Tile"/> to
/// <see cref="NextTile"/> while <see cref="Progress"/> runs from 0 to 1.
/// </summary>
public abstract class MovableEntity(string id, GridPos tile) : Entity(id, tile)
{
    public GridPos? NextTile { get; private set; }

    /// <summary>How far along the current step, 0..1.</summary>
    public double Progress { get; private set; }

    /// <summary>Direction of the current or last step.</summary>
    public Direction Facing { get; private set; } = Direction.Down;

    public bool IsMoving => NextTile is not null;

    /// <summary>Starts a step to the neighbouring tile. Rules must be checked by the caller.</summary>
    public void BeginStep(Direction direction)
    {
        if (direction == Direction.None)
        {
            throw new ArgumentException("A step needs a direction.", nameof(direction));
        }
        NextTile = Tile.Step(direction);
        Progress = 0;
        Facing = direction;
    }

    /// <summary>
    /// Moves along the current step by <paramref name="distance"/> tiles. Returns true on arrival;
    /// <paramref name="leftover"/> is the distance past the new tile, which the caller may spend on the next step.
    /// </summary>
    public bool Advance(double distance, out double leftover)
    {
        leftover = 0;
        if (NextTile is not { } next)
        {
            return false;
        }

        Progress += distance;
        if (Progress < 1)
        {
            return false;
        }

        leftover = Progress - 1;
        Tile = next;
        NextTile = null;
        Progress = 0;
        return true;
    }

    /// <summary>Abandons the current step; the entity stays on its origin tile.</summary>
    public void CancelStep()
    {
        NextTile = null;
        Progress = 0;
    }

    /// <summary>Moves instantly to a tile, cancelling any step (zombie reset, level start).</summary>
    public void TeleportTo(GridPos tile)
    {
        Tile = tile;
        CancelStep();
    }

    /// <summary>Interpolated position for smooth rendering.</summary>
    public (double X, double Y) RenderPosition => NextTile is { } next
        ? (Tile.X + (next.X - Tile.X) * Progress, Tile.Y + (next.Y - Tile.Y) * Progress)
        : (Tile.X, Tile.Y);
}
