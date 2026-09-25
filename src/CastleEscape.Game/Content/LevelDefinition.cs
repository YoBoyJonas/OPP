namespace CastleEscapeClient.Models;

public class Level
{
    public string Id { get; set; } = string.Empty;
    public int Index { get; set; }
    public GridSize RoomSize { get; set; } = new();
    public int ObstacleCount { get; set; }
    public List<Obstacle> ObstacleTable { get; set; } = new();
    public int ZombieCount { get; set; }
    public List<ZombieDefinition> ZombieTable { get; set; } = new();
    public int PickupCount { get; set; }
    public List<Consumable> ConsumableTable { get; set; } = new();
    public double TimeLimitSeconds { get; set; }
}
