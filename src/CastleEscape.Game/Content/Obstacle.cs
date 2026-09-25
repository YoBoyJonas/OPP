namespace CastleEscapeClient.Models;

public abstract class Obstacle
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public abstract ObstacleKind Kind { get; }
    public PowerType TraversalRequirements { get; set; }
    public bool BlocksZombies { get; set; }
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
}
