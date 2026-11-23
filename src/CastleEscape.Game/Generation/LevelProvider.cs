using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>Picks the theme factory and the builder for a level and lets the director build it.</summary>
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
        catch (LevelBuildException ex)
        {
            errors = [ex.Message];
        }
        throw new LevelGenerationException(levelIndex, errors);
    }

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
            catch (LevelBuildException ex)
            {
                errors = [ex.Message];
            }
        }
        throw new LevelGenerationException(definition.Index, errors);
    }

    public LevelState BuildPreset(LevelDefinition definition, string presetName, int seed = 0)
    {
        IThemeFactory theme = ThemeFactories.For(definition.Theme, catalog);
        var builder = new PresetLevelBuilder(catalog, definition, theme, PresetMaps.ReadRows(presetName), seed);
        return _director.Construct(builder);
    }
}
