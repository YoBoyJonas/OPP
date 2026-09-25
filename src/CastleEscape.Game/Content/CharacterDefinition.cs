namespace CastleEscape.Game.Content;

/// <summary>A playable character (PLR-1). Loaded from <c>content/characters.json</c>.</summary>
public class CharacterDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    /// <summary>Lives at the start of the game.</summary>
    public int MaxHealth { get; init; }

    /// <summary>Tiles per second on floor.</summary>
    public double BaseMoveSpeed { get; init; }

    /// <summary>Jump parameter: speed multiplier while crossing a pit with Jump active.</summary>
    public double BaseJumpForce { get; init; } = 1.0;

    public StatModifiers BaseModifiers { get; init; } = StatModifiers.Identity;
}
