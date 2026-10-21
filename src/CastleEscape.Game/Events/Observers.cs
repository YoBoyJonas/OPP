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
        if (slots.FirstOrDefault(s => s.PlayerId == gameEvent.PlayerId) is { } slot)
        {
            slot.Stats[gameEvent.Type] = slot.Stats.GetValueOrDefault(gameEvent.Type) + 1;
        }
    }
}
