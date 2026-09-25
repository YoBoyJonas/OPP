using CastleEscape.Game.Content;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.Items;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Builds a level from a hand-made ASCII map (<c>content/presets/*.txt</c>, legend in <see cref="MapLegend"/>).
/// The same steps as the procedural builder, but each one reads its objects from the map.
/// The seed is ignored, so one attempt is enough.
/// </summary>
[DesignPattern("Builder", "ConcreteBuilder")]
public sealed class PresetLevelBuilder : ILevelBuilder
{
    private readonly ContentCatalog _catalog;
    private readonly IReadOnlyList<string> _rows;
    private LevelDefinition _definition = null!;
    private IThemeFactory _theme = null!;
    private LevelState _level = null!;

    public PresetLevelBuilder(ContentCatalog catalog, IReadOnlyList<string> rows)
    {
        if (rows.Count == 0 || rows.Any(r => r.Length != rows[0].Length))
        {
            throw new LevelBuildException("Preset rows must be non-empty and all the same length.");
        }
        _catalog = catalog;
        _rows = rows;
    }

    /// <summary>A builder for <c>content/presets/{name}.txt</c>.</summary>
    public static PresetLevelBuilder FromPreset(ContentCatalog catalog, string name) => new(catalog, PresetMaps.ReadRows(name));

    public bool IsRandom => false;

    public void Reset(LevelDefinition definition, int seed, IThemeFactory theme)
    {
        _definition = definition;
        _theme = theme;
        _level = new LevelState(definition, seed, new Grid(_rows[0].Length, _rows.Count));
    }

    /// <summary>Every tile of the map with its legend character.</summary>
    private IEnumerable<(GridPos Pos, char Char)> Cells() => _level.Grid.Positions().Select(p => (p, _rows[p.Y][p.X]));

    private IEnumerable<GridPos> Find(params char[] chars) => Cells().Where(c => chars.Contains(c.Char)).Select(c => c.Pos);

    public void BuildTerrain()
    {
        // Walls here; water, pits, the door and exits in their own steps. Everything else is floor.
        var wall = _theme.CreateWall();
        foreach (var (pos, c) in Cells())
        {
            TerrainKind terrain;
            try
            {
                terrain = MapLegend.TerrainOf(c);
            }
            catch (FormatException ex)
            {
                throw new LevelBuildException($"{ex.Message} (at {pos})");
            }
            if (terrain == TerrainKind.Wall)
            {
                _level.Grid.SetTile(pos, TerrainKind.Wall, wall);
            }
        }
    }

    public void PlaceExitAndDoor()
    {
        foreach (var pos in Find(MapLegend.Door))
        {
            _level.Grid.SetTile(pos, TerrainKind.Door);
            _level.SetDoor(new ExitDoor("door", pos));
        }
        foreach (var pos in Find(MapLegend.Exit))
        {
            _level.Grid.SetTile(pos, TerrainKind.Exit);
            _level.AddExitTile(pos);
        }
    }

    public void PlaceStartTiles()
    {
        var p1 = Find(MapLegend.Player1Start).ToList();
        var p2 = Find(MapLegend.Player2Start).ToList();
        if (p1.Count != 1 || p2.Count != 1)
        {
            throw new LevelBuildException("Preset needs exactly one '1' and one '2' start tile.");
        }
        _level.AddStartTile(p1[0]);
        _level.AddStartTile(p2[0]);
    }

    public void PlaceLevers()
    {
        foreach (var pos in Find(MapLegend.Lever))
        {
            _level.AddLever(new Lever(_level.NextEntityId("lever"), pos));
        }
    }

    public void PlacePowerObstacles()
    {
        var water = _theme.CreateWater();
        var pit = _theme.CreatePit();
        foreach (var pos in Find(MapLegend.Water))
        {
            _level.Grid.SetTile(pos, TerrainKind.Water, water);
        }
        foreach (var pos in Find(MapLegend.Pit))
        {
            _level.Grid.SetTile(pos, TerrainKind.Pit, pit);
        }
    }

    public void PlaceItems()
    {
        foreach (var (pos, c) in Cells())
        {
            Consumable? item = c switch
            {
                MapLegend.HealthItem => _catalog.Consumables.First(i => i.Kind == ConsumableKind.Health),
                MapLegend.RewardItem => _catalog.Consumables.First(i => i.Kind == ConsumableKind.Reward),
                MapLegend.JumpItem or MapLegend.SprintItem or MapLegend.SwimItem =>
                    _catalog.Consumables.First(i => i.Grant?.Power == MapLegend.PowerOf(c)),
                _ => null,
            };
            if (item is not null)
            {
                ItemSpawners.Spawn(_level, item, pos);
            }
        }
    }

    public void PlaceZombies()
    {
        // Every 'Z' is the level's first zombie type.
        var type = _definition.ZombieTable.Count > 0 ? _catalog.GetZombie(_definition.ZombieTable[0].Id) : _catalog.Zombies[0];
        foreach (var pos in Find(MapLegend.ZombieSpawn))
        {
            _level.AddZombie(_theme.CreateZombie(_level.NextEntityId("zombie"), type, pos));
        }
    }

    public LevelState GetResult() => _level;
}

/// <summary>The preset map files in <c>content/presets</c>.</summary>
public static class PresetMaps
{
    public static string Directory => Path.Combine(ContentLoader.DefaultDirectory, "presets");

    /// <summary>Names of the available presets (file names without .txt).</summary>
    public static IReadOnlyList<string> Available() =>
        System.IO.Directory.Exists(Directory)
            ? System.IO.Directory.GetFiles(Directory, "*.txt").Select(Path.GetFileNameWithoutExtension).OfType<string>().Order().ToList()
            : [];

    public static string[] ReadRows(string name)
    {
        var path = Path.Combine(Directory, name + ".txt");
        if (!File.Exists(path))
        {
            throw new LevelBuildException($"Preset '{name}' not found. Available: {string.Join(", ", Available())}");
        }
        return File.ReadAllLines(path).Where(r => r.Length > 0).ToArray();
    }
}
