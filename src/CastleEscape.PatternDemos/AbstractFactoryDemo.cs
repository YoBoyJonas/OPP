using CastleEscape.Contracts;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.World;

namespace CastleEscape.PatternDemos;

/// <summary>Abstract Factory requirement: at least 2 concrete factories, at least 3 classes per family.</summary>
public static class AbstractFactoryDemo
{
    public static void Run()
    {
        var catalog = ContentCatalog.Instance;
        var zombieType = catalog.Zombies.First(z => z.MovementStrategy == MovementStrategyKind.Greedy);

        // 1. Each concrete factory makes its own family of 4 products.
        IThemeFactory[] factories =
        [
            ThemeFactories.For(LevelTheme.Dungeon, catalog),
            ThemeFactories.For(LevelTheme.Crypt, catalog),
        ];
        foreach (var factory in factories)
        {
            Console.WriteLine(factory.GetType().Name + ":");
            PrintFamily(factory, zombieType);
        }

        // 2. In the game, the level's theme picks the factory, and the whole level is built with that one factory.
        Console.WriteLine();
        var levels = new LevelProvider(catalog, new GameOptions(), new GenerationOptions());
        foreach (var index in new[] { 1, 6 })
        {
            var level = levels.CreateLevel(index, seed: 7);
            var parts = level.Grid.Positions()
                .Select(p => level.Grid.GetTile(p).Obstacle?.GetType().Name)
                .Concat(level.Zombies.Select(z => z.GetType().Name))
                .OfType<string>()
                .Distinct();
            Console.WriteLine($"Level {index} ({level.Theme}) contains: {string.Join(", ", parts)}");
        }
    }

    /// <summary>Client code: it only knows <see cref="IThemeFactory"/> and the abstract products.</summary>
    private static void PrintFamily(IThemeFactory factory, ZombieDefinition zombieType)
    {
        Wall wall = factory.CreateWall();
        Water water = factory.CreateWater();
        Pit pit = factory.CreatePit();
        ZombieEntity zombie = factory.CreateZombie("demo-zombie", zombieType, new GridPos(1, 1));

        Console.WriteLine($"  wall:   {wall.GetType().Name}");
        Console.WriteLine($"  water:  {water.GetType().Name} (speed x{water.MoveSpeedMultiplier})");
        Console.WriteLine($"  pit:    {pit.GetType().Name} (speed x{pit.MoveSpeedMultiplier})");
        Console.WriteLine($"  zombie: {zombie.GetType().Name} (speed {zombie.Speed:0.##}, chases with {zombie.MovementStrategy})");
    }
}
