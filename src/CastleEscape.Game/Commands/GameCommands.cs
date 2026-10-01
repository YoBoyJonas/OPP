using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Commands;

/// <summary>MOV-3: the direction key a player holds (None = released). Undo restores the previous key.</summary>
[DesignPattern("Command", "ConcreteCommand")]
public sealed class SetDirectionCommand(long sequence, Guid playerId, Direction direction) : IGameCommand
{
    private Direction _previous;
    private long _previousSequence;

    public Guid? PlayerId => playerId;
    public long Sequence => sequence;
    public Direction Direction => direction;
    public string Name => $"SetDirection {direction}";

    public bool Execute(GameWorld world)
    {
        if (world.FindPlayer(playerId) is not { } player)
        {
            return false;
        }
        _previous = player.HeldDirection;
        _previousSequence = player.DirectionSequence;
        player.HeldDirection = direction;
        player.DirectionSequence = sequence;
        return true;
    }

    public void Undo(GameWorld world)
    {
        var player = world.FindPlayer(playerId)!;
        player.HeldDirection = _previous;
        player.DirectionSequence = _previousSequence;
    }
}

/// <summary>
/// MOV-1: a player starts a one-tile step. Created by the tick, not the client; carries the sequence of
/// the direction input it came from, so when two players claim the same tile the later input is undone (D7).
/// </summary>
[DesignPattern("Command", "ConcreteCommand")]
public sealed class StartStepCommand(long sequence, Guid playerId, Direction direction) : IGameCommand
{
    public Guid? PlayerId => playerId;
    public long Sequence => sequence;
    public string Name => $"StartStep {direction}";

    public GridPos? Target { get; private set; }

    public bool Execute(GameWorld world)
    {
        if (world.FindPlayer(playerId) is not { IsMoving: false } player)
        {
            return false;
        }
        player.BeginStep(direction);
        Target = player.NextTile;
        return true;
    }

    public void Undo(GameWorld world) => world.FindPlayer(playerId)!.CancelStep();
}

/// <summary>
/// D8: the level from the start (a fresh prototype clone), lives and score back to the checkpoint.
/// Undo resumes the level that was being played, with every player's lives, score and tile.
/// Active powers are not brought back: that needs a snapshot of the power timers (the P2 Memento seam).
/// </summary>
[DesignPattern("Command", "ConcreteCommand")]
public sealed class RestartLevelCommand(long sequence, Guid playerId) : IGameCommand
{
    private LevelState? _previousLevel;
    private double _previousElapsed;
    private readonly Dictionary<Guid, (int Lives, int Score, GridPos Tile)> _previousPlayers = [];

    public Guid? PlayerId => playerId;
    public long Sequence => sequence;
    public string Name => "RestartLevel";

    public bool Execute(GameWorld world)
    {
        if (world.Level is null || world.FindPlayer(playerId) is not { } requestedBy)
        {
            return false;
        }

        _previousLevel = world.Level;
        _previousElapsed = world.LevelElapsed;
        _previousPlayers.Clear();
        foreach (var player in world.Players)
        {
            _previousPlayers[player.PlayerId] = (player.Lives, player.Score, player.Tile);
            var (lives, score) = world.Checkpoint!.Players[player.PlayerId];
            player.RestoreStats(lives, score);
        }

        world.StartOn(world.FreshCopy());
        world.Events.Add(new LevelRestarted(playerId, $"{requestedBy.Name} restarted level {world.Level!.Index}.", world.Level.Index));
        return true;
    }

    public void Undo(GameWorld world)
    {
        world.Resume(_previousLevel!, _previousElapsed);
        foreach (var player in world.Players)
        {
            var (lives, score, tile) = _previousPlayers[player.PlayerId];
            player.RestoreStats(lives, score);
            player.ReturnTo(tile);
        }
    }
}

/// <summary>A power given without an item (dev tools, demos). Undo puts the previous timer and level back.</summary>
[DesignPattern("Command", "ConcreteCommand")]
public sealed class GivePowerCommand(long sequence, Guid playerId, PowerGrant grant, double durationSeconds, int maxLevel,
    IReadOnlyList<PowerCombo> combos) : IGameCommand
{
    private ActivePower? _previous;

    public Guid? PlayerId => playerId;
    public long Sequence => sequence;
    public string Name => $"GivePower {grant.Power}";

    public bool Execute(GameWorld world)
    {
        if (world.FindPlayer(playerId) is not { } player)
        {
            return false;
        }
        _previous = player.Powers.Get(grant.Power)?.Copy();
        var power = player.Powers.Gain(grant, durationSeconds, maxLevel);
        world.Events.Add(new PowerGained(playerId, $"{player.Name} was given {power.Power} (level {power.Level}).",
            power.Power, power.Level, Math.Round(power.RemainingSeconds, 1)));
        Powers.PowerRules.RefreshCombos(player, combos, world.Events);
        return true;
    }

    public void Undo(GameWorld world)
    {
        var player = world.FindPlayer(playerId)!;
        player.Powers.Restore(grant.Power, _previous);
        player.Powers.RefreshCombos(combos);
    }
}

/// <summary>Dev tools: every zombie in the level switches to one chase strategy (Strategy pattern). Undo restores each zombie's own.</summary>
[DesignPattern("Command", "ConcreteCommand")]
public sealed class SetZombieStrategyCommand(long sequence, MovementStrategyKind kind) : IGameCommand
{
    private readonly Dictionary<string, AI.IZombieMovementStrategy> _previous = [];

    public Guid? PlayerId => null;
    public long Sequence => sequence;
    public string Name => $"SetZombieStrategy {kind}";

    public bool Execute(GameWorld world)
    {
        if (world.Level is null)
        {
            return false;
        }
        _previous.Clear();
        foreach (var zombie in world.Level.Zombies)
        {
            _previous[zombie.Id] = zombie.Strategy;
            zombie.Strategy = AI.ZombieStrategies.For(kind);
        }
        return true;
    }

    public void Undo(GameWorld world)
    {
        foreach (var zombie in world.Level!.Zombies.Where(z => _previous.ContainsKey(z.Id)))
        {
            zombie.Strategy = _previous[zombie.Id];
        }
    }
}
