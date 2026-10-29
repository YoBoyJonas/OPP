using CastleEscape.Contracts;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class AbstractFactoryTests
{
    private static ContentCatalog Catalog => ContentCatalog.Instance;

    private static ZombieDefinition Greedy => Catalog.Zombies.First(z => z.MovementStrategy == MovementStrategyKind.Greedy);

    private static readonly LevelProvider Levels = new(Catalog, new GameOptions(), new GenerationOptions());

    /// <summary>The classes of one of every product the factory makes.</summary>
    private static Type[] Family(IThemeFactory factory) =>
    [
        factory.CreateWall().GetType(),
        factory.CreateWater().GetType(),
        factory.CreatePit().GetType(),
        factory.CreateZombie("z", Greedy, new GridPos(1, 1)).GetType(),
    ];

    /// <summary>The classes of the obstacles and zombies actually placed in a level.</summary>
    private static Type[] PartsIn(LevelState level) =>
        level.Grid.Positions().Select(p => level.Grid.GetTile(p).Obstacle).OfType<object>()
            .Concat(level.Zombies)
            .Select(o => o.GetType())
            .Distinct()
            .ToArray();

    [Fact]
    public void EachThemeHasItsOwnFactory()
    {
        Assert.IsType<DungeonThemeFactory>(ThemeFactories.For(LevelTheme.Dungeon, Catalog));
        Assert.IsType<CryptThemeFactory>(ThemeFactories.For(LevelTheme.Crypt, Catalog));
    }

    [Fact]
    public void TwoConcreteFactories_WithFourProductsEach()
    {
        var dungeon = Family(ThemeFactories.For(LevelTheme.Dungeon, Catalog));
        var crypt = Family(ThemeFactories.For(LevelTheme.Crypt, Catalog));

        Assert.Equal([typeof(StoneWall), typeof(MurkyWater), typeof(SpikePit), typeof(DungeonZombie)], dungeon);
        Assert.Equal([typeof(BoneWall), typeof(PoisonWater), typeof(AbyssPit), typeof(CryptZombie)], crypt);
        Assert.Empty(dungeon.Intersect(crypt)); // no class is shared between families
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

        Assert.Equal(player.Abilities.SpeedOn(spike) * AbyssPit.SpeedMultiplier, player.Abilities.SpeedOn(abyss), 6);
    }

    [Fact]
    public void GeneratedLevels_NeverMixFamilies()
    {
        foreach (var definition in Catalog.Levels)
        {
            var family = Family(ThemeFactories.For(definition.Theme, Catalog));
            for (var seed = 1; seed <= 5; seed++)
            {
                var level = Levels.Generate(definition, seed);
                Assert.All(PartsIn(level), part => Assert.Contains(part, family));
            }
        }
    }

    [Fact]
    public void PresetLevels_UseTheLevelsFamilyToo()
    {
        var level = Levels.BuildPreset(Catalog.GetLevel(7), "arena");

        Assert.All(PartsIn(level), part => Assert.Contains(part, Family(ThemeFactories.For(LevelTheme.Crypt, Catalog))));
    }

    [Fact]
    public void LevelLayout_ListsTheThemedObstacles()
    {
        var level = Levels.Generate(Catalog.GetLevel(8), seed: 3);

        var layout = SnapshotMapper.ToLayout(Guid.NewGuid(), level);

        Assert.Equal(LevelTheme.Crypt, layout.Theme);
        Assert.Contains(layout.Obstacles, o => o.Variant == nameof(BoneWall));
        Assert.DoesNotContain(layout.Obstacles, o => o.Variant == nameof(StoneWall));
    }
}
