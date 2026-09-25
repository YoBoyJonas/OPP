using CastleEscape.Contracts;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.World;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Abstract Factory requirement: at least 2 concrete factories, at least 3 classes per family.</summary>
public sealed class AbstractFactoryDemo : IPatternDemo
{
    public string Key => "abstract-factory";

    public PatternDemoResponse Run(DemoOptions options)
    {
        var themeOption = options.Get("theme", "both");
        var themes = themeOption.Equals("both", StringComparison.OrdinalIgnoreCase)
            ? Enum.GetValues<LevelTheme>()
            : [Enum.Parse<LevelTheme>(themeOption, ignoreCase: true)];
        var seed = options.GetInt("seed", 7);
        var catalog = DemoWorld.Catalog;
        var generator = new LevelGenerator(catalog, new GameOptions(), new GenerationOptions());
        var trace = new DemoTrace("Abstract Factory");
        var families = new Dictionary<string, object?>();
        var mixed = 0;

        foreach (var theme in themes)
        {
            var factory = ThemeFactories.For(theme, catalog);
            var zombieType = catalog.Zombies.First(z => z.MovementStrategy == MovementStrategyKind.Greedy);
            var family = Family(factory, zombieType);
            trace.Line($"{factory.GetType().Name}: {Describe(family)}.");

            // A real level of this theme: every part must come from this one factory.
            var definition = catalog.Levels.First(l => l.Theme == theme && l.ZombieCount > 0);
            var level = generator.Generate(definition, seed);
            var used = PartsIn(level);
            var foreign = used.Except(family.Select(p => p.GetType().Name)).ToArray();
            mixed += foreign.Length;
            trace.Line($"  Level {definition.Index} ({theme}, seed {seed}) was built with it and contains: {string.Join(", ", used)}"
                       + (foreign.Length == 0 ? " (all from this family)." : $". FOREIGN: {string.Join(", ", foreign)}"));

            families[factory.GetType().Name] = family.Select(p => p switch
            {
                ZombieEntity z => new Dictionary<string, object?> { ["class"] = z.GetType().Name, ["speed"] = z.Speed, ["strategy"] = z.MovementStrategy.ToString() },
                Obstacle o => new Dictionary<string, object?> { ["class"] = o.GetType().Name, ["id"] = o.Id, ["moveSpeedMultiplier"] = o.MoveSpeedMultiplier },
                _ => null,
            }).ToArray();
        }

        trace.Evidence("families", families)
            .Evidence("classesPerFamily", 4)
            .Evidence("mixedParts", mixed);
        return trace.Done($"{themes.Length} concrete factor{(themes.Length == 1 ? "y" : "ies")}, 4 products each (wall, water, pit, zombie); "
                          + $"{mixed} parts from a foreign family.");
    }

    /// <summary>One of every product the factory makes.</summary>
    public static object[] Family(IThemeFactory factory, ZombieDefinition zombieType) =>
    [
        factory.CreateWall(),
        factory.CreateWater(),
        factory.CreatePit(),
        factory.CreateZombie("demo-zombie", zombieType, new GridPos(1, 1)),
    ];

    /// <summary>Class names of the obstacles and zombies actually placed in a level.</summary>
    public static string[] PartsIn(LevelState level) =>
        level.Grid.Positions().Select(p => level.Grid.GetTile(p).Obstacle).OfType<object>()
            .Concat(level.Zombies)
            .Select(o => o.GetType().Name)
            .Distinct()
            .Order()
            .ToArray();

    private static string Describe(object[] family) => string.Join(", ", family.Select(p => p switch
    {
        ZombieEntity z => $"{z.GetType().Name} (speed {z.Speed:0.##}, {z.MovementStrategy})",
        Obstacle o => $"{o.GetType().Name} (x{o.MoveSpeedMultiplier:0.##})",
        _ => p.GetType().Name,
    }));
}
