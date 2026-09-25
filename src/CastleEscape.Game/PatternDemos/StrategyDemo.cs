using CastleEscape.Contracts;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Game.AI;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.World;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Strategy requirement: at least 4 strategy classes, swappable at runtime.</summary>
public sealed class StrategyDemo : IPatternDemo
{
    public string Key => "strategy";

    /// <summary>A wall between the zombie (Z) and the player (1), who walks down towards the gap row.</summary>
    public static readonly string[] Map =
    [
        "############",
        "#Z....#....#",
        "#.....#.1..#",
        "#.....#....#",
        "#..........#",
        "#.......2..#",
        "############",
    ];

    public PatternDemoResponse Run(DemoOptions options)
    {
        var maxSteps = Math.Clamp(options.GetInt("steps", 12), 1, 60);
        var only = options.Get("strategy", "all");
        var strategies = ZombieStrategies.All
            .Where(s => only.Equals("all", StringComparison.OrdinalIgnoreCase) || s.Kind.ToString().Equals(only, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var trace = new DemoTrace("Strategy");
        var runs = new Dictionary<string, object?>();

        trace.Line("Map: zombie at (1,1), wall at x=6 with a gap at y=4; player 1 at (8,2) walking down (player 2 is far away).");
        foreach (var strategy in strategies)
        {
            var (level, player) = Setup();
            var zombie = level.Zombies[0];
            zombie.Strategy = strategy;                       // the swap: same zombie, another algorithm
            var world = new WorldView(level, [player]);

            var path = new List<GridPos> { zombie.Tile };
            for (var i = 0; i < maxSteps && zombie.Tile != player.Tile; i++)
            {
                var direction = zombie.Strategy.NextStep(zombie, world);
                if (direction == Direction.None)
                {
                    break;
                }
                zombie.TeleportTo(zombie.Tile.Step(direction));
                path.Add(zombie.Tile);
            }

            var distance = zombie.Tile.ManhattanTo(player.Tile);
            trace.Line($"{strategy.GetType().Name,-24} {path.Count - 1,2} steps: {string.Join(" ", path.Select(p => $"({p.X},{p.Y})"))} -> "
                       + (path.Count == 1 ? "did not move (no step gets closer)" : $"{distance} tiles from the player"));
            runs[strategy.Kind.ToString()] = new Dictionary<string, object?>
            {
                ["class"] = strategy.GetType().Name,
                ["steps"] = path.Count - 1,
                ["path"] = path.Select(p => new[] { p.X, p.Y }).ToArray(),
                ["endDistance"] = distance,
            };
        }

        var (searchLevel, searchPlayer) = Setup();
        var from = searchLevel.Zombies[0].Tile;
        bool Passable(GridPos p) => MovementRules.CanZombieEnter(p, searchLevel);
        var bfs = PathFinder.ShortestPath(searchLevel.Grid, from, searchPlayer.Tile, Passable, out var bfsExpanded);
        var astar = PathFinder.AStar(searchLevel.Grid, from, searchPlayer.Tile, Passable, out var astarExpanded);
        trace.Line($"Search cost for the same route: BFS expanded {bfsExpanded} tiles, A* {astarExpanded} (both paths {bfs?.Count} / {astar?.Count} steps).");

        trace.Evidence("strategies", ZombieStrategies.All.Select(s => s.GetType().Name).ToArray())
            .Evidence("runs", runs)
            .Evidence("searchCost", new Dictionary<string, int> { ["bfsExpanded"] = bfsExpanded, ["aStarExpanded"] = astarExpanded });
        return trace.Done($"{ZombieStrategies.All.Count} strategies behind IZombieMovementStrategy; the zombie and the session code "
                          + "are the same for all of them, only zombie.Strategy changes.");
    }

    private static (LevelState Level, PlayerEntity Player) Setup()
    {
        var catalog = DemoWorld.Catalog;
        var level = new LevelDirector(catalog, new GenerationOptions())
            .Assemble(new PresetLevelBuilder(catalog, Map), catalog.GetLevel(1), seed: 0);
        var player = new PlayerEntity(Guid.NewGuid(), "Ana", catalog.GetCharacter("scout"), level.StartTiles[0]);
        player.BeginStep(Direction.Down); // walking towards the gap: the predictive zombie aims ahead of her
        return (level, player);
    }
}
