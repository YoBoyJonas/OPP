using CastleEscape.Game.World;

namespace CastleEscape.Game.Sessions;

/// <summary>A player's seat in a session: identity, token, lobby choices and (once playing) their entity.</summary>
public class PlayerSlot(Guid playerId, string token, string name, int slot)
{
    public Guid PlayerId { get; } = playerId;

    /// <summary>Secret returned on create/join; proves who is calling (X-Player-Token, hub playerToken).</summary>
    public string Token { get; } = token;

    public string Name { get; } = name;

    /// <summary>1 or 2; decides which start tile the player gets.</summary>
    public int Slot { get; } = slot;

    public string? CharacterId { get; set; }

    /// <summary>The SignalR connection, when connected.</summary>
    public string? ConnectionId { get; set; }

    public bool Connected => ConnectionId is not null;

    /// <summary>Created when the first level loads; lives across levels.</summary>
    public PlayerEntity? Entity { get; set; }

    /// <summary>Per-player counters for the HUD ("ItemCollected" → 3, "LifeLost" → 1, ...).</summary>
    public Dictionary<string, int> Stats { get; } = [];
}
