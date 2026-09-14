namespace CastleEscapeClient.Models;

public class ConsumableDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ConsumableKind Kind { get; set; }
    public PowerGrant? Grant { get; set; }
    public int ScoreValue { get; set; }
    public int HealthValue { get; set; }
    public double SpawnWeight { get; set; }
    public string IconPath { get; set; } = string.Empty;
}
