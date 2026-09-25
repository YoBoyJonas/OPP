using CastleEscape.Contracts;
using CastleEscape.Game.Commands;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Sessions;

/// <summary>
/// MOV-1/MOV-3: each player steps tile by tile in their held direction, independently. Every step
/// is a <see cref="StartStepCommand"/>. Players standing still decide together, so two of them can
/// claim the same tile in one tick; then the step from the later direction input is undone (D7).
/// </summary>
public static class PlayerMovement
{
    public static void Move(GameWorld world, CommandProcessor commands, long tick, double seconds)
    {
        var level = world.Level!;
        var players = world.Players;

        // 1. Players standing still pick their step at the same moment, against the tiles claimed so far.
        var claimedBefore = players.ToDictionary(p => p.PlayerId, p => Claims(p).ToList());
        var started = new List<StartStepCommand>();
        foreach (var player in players.Where(p => !p.IsMoving))
        {
            var claimed = players.Where(o => o != player).SelectMany(o => claimedBefore[o.PlayerId]).ToList();
            if (TryStartStep(player, level, claimed, world, commands, tick) is { } step)
            {
                started.Add(step);
            }
        }

        // 2. Both stepped into the same tile: the later input loses its step.
        foreach (var conflict in started.GroupBy(s => s.Target).Where(g => g.Count() > 1))
        {
            foreach (var loser in conflict.OrderBy(s => s.Sequence).Skip(1))
            {
                commands.Undo(loser, world, "the other player claimed that tile first (D7)");
            }
        }

        // 3. Move. On arrival, step on at once; every tile is claimed by now, so no new conflict can start.
        foreach (var player in players)
        {
            var distance = player.IsMoving ? player.Abilities.SpeedOn(level.Grid.GetTile(player.NextTile!.Value)) * seconds : 0;
            if (!player.Advance(distance, out var leftover))
            {
                continue;
            }
            var claimed = players.Where(o => o != player).SelectMany(Claims).ToList();
            if (TryStartStep(player, level, claimed, world, commands, tick) is not null)
            {
                player.Advance(Math.Min(leftover, 0.99), out _); // one arrival per tick at most
            }
        }
    }

    /// <summary>The tiles a player occupies: where they stand and, while stepping, where they are going.</summary>
    private static IEnumerable<GridPos> Claims(PlayerEntity player) =>
        player.NextTile is { } next ? [player.Tile, next] : [player.Tile];

    private static StartStepCommand? TryStartStep(PlayerEntity player, LevelState level, IReadOnlyCollection<GridPos> claimed,
        GameWorld world, CommandProcessor commands, long tick)
    {
        if (player.HeldDirection == Direction.None)
        {
            return null;
        }
        var target = player.Tile.Step(player.HeldDirection);
        if (!MovementRules.CanPlayerEnter(player, target, level, claimed)) // COL-1: rejected, stays put
        {
            return null;
        }
        var step = new StartStepCommand(player.DirectionSequence, player.PlayerId, player.HeldDirection);
        return commands.Execute(step, world, tick) ? step : null;
    }
}
