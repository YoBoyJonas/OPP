namespace CastleEscapeClient.Models;

public class Character
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxHealth { get; set; }
    public double BaseMoveSpeed { get; set; }
    public double BaseJumpForce { get; set; }
    public StatModifiers BaseModifiers { get; set; } = new();
}
