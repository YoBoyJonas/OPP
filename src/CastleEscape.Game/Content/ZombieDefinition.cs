using CastleEscape.Contracts;

namespace CastleEscape.Game.Content;

/// <summary>A zombie type. Types differ by data and movement strategy, not by subclass.</summary>
public class ZombieDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    /// <summary>Tiles per second.</summary>
    public double Speed { get; init; }

    /// <summary>Lives taken on contact (ZMB-2).</summary>
    public int ContactDamage { get; init; } = 1;

    public double AttackCooldownSeconds { get; init; }
    public int Size { get; init; } = 1;

    /// <summary>Default way this type chases the nearest player (ZMB-1).</summary>
    public MovementStrategyKind MovementStrategy { get; init; }
}
