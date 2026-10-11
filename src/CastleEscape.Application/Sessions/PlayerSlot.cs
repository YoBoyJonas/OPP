using CastleEscape.Domain.Sessions;

namespace CastleEscape.Application.Sessions;

/// <summary>Application-owned credentials and connection for a domain player.</summary>
public sealed class PlayerSlot(Guid playerId, string token, string name, int slot) : PlayerState(playerId, name, slot)
{
    public string Token { get; } = token;
    public string? ConnectionId { get; set; }
    public bool Connected => ConnectionId is not null;
}
