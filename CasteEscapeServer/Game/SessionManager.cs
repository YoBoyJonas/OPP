using CasteEscapeServer.Hubs;
using CasteEscapeServer.Models;
using Microsoft.AspNetCore.SignalR;

namespace CasteEscapeServer.Game;

public class SessionManager
{
    private readonly IHubContext<GameHub> _hub;
    private readonly object _gate = new();
    private readonly Dictionary<string, GameSession> _sessionsByConnection = new();
    private GameSession? _waitingSession;

    public SessionManager(IHubContext<GameHub> hub)
    {
        _hub = hub;
    }

    public JoinResult Join(string connectionId, string playerName, string characterId)
    {
        lock (_gate)
        {
            GameSession session;
            int playerIndex;

            if (_waitingSession != null)
            {
                session = _waitingSession;
                playerIndex = 1;
                _waitingSession = null;
            }
            else
            {
                session = new GameSession(Guid.NewGuid().ToString("N"), _hub);
                playerIndex = 0;
                _waitingSession = session;
            }

            session.AddPlayer(playerIndex, Characters.Create(characterId, connectionId, playerName));
            _sessionsByConnection[connectionId] = session;

            var started = session.Players[0] != null && session.Players[1] != null;
            if (started)
            {
                session.Start();
            }

            return new JoinResult { SessionId = session.SessionId, PlayerIndex = playerIndex, Started = started };
        }
    }

    public GameSession? GetSession(string connectionId)
    {
        lock (_gate)
        {
            return _sessionsByConnection.GetValueOrDefault(connectionId);
        }
    }

    public GameSession? Disconnect(string connectionId)
    {
        lock (_gate)
        {
            if (!_sessionsByConnection.TryGetValue(connectionId, out var session))
            {
                return null;
            }

            _sessionsByConnection.Remove(connectionId);
            session.Stop();
            if (_waitingSession == session)
            {
                _waitingSession = null;
            }

            return session;
        }
    }
}
