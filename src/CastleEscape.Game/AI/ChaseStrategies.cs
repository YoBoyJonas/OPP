using CastleEscape.Contracts;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.World;

namespace CastleEscape.Game.AI;

/// <summary>
/// Steps to the neighbour that most reduces the straight distance. Cheap and easy to fool:
/// it waits behind a wall when no step gets closer.
/// </summary>
[DesignPattern("Strategy", "ConcreteStrategy")]
public sealed class GreedyChaseStrategy : ChaseStrategy
{
    public override MovementStrategyKind Kind => MovementStrategyKind.Greedy;

    protected override Direction StepTowards(ZombieEntity zombie, GridPos goal, WorldView world)
    {
        var best = Direction.None;
        var bestDistance = zombie.Tile.ManhattanTo(goal);
        foreach (var direction in DirectionExtensions.Cardinal)
        {
            var next = zombie.Tile.Step(direction);
            var distance = next.ManhattanTo(goal);
            if (distance < bestDistance && world.CanZombieEnter(next))
            {
                best = direction;
                bestDistance = distance;
            }
        }
        return best;
    }
}

/// <summary>Follows a shortest path found by breadth-first search: never stuck, but searches in every direction.</summary>
[DesignPattern("Strategy", "ConcreteStrategy")]
public sealed class BfsChaseStrategy : ChaseStrategy
{
    public override MovementStrategyKind Kind => MovementStrategyKind.Bfs;

    protected override Direction StepTowards(ZombieEntity zombie, GridPos goal, WorldView world) =>
        FirstStep(zombie, PathFinder.ShortestPath(world.Grid, zombie.Tile, goal, world.CanZombieEnter));
}

/// <summary>Follows a shortest path found by A*: as short as BFS, but searches towards the player first.</summary>
[DesignPattern("Strategy", "ConcreteStrategy")]
public sealed class AStarChaseStrategy : ChaseStrategy
{
    public override MovementStrategyKind Kind => MovementStrategyKind.AStar;

    protected override Direction StepTowards(ZombieEntity zombie, GridPos goal, WorldView world) =>
        FirstStep(zombie, PathFinder.AStar(world.Grid, zombie.Tile, goal, world.CanZombieEnter, out _));
}

/// <summary>
/// Heads for where the player is going, not where the player is: up to <see cref="LookAhead"/> tiles
/// ahead in the player's direction while the player moves. Cuts players off instead of trailing them.
/// </summary>
[DesignPattern("Strategy", "ConcreteStrategy")]
public sealed class PredictiveChaseStrategy : ChaseStrategy
{
    public const int LookAhead = 3;

    public override MovementStrategyKind Kind => MovementStrategyKind.Predictive;

    protected override GridPos Goal(ZombieEntity zombie, PlayerEntity target, WorldView world)
    {
        if (target.NextTile is not { } next)
        {
            return target.Tile;
        }
        var goal = next;
        for (var i = 1; i < LookAhead && world.CanZombieEnter(goal.Step(target.Facing)); i++)
        {
            goal = goal.Step(target.Facing);
        }
        return goal == zombie.Tile ? target.Tile : goal;
    }

    protected override Direction StepTowards(ZombieEntity zombie, GridPos goal, WorldView world)
    {
        var step = FirstStep(zombie, PathFinder.AStar(world.Grid, zombie.Tile, goal, world.CanZombieEnter, out _));
        if (step != Direction.None)
        {
            return step;
        }
        // The predicted tile is unreachable: chase the player directly.
        var target = world.NearestPlayer(zombie)!;
        return FirstStep(zombie, PathFinder.AStar(world.Grid, zombie.Tile, target.Tile, world.CanZombieEnter, out _));
    }
}

/// <summary>The strategy objects, one per kind (they are stateless).</summary>
public static class ZombieStrategies
{
    public static IReadOnlyList<IZombieMovementStrategy> All { get; } =
        [new GreedyChaseStrategy(), new BfsChaseStrategy(), new AStarChaseStrategy(), new PredictiveChaseStrategy()];

    public static IZombieMovementStrategy For(MovementStrategyKind kind) =>
        All.FirstOrDefault(s => s.Kind == kind) ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, "No strategy of this kind.");
}
