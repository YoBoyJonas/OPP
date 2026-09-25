using System.Globalization;
using System.Reflection;
using CastleEscape.Contracts;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;

namespace CastleEscape.Game.Events;

/// <summary>
/// A gameplay fact noted during a tick. The rules add events to the world's list; at the end of the tick
/// the session publishes them to every observer. <see cref="Type"/> is the class name, which is also the
/// <c>GameEventTypes</c> name clients see; <see cref="Data"/> holds the subclass's own properties.
/// </summary>
public abstract record GameEvent(Guid? PlayerId, string Message)
{
    public string Type => GetType().Name;

    /// <summary>The event's own fields as text, camelCase keys (the wire format of <c>GameEventMessage.Data</c>).</summary>
    public Dictionary<string, string> Data() =>
        GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType != typeof(GameEvent))
            .ToDictionary(
                p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..],
                p => Convert.ToString(p.GetValue(this), CultureInfo.InvariantCulture) ?? "");
}

// Lobby and flow
public sealed record PlayerJoined(Guid? PlayerId, string Message, int Slot) : GameEvent(PlayerId, Message);
public sealed record PlayerLeft(Guid? PlayerId, string Message) : GameEvent(PlayerId, Message);
public sealed record PhaseChanged(Guid? PlayerId, string Message, SessionPhase From, SessionPhase To) : GameEvent(PlayerId, Message);
public sealed record LevelStarted(Guid? PlayerId, string Message, int Level, LevelTheme Theme) : GameEvent(PlayerId, Message);
public sealed record LevelRestarted(Guid? PlayerId, string Message, int Level) : GameEvent(PlayerId, Message);
public sealed record LevelCompleted(Guid? PlayerId, string Message, int Level) : GameEvent(PlayerId, Message);
public sealed record GameWon(Guid? PlayerId, string Message) : GameEvent(PlayerId, Message);
public sealed record GameLost(Guid? PlayerId, string Message) : GameEvent(PlayerId, Message);

// Items and powers
public sealed record ItemCollected(Guid? PlayerId, string Message, string ItemId, string ConsumableId, ConsumableKind Kind, int Lives, int Score)
    : GameEvent(PlayerId, Message);
public sealed record PowerGained(Guid? PlayerId, string Message, PowerType Power, int Level, double RemainingSeconds) : GameEvent(PlayerId, Message);
public sealed record PowerExpired(Guid? PlayerId, string Message, PowerType Power) : GameEvent(PlayerId, Message);
public sealed record ComboActivated(Guid? PlayerId, string Message, PowerType Power) : GameEvent(PlayerId, Message);

// Zombies, levers, door, exit
public sealed record LifeLost(Guid? PlayerId, string Message, string ZombieId, int Lives) : GameEvent(PlayerId, Message);
public sealed record LeverChanged(Guid? PlayerId, string Message, string LeverId, bool Active) : GameEvent(PlayerId, Message);
public sealed record DoorOpened(Guid? PlayerId, string Message, DoorMode DoorMode) : GameEvent(PlayerId, Message);
public sealed record DoorClosed(Guid? PlayerId, string Message, DoorMode DoorMode) : GameEvent(PlayerId, Message);
public sealed record PlayerReachedExit(Guid? PlayerId, string Message) : GameEvent(PlayerId, Message);

// Commands and presentation
public sealed record CommandUndone(Guid? PlayerId, string Message, string Command, long Sequence, string Reason) : GameEvent(PlayerId, Message);

/// <summary>A sound the clients should play, e.g. <c>life_lost</c>. Produced by <see cref="SoundCueObserver"/> from other events.</summary>
public sealed record SoundCue(Guid? PlayerId, string Message, string Cue, string Source) : GameEvent(PlayerId, Message);
