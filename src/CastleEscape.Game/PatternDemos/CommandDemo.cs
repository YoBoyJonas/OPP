using CastleEscape.Contracts;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Commands;
using CastleEscape.Game.Sessions;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Command requirement: commands support undo().</summary>
public sealed class CommandDemo : IPatternDemo
{
    public string Key => "command";

    /// <summary>Ana (1) and Ben (2) with one free tile between them, a coin (r) behind Ben.</summary>
    public static readonly string[] Map =
    [
        "##########",
        "#1.2r....#",
        "#........#",
        "##########",
    ];

    public PatternDemoResponse Run(DemoOptions options)
    {
        var undoRestart = options.GetBool("undoRestart", true);
        var (session, ana, ben) = DemoWorld.PlayingSession(Map);
        var trace = new DemoTrace("Command");
        PlayerStateDto Player(PlayerSlot slot) => session.Snapshot.State!.Players.Single(p => p.PlayerId == slot.PlayerId);
        string Where(PlayerSlot slot) => $"({Player(slot).TileX},{Player(slot).TileY})";

        // 1. Both press towards the same free tile in the same tick. D7 is resolved by undoing a command.
        session.SubmitDirection(ana.PlayerId, Direction.Right);
        session.SubmitDirection(ben.PlayerId, Direction.Left);
        session.Tick(session.TickSeconds);
        trace.Line($"Ana presses Right, then Ben presses Left: both StartStep commands target (2,1) in the same tick.");
        trace.Line($"  The later one (Ben's) is undone -> Ana moving: {Player(ana).IsMoving}, Ben moving: {Player(ben).IsMoving}.");

        // 2. Ben collects the coin behind him.
        session.SubmitDirection(ana.PlayerId, Direction.None);
        session.SubmitDirection(ben.PlayerId, Direction.Right);
        DemoWorld.RunUntil(session, () => Player(ben).Score > 0);
        session.SubmitDirection(ben.PlayerId, Direction.None);
        DemoWorld.RunUntil(session, () => !Player(ben).IsMoving);
        trace.Line($"Ben walks right and picks up the coin: score {Player(ben).Score}, items left {session.Snapshot.State!.Items.Length}, Ben at {Where(ben)}.");

        // 3. Restart (a command), then undo it.
        session.RequestRestart(ana.PlayerId);
        session.Tick(session.TickSeconds);
        trace.Line($"Ana restarts the level: Ben's score {Player(ben).Score}, items {session.Snapshot.State!.Items.Length}, Ben at {Where(ben)}.");

        string? undone = null;
        if (undoRestart)
        {
            undone = session.UndoLastCommand();
            trace.Line($"UndoLastCommand() -> undid '{undone}': Ben's score {Player(ben).Score}, items {session.Snapshot.State!.Items.Length}, "
                       + $"Ben at {Where(ben)} (the level as it was before the restart).");
        }

        var history = session.CommandHistory();
        trace.Line($"History ({history.Count} commands, newest last):");
        foreach (var record in history)
        {
            var who = record.PlayerId == ana.PlayerId ? "Ana" : record.PlayerId == ben.PlayerId ? "Ben" : "-";
            trace.Line($"  #{record.Sequence,-3} tick {record.Tick,-4} {who,-4} {record.Name}{(record.Undone ? "  [undone]" : "")}");
        }

        trace.Evidence("history", history)
            .Evidence("undoneByConflict", history.Count(r => r.Undone && r.Name.StartsWith("StartStep", StringComparison.Ordinal)))
            .Evidence("undoneRestart", undone);
        return trace.Done("Every input and every step is a command object: queued, run in sequence order, recorded, and undoable.");
    }
}
