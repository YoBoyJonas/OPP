using CastleEscape.Contracts;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Powers;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class AbstractFactoryTests
{
    private static ContentCatalog Catalog => ContentCatalog.Instance;

    private static ZombieDefinition Greedy => Catalog.Zombies.First(z => z.MovementStrategy == MovementStrategyKind.Greedy);

    [Fact]
    public void AtLeastTwoConcreteFactories_WithAtLeastThreeProductsEach()
    {
        var factories = typeof(IThemeFactory).Assembly.GetTypes()
            .Where(t => typeof(IThemeFactory).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .ToList();
        Assert.True(factories.Count >= 2);

        var families = Enum.GetValues<LevelTheme>()
            .Select(theme => AbstractFactoryDemo.Family(ThemeFactories.For(theme, Catalog), Greedy).Select(p => p.GetType()).ToArray())
            .ToList();

        Assert.All(families, f => Assert.True(f.Distinct().Count() >= 3));
        Assert.Empty(families[0].Intersect(families[1])); // no class is shared between families
    }

    [Fact]
    public void FamiliesHaveRealGameplayDifferences()
    {
        var dungeon = ThemeFactories.For(LevelTheme.Dungeon, Catalog);
        var crypt = ThemeFactories.For(LevelTheme.Crypt, Catalog);
        var dungeonZombie = dungeon.CreateZombie("z1", Greedy, new GridPos(1, 1));
        var cryptZombie = crypt.CreateZombie("z2", Greedy, new GridPos(1, 1));

        Assert.True(crypt.CreateWater().MoveSpeedMultiplier < dungeon.CreateWater().MoveSpeedMultiplier);
        Assert.True(crypt.CreatePit().MoveSpeedMultiplier < dungeon.CreatePit().MoveSpeedMultiplier);
        Assert.Equal(Greedy.Speed, dungeonZombie.Speed);
        Assert.Equal(Greedy.Speed * CryptZombie.SpeedBonus, cryptZombie.Speed, 6);
        Assert.Equal(MovementStrategyKind.Greedy, dungeonZombie.MovementStrategy);
        Assert.Equal(MovementStrategyKind.Bfs, cryptZombie.MovementStrategy);
    }

    [Fact]
    public void ThemedProductsKeepTheTemplateRules()
    {
        var water = ThemeFactories.For(LevelTheme.Crypt, Catalog).CreateWater();
        var template = Catalog.Obstacles.OfType<Water>().First();

        Assert.IsType<PoisonWater>(water);
        Assert.Equal(template.TraversalRequirements, water.TraversalRequirements);
        Assert.Equal(template.BlocksZombies, water.BlocksZombies);
        Assert.Equal(template.MaxSize, water.MaxSize);
    }

    [Fact]
    public void ThemedPitSpeed_ChangesHowFastPlayersCross()
    {
        var player = new PlayerEntity(Guid.NewGuid(), "Ana", Catalog.GetCharacter("scout"), new GridPos(1, 1));
        var spike = new Tile(TerrainKind.Pit, ThemeFactories.For(LevelTheme.Dungeon, Catalog).CreatePit());
        var abyss = new Tile(TerrainKind.Pit, ThemeFactories.For(LevelTheme.Crypt, Catalog).CreatePit());

        Assert.Equal(PlayerAbilities.SpeedOn(player, spike) * AbyssPit.SpeedMultiplier, PlayerAbilities.SpeedOn(player, abyss), 6);
    }

    [Fact]
    public void GeneratedLevels_NeverMixFamilies()
    {
        var generator = new LevelGenerator(Catalog, new GameOptions(), new GenerationOptions());

        foreach (var definition in Catalog.Levels)
        {
            var family = AbstractFactoryDemo.Family(ThemeFactories.For(definition, Catalog), Greedy)
                .Select(p => p.GetType().Name).ToHashSet();
            for (var seed = 1; seed <= 5; seed++)
            {
                var level = generator.Generate(definition, seed);
                Assert.All(AbstractFactoryDemo.PartsIn(level), part => Assert.Contains(part, family));
            }
        }
    }

    [Fact]
    public void LevelLayout_ListsTheThemedObstacles()
    {
        var level = new LevelGenerator(Catalog, new GameOptions(), new GenerationOptions()).Generate(Catalog.GetLevel(8), seed: 3);

        var layout = SnapshotMapper.ToLayout(Guid.NewGuid(), level);

        Assert.Equal(LevelTheme.Crypt, layout.Theme);
        Assert.Contains(layout.Obstacles, o => o.Variant == nameof(BoneWall));
        Assert.DoesNotContain(layout.Obstacles, o => o.Variant == nameof(StoneWall));
    }

    [Fact]
    public void Demo_ReportsNoMixedParts()
    {
        var result = new AbstractFactoryDemo().Run(DemoOptions.None);

        Assert.Equal(0, result.Evidence["mixedParts"]);
    }
}
