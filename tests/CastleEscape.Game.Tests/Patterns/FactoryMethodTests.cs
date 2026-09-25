using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.Items;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class FactoryMethodTests
{
    private static ContentCatalog Catalog => ContentCatalog.Instance;

    private static LevelState EmptyLevel() => new(Catalog.GetLevel(1), seed: 1, new Grid(10, 10));

    private static Consumable First(ConsumableKind kind) => Catalog.Consumables.First(c => c.Kind == kind);

    private static ItemEffectContext Context(List<GameEvent> events) =>
        new(Catalog.Combos, new InteractionSettings(10, 3, false), events);

    [Fact]
    public void ProductFamily_HasAtLeastThreeClasses()
    {
        var products = typeof(ItemEntity).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ItemEntity)) && !t.IsAbstract)
            .ToList();

        Assert.True(products.Count >= 3);
        Assert.Contains(typeof(HealthItem), products);
        Assert.Contains(typeof(RewardItem), products);
        Assert.Contains(typeof(PowerItem), products);
    }

    [Theory]
    [InlineData(ConsumableKind.Health, typeof(HealthItemSpawner), typeof(HealthItem))]
    [InlineData(ConsumableKind.Reward, typeof(RewardItemSpawner), typeof(RewardItem))]
    [InlineData(ConsumableKind.Power, typeof(PowerItemSpawner), typeof(PowerItem))]
    public void EachCreator_MakesItsOwnProduct(ConsumableKind kind, Type creatorType, Type productType)
    {
        var level = EmptyLevel();
        var spawner = ItemSpawners.For(kind);

        var item = spawner.Spawn(level, First(kind), new GridPos(3, 3));

        Assert.IsType(creatorType, spawner);
        Assert.IsType(productType, item);
        Assert.Same(item, level.ItemAt(new GridPos(3, 3)));
    }

    [Fact]
    public void Spawn_GivesUniqueIds()
    {
        var level = EmptyLevel();

        var a = ItemSpawners.Spawn(level, First(ConsumableKind.Reward), new GridPos(1, 1));
        var b = ItemSpawners.Spawn(level, First(ConsumableKind.Health), new GridPos(2, 1));

        Assert.Equal("item-1", a.Id);
        Assert.Equal("item-2", b.Id);
    }

    [Fact]
    public void Spawn_RejectsWrongKindWallAndOccupiedTile()
    {
        var level = EmptyLevel();
        level.Grid.SetTile(new GridPos(5, 5), TerrainKind.Wall);
        ItemSpawners.Spawn(level, First(ConsumableKind.Reward), new GridPos(1, 1));

        Assert.Throws<ArgumentException>(() => new HealthItemSpawner().Spawn(level, First(ConsumableKind.Power), new GridPos(2, 2)));
        Assert.Throws<InvalidOperationException>(() => ItemSpawners.Spawn(level, First(ConsumableKind.Reward), new GridPos(5, 5)));
        Assert.Throws<InvalidOperationException>(() => ItemSpawners.Spawn(level, First(ConsumableKind.Reward), new GridPos(1, 1)));
    }

    [Fact]
    public void Products_ApplyTheirOwnEffect()
    {
        var level = EmptyLevel();
        var player = new PlayerEntity(Guid.NewGuid(), "Ana", Catalog.GetCharacter("warrior"), new GridPos(1, 1));
        player.LoseLives(2);
        var events = new List<GameEvent>();
        var livesBefore = player.Lives;

        ItemSpawners.Spawn(level, First(ConsumableKind.Health), new GridPos(1, 2)).Apply(player, Context(events));
        ItemSpawners.Spawn(level, First(ConsumableKind.Reward), new GridPos(1, 3)).Apply(player, Context(events));
        var power = (PowerItem)ItemSpawners.Spawn(level, First(ConsumableKind.Power), new GridPos(1, 4));
        power.Apply(player, Context(events));

        Assert.Equal(livesBefore + First(ConsumableKind.Health).HealthValue, player.Lives);
        Assert.Equal(First(ConsumableKind.Reward).ScoreValue, player.Score);
        Assert.NotNull(player.Powers.Get(power.Grant.Power));
        Assert.Contains(events, e => e.Type == GameEventTypes.PowerGained);
    }

    [Fact]
    public void GeneratedLevels_ContainOnlyConcreteProducts()
    {
        var level = DemoWorld.TutorialLevel();

        Assert.NotEmpty(level.Items);
        Assert.All(level.Items, i => Assert.True(i is HealthItem or RewardItem or PowerItem));
        Assert.Equal(level.Items.Count, level.Items.Select(i => i.Id).Distinct().Count());
    }

    [Fact]
    public void Demo_CreatesAllThreeProducts()
    {
        var result = new FactoryMethodDemo().Run(DemoOptions.None);

        var family = Assert.IsType<string[]>(result.Evidence["productFamily"]);
        Assert.Equal(["HealthItem", "PowerItem", "RewardItem"], family);
        Assert.Equal(3, Assert.IsType<List<Dictionary<string, object?>>>(result.Evidence["created"]).Count);
    }
}
