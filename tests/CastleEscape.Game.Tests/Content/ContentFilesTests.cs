using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Content;

/// <summary>Checks the real files in /content, as copied to the test output.</summary>
public class ContentFilesTests
{
    private static readonly ContentCatalog Catalog = ContentLoader.LoadFromDirectory(ContentLoader.DefaultDirectory);

    [Fact]
    public void Load_ReadsEveryFile()
    {
        Assert.Equal(3, Catalog.Characters.Count);
        Assert.Equal(6, Catalog.Consumables.Count);
        Assert.Equal(2, Catalog.Combos.Count);
        Assert.Equal(3, Catalog.Obstacles.Count);
        Assert.Equal(4, Catalog.Zombies.Count);
        Assert.Equal(10, Catalog.Levels.Count);
        Assert.Empty(Catalog.Validate());
    }

    [Fact]
    public void Characters_DifferInLivesSpeedAndJump_Plr1()
    {
        Assert.Equal(3, Catalog.Characters.Select(c => c.MaxHealth).Distinct().Count());
        Assert.Equal(3, Catalog.Characters.Select(c => c.BaseMoveSpeed).Distinct().Count());
        Assert.Equal(3, Catalog.Characters.Select(c => c.BaseJumpForce).Distinct().Count());
    }

    [Fact]
    public void Characters_KeepTheTeamsOriginalNumbers()
    {
        Assert.Equal((5, 3.5), Stats("warrior"));
        Assert.Equal((3, 6.0), Stats("scout"));
        Assert.Equal((4, 4.5), Stats("swimmer"));

        static (int, double) Stats(string id) => (Catalog.GetCharacter(id).MaxHealth, Catalog.GetCharacter(id).BaseMoveSpeed);
    }

    [Fact]
    public void Consumables_UseTheRightSubclasses()
    {
        Assert.IsType<Health>(Catalog.GetConsumable("health-potion"));
        Assert.IsType<Reward>(Catalog.GetConsumable("ruby"));
        var boots = Assert.IsType<Power>(Catalog.GetConsumable("jump-boots"));
        Assert.IsType<Jump>(boots.Grant);
        Assert.Equal(PowerType.Jump, boots.Grant.Power);
    }

    [Fact]
    public void Consumables_CoverAllThreeKinds_Itm1()
    {
        Assert.Equal(
            [ConsumableKind.Health, ConsumableKind.Reward, ConsumableKind.Power],
            Catalog.Consumables.Select(c => c.Kind).Distinct().Order());
    }

    [Fact]
    public void Combos_AreJumpDashAndFastSwim_Pwr2()
    {
        var jumpDash = Catalog.Combos.Single(c => c.Granted == PowerType.JumpDash);
        var fastSwim = Catalog.Combos.Single(c => c.Granted == PowerType.FastSwim);

        Assert.Equal([PowerType.Jump, PowerType.Sprint], jumpDash.RequiredPowers.Order());
        Assert.Equal([PowerType.Sprint, PowerType.Swim], fastSwim.RequiredPowers.Order());
    }

    [Fact]
    public void Obstacles_NeedTheMatchingPower_Mov2()
    {
        Assert.Equal(PowerType.Swim, Assert.IsType<Water>(Catalog.GetObstacle("water")).TraversalRequirements);
        Assert.Equal(PowerType.Jump, Assert.IsType<Pit>(Catalog.GetObstacle("pit")).TraversalRequirements);
        Assert.Equal(PowerType.None, Assert.IsType<Wall>(Catalog.GetObstacle("wall")).TraversalRequirements);
    }

    [Fact]
    public void Zombies_CoverEveryMovementStrategy()
    {
        Assert.Equal(Enum.GetValues<MovementStrategyKind>(), Catalog.Zombies.Select(z => z.MovementStrategy).Distinct().Order());
    }

    [Fact]
    public void Levels_ZombieCountNeverDecreases_Zmb4()
    {
        var counts = Catalog.Levels.OrderBy(l => l.Index).Select(l => l.ZombieCount).ToList();

        Assert.Equal(counts.Order(), counts);
    }

    [Fact]
    public void Levels_OneToFiveDungeon_SixToTenCrypt()
    {
        Assert.All(Catalog.Levels, l => Assert.Equal(l.Index <= 5 ? LevelTheme.Dungeon : LevelTheme.Crypt, l.Theme));
    }

    [Theory]
    [InlineData("tutorial.txt", 0)]
    [InlineData("arena.txt", 2)]
    public void Presets_AreWellFormed(string file, int zombies)
    {
        var rows = File.ReadAllLines(Path.Combine(ContentLoader.DefaultDirectory, "presets", file))
            .Where(r => r.Length > 0).ToArray();
        var all = string.Concat(rows);

        Assert.True(rows.Length >= 15, "LVL-2 height");
        Assert.All(rows, r => Assert.Equal(rows[0].Length, r.Length));
        Assert.True(rows[0].Length >= 20, "LVL-2 width");
        Assert.All(all, c => MapLegend.TerrainOf(c)); // every character is in the legend
        Assert.Equal(1, all.Count(c => c == MapLegend.Player1Start));
        Assert.Equal(1, all.Count(c => c == MapLegend.Player2Start));
        Assert.True(all.Count(c => c == MapLegend.Lever) >= 2, "DOOR-1 levers");
        Assert.Equal(1, all.Count(c => c == MapLegend.Door));
        Assert.True(all.Count(c => c == MapLegend.Exit) >= 2, "DOOR-2 exit tiles");
        Assert.True(all.Any(c => c is MapLegend.Water or MapLegend.Pit), "GEN-3 power obstacle");
        Assert.Equal(zombies, all.Count(c => c == MapLegend.ZombieSpawn));
    }
}
