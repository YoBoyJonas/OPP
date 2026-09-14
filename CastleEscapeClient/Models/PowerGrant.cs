namespace CastleEscapeClient.Models;

public class PowerGrant
{
    public PowerType Power { get; set; }
    public double DurationSeconds { get; set; }
    public StatModifiers Modifiers { get; set; } = new();
}
