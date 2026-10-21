using CasteEscapeServer.Models;

namespace CasteEscapeServer.Game.PowerGrants;

public class SprintGrantStrategy : IPowerGrantStrategy
{
    private const double SpeedMultiplier = 1.5;

    public PowerType Power => PowerType.Sprint;

    public void Grant(PlayerState player, double durationSeconds)
    {
        if (!player.HasPower(Power))
        {
            player.MoveSpeed *= SpeedMultiplier;
        }
        player.ActivePowers[Power] = durationSeconds;
    }

    public void Revoke(PlayerState player)
    {
        if (player.ActivePowers.Remove(Power))
        {
            player.MoveSpeed /= SpeedMultiplier;
        }
    }
}