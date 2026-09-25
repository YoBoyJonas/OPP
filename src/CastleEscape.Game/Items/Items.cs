using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.Powers;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Items;

/// <summary>A health potion: +lives, up to the character's maximum (ITM-1, D14).</summary>
[DesignPattern("Factory Method", "ConcreteProduct")]
public sealed class HealthItem(string id, Health definition, GridPos tile) : ItemEntity(id, definition, tile)
{
    public override void Apply(PlayerEntity player, ItemEffectContext context) =>
        player.GainLives(Definition.HealthValue);
}

/// <summary>A coin or gem: adds to the player's score (ITM-1, D4).</summary>
[DesignPattern("Factory Method", "ConcreteProduct")]
public sealed class RewardItem(string id, Reward definition, GridPos tile) : ItemEntity(id, definition, tile)
{
    public override void Apply(PlayerEntity player, ItemEffectContext context) =>
        player.AddScore(Definition.ScoreValue);
}

/// <summary>A temporary power (PWR-1); may complete a combo (PWR-2).</summary>
[DesignPattern("Factory Method", "ConcreteProduct")]
public sealed class PowerItem(string id, Power definition, GridPos tile) : ItemEntity(id, definition, tile)
{
    public PowerGrant Grant { get; } = definition.Grant
        ?? throw new ArgumentException($"Power item '{definition.Id}' has no grant.", nameof(definition));

    public override void Apply(PlayerEntity player, ItemEffectContext context)
    {
        var settings = context.Settings;
        var power = player.Powers.Gain(Grant, Grant.DurationSeconds ?? settings.DefaultPowerDurationSeconds, settings.MaxPowerStackLevel);
        context.Events.Add(new PowerGained(player.PlayerId, $"{player.Name} gained {power.Power} (level {power.Level}).",
            power.Power, power.Level, Math.Round(power.RemainingSeconds, 1)));
        PowerRules.RefreshCombos(player, context.Combos, context.Events);
    }
}
