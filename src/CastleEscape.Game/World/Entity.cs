namespace CastleEscape.Game.World;

/// <summary>Anything that sits on a tile: players, zombies, items, levers, the door.</summary>
public abstract class Entity(string id, GridPos tile)
{
    public string Id { get; } = id;

    /// <summary>The tile the entity is on (for a moving entity: the tile it is leaving).</summary>
    public GridPos Tile { get; protected set; } = tile;
}
