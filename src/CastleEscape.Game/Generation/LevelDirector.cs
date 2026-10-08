using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Knows the order of the construction steps and the rules a finished level must pass.
/// The same director works with any <see cref="ILevelBuilder"/> (start-level activity diagram:
/// generate, validate, retry).
/// </summary>
[DesignPattern("Builder", "Director")]
public sealed class LevelDirector(ContentCatalog catalog, GenerationOptions generation)
{
    /// <summary>The step order. Terrain and exit come first: starts, levers and obstacles depend on them.</summary>
    public static readonly string[] Steps =
    [
        nameof(ILevelBuilder.BuildTerrain), nameof(ILevelBuilder.PlaceExitAndDoor), nameof(ILevelBuilder.PlaceStartTiles),
        nameof(ILevelBuilder.PlaceLevers), nameof(ILevelBuilder.PlacePowerObstacles), nameof(ILevelBuilder.PlaceItems),
        nameof(ILevelBuilder.PlaceZombies),
    ];

    /// <summary>Builds a valid level: runs the steps, validates, and retries with a new seed (random builders only).</summary>
    /// <exception cref="LevelGenerationException">No valid level after the allowed attempts.</exception>
    public LevelState Construct(ILevelBuilder builder, LevelDefinition definition, int seed)
    {
        var attempts = builder.IsRandom ? Math.Max(1, generation.MaxAttempts) : 1;
        List<string> errors = [];
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                var level = Assemble(builder, definition, unchecked(seed + attempt * 7919));
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

    /// <summary>Runs every step once, without validation (tests use it for tiny hand-made maps).</summary>
    public LevelState Assemble(ILevelBuilder builder, LevelDefinition definition, int seed)
    {
        builder.Reset(definition, seed, ThemeFactories.For(definition, catalog));
        builder.BuildTerrain();
        builder.PlaceExitAndDoor();
        builder.PlaceStartTiles();
        builder.PlaceLevers();
        builder.PlacePowerObstacles();
        builder.PlaceItems();
        builder.PlaceZombies();
        return builder.GetResult();
    }
}
