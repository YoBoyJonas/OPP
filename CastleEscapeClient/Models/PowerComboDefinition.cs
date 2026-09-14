namespace CastleEscapeClient.Models;

public class PowerComboDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<PowerType> RequiredPowers { get; set; } = new();
    public PowerType Granted { get; set; }
    public StatModifiers Modifiers { get; set; } = new();
}
