namespace CastleEscapeClient.Models;

public class PowerCombo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PowerType RequiredPowers { get; set; }
    public PowerType Granted { get; set; }
    public StatModifiers Modifiers { get; set; } = new();
}
