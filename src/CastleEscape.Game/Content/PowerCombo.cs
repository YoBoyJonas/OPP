using CastleEscape.Contracts;

namespace CastleEscape.Game.Content;

/// <summary>A super power that is active while all required base powers are active (PWR-2).</summary>
public class PowerCombo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<PowerType> RequiredPowers { get; init; } = [];
    public PowerType Granted { get; init; }
    public StatModifiers Modifiers { get; init; } = StatModifiers.Identity;
}
