using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Where a session's levels come from, and the client of both creational patterns:
/// it picks the theme factory (Abstract Factory) and the builder (Builder), and lets the director build.
/// Normally levels are generated; with <c>Generation:PresetLevel</c> set, every level is that preset map.
/// </summary>
public class LevelProvider(ContentCatalog catalog, GameOptions game, GenerationOptions generation) : ILevelProvider
{
    private readonly LevelDirector _director = new();

    public LevelState CreateLevel(int levelIndex, int seed)
    {
        var definition = catalog.GetLevel(levelIndex);
        if (string.IsNullOrWhiteSpace(generation.PresetLevel))
        {
            return Generate(definition, seed);
        }

        List<string> errors;
        try
        {
            var level = BuildPreset(definition, generation.PresetLevel, seed);
            errors = LevelValidator.Validate(level, generation.MinZombieDistanceFromStart);
            if (errors.Count == 0)
            {
                return level;
            }
        }
        catch (LevelBuildException ex) // a bad preset name or map
        {
            errors = [ex.Message];
        }
        throw new LevelGenerationException(levelIndex, errors);
    }

    /// <summary>
    /// A random level that passes validation. A failed attempt is retried with another seed
    /// (start-level activity diagram: generate, validate, retry).
    /// </summary>
    /// <exception cref="LevelGenerationException">No valid level after <c>Generation:MaxAttempts</c> attempts.</exception>
    public LevelState Generate(LevelDefinition definition, int seed)
    {
        IThemeFactory theme = ThemeFactories.For(definition.Theme, catalog);
        List<string> errors = [];
        for (var attempt = 0; attempt < generation.MaxAttempts; attempt++)
        {
            var attemptSeed = unchecked(seed + attempt * 7919);
            var builder = new ProceduralLevelBuilder(catalog, definition, theme, attemptSeed, game, generation);
            try
            {
                var level = _director.Construct(builder);
                errors = LevelValidator.Validate(level, generation.MinZombieDistanceFromStart);
                if (errors.Count == 0)
                {
                    return level;
                }
            }
            catch (LevelBuildException ex) // a step found no room; try another seed
            {
                errors = [ex.Message];
            }
        }
        throw new LevelGenerationException(definition.Index, errors);
    }

    /// <summary>The preset map <c>content/presets/{name}.txt</c>, built exactly as drawn (not validated).</summary>
    /// <exception cref="LevelBuildException">Unknown preset, or the map is broken.</exception>
    public LevelState BuildPreset(LevelDefinition definition, string presetName, int seed = 0)
    {
        IThemeFactory theme = ThemeFactories.For(definition.Theme, catalog);
        var builder = new PresetLevelBuilder(catalog, definition, theme, PresetMaps.ReadRows(presetName), seed);
        return _director.Construct(builder);
    }
}
