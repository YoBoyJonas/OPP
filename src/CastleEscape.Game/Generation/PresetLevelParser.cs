using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.Items;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>Turns an ASCII preset map (content/presets/*.txt) into a <see cref="LevelState"/>.</summary>
public static class PresetLevelParser
{
    public static string PresetDirectory => Path.Combine(ContentLoader.DefaultDirectory, "presets");

    /// <summary>Names of the available presets (file names without .txt).</summary>
    public static IReadOnlyList<string> AvailablePresets() =>
        Directory.Exists(PresetDirectory)
            ? Directory.GetFiles(PresetDirectory, "*.txt").Select(Path.GetFileNameWithoutExtension).OfType<string>().Order().ToList()
            : [];

    public static string[] ReadRows(string presetName)
    {
        var path = Path.Combine(PresetDirectory, presetName + ".txt");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Preset '{presetName}' not found. Available: {string.Join(", ", AvailablePresets())}", path);
        }
        return File.ReadAllLines(path).Where(r => r.Length > 0).ToArray();
    }

    public static LevelState Parse(IReadOnlyList<string> rows, LevelDefinition definition, ContentCatalog catalog, int seed = 0)
    {
        if (rows.Count == 0 || rows.Any(r => r.Length != rows[0].Length))
        {
            throw new FormatException("Preset rows must be non-empty and all the same length.");
        }

        var grid = new Grid(rows[0].Length, rows.Count);
        var level = new LevelState(definition, seed, grid);
        var wall = catalog.Obstacles.OfType<Wall>().First();
        var water = catalog.Obstacles.OfType<Water>().First();
        var pit = catalog.Obstacles.OfType<Pit>().First();
        var zombieType = definition.ZombieTable.Count > 0 ? catalog.GetZombie(definition.ZombieTable[0].Id) : catalog.Zombies[0];
        GridPos? start1 = null, start2 = null;
        int zombies = 0, levers = 0;

        foreach (var pos in grid.Positions())
        {
            var c = rows[pos.Y][pos.X];
            var terrain = MapLegend.TerrainOf(c);
            Obstacle? obstacle = terrain switch
            {
                TerrainKind.Wall => wall,
                TerrainKind.Water => water,
                TerrainKind.Pit => pit,
                _ => null,
            };
            grid.SetTile(pos, terrain, obstacle);

            switch (c)
            {
                case MapLegend.Player1Start: start1 = pos; break;
                case MapLegend.Player2Start: start2 = pos; break;
                case MapLegend.Lever: level.AddLever(new Lever($"lever-{++levers}", pos)); break;
                case MapLegend.Door: level.SetDoor(new ExitDoor("door", pos)); break;
                case MapLegend.Exit: level.AddExitTile(pos); break;
                case MapLegend.ZombieSpawn: level.AddZombie(new ZombieEntity($"zombie-{++zombies}", zombieType, pos)); break;
                case MapLegend.HealthItem:
                    ItemSpawners.Spawn(level, catalog.Consumables.First(i => i.Kind == ConsumableKind.Health), pos);
                    break;
                case MapLegend.RewardItem:
                    ItemSpawners.Spawn(level, catalog.Consumables.First(i => i.Kind == ConsumableKind.Reward), pos);
                    break;
                case MapLegend.JumpItem or MapLegend.SprintItem or MapLegend.SwimItem:
                    var power = MapLegend.PowerOf(c);
                    ItemSpawners.Spawn(level, catalog.Consumables.First(i => i.Grant?.Power == power), pos);
                    break;
            }
        }

        if (start1 is null || start2 is null)
        {
            throw new FormatException("Preset needs both start tiles '1' and '2'.");
        }
        level.AddStartTile(start1.Value);
        level.AddStartTile(start2.Value);
        return level;
    }
}
