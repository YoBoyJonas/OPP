namespace CastleEscape.Game.World;

/// <summary>
/// Every "may this entity enter that tile?" check (COL-1, COL-4, MOV-1, MOV-2, D7) in one place.
/// Seam for the P2 Chain of Responsibility.
/// </summary>
public static class MovementRules
{
    /// <param name="claimedByOthers">Tiles other players stand on or are stepping into (D7).</param>
    public static bool CanPlayerEnter(PlayerEntity player, GridPos target, LevelState level, IReadOnlyCollection<GridPos> claimedByOthers)
    {
        if (!level.Grid.InBounds(target))
        {
            return false;
        }

        var terrain = level.Grid.GetTerrain(target);
        if (terrain == TerrainKind.Wall)
        {
            return false;
        }
        if (terrain == TerrainKind.Door && level.Door is not { IsOpen: true })
        {
            return false;
        }
        if (!player.Abilities.CanEnter(terrain))
        {
            return false;
        }

        // D7: never share a tile, and never enter a tile another player is standing on or stepping into.
        return !claimedByOthers.Contains(target);
    }

    public static bool CanZombieEnter(GridPos target, LevelState level)
    {
        var tile = level.Grid.GetTile(target);
        return tile.Terrain switch
        {
            TerrainKind.Wall => false,
            TerrainKind.Door => level.Door is { IsOpen: true },
            TerrainKind.Water or TerrainKind.Pit => tile.Obstacle is { BlocksZombies: false },
            _ => true,
        };
    }

    /// <summary>
    /// Walkable with no powers at all. Used by generation to keep the critical path power-free (D5).
    /// </summary>
    public static bool IsWalkableWithoutPowers(Grid grid, GridPos pos, bool doorOpen) => grid.GetTerrain(pos) switch
    {
        TerrainKind.Floor or TerrainKind.Exit => true,
        TerrainKind.Door => doorOpen,
        _ => false,
    };
}
