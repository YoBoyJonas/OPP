using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Default level source: generates each level, or loads the configured preset map for every level
/// (<c>Generation:PresetLevel</c>) when testing.
/// </summary>
public class LevelProvider(ContentCatalog catalog, GameOptions game, GenerationOptions generation) : ILevelProvider
{
    private readonly LevelGenerator _generator = new(catalog, game, generation);

    public LevelState CreateLevel(int levelIndex, int seed)
    {
        var definition = catalog.GetLevel(levelIndex);

        if (string.IsNullOrWhiteSpace(generation.PresetLevel))
        {
            return _generator.Generate(definition, seed);
        }

        var level = PresetLevelParser.Parse(PresetLevelParser.ReadRows(generation.PresetLevel), definition, catalog, seed);
        var errors = LevelValidator.Validate(level, generation.MinZombieDistanceFromStart);
        if (errors.Count > 0)
        {
            throw new LevelGenerationException(levelIndex, errors);
        }
        return level;
    }
}
