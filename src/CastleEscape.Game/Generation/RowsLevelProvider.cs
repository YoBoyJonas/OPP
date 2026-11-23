using CastleEscape.Game.Content;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>Hand-written maps as levels, for tests and demos.</summary>
public sealed class RowsLevelProvider(ContentCatalog catalog, params string[][] levels) : ILevelProvider
{
    public int? DefinitionIndex { get; init; }

    public LevelState CreateLevel(int levelIndex, int seed)
    {
        var rows = levels[Math.Min(levelIndex, levels.Length) - 1];
        var definition = catalog.GetLevel(DefinitionIndex ?? levelIndex);
        var builder = new PresetLevelBuilder(catalog, definition, ThemeFactories.For(definition.Theme, catalog), rows, seed);
        return new LevelDirector().Construct(builder);
    }
}
