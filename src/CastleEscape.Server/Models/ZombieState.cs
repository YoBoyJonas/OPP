namespace CasteEscapeServer.Models;

public class ZombieState
{
    public string Id { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public int HomeX { get; set; }
    public int HomeY { get; set; }
    public double Speed { get; set; }
    public int ContactDamage { get; set; }
}
