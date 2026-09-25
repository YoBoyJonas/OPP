using CastleEscape.Game.Patterns;
using CastleEscape.Game.Sessions;

namespace CastleEscape.Game.Events;

/// <summary>Sends every event to the session's clients (as a <c>GameEvent</c> message).</summary>
[DesignPattern("Observer", "ConcreteObserver")]
public sealed class ClientNotificationObserver(Action<GameEvent> send) : IGameEventObserver
{
    public void OnEvent(GameEvent gameEvent) => send(gameEvent);
}

/// <summary>Counts each player's events for the HUD statistics ("ItemCollected" → 3, "LifeLost" → 1, ...).</summary>
[DesignPattern("Observer", "ConcreteObserver")]
public sealed class SessionStatisticsObserver(IReadOnlyList<PlayerSlot> slots) : IGameEventObserver
{
    public void OnEvent(GameEvent gameEvent)
    {
        if (gameEvent is SoundCue || gameEvent.PlayerId is not { } playerId)
        {
            return;
        }
        if (slots.FirstOrDefault(s => s.PlayerId == playerId) is { } slot)
        {
            slot.Stats[gameEvent.Type] = slot.Stats.GetValueOrDefault(gameEvent.Type) + 1;
        }
    }
}

/// <summary>One logged event.</summary>
public sealed record EventLogEntry(long Tick, DateTimeOffset At, string Type, Guid? PlayerId, string Message, IReadOnlyDictionary<string, string> Data);

/// <summary>Keeps the last <see cref="Capacity"/> events for diagnostics (<c>/api/diagnostics/sessions/{id}/events</c>).</summary>
[DesignPattern("Observer", "ConcreteObserver")]
public sealed class EventLogObserver(Func<long> currentTick, int capacity = EventLogObserver.DefaultCapacity) : IGameEventObserver
{
    public const int DefaultCapacity = 200;

    private readonly Queue<EventLogEntry> _entries = new();

    public int Capacity => capacity;

    public void OnEvent(GameEvent gameEvent)
    {
        _entries.Enqueue(new EventLogEntry(currentTick(), DateTimeOffset.UtcNow, gameEvent.Type, gameEvent.PlayerId, gameEvent.Message, gameEvent.Data()));
        while (_entries.Count > capacity)
        {
            _entries.Dequeue();
        }
    }

    /// <summary>The logged events, oldest first.</summary>
    public IReadOnlyList<EventLogEntry> Entries => _entries.ToList();
}

/// <summary>
/// Turns gameplay events into sound cues for the clients (e.g. LifeLost → <c>life_lost</c>), published as
/// <see cref="SoundCue"/> events. Clients only need to map cue names to sound files.
/// </summary>
[DesignPattern("Observer", "ConcreteObserver")]
public sealed class SoundCueObserver(GameEventPublisher publisher) : IGameEventObserver
{
    public static readonly IReadOnlyDictionary<Type, string> Cues = new Dictionary<Type, string>
    {
        [typeof(ItemCollected)] = "pickup",
        [typeof(PowerGained)] = "power_up",
        [typeof(ComboActivated)] = "combo",
        [typeof(LifeLost)] = "life_lost",
        [typeof(DoorOpened)] = "door_open",
        [typeof(LevelCompleted)] = "level_complete",
        [typeof(GameWon)] = "victory",
        [typeof(GameLost)] = "game_over",
    };

    public void OnEvent(GameEvent gameEvent)
    {
        if (Cues.TryGetValue(gameEvent.GetType(), out var cue))
        {
            publisher.Publish(new SoundCue(gameEvent.PlayerId, $"Play '{cue}'.", cue, gameEvent.Type));
        }
    }
}
