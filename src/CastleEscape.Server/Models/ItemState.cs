namespace CasteEscapeServer.Models;

public class ItemState
{
    public string Id { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public ItemKind Kind { get; set; }
    public PowerType? GrantedPower { get; set; }
    public double GrantDurationSeconds { get; set; }
    public int ScoreValue { get; set; }
    public int HealthValue { get; set; }
}
