namespace CastleEscapeClient.Models;

public class StatModifiers
{
    public static readonly StatModifiers Identity = new();

    public double MoveSpeedMultiplier { get; set; } = 1.0;
    public double JumpForceMultiplier { get; set; } = 1.0;
    public double SwimSpeedMultiplier { get; set; } = 1.0;
}
