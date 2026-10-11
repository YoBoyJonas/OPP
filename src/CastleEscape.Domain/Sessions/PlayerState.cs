using CastleEscape.Domain.World;

namespace CastleEscape.Domain.Sessions;

/// <summary>A participant in the simulation, without network credentials.</summary>
public class PlayerState(Guid playerId, string name, int slot)
{
    public Guid PlayerId { get; } = playerId;

    public string Name { get; } = name;

    /// <summary>1 or 2; decides which start tile the player gets.</summary>
    public int Slot { get; } = slot;

    public string? CharacterId { get; set; }

    /// <summary>Created when the first level loads; lives across levels.</summary>
    public PlayerEntity? Entity { get; set; }

    /// <summary>Per-player counters for the HUD ("ItemCollected" → 3, "LifeLost" → 1, ...).</summary>
    public Dictionary<string, int> Stats { get; } = [];
}
