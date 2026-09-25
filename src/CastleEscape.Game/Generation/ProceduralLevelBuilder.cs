using CastleEscape.Contracts;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.Items;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Builds a random level from a <see cref="LevelDefinition"/> with a seeded RNG. Same seed, same level.
/// The exit is in an alcove on the top or bottom border; the players start on the opposite side.
/// </summary>
[DesignPattern("Builder", "ConcreteBuilder")]
public sealed class ProceduralLevelBuilder(ContentCatalog catalog, GameOptions game, GenerationOptions generation) : ILevelBuilder
{
    private LevelDefinition _definition = null!;
    private IThemeFactory _theme = null!;
    private Random _rng = null!;
    private Grid _grid = null!;
    private LevelState _level = null!;
    private Wall _wall = null!;
    private readonly HashSet<GridPos> _reserved = [];
    private readonly HashSet<GridPos> _occupied = [];

    private bool _exitAtBottom;
    private int _exitX, _exitY, _inward;
    private GridPos _doorEntry;
    private GridPos _start1, _start2;
    private readonly List<GridPos> _levers = [];

    public bool IsRandom => true;

    public void Reset(LevelDefinition definition, int seed, IThemeFactory theme)
    {
        _definition = definition;
        _theme = theme;
        _rng = new Random(seed);
        _grid = new Grid(definition.RoomSize?.Width ?? game.GridWidth, definition.RoomSize?.Height ?? game.GridHeight);
        _level = new LevelState(definition, seed, _grid);
        _wall = theme.CreateWall();
        _reserved.Clear();
        _occupied.Clear();
        _levers.Clear();
    }

    private int Width => _grid.Width;
    private int Height => _grid.Height;

    private bool Walkable(GridPos p) => MovementRules.IsWalkableWithoutPowers(_grid, p, doorOpen: false);

    public void BuildTerrain()
    {
        foreach (var pos in _grid.Positions().Where(p => p.X == 0 || p.Y == 0 || p.X == Width - 1 || p.Y == Height - 1))
        {
            _grid.SetTile(pos, TerrainKind.Wall, _wall);
        }

        // Decide where the exit goes now, so inner walls keep the way to the door free.
        _exitAtBottom = _rng.Next(2) == 0;
        _exitY = _exitAtBottom ? Height - 2 : 1;
        _inward = _exitAtBottom ? -1 : 1;
        _exitX = _rng.Next(3, Width - 4);
        var door = new GridPos(_exitX, _exitY + _inward);
        _doorEntry = new GridPos(_exitX, _exitY + 2 * _inward);
        _reserved.UnionWith([door, _doorEntry, _doorEntry.Step(Direction.Left), _doorEntry.Step(Direction.Right)]);

        var segments = Width * Height / 45;
        for (var i = 0; i < segments; i++)
        {
            var horizontal = _rng.Next(2) == 0;
            var length = _rng.Next(Math.Max(2, _wall.MinSize), Math.Max(3, _wall.MaxSize) + 1);
            var start = new GridPos(_rng.Next(2, Width - 2), _rng.Next(2, Height - 2));
            for (var k = 0; k < length; k++)
            {
                var pos = horizontal ? new GridPos(start.X + k, start.Y) : new GridPos(start.X, start.Y + k);
                if (pos.X < 1 || pos.Y < 1 || pos.X > Width - 2 || pos.Y > Height - 2 || _reserved.Contains(pos)
                    || _grid.GetTerrain(pos) != TerrainKind.Floor)
                {
                    break;
                }
                _grid.SetTile(pos, TerrainKind.Wall, _wall);
            }
        }
    }

    public void PlaceExitAndDoor()
    {
        // An alcove against the border: two exit tiles, walled in, reachable only through the door.
        foreach (var x in new[] { _exitX - 1, _exitX + 2 })
        {
            _grid.SetTile(new GridPos(x, _exitY), TerrainKind.Wall, _wall);
        }
        foreach (var x in new[] { _exitX - 1, _exitX + 1, _exitX + 2 })
        {
            _grid.SetTile(new GridPos(x, _exitY + _inward), TerrainKind.Wall, _wall);
        }
        var door = new GridPos(_exitX, _exitY + _inward);
        _grid.SetTile(door, TerrainKind.Door);
        _level.SetDoor(new ExitDoor("door", door));
        foreach (var x in new[] { _exitX, _exitX + 1 })
        {
            var exit = new GridPos(x, _exitY);
            _grid.SetTile(exit, TerrainKind.Exit);
            _level.AddExitTile(exit);
        }
    }

    public void PlaceStartTiles()
    {
        // On the side away from the exit, both in the open area that leads to the door.
        var farRows = _exitAtBottom ? Enumerable.Range(1, 3) : Enumerable.Range(Height - 4, 3);
        var candidates = _grid.Positions().Where(p => farRows.Contains(p.Y) && _grid.GetTerrain(p) == TerrainKind.Floor).ToList();
        if (candidates.Count < 2)
        {
            throw new LevelBuildException("no room for start tiles");
        }
        _start1 = candidates[_rng.Next(candidates.Count)];
        var reach = PathFinder.Distances(_grid, _start1, Walkable);
        var second = candidates.Where(p => p != _start1 && reach.ContainsKey(p) && p.ManhattanTo(_start1) >= 6).ToList();
        if (second.Count == 0 || !reach.ContainsKey(_doorEntry))
        {
            throw new LevelBuildException("start area is not connected to the door");
        }
        _start2 = second[_rng.Next(second.Count)];
        _level.AddStartTile(_start1);
        _level.AddStartTile(_start2);
        _reserved.UnionWith([_start1, _start2]);
    }

    public void PlaceLevers()
    {
        // Two levers, apart from each other and from the starts, reachable without powers.
        var reach = PathFinder.Distances(_grid, _start1, Walkable);
        var candidates = reach.Keys
            .Where(p => _grid.GetTerrain(p) == TerrainKind.Floor && !_reserved.Contains(p)
                        && p.ManhattanTo(_start1) >= 3 && p.ManhattanTo(_start2) >= 3)
            .ToList();
        for (var tries = 0; tries < 50 && _levers.Count < 2 && candidates.Count > 0; tries++)
        {
            var candidate = candidates[_rng.Next(candidates.Count)];
            if (_levers.All(l => l.ManhattanTo(candidate) >= 6))
            {
                _levers.Add(candidate);
            }
        }
        if (_levers.Count < 2)
        {
            throw new LevelBuildException("no room for two levers");
        }
        foreach (var lever in _levers)
        {
            _level.AddLever(new Lever(_level.NextEntityId("lever"), lever));
        }
        _reserved.UnionWith(_levers);
    }

    public void PlacePowerObstacles()
    {
        // Each patch is kept only if the critical path stays walkable without powers (D5).
        var critical = new List<GridPos> { _start2, _doorEntry };
        critical.AddRange(_levers);
        var placed = 0;
        for (var tries = 0; tries < _definition.ObstacleCount * 10 && placed < _definition.ObstacleCount; tries++)
        {
            var obstacle = _theme.CreateObstacle(PickWeighted(_definition.ObstacleTable, id => catalog.GetObstacle(id)).Kind);
            if (obstacle.Kind == ObstacleKind.Wall)
            {
                continue;
            }
            var terrain = obstacle.Kind == ObstacleKind.Water ? TerrainKind.Water : TerrainKind.Pit;
            var patch = GrowPatch(_rng.Next(obstacle.MinSize, obstacle.MaxSize + 1));
            if (patch.Count == 0)
            {
                continue;
            }

            foreach (var pos in patch) _grid.SetTile(pos, terrain, obstacle);
            var reachable = PathFinder.Distances(_grid, _start1, Walkable);
            if (critical.All(reachable.ContainsKey))
            {
                placed++;
            }
            else
            {
                foreach (var pos in patch) _grid.SetTile(pos, TerrainKind.Floor);
            }
        }
        if (placed == 0)
        {
            throw new LevelBuildException("GEN-3: no power obstacle could be placed");
        }
    }

    public void PlaceItems()
    {
        // Power items only where no power is needed to get them; the others anywhere reachable.
        var reachNoPower = PathFinder.Distances(_grid, _start1, Walkable);
        var reachAnyPower = PathFinder.Distances(_grid, _start1, p => _grid.GetTerrain(p) is not (TerrainKind.Wall or TerrainKind.Door));
        _occupied.UnionWith(_reserved);
        for (var i = 0; i < _definition.PickupCount; i++)
        {
            var consumable = PickWeighted(_definition.ConsumableTable, id => catalog.GetConsumable(id));
            var area = consumable.Kind == ConsumableKind.Power ? reachNoPower : reachAnyPower;
            var spots = area.Keys.Where(p => _grid.GetTerrain(p) == TerrainKind.Floor && !_occupied.Contains(p)).ToList();
            if (spots.Count == 0)
            {
                break;
            }
            var spot = spots[_rng.Next(spots.Count)];
            _occupied.Add(spot);
            ItemSpawners.Spawn(_level, consumable, spot);
        }
    }

    public void PlaceZombies()
    {
        var fromStart1 = PathFinder.Distances(_grid, _start1, Walkable);
        var fromStart2 = PathFinder.Distances(_grid, _start2, Walkable);
        var spots = fromStart1
            .Where(kv => kv.Value >= generation.MinZombieDistanceFromStart
                         && fromStart2.GetValueOrDefault(kv.Key, int.MaxValue) >= generation.MinZombieDistanceFromStart
                         && _grid.GetTerrain(kv.Key) == TerrainKind.Floor && !_occupied.Contains(kv.Key))
            .Select(kv => kv.Key)
            .ToList();
        if (spots.Count < _definition.ZombieCount)
        {
            throw new LevelBuildException("no room for zombies");
        }
        for (var i = 0; i < _definition.ZombieCount; i++)
        {
            var spot = spots[_rng.Next(spots.Count)];
            spots.Remove(spot);
            _occupied.Add(spot);
            var type = PickWeighted(_definition.ZombieTable, id => catalog.GetZombie(id));
            _level.AddZombie(_theme.CreateZombie(_level.NextEntityId("zombie"), type, spot));
        }
    }

    public LevelState GetResult() => _level;

    private List<GridPos> GrowPatch(int size)
    {
        bool Free(GridPos p) => _grid.GetTerrain(p) == TerrainKind.Floor && !_reserved.Contains(p)
                                && p.X > 1 && p.Y > 1 && p.X < Width - 2 && p.Y < Height - 2;

        var seed = new GridPos(_rng.Next(2, Width - 2), _rng.Next(2, Height - 2));
        if (!Free(seed))
        {
            return [];
        }

        var patch = new List<GridPos> { seed };
        for (var tries = 0; patch.Count < size && tries < size * 8; tries++)
        {
            var from = patch[_rng.Next(patch.Count)];
            var next = from.Step(DirectionExtensions.Cardinal[_rng.Next(4)]);
            if (Free(next) && !patch.Contains(next))
            {
                patch.Add(next);
            }
        }
        return patch;
    }

    private T PickWeighted<T>(IReadOnlyList<SpawnTableEntry> table, Func<string, T> resolve)
    {
        var roll = _rng.NextDouble() * table.Sum(e => e.Weight);
        foreach (var entry in table)
        {
            roll -= entry.Weight;
            if (roll <= 0)
            {
                return resolve(entry.Id);
            }
        }
        return resolve(table[^1].Id);
    }
}
