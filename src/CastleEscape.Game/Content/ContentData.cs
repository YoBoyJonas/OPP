namespace CastleEscape.Game.Content;

/// <summary>The raw content read from the JSON files, before it becomes the <see cref="ContentCatalog"/>.</summary>
public sealed class ContentData
{
    public IReadOnlyList<CharacterDefinition> Characters { get; init; } = [];
    public IReadOnlyList<Consumable> Consumables { get; init; } = [];
    public IReadOnlyList<PowerCombo> Combos { get; init; } = [];
    public IReadOnlyList<Obstacle> Obstacles { get; init; } = [];
    public IReadOnlyList<ZombieDefinition> Zombies { get; init; } = [];
    public IReadOnlyList<LevelDefinition> Levels { get; init; } = [];
}
