
namespace CastleEscape.Game.Events;

/// <summary>Something that reacts to game events.</summary>
public interface IGameEventObserver
{
    void OnEvent(GameEvent gameEvent);
}

/// <summary>
/// The subject: every published event goes to each attached observer, in attach order.
/// The session owns one publisher and publishes each tick's events at its end.
/// Observers must not attach or detach from inside <see cref="IGameEventObserver.OnEvent"/>.
/// </summary>
public sealed class GameEventPublisher
{
    private readonly List<IGameEventObserver> _observers = [];

    public void Attach(IGameEventObserver observer) => _observers.Add(observer);

    public void Detach(IGameEventObserver observer) => _observers.Remove(observer);

    public void Publish(GameEvent gameEvent)
    {
        foreach (var observer in _observers)
        {
            observer.OnEvent(gameEvent);
        }
    }
}
