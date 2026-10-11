namespace CastleEscape.Domain.Content;

/// <summary>A temporary base power granted by a Power item (PWR-1). The JSON field <c>type</c> picks the subclass.</summary>
public abstract class PowerGrant
{
    public abstract PowerType Power { get; }

    /// <summary>How long the power lasts. Null means the <c>Game:PowerDurationSeconds</c> setting.</summary>
    public double? DurationSeconds { get; init; }

    public StatModifiers Modifiers { get; init; } = StatModifiers.Identity;
}
