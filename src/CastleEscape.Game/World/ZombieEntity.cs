using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.World;

/// <summary>
/// A zombie on the map. Chases the nearest player; returns to its spawn tile after contact (ZMB-3).
/// Each level theme has its own subclass, made by the theme's factory.
/// </summary>
[DesignPattern("Abstract Factory", "AbstractProduct")]
public abstract class ZombieEntity(string id, ZombieDefinition definition, GridPos spawn) : MovableEntity(id, spawn)
{
    public ZombieDefinition Definition { get; } = definition;
    public GridPos SpawnTile { get; } = spawn;

    /// <summary>Tiles per second.</summary>
    public virtual double Speed => Definition.Speed;

    /// <summary>How this zombie chases players (ZMB-1). Defaults to its type's strategy.</summary>
    public virtual MovementStrategyKind MovementStrategy => Definition.MovementStrategy;

    public void ResetToSpawn() => TeleportTo(SpawnTile);
}
