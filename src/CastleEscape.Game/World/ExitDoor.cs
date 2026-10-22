namespace CastleEscape.Game.World;

/// <summary>The exit door tile. Closed at level start.</summary>
public class ExitDoor(string id, GridPos tile) : Entity(id, tile)
{
    public bool IsOpen { get; set; }
}
