namespace CastleEscapeClient.Models;

public abstract class PowerGrant
{
    public abstract PowerType Power { get; }
    public double DurationSeconds { get; set; }
    public StatModifiers Modifiers { get; set; } = new();
}
