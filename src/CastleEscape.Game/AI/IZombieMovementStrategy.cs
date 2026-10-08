using CastleEscape.Contracts;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.World;

namespace CastleEscape.Game.AI;

/// <summary>
/// How a zombie chases (ZMB-1). Called whenever the zombie stands on a tile; returns its next step,
/// or <see cref="Direction.None"/> to wait. Strategies are stateless, so one instance serves every zombie.
/// </summary>
[DesignPattern("Strategy", "Strategy")]
public interface IZombieMovementStrategy
{
    MovementStrategyKind Kind { get; }

    Direction NextStep(ZombieEntity zombie, WorldView world);
}

/// <summary>What a strategy may look at: the grid, the players, and where zombies may walk. Read-only.</summary>
[DesignPattern("Strategy", "Context data")]
public sealed class WorldView(LevelState level, IReadOnlyList<PlayerEntity> players)
{
    public Grid Grid => level.Grid;
    public IReadOnlyList<PlayerEntity> Players => players;

    public bool CanZombieEnter(GridPos pos) => MovementRules.CanZombieEnter(pos, level);

    /// <summary>The living player closest in a straight line (D16), or null if none.</summary>
    public PlayerEntity? NearestPlayer(ZombieEntity zombie)
    {
        var (zx, zy) = zombie.RenderPosition;
        return players
            .Where(p => !p.IsDead)
            .OrderBy(p =>
            {
                var (px, py) = p.RenderPosition;
                return (px - zx) * (px - zx) + (py - zy) * (py - zy);
            })
            .FirstOrDefault();
    }
}

/// <summary>
/// Shared shape of the chase strategies: find the nearest player, pick a goal tile (usually the
/// player's tile), then take one step towards it. Subclasses decide the step (and may pick another goal).
/// </summary>
public abstract class ChaseStrategy : IZombieMovementStrategy
{
    public abstract MovementStrategyKind Kind { get; }

    public Direction NextStep(ZombieEntity zombie, WorldView world)
    {
        var target = world.NearestPlayer(zombie);
        if (target is null || target.Tile == zombie.Tile)
        {
            return Direction.None;
        }
        return StepTowards(zombie, Goal(zombie, target, world), world);
    }

    /// <summary>Where to head for. Default: the tile the player is on.</summary>
    protected virtual GridPos Goal(ZombieEntity zombie, PlayerEntity target, WorldView world) => target.Tile;

    protected abstract Direction StepTowards(ZombieEntity zombie, GridPos goal, WorldView world);

    /// <summary>The first step of a path, or None if there is no path.</summary>
    protected static Direction FirstStep(ZombieEntity zombie, IReadOnlyList<GridPos>? path) =>
        path is { Count: > 0 } ? zombie.Tile.DirectionTo(path[0]) : Direction.None;
}
