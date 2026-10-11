namespace CastleEscape.Domain.Content;

/// <summary>Validated game definitions, independent of their storage format.</summary>
public interface IContentCatalog
{
    IReadOnlyList<CharacterDefinition> Characters { get; }
    IReadOnlyList<Consumable> Consumables { get; }
    IReadOnlyList<PowerCombo> Combos { get; }
    IReadOnlyList<Obstacle> Obstacles { get; }
    IReadOnlyList<ZombieDefinition> Zombies { get; }
    IReadOnlyList<LevelDefinition> Levels { get; }
    CharacterDefinition? FindCharacter(string id);
    CharacterDefinition GetCharacter(string id);
    Consumable GetConsumable(string id);
    Obstacle GetObstacle(string id);
    ZombieDefinition GetZombie(string id);
    LevelDefinition GetLevel(int index);
    string ComputeContentHash();
}
