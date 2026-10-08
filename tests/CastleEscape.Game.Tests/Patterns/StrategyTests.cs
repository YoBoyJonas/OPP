using CastleEscape.Contracts;
using CastleEscape.Game.AI;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class StrategyTests
{
    private static ContentCatalog Catalog => ContentCatalog.Instance;

    private static LevelState Level(string[] rows) =>
        new LevelDirector(Catalog, new GenerationOptions()).Assemble(new PresetLevelBuilder(Catalog, rows), Catalog.GetLevel(1), seed: 0);

    private static (ZombieEntity Zombie, PlayerEntity Player, WorldView World) Setup(string[] rows)
    {
        var level = Level(rows);
        var player = new PlayerEntity(Guid.NewGuid(), "Ana", Catalog.GetCharacter("scout"), level.StartTiles[0]);
        return (level.Zombies[0], player, new WorldView(level, [player]));
    }

    /// <summary>Lets the zombie walk (teleporting tile by tile) until it waits, arrives, or runs out of steps.</summary>
    private static int Walk(ZombieEntity zombie, PlayerEntity player, WorldView world, int maxSteps = 40)
    {
        var steps = 0;
        while (steps < maxSteps && zombie.Tile != player.Tile)
        {
            var direction = zombie.Strategy.NextStep(zombie, world);
            if (direction == Direction.None)
            {
                break;
            }
            zombie.TeleportTo(zombie.Tile.Step(direction));
            steps++;
        }
        return steps;
    }

    [Fact]
    public void AtLeastFourStrategyClasses()
    {
        var strategies = typeof(IZombieMovementStrategy).Assembly.GetTypes()
            .Where(t => typeof(IZombieMovementStrategy).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .ToList();

        Assert.True(strategies.Count >= 4);
        Assert.Equal(Enum.GetValues<MovementStrategyKind>().Length, ZombieStrategies.All.Select(s => s.Kind).Distinct().Count());
    }

    [Fact]
    public void Greedy_GetsStuckBehindAWall_PathFindersGoAround()
    {
        foreach (var kind in Enum.GetValues<MovementStrategyKind>())
        {
            var (zombie, player, world) = Setup(StrategyDemo.Map);
            zombie.Strategy = ZombieStrategies.For(kind);

            Walk(zombie, player, world);

            if (kind == MovementStrategyKind.Greedy)
            {
                Assert.NotEqual(player.Tile, zombie.Tile);
                Assert.Equal(Direction.None, zombie.Strategy.NextStep(zombie, world));
            }
            else
            {
                Assert.Equal(player.Tile, zombie.Tile);
            }
        }
    }

    [Fact]
    public void AStar_IsAsShortAsBfs_AndSearchesLess()
    {
        var level = Level(StrategyDemo.Map);
        bool Passable(GridPos p) => MovementRules.CanZombieEnter(p, level);
        var from = level.Zombies[0].Tile;
        var to = level.StartTiles[0];

        var bfs = PathFinder.ShortestPath(level.Grid, from, to, Passable, out var bfsExpanded);
        var astar = PathFinder.AStar(level.Grid, from, to, Passable, out var astarExpanded);

        Assert.Equal(bfs!.Count, astar!.Count);
        Assert.True(astarExpanded < bfsExpanded);
    }

    [Fact]
    public void Predictive_HeadsWhereThePlayerIsGoing()
    {
        var (zombie, player, world) = Setup(
        [
            "##########",
            "#....1...#",
            "#........#",
            "#........#",
            "#......Z.#",
            "#.......2#",
            "##########",
        ]);
        player.BeginStep(Direction.Down); // from (5,1): predicted goal (5,4), straight left of the zombie

        zombie.Strategy = ZombieStrategies.For(MovementStrategyKind.Predictive);
        var predictive = zombie.Strategy.NextStep(zombie, world);
        zombie.Strategy = ZombieStrategies.For(MovementStrategyKind.AStar);
        var direct = zombie.Strategy.NextStep(zombie, world);

        Assert.Equal(Direction.Left, predictive);
        Assert.Equal(Direction.Up, direct); // A* towards the player's current tile tries up first
    }

    [Fact]
    public void Strategy_StartsFromTheZombieType_AndCanBeSwappedAtRuntime()
    {
        var hunter = Catalog.Zombies.First(z => z.MovementStrategy == MovementStrategyKind.Bfs);
        var greedy = Catalog.Zombies.First(z => z.MovementStrategy == MovementStrategyKind.Greedy);
        var dungeon = new DungeonZombie("z1", hunter, new GridPos(1, 1));
        var crypt = new CryptZombie("z2", greedy, new GridPos(1, 1));

        Assert.IsType<BfsChaseStrategy>(dungeon.Strategy);
        Assert.IsType<BfsChaseStrategy>(crypt.Strategy); // the Crypt theme upgrades Greedy

        dungeon.Strategy = new PredictiveChaseStrategy();

        Assert.Equal(MovementStrategyKind.Predictive, dungeon.Strategy.Kind);
    }

    [Fact]
    public void TickState_ReportsEachZombiesCurrentStrategy()
    {
        var level = Level(StrategyDemo.Map);
        level.Zombies[0].Strategy = ZombieStrategies.For(MovementStrategyKind.AStar);
        var (p1, p2) = DemoWorld.TwoPlayers(level);

        var state = SnapshotMapper.ToTickState(Guid.NewGuid(), 1, 1, SessionPhase.Playing, level, [p1, p2]);

        Assert.Equal(MovementStrategyKind.AStar, state.Zombies[0].Strategy);
    }

    [Fact]
    public void Demo_RunsAllFourStrategies()
    {
        var result = new StrategyDemo().Run(DemoOptions.None);

        var runs = Assert.IsType<Dictionary<string, object?>>(result.Evidence["runs"]);
        Assert.Equal(4, runs.Count);
    }
}
