using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Director. Knows the order of the construction steps, and nothing else: the same director builds a
/// level with any <see cref="ILevelBuilder"/>.
/// </summary>
public sealed class LevelDirector
{
    /// <summary>Terrain and exit come first: the start tiles, levers and obstacles depend on them.</summary>
    public LevelState Construct(ILevelBuilder builder)
    {
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
