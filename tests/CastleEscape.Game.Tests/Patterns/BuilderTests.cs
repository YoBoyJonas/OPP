using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class BuilderTests
{
    private static ContentCatalog Catalog => ContentCatalog.Instance;
    private static readonly GenerationOptions Generation = new();
    private static readonly LevelDirector Director = new(Catalog, Generation);

    /// <summary>Records the calls it gets; wraps the tutorial preset so the result is a valid level.</summary>
    private sealed class RecordingBuilder(bool isRandom, int failures = 0) : ILevelBuilder
    {
        private readonly PresetLevelBuilder _inner = PresetLevelBuilder.FromPreset(Catalog, "tutorial");
        private int _failuresLeft = failures;

        public List<string> Calls { get; } = [];
        public List<int> Seeds { get; } = [];
        public bool IsRandom => isRandom;

        public void Reset(LevelDefinition definition, int seed, IThemeFactory theme)
        {
            Calls.Add(nameof(Reset));
            Seeds.Add(seed);
            _inner.Reset(definition, seed, theme);
        }

        public void BuildTerrain() { Calls.Add(nameof(BuildTerrain)); _inner.BuildTerrain(); }
        public void PlaceExitAndDoor() { Calls.Add(nameof(PlaceExitAndDoor)); _inner.PlaceExitAndDoor(); }
        public void PlaceStartTiles() { Calls.Add(nameof(PlaceStartTiles)); _inner.PlaceStartTiles(); }

        public void PlaceLevers()
        {
            Calls.Add(nameof(PlaceLevers));
            if (_failuresLeft-- > 0)
            {
                throw new LevelBuildException("no room for two levers");
            }
            _inner.PlaceLevers();
        }

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
    public void AtLeastTwoConcreteBuilders()
    {
        var builders = typeof(ILevelBuilder).Assembly.GetTypes()
            .Where(t => typeof(ILevelBuilder).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .ToList();

        Assert.Contains(typeof(ProceduralLevelBuilder), builders);
        Assert.Contains(typeof(PresetLevelBuilder), builders);
    }

    [Fact]
    public void Director_CallsTheStepsInOrder()
    {
        var builder = new RecordingBuilder(isRandom: false);

        Director.Construct(builder, Catalog.GetLevel(1), seed: 5);

        Assert.Equal(["Reset", .. LevelDirector.Steps, "GetResult"], builder.Calls);
    }

    [Fact]
    public void Director_RetriesRandomBuildersWithNewSeeds()
    {
        var builder = new RecordingBuilder(isRandom: true, failures: 2);

        Director.Construct(builder, Catalog.GetLevel(1), seed: 5);

        Assert.Equal([5, 5 + 7919, 5 + 2 * 7919], builder.Seeds);
    }

    [Fact]
    public void Director_TriesDeterministicBuildersOnce()
    {
        var builder = new RecordingBuilder(isRandom: false, failures: 1);

        var ex = Assert.Throws<LevelGenerationException>(() => Director.Construct(builder, Catalog.GetLevel(1), seed: 5));

        Assert.Single(builder.Seeds);
        Assert.Contains("no room for two levers", ex.Errors);
    }

    [Fact]
    public void Director_RejectsInvalidPresets()
    {
        var builder = new PresetLevelBuilder(Catalog,
        [
            "#########",
            "#1....L.#",
            "#~~~~~~~#",
            "#.L....2#",
            "####D####",
            "###.EE.##",
            "#########",
        ]);

        var ex = Assert.Throws<LevelGenerationException>(() => Director.Construct(builder, Catalog.GetLevel(1), seed: 0));

        Assert.Contains(ex.Errors, e => e.StartsWith("D5"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void BothBuilders_MakeValidLevelsOfTheLevelsTheme(int index)
    {
        var definition = Catalog.GetLevel(index);
        ILevelBuilder[] builders =
        [
            new ProceduralLevelBuilder(Catalog, new GameOptions(), Generation),
            PresetLevelBuilder.FromPreset(Catalog, "arena"),
        ];

        foreach (var builder in builders)
        {
            var level = Director.Construct(builder, definition, seed: 11);

            Assert.Empty(LevelValidator.Validate(level, Generation.MinZombieDistanceFromStart));
            Assert.Equal(definition.Theme, level.Theme);
            Assert.All(level.Zombies, z => Assert.Equal(ThemeFactories.For(definition, Catalog).CreateZombie("x", z.Definition, z.Tile).GetType(), z.GetType()));
        }
    }

    [Fact]
    public void Provider_ReportsAnUnknownPreset()
    {
        var provider = new LevelProvider(Catalog, new GameOptions(), new GenerationOptions { PresetLevel = "nope" });

        var ex = Assert.Throws<LevelGenerationException>(() => provider.CreateLevel(1, 0));

        Assert.Contains("Preset 'nope' not found", ex.Message);
    }

    [Fact]
    public void Demo_BuildsTwoValidLevels()
    {
        var result = new BuilderDemo().Run(DemoOptions.None);

        var builders = Assert.IsType<Dictionary<string, object?>>(result.Evidence["builders"]);
        Assert.Equal(2, builders.Count);
        Assert.All(builders.Values, b => Assert.Equal(0, ((Dictionary<string, object?>)b!)["validationErrors"]));
    }
}
