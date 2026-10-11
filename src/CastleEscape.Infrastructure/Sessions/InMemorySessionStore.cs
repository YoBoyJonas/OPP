using System.Collections.Concurrent;
using CastleEscape.Application.Sessions;

namespace CastleEscape.Infrastructure.Sessions;

/// <summary>Process-local storage. Restarting the host discards its sessions.</summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();
    public IReadOnlyCollection<GameSession> All => _sessions.Values.ToArray();
    public void Add(GameSession session)
    {
        if (!_sessions.TryAdd(session.Id, session)) throw new InvalidOperationException("Duplicate session id.");
    }
    public GameSession? Find(Guid id) => _sessions.GetValueOrDefault(id);
}
