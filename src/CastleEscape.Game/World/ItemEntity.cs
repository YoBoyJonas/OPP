using CastleEscape.Game.Content;

namespace CastleEscape.Game.World;

/// <summary>An item lying on the map (ITM-2: removed when collected).</summary>
public class ItemEntity(string id, Consumable definition, GridPos tile) : Entity(id, tile)
{
    public Consumable Definition { get; } = definition;
    public ConsumableKind Kind => Definition.Kind;
}
