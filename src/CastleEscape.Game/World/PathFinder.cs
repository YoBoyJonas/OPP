namespace CastleEscape.Game.World;

/// <summary>Searches over the grid (BFS and A*). Neighbour order is fixed, so results are deterministic.</summary>
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
    public static IReadOnlyList<GridPos>? ShortestPath(Grid grid, GridPos from, GridPos to, Func<GridPos, bool> passable) =>
        ShortestPath(grid, from, to, passable, out _);

    /// <inheritdoc cref="ShortestPath(Grid, GridPos, GridPos, Func{GridPos, bool})"/>
    /// <param name="expanded">How many tiles the search took from its queue (its cost).</param>
    public static IReadOnlyList<GridPos>? ShortestPath(Grid grid, GridPos from, GridPos to, Func<GridPos, bool> passable, out int expanded)
    {
        expanded = 0;
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
            expanded++;
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

    /// <summary>
    /// A* search with the Manhattan distance as heuristic: as short as BFS, but it looks at tiles
    /// towards the goal first, so it usually expands far fewer. Same contract as <see cref="ShortestPath(Grid, GridPos, GridPos, Func{GridPos, bool})"/>.
    /// </summary>
    public static IReadOnlyList<GridPos>? AStar(Grid grid, GridPos from, GridPos to, Func<GridPos, bool> passable, out int expanded)
    {
        expanded = 0;
        if (from == to)
        {
            return [];
        }

        var cameFrom = new Dictionary<GridPos, GridPos> { [from] = from };
        var cost = new Dictionary<GridPos, int> { [from] = 0 };
        var closed = new HashSet<GridPos>();
        var open = new PriorityQueue<GridPos, (int F, int H, long Order)>();
        long order = 0;
        open.Enqueue(from, (from.ManhattanTo(to), from.ManhattanTo(to), order++));

        while (open.TryDequeue(out var current, out _))
        {
            if (current == to)
            {
                return Unwind(cameFrom, from, to);
            }
            if (!closed.Add(current))
            {
                continue; // a stale queue entry
            }
            expanded++;

            foreach (var next in grid.Neighbours(current))
            {
                if (closed.Contains(next) || (next != to && !passable(next)))
                {
                    continue;
                }
                var nextCost = cost[current] + 1;
                if (cost.TryGetValue(next, out var known) && known <= nextCost)
                {
                    continue;
                }
                cost[next] = nextCost;
                cameFrom[next] = current;
                var h = next.ManhattanTo(to);
                open.Enqueue(next, (nextCost + h, h, order++));
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
