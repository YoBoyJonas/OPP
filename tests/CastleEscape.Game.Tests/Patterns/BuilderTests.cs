using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class BuilderTests
{
    private static ContentCatalog Catalog => ContentCatalog.Instance;
    private static readonly GenerationOptions Generation = new();
    private static readonly LevelProvider Levels = new(Catalog, new GameOptions(), Generation);

    /// <summary>Records the calls it gets; wraps the tutorial preset so the result is a real level.</summary>
    private sealed class RecordingBuilder : ILevelBuilder
    {
        private readonly PresetLevelBuilder _inner = new(Catalog, Catalog.GetLevel(1),
            ThemeFactories.For(Catalog.GetLevel(1).Theme, Catalog), PresetMaps.ReadRows("tutorial"));

        public List<string> Calls { get; } = [];

        public void BuildTerrain() { Calls.Add(nameof(BuildTerrain)); _inner.BuildTerrain(); }
        public void PlaceExitAndDoor() { Calls.Add(nameof(PlaceExitAndDoor)); _inner.PlaceExitAndDoor(); }
        public void PlaceStartTiles() { Calls.Add(nameof(PlaceStartTiles)); _inner.PlaceStartTiles(); }
        public void PlaceLevers() { Calls.Add(nameof(PlaceLevers)); _inner.PlaceLevers(); }
        public void PlacePowerObstacles() { Calls.Add(nameof(PlacePowerObstacles)); _inner.PlacePowerObstacles(); }
        public void PlaceItems() { Calls.Add(nameof(PlaceItems)); _inner.PlaceItems(); }
        public void PlaceZombies() { Calls.Add(nameof(PlaceZombies)); _inner.PlaceZombies(); }

        public LevelState GetResult()
        {
            Calls.Add(nameof(GetResult));
            return _inner.GetResult();
        }
    }

    [Fact]
    public void Director_CallsTheStepsInOrder()
    {
        var builder = new RecordingBuilder();

        new LevelDirector().Construct(builder);

        Assert.Equal(
        [
            "BuildTerrain", "PlaceExitAndDoor", "PlaceStartTiles", "PlaceLevers",
            "PlacePowerObstacles", "PlaceItems", "PlaceZombies", "GetResult",
        ], builder.Calls);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void TwoConcreteBuilders_SameDirector_ValidLevelsOfTheLevelsTheme(int index)
    {
        var definition = Catalog.GetLevel(index);
        var theme = ThemeFactories.For(definition.Theme, Catalog);
        var seed = Levels.Generate(definition, seed: 11).Seed; // a seed that the provider found valid
        ILevelBuilder[] builders =
        [
            new ProceduralLevelBuilder(Catalog, definition, theme, seed, new GameOptions(), Generation),
            new PresetLevelBuilder(Catalog, definition, theme, PresetMaps.ReadRows("arena")),
        ];

        var levels = builders.Select(b => new LevelDirector().Construct(b)).ToList();

        Assert.All(levels, level =>
        {
            Assert.Empty(LevelValidator.Validate(level, Generation.MinZombieDistanceFromStart));
            Assert.Equal(definition.Theme, level.Theme);
        });
        Assert.NotEqual(SnapshotMapper.ToRows(levels[0]), SnapshotMapper.ToRows(levels[1]));
    }

    [Fact]
    public void Provider_RetriesWithNewSeeds_UntilTheLevelIsValid()
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var level = Levels.Generate(Catalog.GetLevel(5), seed);

            Assert.Equal(0, (level.Seed - seed) % 7919); // the seed of the attempt that worked
            Assert.Empty(LevelValidator.Validate(level, Generation.MinZombieDistanceFromStart));
        }
    }

    [Fact]
    public void Provider_GivesUpAfterMaxAttempts()
    {
        var level1 = Catalog.GetLevel(1);
        var impossible = new LevelDefinition
        {
            Index = 1, Theme = level1.Theme, ObstacleCount = level1.ObstacleCount, ObstacleTable = level1.ObstacleTable,
            ZombieCount = 1000, ZombieTable = level1.ZombieTable,
        };
        var provider = new LevelProvider(Catalog, new GameOptions(), new GenerationOptions { MaxAttempts = 3 });

        var ex = Assert.Throws<LevelGenerationException>(() => provider.Generate(impossible, seed: 1));

        Assert.Contains("no room for zombies", ex.Errors);
    }

    [Fact]
    public void Provider_PlaysThePresetFromSettings()
    {
        var provider = new LevelProvider(Catalog, new GameOptions(), new GenerationOptions { PresetLevel = "tutorial" });

        var level = provider.CreateLevel(3, seed: 0);

        Assert.Equal(PresetMaps.ReadRows("tutorial"), SnapshotMapper.ToRows(level));
    }

    [Fact]
    public void Provider_ReportsAnUnknownPreset()
    {
        var provider = new LevelProvider(Catalog, new GameOptions(), new GenerationOptions { PresetLevel = "nope" });

        var ex = Assert.Throws<LevelGenerationException>(() => provider.CreateLevel(1, 0));

        Assert.Contains("Preset 'nope' not found", ex.Message);
    }
}
