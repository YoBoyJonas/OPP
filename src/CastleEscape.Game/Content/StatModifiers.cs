namespace CastleEscape.Game.Content;

/// <summary>Multipliers applied on top of a character's base stats. Immutable.</summary>
public class StatModifiers
{
    public static readonly StatModifiers Identity = new();

    public double MoveSpeedMultiplier { get; init; } = 1.0;
    public double JumpForceMultiplier { get; init; } = 1.0;
    public double SwimSpeedMultiplier { get; init; } = 1.0;

    public StatModifiers Combine(StatModifiers other) => new()
    {
        MoveSpeedMultiplier = MoveSpeedMultiplier * other.MoveSpeedMultiplier,
        JumpForceMultiplier = JumpForceMultiplier * other.JumpForceMultiplier,
        SwimSpeedMultiplier = SwimSpeedMultiplier * other.SwimSpeedMultiplier
    };
}
