namespace CastleEscapeClient.Models;

public abstract class Consumable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public abstract ConsumableKind Kind { get; }
    public PowerGrant? Grant { get; set; }
    public int HealthValue { get; set; }
    public double SpawnWeight { get; set; }
    public string IconPath { get; set; } = string.Empty;
}
