using CastleEscape.Contracts;
using CastleEscape.Game.Content;

namespace CastleEscape.Game.World;

/// <summary>A base power a player has right now: time left and stack level (PWR-1, PWR-3).</summary>
public class ActivePower(PowerGrant grant, double durationSeconds)
{
    public PowerType Power => Grant.Power;
    public PowerGrant Grant { get; } = grant;
    public double RemainingSeconds { get; private set; } = durationSeconds;

    /// <summary>1 after the first pickup, +1 for every repeat pickup up to the configured maximum.</summary>
    public int Level { get; private set; } = 1;

    public bool IsExpired => RemainingSeconds <= 0;

    /// <summary>Picking up the same power again: more time and a higher level (PWR-3).</summary>
    public void Stack(double durationSeconds, int maxLevel)
    {
        RemainingSeconds += durationSeconds;
        Level = Math.Min(Level + 1, maxLevel);
    }

    public void Tick(double seconds) => RemainingSeconds -= seconds;

}
