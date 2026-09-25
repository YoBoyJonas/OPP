using CastleEscape.Game.Content;

namespace CastleEscape.Game.World;

/// <summary>A zombie on the map. Chases the nearest player; returns to its spawn tile after contact (ZMB-3).</summary>
public class ZombieEntity(string id, ZombieDefinition definition, GridPos spawn) : MovableEntity(id, spawn)
{
    public ZombieDefinition Definition { get; } = definition;
    public GridPos SpawnTile { get; } = spawn;

    public virtual double Speed => Definition.Speed;

    public void ResetToSpawn() => TeleportTo(SpawnTile);
}
