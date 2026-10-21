using CasteEscapeServer.Models;

namespace CasteEscapeServer.Game.PowerGrants;

public interface IPowerGrantStrategy
{
    PowerType Power { get; }
    void Grant(PlayerState player, double durationSeconds);
    void Revoke(PlayerState player);
}