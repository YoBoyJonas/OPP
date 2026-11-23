using CastleEscape.Game.Configuration;
using CastleEscape.Game.Events;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Sessions;

public sealed class GameWorld(IReadOnlyList<PlayerSlot> slots, GameOptions options, PatternOptions patterns)
{
    /// <summary>A clone of <see cref="Pristine"/>.</summary>
    public LevelState? Level { get; private set; }

    /// <summary>The level as it was built (the prototype); never played on.</summary>
    public LevelState? Pristine { get; private set; }

    public ExitMechanism? Exit { get; private set; }
    public LevelCheckpoint? Checkpoint { get; private set; }
    public double LevelElapsed { get; set; }
    public int LevelVersion { get; private set; }
    public List<GameEvent> Events { get; } = [];

    public IReadOnlyList<PlayerEntity> Players => slots.Where(s => s.Entity is not null).Select(s => s.Entity!).ToList();

    public PlayerEntity? FindPlayer(Guid playerId) => slots.FirstOrDefault(s => s.PlayerId == playerId)?.Entity;

    public GameOptions Options => options;

    public void BeginLevel(LevelState built)
    {
        Pristine = built;
        Checkpoint = new LevelCheckpoint(Players.ToDictionary(p => p.PlayerId, p => (p.Lives, p.Score)));
        StartOn(FreshCopy());
    }

    public LevelState FreshCopy() => Pristine!.Clone(patterns.PrototypeCloneMode);

    public void StartOn(LevelState level)
    {
        Replace(level, elapsed: 0);
        foreach (var slot in slots.Where(s => s.Entity is not null))
        {
            slot.Entity!.PlaceAt(level.StartTiles[slot.Slot - 1]);
            slot.Entity.Powers.Clear();
        }
    }

    public void Resume(LevelState level, double elapsed) => Replace(level, elapsed);

    private void Replace(LevelState level, double elapsed)
    {
        Level = level;
        Exit = new ExitMechanism(level, options.DoorMode);
        LevelElapsed = elapsed;
        LevelVersion++;
    }
}
