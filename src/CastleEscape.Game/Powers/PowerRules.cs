using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Powers;

/// <summary>Power timers and super-power combos (PWR-1, PWR-2).</summary>
public static class PowerRules
{
    /// <summary>Counts down every player's powers and refreshes their combos.</summary>
    public static void TickPowers(IReadOnlyList<PlayerEntity> players, IReadOnlyList<PowerCombo> combos, double seconds, List<PendingEvent> events)
    {
        foreach (var player in players)
        {
            foreach (var expired in player.TickPowers(seconds))
            {
                events.Add(PendingEvent.Of(GameEventTypes.PowerExpired, player.PlayerId,
                    $"{player.Name}'s {expired} wore off.", new { power = expired }));
            }
            RefreshCombos(player, combos, events);
        }
    }

    /// <summary>A combo is active while all its required base powers are active.</summary>
    public static void RefreshCombos(PlayerEntity player, IReadOnlyList<PowerCombo> combos, List<PendingEvent> events)
    {
        var active = combos
            .Where(c => c.RequiredPowers.All(p => player.GetPower(p) is not null))
            .ToList();

        foreach (var combo in active.Where(c => !player.Combos.Contains(c)))
        {
            events.Add(PendingEvent.Of(GameEventTypes.ComboActivated, player.PlayerId,
                $"{player.Name} activated {combo.Name}!", new { power = combo.Granted }));
        }
        player.SetCombos(active);
    }
}
