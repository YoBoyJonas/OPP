namespace CastleEscapeClient.Models;

public class ZombieDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Speed { get; set; }
    public int ContactDamage { get; set; }
    public double AttackCooldownSeconds { get; set; }
    public int Size { get; set; }
}
