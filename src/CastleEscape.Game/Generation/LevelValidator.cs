using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Checks a level against the generation rules: LVL-3, GEN-1, GEN-2, GEN-3 and D5.
/// Empty result = valid.
/// </summary>
public static class LevelValidator
{
    public static List<string> Validate(LevelState level, int minZombieDistanceFromStart)
    {
        var errors = new List<string>();
        var grid = level.Grid;

        // Required objects (LVL-3, GEN-3).
        if (level.StartTiles.Count != 2) errors.Add("Level needs exactly 2 start tiles.");
        if (level.Levers.Count < 2) errors.Add("GEN-3: level needs at least 2 levers.");
        if (level.Door is null) errors.Add("GEN-3: level needs an exit door.");
        if (level.ExitTiles.Count < 2) errors.Add("DOOR-2: level needs at least 2 exit tiles (players can't share one).");
        if (!grid.Positions().Any(p => grid.GetTerrain(p) is TerrainKind.Water or TerrainKind.Pit))
        {
            errors.Add("GEN-3: level needs at least one obstacle that requires a power.");
        }
        if (errors.Count > 0)
        {
            return errors;
        }

        // GEN-2: nothing on walls or start tiles; entities stand on floor.
        var starts = level.StartTiles.ToHashSet();
        foreach (var start in starts.Where(s => grid.GetTerrain(s) != TerrainKind.Floor))
        {
            errors.Add($"LVL-3: start tile {start} is not floor.");
        }
        var placed = level.Items.Select(i => ("item", i.Id, i.Tile))
            .Concat(level.Zombies.Select(z => ("zombie", z.Id, z.Tile)))
            .Concat(level.Levers.Select(l => ("lever", l.Id, l.Tile)));
        foreach (var (what, id, tile) in placed)
        {
            if (grid.GetTerrain(tile) != TerrainKind.Floor)
            {
                errors.Add($"GEN-2: {what} {id} at {tile} is on {grid.GetTerrain(tile)}.");
            }
            if (starts.Contains(tile))
            {
                errors.Add($"GEN-2: {what} {id} is on a start tile {tile}.");
            }
        }

        // D5: the critical path must be walkable without powers.
        var door = level.Door!;
        foreach (var start in level.StartTiles)
        {
            var closed = PathFinder.Distances(grid, start, p => MovementRules.IsWalkableWithoutPowers(grid, p, doorOpen: false));
            var open = PathFinder.Distances(grid, start, p => MovementRules.IsWalkableWithoutPowers(grid, p, doorOpen: true));

            foreach (var lever in level.Levers.Where(l => !closed.ContainsKey(l.Tile)))
            {
                errors.Add($"D5: {lever.Id} at {lever.Tile} is not reachable from start {start} without powers.");
            }
            if (!grid.Neighbours(door.Tile).Any(closed.ContainsKey))
            {
                errors.Add($"D5: the door at {door.Tile} is not reachable from start {start} without powers.");
            }
            foreach (var exit in level.ExitTiles.Where(e => !open.ContainsKey(e)))
            {
                errors.Add($"D5: exit tile {exit} is not reachable from start {start} through the open door.");
            }
            foreach (var zombie in level.Zombies.Where(z => closed.TryGetValue(z.Tile, out var d) && d < minZombieDistanceFromStart))
            {
                errors.Add($"Zombie {zombie.Id} at {zombie.Tile} is closer than {minZombieDistanceFromStart} steps to start {start}.");
            }
        }

        // Exit tiles must only be reachable through the door.
        foreach (var start in level.StartTiles)
        {
            var closed = PathFinder.Distances(grid, start, p => grid.GetTerrain(p) is not (TerrainKind.Wall or TerrainKind.Door));
            foreach (var exit in level.ExitTiles.Where(closed.ContainsKey))
            {
                errors.Add($"Exit tile {exit} can be reached without going through the door.");
            }
        }

        return errors;
    }
}
