namespace CastleEscapeClient.Models;

public class ObstacleDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ObstacleKind Kind { get; set; }
    public PowerType TraversalRequirements { get; set; }
    public bool BlocksZombies { get; set; }
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
}
