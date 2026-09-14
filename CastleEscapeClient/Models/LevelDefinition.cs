namespace CastleEscapeClient.Models;

public class LevelDefinition
{
    public string Id { get; set; } = string.Empty;
    public int Index { get; set; }
    public GridSize RoomSize { get; set; } = new();
    public int ObstacleCount { get; set; }
    public List<ObstacleDefinition> ObstacleTable { get; set; } = new();
    public int ZombieCount { get; set; }
    public List<ZombieDefinition> ZombieTable { get; set; } = new();
    public int PickupCount { get; set; }
    public List<ConsumableDefinition> ConsumableTable { get; set; } = new();
    public double TimeLimitSeconds { get; set; }
    public bool HasDoorPuzzle { get; set; }
}
