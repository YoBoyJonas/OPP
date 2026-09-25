using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Default level source: the director builds each level with the procedural builder, or with the
/// preset builder for every level when <c>Generation:PresetLevel</c> is set (tests, demos).
/// </summary>
public class LevelProvider(ContentCatalog catalog, GameOptions game, GenerationOptions generation) : ILevelProvider
{
    private readonly LevelDirector _director = new(catalog, generation);

    public LevelState CreateLevel(int levelIndex, int seed)
    {
        var definition = catalog.GetLevel(levelIndex);
        try
        {
            return _director.Construct(CreateBuilder(), definition, seed);
        }
        catch (LevelBuildException ex) // a bad preset name or map
        {
            throw new LevelGenerationException(levelIndex, [ex.Message]);
        }
    }

    /// <summary>Builders keep per-level state, so each level gets a new one.</summary>
    private ILevelBuilder CreateBuilder() =>
        string.IsNullOrWhiteSpace(generation.PresetLevel)
            ? new ProceduralLevelBuilder(catalog, game, generation)
            : PresetLevelBuilder.FromPreset(catalog, generation.PresetLevel);
}
