using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>Director: the order of the build steps, the same for every builder.</summary>
public sealed class LevelDirector
{
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
