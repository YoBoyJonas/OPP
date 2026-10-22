using System.ComponentModel.DataAnnotations;

namespace CastleEscape.Game.Configuration;

/// <summary>Core simulation settings, bound from the <c>Game</c> configuration section.</summary>
public sealed class GameOptions
{
    public const string SectionName = "Game";

    /// <summary>Simulation ticks per second.</summary>
    [Range(1, 120)]
    public int TickRate { get; set; } = 20;

    /// <summary>Default duration of a base power (PWR-1).</summary>
    [Range(1, 600)]
    public double PowerDurationSeconds { get; set; } = 10;

    /// <summary>Level width in tiles (LVL-2: at least 20).</summary>
    [Range(20, 200)]
    public int GridWidth { get; set; } = 24;

    /// <summary>Level height in tiles (LVL-2: at least 15).</summary>
    [Range(15, 200)]
    public int GridHeight { get; set; } = 16;

    /// <summary>Number of levels in a run (LVL-1).</summary>
    [Range(1, 100)]
    public int MaxLevel { get; set; } = 10;

    /// <summary>Pause between completing a level and loading the next one.</summary>
    [Range(0, 60)]
    public double LevelTransitionSeconds { get; set; } = 2;

    /// <summary>Highest stack level a repeated power reaches (PWR-3); more pickups still extend the time.</summary>
    [Range(1, 10)]
    public int MaxPowerStackLevel { get; set; } = 3;
}
