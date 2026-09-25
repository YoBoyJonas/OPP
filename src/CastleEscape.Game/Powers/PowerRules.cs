using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Powers;

/// <summary>Runs every player's power timers and combos each tick (PWR-1, PWR-2) and reports the changes as events.</summary>
public static class PowerRules
{
    public static void TickPowers(IReadOnlyList<PlayerEntity> players, IReadOnlyList<PowerCombo> combos, double seconds, List<PendingEvent> events)
    {
        foreach (var player in players)
        {
            foreach (var expired in player.Powers.Tick(seconds))
            {
                events.Add(PendingEvent.Of(GameEventTypes.PowerExpired, player.PlayerId,
                    $"{player.Name}'s {expired} wore off.", new { power = expired }));
            }
            RefreshCombos(player, combos, events);
        }
    }

    public static void RefreshCombos(PlayerEntity player, IReadOnlyList<PowerCombo> combos, List<PendingEvent> events)
    {
        foreach (var combo in player.Powers.RefreshCombos(combos))
        {
            events.Add(PendingEvent.Of(GameEventTypes.ComboActivated, player.PlayerId,
                $"{player.Name} activated {combo.Name}!", new { power = combo.Granted }));
        }
    }
}
