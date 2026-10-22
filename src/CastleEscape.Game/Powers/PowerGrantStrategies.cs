   using CastleEscape.Contracts;
   using CastleEscape.Game.Content;
   using CastleEscape.Game.World;

   namespace CastleEscape.Game.Powers;

public sealed class StackGrantStrategy : IPowerGrantStrategy
{
    public PowerGrantStrategyKind Kind => PowerGrantStrategyKind.Stack;
    public ActivePower Apply(ActivePower? current, PowerGrant grant, double d, int maxLevel)
    {
        if (current is null) return new ActivePower(grant, d);
        current.Stack(d, maxLevel);
        return current;
    }
}

public sealed class ExtendTimeGrantStrategy : IPowerGrantStrategy
{
    public PowerGrantStrategyKind Kind => PowerGrantStrategyKind.ExtendTime;
    public ActivePower Apply(ActivePower? current, PowerGrant grant, double d, int maxLevel)
    {
        if (current is null) return new ActivePower(grant, d);
        current.Extend(d);
        return current;
    }
}

public sealed class RefreshGrantStrategy : IPowerGrantStrategy
{
    public PowerGrantStrategyKind Kind => PowerGrantStrategyKind.Refresh;
    public ActivePower Apply(ActivePower? current, PowerGrant grant, double d, int maxLevel)
    {
        if (current is null) return new ActivePower(grant, d);
        current.Refresh(d);
        return current;
    }
}

public sealed class IgnoreWhileActiveGrantStrategy : IPowerGrantStrategy
{
    public PowerGrantStrategyKind Kind => PowerGrantStrategyKind.IgnoreWhileActive;
    public ActivePower Apply(ActivePower? current, PowerGrant grant, double d, int maxLevel)
        => current ?? new ActivePower(grant, d);
}

public static class PowerGrantStrategies
{
    public static IReadOnlyList<IPowerGrantStrategy> All { get; } =
        [new StackGrantStrategy(), new ExtendTimeGrantStrategy(), new RefreshGrantStrategy(), new IgnoreWhileActiveGrantStrategy()];

    public static IPowerGrantStrategy For(PowerGrantStrategyKind kind) =>
        All.FirstOrDefault(s => s.Kind == kind)
        ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, "No strategy of this kind.");
}