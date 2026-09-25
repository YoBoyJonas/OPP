using System.Collections.Concurrent;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Events;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.Sessions;

namespace CastleEscape.Game.Commands;

/// <summary>
/// Runs commands and remembers them. Clients queue commands from any thread (<see cref="Enqueue"/>);
/// the tick runs them in sequence order (<see cref="ExecutePending"/>). The last <see cref="HistoryLimit"/>
/// executed commands are kept so they can be listed and undone.
/// </summary>
[DesignPattern("Command", "Invoker")]
public sealed class CommandProcessor
{
    public const int HistoryLimit = 100;

    private readonly ConcurrentQueue<IGameCommand> _pending = new();
    private readonly LinkedList<Entry> _history = new();
    private long _sequence;

    private sealed class Entry(IGameCommand command, long tick)
    {
        public IGameCommand Command { get; } = command;
        public long Tick { get; } = tick;
        public bool Undone { get; set; }
    }

    /// <summary>A new sequence number. Thread-safe, so inputs from both players get one global order.</summary>
    public long NextSequence() => Interlocked.Increment(ref _sequence);

    /// <summary>Queues a command for the next tick. Thread-safe.</summary>
    public void Enqueue(IGameCommand command) => _pending.Enqueue(command);

    /// <summary>Drops queued commands (inputs sent while nothing can move, e.g. between levels).</summary>
    public void DiscardPending()
    {
        while (_pending.TryDequeue(out _))
        {
        }
    }

    /// <summary>Runs every queued command, lowest sequence first. Returns the ones that were applied.</summary>
    public List<IGameCommand> ExecutePending(GameWorld world, long tick)
    {
        var queued = new List<IGameCommand>();
        while (_pending.TryDequeue(out var command))
        {
            queued.Add(command);
        }
        return queued.OrderBy(c => c.Sequence).Where(c => Execute(c, world, tick)).ToList();
    }

    /// <summary>Runs one command now and records it if it was applied.</summary>
    public bool Execute(IGameCommand command, GameWorld world, long tick)
    {
        if (!command.Execute(world))
        {
            return false;
        }
        _history.AddLast(new Entry(command, tick));
        if (_history.Count > HistoryLimit)
        {
            _history.RemoveFirst();
        }
        return true;
    }

    /// <summary>Undoes one recorded command (e.g. the losing step of a tile conflict).</summary>
    public bool Undo(IGameCommand command, GameWorld world, string reason)
    {
        var entry = _history.LastOrDefault(e => e.Command == command && !e.Undone);
        if (entry is null)
        {
            return false;
        }
        command.Undo(world);
        entry.Undone = true;
        world.Events.Add(PendingEvent.Of(GameEventTypes.CommandUndone, command.PlayerId, $"Undid {command.Name}: {reason}.",
            new { command = command.Name, sequence = command.Sequence, reason }));
        return true;
    }

    /// <summary>Undoes the most recent command that is not undone yet, optionally only one of a given player's.</summary>
    public IGameCommand? UndoLast(GameWorld world, Guid? playerId = null, string reason = "undo requested")
    {
        var entry = _history.Reverse().FirstOrDefault(e => !e.Undone && (playerId is null || e.Command.PlayerId == playerId));
        if (entry is null)
        {
            return null;
        }
        Undo(entry.Command, world, reason);
        return entry.Command;
    }

    /// <summary>The recorded commands, oldest first.</summary>
    public IReadOnlyList<CommandRecord> History() =>
        _history.Select(e => new CommandRecord(e.Command.Sequence, e.Command.Name, e.Command.PlayerId, e.Tick, e.Undone)).ToList();
}
