using CastleEscape.Contracts;
using CastleEscape.Game.AI;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;

namespace CastleEscape.Game.World;

/// <summary>
/// A zombie on the map. Chases the nearest player; returns to its spawn tile after contact (ZMB-3).
/// Abstract product of the theme factories: each level theme has its own subclass.
/// </summary>
public abstract class Zombie(string id, ZombieDefinition definition, GridPos spawn) : MovableEntity(id, spawn)
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

    /// <summary>Template Method: one tick of movement. The order is fixed; themes change it only through the hooks below.</summary>
    public void Update(WorldView world, double seconds)
    {
        if (!IsMoving)
        {
            TryStartStep(world);
        }
        if (Advance(EffectiveSpeed(world) * seconds, out var leftover) && TryStartStep(world))
        {
            Advance(Math.Min(leftover, 0.99), out _);
        }
    }

    /// <summary>Template Method: what happens when this zombie catches a player.</summary>
    public void OnContact(PlayerEntity victim, InteractionSettings settings, List<GameEvent> events)
    {
        victim.LoseLives(DamageFor(victim));
        AfterHit(victim);
        if (settings.RespawnPlayerOnHit)
        {
            victim.PlaceAt(victim.StartTile);
        }
        events.Add(new LifeLost(victim.PlayerId, Message(victim), Id, victim.Lives));
    }

    /// <summary>Hook: tiles per second for this tick.</summary>
    protected virtual double EffectiveSpeed(WorldView world) => Speed;

    /// <summary>Hook: lives taken on contact.</summary>
    protected virtual int DamageFor(PlayerEntity victim) => Definition.ContactDamage;

    /// <summary>Hook: what the zombie does after hitting (default: back to its spawn tile, ZMB-3).</summary>
    protected virtual void AfterHit(PlayerEntity victim) => ResetToSpawn();

    /// <summary>Hook: the text of the life-lost event.</summary>
    protected virtual string Message(PlayerEntity victim) =>
        $"{Definition.Name} caught {victim.Name}! {victim.Lives} lives left.";

    private bool TryStartStep(WorldView world)
    {
        var direction = Strategy.NextStep(this, world);
        if (direction == Direction.None || !world.CanZombieEnter(Tile.Step(direction)))
        {
            return false;
        }
        BeginStep(direction);
        return true;
    }
}
