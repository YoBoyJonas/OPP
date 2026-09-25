using System.ComponentModel.DataAnnotations;

namespace CastleEscape.Game.Configuration;

/// <summary>Level generation settings, bound from the <c>Generation</c> configuration section.</summary>
public sealed class GenerationOptions
{
    public const string SectionName = "Generation";

    /// <summary>How many seeds to try before generation fails (start-level activity diagram retry loop).</summary>
    [Range(1, 1000)]
    public int MaxAttempts { get; set; } = 50;

    /// <summary>Allow a power obstacle on the critical path if its power item is reachable first (decision D5).</summary>
    public bool RequirePowerOnCriticalPath { get; set; }

    /// <summary>Minimum path distance in tiles between a zombie spawn and any start tile.</summary>
    [Range(0, 100)]
    public int MinZombieDistanceFromStart { get; set; } = 5;
}
