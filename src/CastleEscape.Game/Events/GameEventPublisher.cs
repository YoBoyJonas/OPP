using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.Events;

/// <summary>Something that reacts to game events.</summary>
[DesignPattern("Observer", "Observer")]
public interface IGameEventObserver
{
    void OnEvent(GameEvent gameEvent);
}

/// <summary>
/// The subject: observers attach and detach at any time, and every published event goes to each of
/// them in attach order. The session owns one publisher and publishes each tick's events at its end.
/// </summary>
[DesignPattern("Observer", "Subject")]
public sealed class GameEventPublisher
{
    private readonly List<IGameEventObserver> _observers = [];

    public IReadOnlyList<IGameEventObserver> Observers => _observers;

    public void Attach(IGameEventObserver observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    public void Detach(IGameEventObserver observer) => _observers.Remove(observer);

    /// <summary>Notifies every observer. An observer may publish further events from inside its handler.</summary>
    public void Publish(GameEvent gameEvent)
    {
        foreach (var observer in _observers.ToList()) // a copy: observers may attach or detach while notified
        {
            observer.OnEvent(gameEvent);
        }
    }
}
