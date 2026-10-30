using CastleEscape.Game.Content;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.Items;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>Concrete builder: a level from a hand-made map in <c>content/presets</c>.</summary>
public sealed class PresetLevelBuilder : ILevelBuilder
{
    private readonly ContentCatalog _catalog;
    private readonly LevelDefinition _definition;
    private readonly IThemeFactory _theme;
    private readonly IReadOnlyList<string> _rows;
    private readonly LevelState _level;

    public PresetLevelBuilder(ContentCatalog catalog, LevelDefinition definition, IThemeFactory theme, IReadOnlyList<string> rows, int seed = 0)
    {
        if (rows.Count == 0 || rows.Any(r => r.Length != rows[0].Length))
        {
            throw new LevelBuildException("Preset rows must be non-empty and all the same length.");
        }
        _catalog = catalog;
        _definition = definition;
        _theme = theme;
        _rows = rows;
        _level = new LevelState(definition, seed, new Grid(rows[0].Length, rows.Count));
    }

    private IEnumerable<GridPos> Find(params char[] chars) =>
        _level.Grid.Positions().Where(p => chars.Contains(CharAt(p)));

    private char CharAt(GridPos pos) => _rows[pos.Y][pos.X];

    public void BuildTerrain()
    {
        var wall = _theme.CreateWall();
        foreach (var pos in _level.Grid.Positions())
        {
            TerrainKind terrain;
            try
            {
                terrain = MapLegend.TerrainOf(CharAt(pos));
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
        foreach (var pos in _level.Grid.Positions())
        {
            var c = CharAt(pos);
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
        var type = _definition.ZombieTable.Count > 0 ? _catalog.GetZombie(_definition.ZombieTable[0].Id) : _catalog.Zombies[0];
        foreach (var pos in Find(MapLegend.ZombieSpawn))
        {
            _level.AddZombie(_theme.CreateZombie(_level.NextEntityId("zombie"), type, pos));
        }
    }

    public LevelState GetResult() => _level;
}
