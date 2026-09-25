using CastleEscape.Contracts.Patterns;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Builder requirement: at least 2 concrete builders.</summary>
public sealed class BuilderDemo : IPatternDemo
{
    public string Key => "builder";

    public PatternDemoResponse Run(DemoOptions options)
    {
        var catalog = DemoWorld.Catalog;
        var definition = catalog.GetLevel(Math.Clamp(options.GetInt("level", 1), 1, catalog.Levels.Count));
        var seed = options.GetInt("seed", 7);
        var preset = options.Get("preset", "tutorial");
        var generation = new GenerationOptions();
        var director = new LevelDirector(catalog, generation);
        var trace = new DemoTrace("Builder");

        ILevelBuilder[] builders =
        [
            new ProceduralLevelBuilder(catalog, new GameOptions(), generation),
            PresetLevelBuilder.FromPreset(catalog, preset),
        ];

        trace.Line($"LevelDirector runs the same steps for every builder: Reset -> {string.Join(" -> ", LevelDirector.Steps)} -> GetResult, "
                   + "then validates (GEN-1..3, LVL-3, D5) and retries random builders with a new seed.");
        var results = new Dictionary<string, object?>();
        foreach (var builder in builders)
        {
            var level = director.Construct(builder, definition, seed);
            var name = builder.GetType().Name;
            trace.Line($"{name}: level {definition.Index} ({level.Theme}), {Describe(level)}; "
                       + $"{(builder.IsRandom ? $"seed {seed} -> built with seed {level.Seed}" : "seed ignored, one attempt")}.");
            results[name] = new Dictionary<string, object?>
            {
                ["seed"] = level.Seed,
                ["size"] = $"{level.Grid.Width}x{level.Grid.Height}",
                ["levers"] = level.Levers.Count,
                ["items"] = level.Items.Count,
                ["zombies"] = level.Zombies.Count,
                ["validationErrors"] = LevelValidator.Validate(level, generation.MinZombieDistanceFromStart).Count,
                ["rows"] = SnapshotMapper.ToRows(level),
            };
        }

        trace.Evidence("steps", LevelDirector.Steps).Evidence("builders", results);
        return trace.Done($"{builders.Length} concrete builders, one director: same steps, different levels (a random map and the '{preset}' preset).");
    }

    private static string Describe(LevelState level)
    {
        var powerTiles = level.Grid.Positions().Count(p => level.Grid.GetTerrain(p) is TerrainKind.Water or TerrainKind.Pit);
        return $"{level.Grid.Width}x{level.Grid.Height}, {level.Levers.Count} levers, {level.ExitTiles.Count} exit tiles, "
               + $"{powerTiles} water/pit tiles, {level.Items.Count} items, {level.Zombies.Count} zombies";
    }
}
