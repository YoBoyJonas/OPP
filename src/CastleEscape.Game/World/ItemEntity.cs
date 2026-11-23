using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.World;

/// <summary>What an item needs from the game when it is picked up.</summary>
public sealed record ItemEffectContext(IReadOnlyList<PowerCombo> Combos, InteractionSettings Settings, List<GameEvent> Events);

/// <summary>
/// An item lying on the map (ITM-2: removed when collected). Each kind applies its own effect,
/// so the pickup code never asks which kind it has. Created only by an <c>ItemSpawner</c>.
/// </summary>
[DesignPattern("Factory Method", "Product")]
public abstract class ItemEntity(string id, Consumable definition, GridPos tile) : Entity(id, tile)
{
    public Consumable Definition { get; } = definition;
    public ConsumableKind Kind => Definition.Kind;

    /// <summary>Applies the item's effect to the player who picked it up (COL-2).</summary>
    public abstract void Apply(PlayerEntity player, ItemEffectContext context);
}
