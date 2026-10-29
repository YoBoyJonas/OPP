using CastleEscape.Contracts;
using CastleEscape.Contracts.Content;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>Builds levels outside any session so they can be looked at (content design, tests, the defence).</summary>
public sealed class LevelPreviewer(ContentCatalog catalog, GameOptions game, GenerationOptions generation)
{
    private readonly LevelProvider _levels = new(catalog, game, generation);

    /// <summary>
    /// Level <paramref name="index"/> generated from <paramref name="seed"/> (validated, retried), or the
    /// <paramref name="preset"/> map (built as drawn and then checked, so a broken preset shows its errors).
    /// </summary>
    /// <exception cref="GameException">Unknown level or preset, or no valid level could be generated.</exception>
    public LevelPreviewResponse Preview(int index, int seed, string? preset = null)
    {
        var definition = catalog.Levels.FirstOrDefault(l => l.Index == index)
            ?? throw new GameException(GameErrorCode.InvalidRequest, $"There is no level {index}; levels are 1-{catalog.Levels.Count}.");

        string builder;
        LevelState level;
        if (string.IsNullOrWhiteSpace(preset))
        {
            builder = nameof(ProceduralLevelBuilder);
            try
            {
                level = _levels.Generate(definition, seed);
            }
            catch (LevelGenerationException ex)
            {
                throw new GameException(GameErrorCode.LevelGenerationFailed, ex.Message);
            }
        }
        else
        {
            builder = nameof(PresetLevelBuilder);
            try
            {
                level = _levels.BuildPreset(definition, preset, seed);
            }
            catch (LevelBuildException ex)
            {
                throw new GameException(GameErrorCode.InvalidRequest, ex.Message);
            }
        }

        var errors = LevelValidator.Validate(level, generation.MinZombieDistanceFromStart);
        var layout = SnapshotMapper.ToLayout(Guid.Empty, level);
        return new LevelPreviewResponse(index, level.Theme, builder, seed, level.Seed, level.Grid.Width, level.Grid.Height,
            layout.Rows, layout.Obstacles,
            level.Zombies.Select(z => new PreviewZombieDto(z.Id, z.Definition.Id, z.GetType().Name, z.Strategy.Kind, z.Tile.X, z.Tile.Y)).ToArray(),
            errors.Count == 0, errors.ToArray());
    }

    public IReadOnlyList<string> Presets() => PresetMaps.Available();
}
