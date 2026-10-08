namespace CastleEscape.Game.Content;

/// <summary>A reference to content by id with a relative spawn weight, used in level tables.</summary>
public class SpawnTableEntry
{
    public string Id { get; init; } = string.Empty;
    public double Weight { get; init; } = 1.0;
}
