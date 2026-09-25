using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Items;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Generates a random level from a <see cref="LevelDefinition"/> with a seeded RNG, validates it and retries
/// with a new seed until it is valid (start-level activity diagram). Same seed, same level.
/// </summary>
public class LevelGenerator(ContentCatalog catalog, GameOptions game, GenerationOptions generation)
{
    public LevelState Generate(LevelDefinition definition, int seed)
    {
        var errors = new List<string>();
        for (var attempt = 0; attempt < generation.MaxAttempts; attempt++)
        {
            var attemptSeed = unchecked(seed + attempt * 7919);
            var level = TryBuild(definition, attemptSeed, errors);
            if (level is null)
            {
                continue;
            }

            errors = LevelValidator.Validate(level, generation.MinZombieDistanceFromStart);
            if (errors.Count == 0)
            {
                return level;
            }
        }

        throw new LevelGenerationException(definition.Index, errors.Count > 0 ? errors : ["no attempt succeeded"]);
    }

    private LevelState? TryBuild(LevelDefinition definition, int seed, List<string> errors)
    {
        errors.Clear();
        var rng = new Random(seed);
        var width = definition.RoomSize?.Width ?? game.GridWidth;
        var height = definition.RoomSize?.Height ?? game.GridHeight;
        var grid = new Grid(width, height);
        var level = new LevelState(definition, seed, grid);
        var wall = catalog.Obstacles.OfType<Wall>().First();
        var reserved = new HashSet<GridPos>();

        // 1. Border walls.
        foreach (var pos in grid.Positions().Where(p => p.X == 0 || p.Y == 0 || p.X == width - 1 || p.Y == height - 1))
        {
            grid.SetTile(pos, TerrainKind.Wall, wall);
        }

        // 2. Exit alcove against the top or bottom border: two exit tiles behind the door.
        var bottom = rng.Next(2) == 0;
        var exitY = bottom ? height - 2 : 1;
        var inward = bottom ? -1 : 1;
        var exitX = rng.Next(3, width - 4);
        var door = new GridPos(exitX, exitY + inward);
        var doorEntry = new GridPos(exitX, exitY + 2 * inward);
        foreach (var x in new[] { exitX - 1, exitX + 2 })
        {
            grid.SetTile(new GridPos(x, exitY), TerrainKind.Wall, wall);
        }
        foreach (var x in new[] { exitX - 1, exitX + 1, exitX + 2 })
        {
            grid.SetTile(new GridPos(x, exitY + inward), TerrainKind.Wall, wall);
        }
        grid.SetTile(door, TerrainKind.Door);
        level.SetDoor(new ExitDoor("door", door));
        foreach (var x in new[] { exitX, exitX + 1 })
        {
            var exit = new GridPos(x, exitY);
            grid.SetTile(exit, TerrainKind.Exit);
            level.AddExitTile(exit);
        }
        reserved.UnionWith([door, doorEntry, doorEntry.Step(Contracts.Direction.Left), doorEntry.Step(Contracts.Direction.Right)]);

        // 3. Interior wall segments.
        var segments = width * height / 45;
        for (var i = 0; i < segments; i++)
        {
            var horizontal = rng.Next(2) == 0;
            var length = rng.Next(Math.Max(2, wall.MinSize), Math.Max(3, wall.MaxSize) + 1);
            var start = new GridPos(rng.Next(2, width - 2), rng.Next(2, height - 2));
            for (var k = 0; k < length; k++)
            {
                var pos = horizontal ? new GridPos(start.X + k, start.Y) : new GridPos(start.X, start.Y + k);
                if (pos.X < 1 || pos.Y < 1 || pos.X > width - 2 || pos.Y > height - 2 || reserved.Contains(pos)
                    || grid.GetTerrain(pos) != TerrainKind.Floor)
                {
                    break;
                }
                grid.SetTile(pos, TerrainKind.Wall, wall);
            }
        }

        bool Walkable(GridPos p) => MovementRules.IsWalkableWithoutPowers(grid, p, doorOpen: false);

        // 4. Start tiles on the side away from the exit, both in the same open area.
        var farRows = bottom ? Enumerable.Range(1, 3) : Enumerable.Range(height - 4, 3);
        var startCandidates = grid.Positions().Where(p => farRows.Contains(p.Y) && grid.GetTerrain(p) == TerrainKind.Floor).ToList();
        if (startCandidates.Count < 2)
        {
            errors.Add("no room for start tiles");
            return null;
        }
        var start1 = startCandidates[rng.Next(startCandidates.Count)];
        var reach = PathFinder.Distances(grid, start1, Walkable);
        var start2Candidates = startCandidates.Where(p => p != start1 && reach.ContainsKey(p) && p.ManhattanTo(start1) >= 6).ToList();
        if (start2Candidates.Count == 0 || !reach.ContainsKey(doorEntry))
        {
            errors.Add("start area is not connected to the door");
            return null;
        }
        var start2 = start2Candidates[rng.Next(start2Candidates.Count)];
        level.AddStartTile(start1);
        level.AddStartTile(start2);
        reserved.UnionWith([start1, start2]);

        // 5. Two levers, apart from each other and from the starts.
        var leverCandidates = reach.Keys
            .Where(p => grid.GetTerrain(p) == TerrainKind.Floor && !reserved.Contains(p)
                        && p.ManhattanTo(start1) >= 3 && p.ManhattanTo(start2) >= 3)
            .ToList();
        var levers = new List<GridPos>();
        for (var tries = 0; tries < 50 && levers.Count < 2 && leverCandidates.Count > 0; tries++)
        {
            var candidate = leverCandidates[rng.Next(leverCandidates.Count)];
            if (levers.All(l => l.ManhattanTo(candidate) >= 6))
            {
                levers.Add(candidate);
            }
        }
        if (levers.Count < 2)
        {
            errors.Add("no room for two levers");
            return null;
        }
        for (var i = 0; i < levers.Count; i++)
        {
            level.AddLever(new Lever($"lever-{i + 1}", levers[i]));
        }
        reserved.UnionWith(levers);

        // 6. Power obstacles (water, pits). Each patch is kept only if the critical path stays power-free (D5).
        var critical = new List<GridPos> { start2, doorEntry };
        critical.AddRange(levers);
        var placedPatches = 0;
        for (var tries = 0; tries < definition.ObstacleCount * 10 && placedPatches < definition.ObstacleCount; tries++)
        {
            var obstacle = PickWeighted(rng, definition.ObstacleTable, id => catalog.GetObstacle(id));
            if (obstacle.Kind == ObstacleKind.Wall)
            {
                continue;
            }
            var terrain = obstacle.Kind == ObstacleKind.Water ? TerrainKind.Water : TerrainKind.Pit;
            var size = rng.Next(obstacle.MinSize, obstacle.MaxSize + 1);
            var patch = GrowPatch(rng, grid, reserved, size);
            if (patch.Count == 0)
            {
                continue;
            }

            foreach (var pos in patch) grid.SetTile(pos, terrain, obstacle);
            var reachable = PathFinder.Distances(grid, start1, Walkable);
            if (critical.All(reachable.ContainsKey))
            {
                placedPatches++;
            }
            else
            {
                foreach (var pos in patch) grid.SetTile(pos, TerrainKind.Floor);
            }
        }
        if (placedPatches == 0)
        {
            errors.Add("GEN-3: no power obstacle could be placed");
            return null;
        }

        // 7. Items. Power items only where no power is needed to get them; others anywhere reachable.
        var reachNoPower = PathFinder.Distances(grid, start1, Walkable);
        var reachAnyPower = PathFinder.Distances(grid, start1, p => grid.GetTerrain(p) is not (TerrainKind.Wall or TerrainKind.Door));
        var occupied = new HashSet<GridPos>(reserved);
        for (var i = 0; i < definition.PickupCount; i++)
        {
            var consumable = PickWeighted(rng, definition.ConsumableTable, id => catalog.GetConsumable(id));
            var area = consumable.Kind == ConsumableKind.Power ? reachNoPower : reachAnyPower;
            var spots = area.Keys.Where(p => grid.GetTerrain(p) == TerrainKind.Floor && !occupied.Contains(p)).ToList();
            if (spots.Count == 0)
            {
                break;
            }
            var spot = spots[rng.Next(spots.Count)];
            occupied.Add(spot);
            ItemSpawners.Spawn(level, consumable, spot);
        }

        // 8. Zombies, far enough from both starts.
        var fromStart2 = PathFinder.Distances(grid, start2, Walkable);
        var zombieSpots = reachNoPower
            .Where(kv => kv.Value >= generation.MinZombieDistanceFromStart
                         && fromStart2.GetValueOrDefault(kv.Key, int.MaxValue) >= generation.MinZombieDistanceFromStart
                         && grid.GetTerrain(kv.Key) == TerrainKind.Floor && !occupied.Contains(kv.Key))
            .Select(kv => kv.Key)
            .ToList();
        if (zombieSpots.Count < definition.ZombieCount)
        {
            errors.Add("no room for zombies");
            return null;
        }
        for (var i = 0; i < definition.ZombieCount; i++)
        {
            var spot = zombieSpots[rng.Next(zombieSpots.Count)];
            zombieSpots.Remove(spot);
            occupied.Add(spot);
            var type = PickWeighted(rng, definition.ZombieTable, id => catalog.GetZombie(id));
            level.AddZombie(new ZombieEntity($"zombie-{i + 1}", type, spot));
        }

        return level;
    }

    private static List<GridPos> GrowPatch(Random rng, Grid grid, HashSet<GridPos> reserved, int size)
    {
        bool Free(GridPos p) => grid.GetTerrain(p) == TerrainKind.Floor && !reserved.Contains(p)
                                && p.X > 1 && p.Y > 1 && p.X < grid.Width - 2 && p.Y < grid.Height - 2;

        var seed = new GridPos(rng.Next(2, grid.Width - 2), rng.Next(2, grid.Height - 2));
        if (!Free(seed))
        {
            return [];
        }

        var patch = new List<GridPos> { seed };
        for (var tries = 0; patch.Count < size && tries < size * 8; tries++)
        {
            var from = patch[rng.Next(patch.Count)];
            var next = from.Step(DirectionExtensions.Cardinal[rng.Next(4)]);
            if (Free(next) && !patch.Contains(next))
            {
                patch.Add(next);
            }
        }
        return patch;
    }

    private static T PickWeighted<T>(Random rng, IReadOnlyList<SpawnTableEntry> table, Func<string, T> resolve)
    {
        var total = table.Sum(e => e.Weight);
        var roll = rng.NextDouble() * total;
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
