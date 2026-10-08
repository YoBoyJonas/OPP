using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;

namespace CastleEscape.Game.World;

/// <summary>Settings the interaction rules need.</summary>
public sealed record InteractionSettings(double DefaultPowerDurationSeconds, int MaxPowerStackLevel, bool RespawnPlayerOnHit);

/// <summary>
/// Item pickup (COL-2, ITM-2) and zombie contact (COL-3, ZMB-2, ZMB-3), all in one place.
/// Seam for the P2 Chain of Responsibility.
/// </summary>
public static class InteractionResolver
{
    public static void CollectItems(LevelState level, IReadOnlyList<PlayerEntity> players, IReadOnlyList<PowerCombo> combos,
        InteractionSettings settings, List<GameEvent> events)
    {
        var context = new ItemEffectContext(combos, settings, events);
        foreach (var player in players)
        {
            if (level.ItemAt(player.Tile) is not { } item)
            {
                continue;
            }

            item.Apply(player, context);   // each item kind knows its own effect

            level.RemoveItem(item);
            events.Add(new ItemCollected(player.PlayerId, $"{player.Name} picked up {item.Definition.Name}.",
                item.Id, item.Definition.Id, item.Kind, player.Lives, player.Score));
        }
    }

    /// <summary>Contact: same tile, swapping tiles, or closer than 0.6 tiles.</summary>
    public static bool IsInContact(ZombieEntity zombie, PlayerEntity player)
    {
        if (zombie.Tile == player.Tile)
        {
            return true;
        }
        if (zombie.NextTile == player.Tile && player.NextTile == zombie.Tile)
        {
            return true;
        }
        var (zx, zy) = zombie.RenderPosition;
        var (px, py) = player.RenderPosition;
        return (zx - px) * (zx - px) + (zy - py) * (zy - py) < ContactDistance * ContactDistance;
    }

    public const double ContactDistance = 0.6;

    public static void ResolveZombieContacts(LevelState level, IReadOnlyList<PlayerEntity> players,
        InteractionSettings settings, List<GameEvent> events)
    {
        foreach (var zombie in level.Zombies)
        {
            var victim = players.FirstOrDefault(p => !p.IsDead && IsInContact(zombie, p));
            if (victim is null)
            {
                continue;
            }

            victim.LoseLives(zombie.Definition.ContactDamage);
            zombie.ResetToSpawn();
            if (settings.RespawnPlayerOnHit)
            {
                victim.PlaceAt(victim.StartTile);
            }

            events.Add(new LifeLost(victim.PlayerId, $"{zombie.Definition.Name} caught {victim.Name}! {victim.Lives} lives left.",
                zombie.Id, victim.Lives));
        }
    }
}
