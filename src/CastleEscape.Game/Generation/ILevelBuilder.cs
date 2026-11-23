using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>Builder: builds one level step by step, in the order <see cref="LevelDirector"/> calls the steps.</summary>
public interface ILevelBuilder
{
    void BuildTerrain();
    void PlaceExitAndDoor();
    void PlaceStartTiles();
    void PlaceLevers();
    void PlacePowerObstacles();
    void PlaceItems();
    void PlaceZombies();
    LevelState GetResult();
}

public sealed class LevelBuildException(string message) : Exception(message);
