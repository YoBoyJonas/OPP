using CastleEscape.Contracts;
using CastleEscape.Contracts.Gameplay;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Sessions;

/// <summary>
/// Turns the runtime world into protocol DTOs. The only place that does so (seam for the P2 Visitor).
/// </summary>
public static class SnapshotMapper
{
    public static SessionDto ToSessionDto(Guid sessionId, string joinCode, SessionPhase phase, int levelIndex, int maxLevel,
        IEnumerable<PlayerSlot> slots) =>
        new(sessionId, joinCode, phase, levelIndex, maxLevel,
            slots.Select(s => new SessionPlayerDto(s.PlayerId, s.Slot, s.Name, s.CharacterId, s.Connected)).ToArray());

    public static TickStateMessage ToTickState(Guid sessionId, long seq, long tick, SessionPhase phase, LevelState level,
        IEnumerable<PlayerSlot> slots) =>
        new(sessionId, seq, tick, phase, level.Index, level.Door is { IsOpen: true },
            slots.Where(s => s.Entity is not null).Select(s => ToPlayerState(s, s.Entity!)).ToArray(),
            level.Zombies.Select(ToZombieState).ToArray(),
            level.Items.Select(ToItemState).ToArray(),
            level.Levers.Select(l => new LeverStateDto(l.Id, l.Tile.X, l.Tile.Y, l.IsActive)).ToArray());

    public static LevelStartedMessage ToLevelStarted(Guid sessionId, long seq, long tick, LevelState level, TickStateMessage state) =>
        new(sessionId, seq, tick, level.Index, level.Theme, level.Grid.Width, level.Grid.Height, ToRows(level), state);

    public static LevelLayoutResponse ToLayout(Guid sessionId, LevelState level) =>
        new(sessionId, level.Index, level.Theme, level.Seed, level.Grid.Width, level.Grid.Height, ToRows(level),
            MapLegend.Descriptions.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));

    public static HudResponse ToHud(Guid sessionId, SessionPhase phase, int levelIndex, int maxLevel, long tick,
        double levelElapsedSeconds, LevelState? level, IEnumerable<PlayerSlot> slots) =>
        new(sessionId, phase, levelIndex, maxLevel, tick, Math.Round(levelElapsedSeconds, 1),
            level?.Levers.Count(l => l.IsActive) ?? 0,
            level?.Levers.Count ?? 0,
            level?.Door is { IsOpen: true },
            level?.Items.Count ?? 0,
            slots.Where(s => s.Entity is not null).Select(s =>
            {
                var p = s.Entity!;
                return new HudPlayerDto(p.PlayerId, s.Slot, p.Name, p.Character.Id, p.Lives, p.MaxLives, p.Score,
                    ToPowers(p), p.Combos.Select(c => c.Granted).ToArray(), new Dictionary<string, int>(s.Stats));
            }).ToArray());

    /// <summary>The level as legend rows: terrain, with entity characters at their starting positions.</summary>
    public static string[] ToRows(LevelState level)
    {
        var grid = level.Grid;
        var chars = new char[grid.Height][];
        for (var y = 0; y < grid.Height; y++)
        {
            chars[y] = new char[grid.Width];
            for (var x = 0; x < grid.Width; x++)
            {
                chars[y][x] = MapLegend.ForTerrain(grid.GetTerrain(new GridPos(x, y)));
            }
        }

        void Mark(GridPos pos, char c) => chars[pos.Y][pos.X] = c;
        foreach (var item in level.Items) Mark(item.Tile, ItemChar(item));
        foreach (var lever in level.Levers) Mark(lever.Tile, MapLegend.Lever);
        foreach (var zombie in level.Zombies) Mark(zombie.SpawnTile, MapLegend.ZombieSpawn);
        if (level.StartTiles.Count == 2)
        {
            Mark(level.StartTiles[0], MapLegend.Player1Start);
            Mark(level.StartTiles[1], MapLegend.Player2Start);
        }

        return chars.Select(r => new string(r)).ToArray();
    }

    private static char ItemChar(ItemEntity item) => item.Kind switch
    {
        ConsumableKind.Health => MapLegend.HealthItem,
        ConsumableKind.Reward => MapLegend.RewardItem,
        _ => item.Definition.Grant?.Power switch
        {
            PowerType.Jump => MapLegend.JumpItem,
            PowerType.Sprint => MapLegend.SprintItem,
            _ => MapLegend.SwimItem,
        },
    };

    private static PlayerStateDto ToPlayerState(PlayerSlot slot, PlayerEntity p)
    {
        var (x, y) = p.RenderPosition;
        return new PlayerStateDto(p.PlayerId, slot.Slot, p.Name, p.Character.Id, p.Tile.X, p.Tile.Y,
            Math.Round(x, 3), Math.Round(y, 3), p.Facing, p.IsMoving, p.Lives, p.MaxLives, p.Score,
            ToPowers(p), p.Combos.Select(c => c.Granted).ToArray());
    }

    private static ActivePowerDto[] ToPowers(PlayerEntity p) =>
        p.Powers.OrderBy(a => a.Power).Select(a => new ActivePowerDto(a.Power, Math.Round(a.RemainingSeconds, 2), a.Level)).ToArray();

    private static ZombieStateDto ToZombieState(ZombieEntity z)
    {
        var (x, y) = z.RenderPosition;
        return new ZombieStateDto(z.Id, z.Definition.Id, z.Tile.X, z.Tile.Y, Math.Round(x, 3), Math.Round(y, 3), z.Facing);
    }

    private static ItemStateDto ToItemState(ItemEntity i) =>
        new(i.Id, i.Definition.Id, Enum.Parse<ItemKind>(i.Kind.ToString()), i.Definition.Grant?.Power, i.Tile.X, i.Tile.Y);
}
