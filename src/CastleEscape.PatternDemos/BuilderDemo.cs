using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.PatternDemos;

public static class BuilderDemo
{
    public static void Run()
    {
        var catalog = ContentCatalog.Instance;
        var definition = catalog.GetLevel(1);
        var theme = ThemeFactories.For(definition.Theme, catalog);

        var director = new LevelDirector();
        ILevelBuilder[] builders =
        [
            new ProceduralLevelBuilder(catalog, definition, theme, seed: 7, new GameOptions(), new GenerationOptions()),
            new PresetLevelBuilder(catalog, definition, theme, PresetMaps.ReadRows("tutorial")),
        ];

        foreach (var builder in builders)
        {
            LevelState level = director.Construct(builder);

            Console.WriteLine($"{builder.GetType().Name}: {level.Grid.Width}x{level.Grid.Height}, {level.Levers.Count} levers, "
                              + $"{level.Items.Count} items, {level.Zombies.Count} zombies");
            foreach (var row in SnapshotMapper.ToRows(level))
            {
                Console.WriteLine("  " + row);
            }
            var errors = LevelValidator.Validate(level, new GenerationOptions().MinZombieDistanceFromStart);
            Console.WriteLine(errors.Count == 0 ? "  valid level" : "  invalid: " + string.Join("; ", errors));
            Console.WriteLine();
        }
    }
}
