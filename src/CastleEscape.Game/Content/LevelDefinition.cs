using CastleEscape.Contracts;

namespace CastleEscape.Game.Content;

/// <summary>
/// Static settings for one of the 10 levels (the class diagram's <c>Level</c>). The generator
/// turns it into a runtime <c>LevelState</c>.
/// </summary>
public class LevelDefinition
{
    public string Id { get; init; } = string.Empty;

    /// <summary>1-based position in the level sequence.</summary>
    public int Index { get; init; }

    public LevelTheme Theme { get; init; }

    /// <summary>Room size. Null means the <c>Game:GridWidth</c> and <c>Game:GridHeight</c> settings.</summary>
    public GridSize? RoomSize { get; init; }

    /// <summary>How many water/pit patches to place.</summary>
    public int ObstacleCount { get; init; }

    /// <summary>Which obstacles the patches use (by obstacle id, weighted).</summary>
    public IReadOnlyList<SpawnTableEntry> ObstacleTable { get; init; } = [];

    public int ZombieCount { get; init; }
    public IReadOnlyList<SpawnTableEntry> ZombieTable { get; init; } = [];

    /// <summary>How many items to place.</summary>
    public int PickupCount { get; init; }
    public IReadOnlyList<SpawnTableEntry> ConsumableTable { get; init; } = [];

    /// <summary>Kept from the class diagram; not enforced (no requirement).</summary>
    public double TimeLimitSeconds { get; init; }
}
