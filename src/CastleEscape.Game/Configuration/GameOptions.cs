using System.ComponentModel.DataAnnotations;

namespace CastleEscape.Game.Configuration;

/// <summary>How the exit door reacts to the levers (decision D1).</summary>
public enum DoorMode
{
    /// <summary>Once both levers are active in the same tick the door stays open for the rest of the level.</summary>
    Latch,

    /// <summary>The door is open only while both levers are active (literal requirement; levels become unwinnable).</summary>
    HoldWhileActive
}

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

    public DoorMode DoorMode { get; set; } = DoorMode.Latch;

    /// <summary>Move the player back to their start tile after a zombie hit (decision D6).</summary>
    public bool RespawnPlayerOnHit { get; set; }

    /// <summary>Level width in tiles (LVL-2: at least 20).</summary>
    [Range(20, 200)]
    public int GridWidth { get; set; } = 24;

    /// <summary>Level height in tiles (LVL-2: at least 15).</summary>
    [Range(15, 200)]
    public int GridHeight { get; set; } = 16;

    /// <summary>Number of levels in a run (LVL-1).</summary>
    [Range(1, 100)]
    public int MaxLevel { get; set; } = 10;
}
