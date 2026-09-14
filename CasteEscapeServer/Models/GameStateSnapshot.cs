namespace CasteEscapeServer.Models;

public class PlayerSnapshot
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Score { get; set; }
    public List<PowerType> ActivePowers { get; set; } = new();
}

public class ZombieSnapshot
{
    public string Id { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
}

public class LeverSnapshot
{
    public string Id { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsActive { get; set; }
}

public class ItemSnapshot
{
    public string Id { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public ItemKind Kind { get; set; }
}

public class GameStateSnapshot
{
    public int LevelIndex { get; set; }
    public List<PlayerSnapshot> Players { get; set; } = new();
    public List<ZombieSnapshot> Zombies { get; set; } = new();
    public List<LeverSnapshot> Levers { get; set; } = new();
    public List<ItemSnapshot> Items { get; set; } = new();
    public bool DoorOpen { get; set; }
    public bool IsGameOver { get; set; }
    public bool IsWin { get; set; }
}
