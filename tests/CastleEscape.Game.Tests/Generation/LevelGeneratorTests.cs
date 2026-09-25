using CastleEscape.Game.Configuration;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.Tests.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Generation;

public class LevelGeneratorTests
{
    private static readonly GenerationOptions Generation = new();
    private static readonly LevelGenerator Generator = new(TestGame.Catalog, new GameOptions(), Generation);

    public static TheoryData<int> LevelIndexes => new(Enumerable.Range(1, 10));

    [Theory]
    [MemberData(nameof(LevelIndexes))]
    public void EveryLevel_ManySeeds_PassesValidation(int index)
    {
        var definition = TestGame.Catalog.GetLevel(index);

        for (var seed = 0; seed < 25; seed++)
        {
            var level = Generator.Generate(definition, seed * 101);

            Assert.Empty(LevelValidator.Validate(level, Generation.MinZombieDistanceFromStart)); // GEN-1..3, D5, LVL-3
            Assert.Equal(definition.ZombieCount, level.Zombies.Count);                          // ZMB-4 via content
            Assert.Equal(definition.PickupCount, level.Items.Count);
            Assert.Equal(24, level.Grid.Width);                                                 // LVL-2 default
            Assert.Equal(16, level.Grid.Height);
            Assert.NotEqual(level.StartTiles[0], level.StartTiles[1]);
        }
    }

    [Fact]
    public void SameSeed_SameLevel()
    {
        var definition = TestGame.Catalog.GetLevel(4);

        var a = SnapshotMapper.ToRows(Generator.Generate(definition, 1234));
        var b = SnapshotMapper.ToRows(Generator.Generate(definition, 1234));

        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentSeeds_DifferentLevels()
    {
        var definition = TestGame.Catalog.GetLevel(4);

        var a = SnapshotMapper.ToRows(Generator.Generate(definition, 1));
        var b = SnapshotMapper.ToRows(Generator.Generate(definition, 2));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void RegeneratingFromTheLevelsSeed_GivesTheSameLevel()
    {
        // Restart (D8) relies on this: the stored seed is the attempt that succeeded.
        var definition = TestGame.Catalog.GetLevel(9);
        var level = Generator.Generate(definition, 77);

        var again = Generator.Generate(definition, level.Seed);

        Assert.Equal(SnapshotMapper.ToRows(level), SnapshotMapper.ToRows(again));
    }

    [Theory]
    [InlineData("tutorial")]
    [InlineData("arena")]
    public void Presets_PassValidation(string preset)
    {
        var level = PresetLevelParser.Parse(PresetLevelParser.ReadRows(preset), TestGame.Catalog.GetLevel(1), TestGame.Catalog);

        Assert.Empty(LevelValidator.Validate(level, Generation.MinZombieDistanceFromStart));
    }

    [Fact]
    public void Validator_ReportsLeverBehindWater_D5()
    {
        var level = PresetLevelParser.Parse(
        [
            "#########",
            "#1....L.#",
            "#~~~~~~~#",
            "#.L....2#",
            "####D####",
            "###.EE.##",
            "#########",
        ], TestGame.Catalog.GetLevel(1), TestGame.Catalog);

        Assert.Contains(LevelValidator.Validate(level, 0), e => e.StartsWith("D5"));
    }
}
