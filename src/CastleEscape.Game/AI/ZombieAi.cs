using CastleEscape.Contracts;
using CastleEscape.Game.World;

namespace CastleEscape.Game.AI;

/// <summary>
/// Chooses a zombie's next step towards the nearest player (ZMB-1), re-evaluated every time the
/// zombie stands on a tile.
/// </summary>
public static class ZombieAi
{
    public static PlayerEntity? NearestPlayer(ZombieEntity zombie, IReadOnlyList<PlayerEntity> players)
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

    public static Direction NextStep(ZombieEntity zombie, LevelState level, IReadOnlyList<PlayerEntity> players)
    {
        var target = NearestPlayer(zombie, players);
        if (target is null || target.Tile == zombie.Tile)
        {
            return Direction.None;
        }

        switch (zombie.Definition.MovementStrategy)
        {
            case MovementStrategyKind.Greedy:
                // Step that most reduces Manhattan distance; stays put if no step gets closer.
                var best = Direction.None;
                var bestDistance = zombie.Tile.ManhattanTo(target.Tile);
                foreach (var direction in DirectionExtensions.Cardinal)
                {
                    var next = zombie.Tile.Step(direction);
                    var distance = next.ManhattanTo(target.Tile);
                    if (distance < bestDistance && MovementRules.CanZombieEnter(next, level))
                    {
                        best = direction;
                        bestDistance = distance;
                    }
                }
                return best;

            default:
                // Bfs. AStar and Predictive use the same search until they get their own algorithms.
                var path = PathFinder.ShortestPath(level.Grid, zombie.Tile, target.Tile,
                    pos => MovementRules.CanZombieEnter(pos, level));
                return path is { Count: > 0 } ? zombie.Tile.DirectionTo(path[0]) : Direction.None;
        }
    }
}
