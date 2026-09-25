using CastleEscape.Game.Content;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Items;

/// <summary>
/// Places an item on a level. <see cref="Spawn"/> is the same for every kind (check the tile,
/// name the item, register it); only the <see cref="CreateItem"/> step differs, and subclasses decide it.
/// </summary>
[DesignPattern("Factory Method", "Creator")]
public abstract class ItemSpawner
{
    /// <summary>The content kind this spawner creates.</summary>
    public abstract ConsumableKind Kind { get; }

    public ItemEntity Spawn(LevelState level, Consumable definition, GridPos pos)
    {
        if (definition.Kind != Kind)
        {
            throw new ArgumentException($"{GetType().Name} spawns {Kind} items, not '{definition.Id}' ({definition.Kind}).", nameof(definition));
        }
        if (level.Grid.GetTerrain(pos) != TerrainKind.Floor)
        {
            throw new InvalidOperationException($"Items go on floor tiles; {pos} is {level.Grid.GetTerrain(pos)}.");
        }
        if (level.ItemAt(pos) is not null)
        {
            throw new InvalidOperationException($"{pos} already has an item.");
        }

        var item = CreateItem(level.NextEntityId("item"), definition, pos);   // the factory method
        level.AddItem(item);
        return item;
    }

    /// <summary>The factory method: which <see cref="ItemEntity"/> class to create.</summary>
    protected abstract ItemEntity CreateItem(string id, Consumable definition, GridPos pos);
}

[DesignPattern("Factory Method", "ConcreteCreator")]
public sealed class HealthItemSpawner : ItemSpawner
{
    public override ConsumableKind Kind => ConsumableKind.Health;
    protected override ItemEntity CreateItem(string id, Consumable definition, GridPos pos) => new HealthItem(id, (Health)definition, pos);
}

[DesignPattern("Factory Method", "ConcreteCreator")]
public sealed class RewardItemSpawner : ItemSpawner
{
    public override ConsumableKind Kind => ConsumableKind.Reward;
    protected override ItemEntity CreateItem(string id, Consumable definition, GridPos pos) => new RewardItem(id, (Reward)definition, pos);
}

[DesignPattern("Factory Method", "ConcreteCreator")]
public sealed class PowerItemSpawner : ItemSpawner
{
    public override ConsumableKind Kind => ConsumableKind.Power;
    protected override ItemEntity CreateItem(string id, Consumable definition, GridPos pos) => new PowerItem(id, (Power)definition, pos);
}

/// <summary>Picks the creator for a content item. The only place that maps kinds to spawners.</summary>
public static class ItemSpawners
{
    public static IReadOnlyList<ItemSpawner> All { get; } = [new HealthItemSpawner(), new RewardItemSpawner(), new PowerItemSpawner()];

    public static ItemSpawner For(ConsumableKind kind) =>
        All.FirstOrDefault(s => s.Kind == kind) ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, "No spawner for this item kind.");

    public static ItemSpawner For(Consumable definition) => For(definition.Kind);

    /// <summary>Convenience: choose the creator and spawn.</summary>
    public static ItemEntity Spawn(LevelState level, Consumable definition, GridPos pos) => For(definition).Spawn(level, definition, pos);
}
