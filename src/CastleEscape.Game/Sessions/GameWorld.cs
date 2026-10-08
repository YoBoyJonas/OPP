using CastleEscape.Game.Configuration;
using CastleEscape.Game.Events;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Sessions;

/// <summary>
/// The playing field of one session: the current level, its pristine prototype, the exit mechanism,
/// the players and the level-start checkpoint. Commands change the game only through this object;
/// the session turns what happened (events, a replaced level) into messages.
/// </summary>
public sealed class GameWorld(IReadOnlyList<PlayerSlot> slots, GameOptions options, PatternOptions patterns)
{
    /// <summary>The level being played (a clone of <see cref="Pristine"/>).</summary>
    public LevelState? Level { get; private set; }

    /// <summary>The level as it was built; never played on (Prototype).</summary>
    public LevelState? Pristine { get; private set; }

    public ExitMechanism? Exit { get; private set; }

    /// <summary>Lives and score of each player when the level started (D8).</summary>
    public LevelCheckpoint? Checkpoint { get; private set; }

    public double LevelElapsed { get; set; }

    /// <summary>Goes up whenever <see cref="Level"/> is replaced, so the session knows to announce it.</summary>
    public int LevelVersion { get; private set; }

    /// <summary>Events produced this tick, sent and counted by the session.</summary>
    public List<GameEvent> Events { get; } = [];

    public IReadOnlyList<PlayerEntity> Players => slots.Where(s => s.Entity is not null).Select(s => s.Entity!).ToList();

    public PlayerEntity? FindPlayer(Guid playerId) => slots.FirstOrDefault(s => s.PlayerId == playerId)?.Entity;

    public GameOptions Options => options;

    /// <summary>A new level: keep it as the prototype, checkpoint the players, and play on a clone.</summary>
    public void BeginLevel(LevelState built)
    {
        Pristine = built;
        Checkpoint = new LevelCheckpoint(Players.ToDictionary(p => p.PlayerId, p => (p.Lives, p.Score)));
        StartOn(FreshCopy());
    }

    /// <summary>A fresh copy of the level as it was built (restart, D8).</summary>
    public LevelState FreshCopy() => Pristine!.Clone(patterns.PrototypeCloneMode);

    /// <summary>Plays on <paramref name="level"/> from the start: players on their start tiles, no powers (D15).</summary>
    public void StartOn(LevelState level)
    {
        Replace(level, elapsed: 0);
        foreach (var slot in slots.Where(s => s.Entity is not null))
        {
            slot.Entity!.PlaceAt(level.StartTiles[slot.Slot - 1]);
            slot.Entity.Powers.Clear();
        }
    }

    /// <summary>Puts back a level that was being played (undoing a restart). Players are placed by the caller.</summary>
    public void Resume(LevelState level, double elapsed) => Replace(level, elapsed);

    private void Replace(LevelState level, double elapsed)
    {
        Level = level;
        Exit = new ExitMechanism(level, options.DoorMode);
        LevelElapsed = elapsed;
        LevelVersion++;
    }
}
