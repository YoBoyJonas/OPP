using CasteEscapeServer.Models;

namespace CasteEscapeServer.Game.PowerGrants;

public static class PowerGrantRegistry
{
    private static readonly Dictionary<PowerType, IPowerGrantStrategy> Strategies =
        new IPowerGrantStrategy[]
        {
            new JumpGrantStrategy(),
            new SwimGrantStrategy(),
            new SprintGrantStrategy()
        }.ToDictionary(s => s.Power);

    public static IPowerGrantStrategy? For(PowerType power) =>
        Strategies.GetValueOrDefault(power);
}