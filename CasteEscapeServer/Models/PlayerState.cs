namespace CasteEscapeServer.Models;

public class PlayerState
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public double MoveSpeed { get; set; }
    public Direction CurrentDirection { get; set; } = Direction.None;
    public Dictionary<PowerType, double> ActivePowers { get; set; } = new();
    public int Score { get; set; }

    public bool HasPower(PowerType power) => ActivePowers.ContainsKey(power);
}
