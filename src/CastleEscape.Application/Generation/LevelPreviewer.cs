using CastleEscape.Domain.Generation;
using Direction = CastleEscape.Domain.Direction;
using PowerType = CastleEscape.Domain.PowerType;
using MovementStrategyKind = CastleEscape.Domain.MovementStrategyKind;
using SessionPhase = CastleEscape.Domain.SessionPhase;
using CastleEscape.Contracts.Content;
using CastleEscape.Application.Configuration;
using CastleEscape.Domain.Configuration;
using CastleEscape.Domain.Content;
using CastleEscape.Application.Sessions;
using CastleEscape.Application.Snapshots;
using CastleEscape.Domain.Sessions;
using CastleEscape.Domain.World;

namespace CastleEscape.Application.Generation;

/// <summary>Builds levels outside any session so they can be looked at (content design, tests, the defence).</summary>
public sealed class LevelPreviewer(IContentCatalog catalog, GameOptions game, GenerationOptions generation, IPresetMapSource presets)
{
    /// <summary>
    /// Level <paramref name="index"/> from the procedural builder (validated, retried), or from a preset map
    /// (built as drawn and then checked, so a broken preset shows its errors instead of failing).
    /// </summary>
    /// <exception cref="GameException">Unknown level or preset, or the procedural builder found no valid level.</exception>
    public LevelPreviewResponse Preview(int index, int seed, string? preset = null)
    {
        var definition = catalog.Levels.FirstOrDefault(l => l.Index == index)
            ?? throw new GameException(Contracts.GameErrorCode.InvalidRequest, $"There is no level {index}; levels are 1-{catalog.Levels.Count}.");
        var director = new LevelDirector(catalog, generation);

        ILevelBuilder builder;
        LevelState level;
        if (string.IsNullOrWhiteSpace(preset))
        {
            builder = new ProceduralLevelBuilder(catalog, game, generation);
            try
            {
                level = director.Construct(builder, definition, seed);
            }
            catch (LevelGenerationException ex)
            {
                throw new GameException(Contracts.GameErrorCode.LevelGenerationFailed, ex.Message);
            }
        }
        else
        {
            try
            {
                builder = new PresetLevelBuilder(catalog, presets.ReadRows(preset));
                level = director.Assemble(builder, definition, seed);
            }
            catch (LevelBuildException ex)
            {
                throw new GameException(Contracts.GameErrorCode.InvalidRequest, ex.Message);
            }
        }

        var errors = LevelValidator.Validate(level, generation.MinZombieDistanceFromStart);
        var layout = SnapshotMapper.ToLayout(Guid.Empty, level);
        return new LevelPreviewResponse(index, (Contracts.LevelTheme)level.Theme, builder.GetType().Name, seed, level.Seed, level.Grid.Width, level.Grid.Height,
            layout.Rows, layout.Obstacles,
            level.Zombies.Select(z => new PreviewZombieDto(z.Id, z.Definition.Id, z.GetType().Name, (Contracts.MovementStrategyKind)z.Strategy.Kind, z.Tile.X, z.Tile.Y)).ToArray(),
            errors.Count == 0, errors.ToArray());
    }

    public IReadOnlyList<string> Presets() => presets.Available();
}
