namespace CastleEscapeClient.Models;

public class ContentCatalog
{
    public List<CharacterDefinition> Characters { get; set; } = new();
    public List<ConsumableDefinition> Consumables { get; set; } = new();
    public List<PowerComboDefinition> Combos { get; set; } = new();
    public List<ObstacleDefinition> Obstacles { get; set; } = new();
    public List<ZombieDefinition> Zombies { get; set; } = new();
    public List<LevelDefinition> Levels { get; set; } = new();
}
