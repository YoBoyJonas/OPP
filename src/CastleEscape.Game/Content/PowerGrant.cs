using CastleEscape.Contracts;

namespace CastleEscape.Game.Content;

/// <summary>A temporary base power granted by a Power item (PWR-1): Jump, Sprint or Swim.</summary>
public class PowerGrant
{
    public PowerType Power { get; init; }

    /// <summary>How long the power lasts. Null means the <c>Game:PowerDurationSeconds</c> setting.</summary>
    public double? DurationSeconds { get; init; }

    public StatModifiers Modifiers { get; init; } = StatModifiers.Identity;
}
