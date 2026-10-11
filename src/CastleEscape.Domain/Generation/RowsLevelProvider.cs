using CastleEscape.Domain.Configuration;
using CastleEscape.Domain.Content;
using CastleEscape.Domain.World;

namespace CastleEscape.Domain.Generation;

/// <summary>
/// Serves hand-written ASCII maps as levels, one per level index (the last map repeats). Not validated,
/// so maps can be tiny. For tests and pattern demos; the server uses <c>LevelProvider</c>.
/// </summary>
public sealed class RowsLevelProvider(IContentCatalog catalog, params string[][] levels) : ILevelProvider
{
    /// <summary>Use this level definition (theme, first zombie type) instead of the level being loaded.</summary>
    public int? DefinitionIndex { get; init; }

    public LevelState CreateLevel(int levelIndex, int seed)
    {
        var rows = levels[Math.Min(levelIndex, levels.Length) - 1];
        return new LevelDirector(catalog, new GenerationOptions())
            .Assemble(new PresetLevelBuilder(catalog, rows), catalog.GetLevel(DefinitionIndex ?? levelIndex), seed);
    }
}
