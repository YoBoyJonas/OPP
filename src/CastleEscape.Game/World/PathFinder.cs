namespace CastleEscape.Game.World;

/// <summary>Breadth-first search over the grid. Neighbour order is fixed, so results are deterministic.</summary>
public static class PathFinder
{
    /// <summary>Step distance from <paramref name="from"/> to every reachable tile.</summary>
    public static Dictionary<GridPos, int> Distances(Grid grid, GridPos from, Func<GridPos, bool> passable)
    {
        var distances = new Dictionary<GridPos, int> { [from] = 0 };
        var frontier = new Queue<GridPos>();
        frontier.Enqueue(from);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var next in grid.Neighbours(current))
            {
                if (!distances.ContainsKey(next) && passable(next))
                {
                    distances[next] = distances[current] + 1;
                    frontier.Enqueue(next);
                }
            }
        }

        return distances;
    }

    /// <summary>
    /// Shortest path from <paramref name="from"/> to <paramref name="to"/>, excluding the start and including the goal.
    /// Null when unreachable; empty when already there. The goal itself need not be passable.
    /// </summary>
    public static IReadOnlyList<GridPos>? ShortestPath(Grid grid, GridPos from, GridPos to, Func<GridPos, bool> passable)
    {
        if (from == to)
        {
            return [];
        }

        var cameFrom = new Dictionary<GridPos, GridPos> { [from] = from };
        var frontier = new Queue<GridPos>();
        frontier.Enqueue(from);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var next in grid.Neighbours(current))
            {
                if (cameFrom.ContainsKey(next) || (next != to && !passable(next)))
                {
                    continue;
                }
                cameFrom[next] = current;
                if (next == to)
                {
                    return Unwind(cameFrom, from, to);
                }
                frontier.Enqueue(next);
            }
        }

        return null;
    }

    private static List<GridPos> Unwind(Dictionary<GridPos, GridPos> cameFrom, GridPos from, GridPos to)
    {
        var path = new List<GridPos>();
        for (var pos = to; pos != from; pos = cameFrom[pos])
        {
            path.Add(pos);
        }
        path.Reverse();
        return path;
    }
}
