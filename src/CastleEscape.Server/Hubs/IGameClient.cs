using CastleEscape.Contracts.Realtime;

namespace CastleEscape.Server.Hubs;

/// <summary>Methods the server calls on connected clients. Names match <see cref="ClientMethods"/>.</summary>
public interface IGameClient
{
    Task SessionUpdated(SessionUpdatedMessage message);
    Task LevelStarted(LevelStartedMessage message);
    Task StateUpdated(TickStateMessage message);
    Task GameEvent(GameEventMessage message);
    Task Error(ErrorMessage message);
    Task Pong(PongMessage message);
}
