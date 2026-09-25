namespace CastleEscape.Game.World;

/// <summary>A lever; active while a player stands on it (DOOR-1, D2).</summary>
public class Lever(string id, GridPos tile) : Entity(id, tile)
{
    public bool IsActive { get; set; }
}
