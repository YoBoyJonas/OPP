namespace CastleEscape.Domain;

/// <summary>Lifecycle of a two-player session.</summary>
public enum SessionPhase
{
    /// <summary>Created; waiting for the second player to join with the code.</summary>
    WaitingForPlayers,

    /// <summary>Both players joined; waiting until both have picked a character.</summary>
    CharacterSelect,

    /// <summary>The next level is being generated (lasts one tick).</summary>
    LoadingLevel,

    Playing,

    /// <summary>Both players reached the exit; the next level loads after a short pause.</summary>
    LevelComplete,

    /// <summary>Level 10 completed (WIN-1). Terminal.</summary>
    Victory,

    /// <summary>A player reached 0 lives (PLR-3). Terminal.</summary>
    Defeat,

    /// <summary>A player left, or the session failed. Terminal.</summary>
    Aborted,
}
