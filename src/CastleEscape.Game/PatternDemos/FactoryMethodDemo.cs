using CastleEscape.Contracts.Patterns;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.Items;
using CastleEscape.Game.World;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Factory Method requirement: at least 3 classes in the product family.</summary>
public sealed class FactoryMethodDemo : IPatternDemo
{
    public string Key => "factory-method";

    public PatternDemoResponse Run(DemoOptions options)
    {
        var kindOption = options.Get("kind", "all");
        var kinds = kindOption.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? Enum.GetValues<ConsumableKind>()
            : [Enum.Parse<ConsumableKind>(kindOption, ignoreCase: true)];
        var trace = new DemoTrace("Factory Method");

        var level = DemoWorld.TutorialLevel();
        var (p1, _) = DemoWorld.TwoPlayers(level);
        var player = p1.Entity!;
        player.LoseLives(1); // so a health potion has something to restore
        var events = new List<PendingEvent>();
        var context = new ItemEffectContext(DemoWorld.Catalog.Combos, new InteractionSettings(10, 3, false), events);

        trace.Line($"The tutorial preset already placed {level.Items.Count} items through the spawners: "
                   + string.Join(", ", level.Items.Select(i => $"{i.Id}={i.GetType().Name}")) + ".");

        var free = level.Grid.Positions()
            .Where(p => level.Grid.GetTerrain(p) == TerrainKind.Floor && level.ItemAt(p) is null && !level.StartTiles.Contains(p))
            .GetEnumerator();
        var created = new List<Dictionary<string, object?>>();
        foreach (var kind in kinds)
        {
            var definition = DemoWorld.Catalog.Consumables.First(c => c.Kind == kind);
            var spawner = ItemSpawners.For(definition);
            free.MoveNext();
            var item = spawner.Spawn(level, definition, free.Current);

            var before = (player.Lives, player.Score, Powers: player.Powers.Count);
            item.Apply(player, context);
            trace.Line($"{spawner.GetType().Name}.Spawn(\"{definition.Id}\") -> CreateItem() made {item.GetType().Name} {item.Id} at {item.Tile}; "
                       + $"Apply: lives {before.Lives}->{player.Lives}, score {before.Score}->{player.Score}, "
                       + $"active powers {before.Powers}->{player.Powers.Count}.");
            created.Add(new()
            {
                ["creator"] = spawner.GetType().Name,
                ["product"] = item.GetType().Name,
                ["itemId"] = item.Id,
                ["consumable"] = definition.Id,
            });
        }

        var family = typeof(ItemEntity).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ItemEntity)) && !t.IsAbstract)
            .Select(t => t.Name).OrderBy(n => n).ToArray();
        trace.Line($"Product family (subclasses of ItemEntity): {string.Join(", ", family)}.")
            .Line("InteractionResolver.CollectItems just calls item.Apply(); it has no switch on the item kind.")
            .Evidence("productFamily", family)
            .Evidence("created", created)
            .Evidence("events", events.Select(e => e.Type).ToArray());

        return trace.Done($"{family.Length} product classes; each spawner's CreateItem() decides which one is made.");
    }
}
