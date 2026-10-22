using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Powers;

public interface IPowerGrantStrategy
{
    PowerGrantStrategyKind Kind { get; }
    ActivePower Apply(ActivePower? current, PowerGrant grant, double durationSeconds, int maxLevel);
}