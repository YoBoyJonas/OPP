using CastleEscape.Game.Patterns;
using CastleEscape.Game.Sessions;

namespace CastleEscape.Game.Commands;

/// <summary>
/// One thing a player (or the game on a player's behalf) does to the world, as an object: it can be
/// queued, ordered by <see cref="Sequence"/>, logged, and undone.
/// </summary>
[DesignPattern("Command", "Command")]
public interface IGameCommand
{
    /// <summary>Who the command is for; null for commands not tied to a player.</summary>
    Guid? PlayerId { get; }

    /// <summary>Order of execution: commands queued in the same tick run in sequence order.</summary>
    long Sequence { get; }

    /// <summary>Short description for logs and diagnostics, e.g. "SetDirection Right".</summary>
    string Name { get; }

    /// <summary>Applies the command. Returns false if it could not be applied (then it is not recorded).</summary>
    bool Execute(GameWorld world);

    /// <summary>Reverses exactly what <see cref="Execute"/> did.</summary>
    void Undo(GameWorld world);
}

/// <summary>A command in the history: when it ran and whether it has been undone.</summary>
public sealed record CommandRecord(long Sequence, string Name, Guid? PlayerId, long Tick, bool Undone);
