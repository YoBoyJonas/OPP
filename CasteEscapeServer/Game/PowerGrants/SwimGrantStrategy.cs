using CasteEscapeServer.Models;

namespace CasteEscapeServer.Game.PowerGrants;

public class SwimGrantStrategy : IPowerGrantStrategy
{
    public PowerType Power => PowerType.Swim;

    public void Grant(PlayerState player, double durationSeconds) =>
        player.ActivePowers[Power] = durationSeconds;

    public void Revoke(PlayerState player) => player.ActivePowers.Remove(Power);
}