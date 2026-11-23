using CastleEscape.Contracts;
using CastleEscape.Game.AI;
using CastleEscape.Game.Content;

namespace CastleEscape.Game.World;

/// <summary>
/// A zombie on the map. Chases the nearest player; returns to its spawn tile after contact (ZMB-3).
/// Abstract product of the theme factories: each level theme has its own subclass.
/// </summary>
public abstract class ZombieEntity(string id, ZombieDefinition definition, GridPos spawn) : MovableEntity(id, spawn)
{
    public ZombieDefinition Definition { get; } = definition;
    public GridPos SpawnTile { get; } = spawn;

    /// <summary>Tiles per second.</summary>
    public virtual double Speed => Definition.Speed;

    /// <summary>The kind of chase this zombie starts with (ZMB-1): its type's, unless the theme changes it.</summary>
    public virtual MovementStrategyKind MovementStrategy => Definition.MovementStrategy;

    private IZombieMovementStrategy? _strategy;

    /// <summary>How this zombie chases players right now. Starts as <see cref="MovementStrategy"/>; can be swapped at runtime.</summary>
    public IZombieMovementStrategy Strategy
    {
        get => _strategy ??= ZombieStrategies.For(MovementStrategy);
        set => _strategy = value;
    }

    public void ResetToSpawn() => TeleportTo(SpawnTile);
}
