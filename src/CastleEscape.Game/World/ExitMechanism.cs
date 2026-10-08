using CastleEscape.Contracts;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Events;

namespace CastleEscape.Game.World;

/// <summary>
/// Levers, exit door and exit tiles of one level (DOOR-1, DOOR-2, COL-4, D1, D2).
/// All lever/door/exit logic lives here; seam for the P2 Mediator.
/// </summary>
public class ExitMechanism(LevelState level, DoorMode doorMode)
{
    private readonly HashSet<Guid> _playersOnExit = [];

    /// <summary>A lever is active while a player stands on it (D2).</summary>
    public void UpdateLevers(IReadOnlyList<PlayerEntity> players, List<GameEvent> events)
    {
        foreach (var lever in level.Levers)
        {
            var standing = players.FirstOrDefault(p => p.Tile == lever.Tile);
            var active = standing is not null;
            if (active == lever.IsActive)
            {
                continue;
            }

            lever.IsActive = active;
            events.Add(new LeverChanged(standing?.PlayerId,
                active ? $"{standing!.Name} pulled {lever.Id}." : $"{lever.Id} was released.", lever.Id, active));
        }
    }

    /// <summary>Opens or closes the door, then checks the exit. Returns true when the level is complete.</summary>
    public bool Update(IReadOnlyList<PlayerEntity> players, List<GameEvent> events)
    {
        UpdateDoor(events);
        return UpdateExit(players, events);
    }

    private void UpdateDoor(List<GameEvent> events)
    {
        if (level.Door is not { } door)
        {
            return;
        }

        var allLeversActive = level.Levers.Count >= 2 && level.Levers.All(l => l.IsActive);
        var shouldBeOpen = doorMode switch
        {
            DoorMode.Latch => door.IsOpen || allLeversActive,
            _ => allLeversActive,
        };

        if (shouldBeOpen == door.IsOpen)
        {
            return;
        }

        door.IsOpen = shouldBeOpen;
        events.Add(shouldBeOpen
            ? new DoorOpened(null, "The exit door is open!", doorMode)
            : new DoorClosed(null, "The exit door closed.", doorMode));
    }

    private bool UpdateExit(IReadOnlyList<PlayerEntity> players, List<GameEvent> events)
    {
        foreach (var player in players)
        {
            var onExit = !player.IsMoving && level.IsExitTile(player.Tile);
            if (onExit && _playersOnExit.Add(player.PlayerId))
            {
                events.Add(new PlayerReachedExit(player.PlayerId, $"{player.Name} reached the exit."));
            }
            else if (!onExit)
            {
                _playersOnExit.Remove(player.PlayerId);
            }
        }

        // DOOR-2: complete only when both players stand on exit tiles at the same time.
        return players.Count == 2 && players.All(p => _playersOnExit.Contains(p.PlayerId));
    }
}
