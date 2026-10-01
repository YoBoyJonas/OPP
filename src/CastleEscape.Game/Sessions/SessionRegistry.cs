using System.Collections.Concurrent;
using CastleEscape.Contracts;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.Sessions;

/// <summary>All sessions on this server, found by id or join code (join-session activity diagram).</summary>
[DesignPattern("Facade", "Subsystem")]
public class SessionRegistry(ContentCatalog catalog, ILevelProvider levels, GameOptions options, PatternOptions? patterns = null)
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O, 1/I
    private const int MaxNameLength = 24;

    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();
    private readonly object _createGate = new();

    public IReadOnlyCollection<GameSession> All => _sessions.Values.OrderBy(s => s.CreatedAt).ToList();

    /// <summary>Creates a session with its first player.</summary>
    /// <param name="playerName">The creator's name.</param>
    /// <param name="seed">Fixed seed for reproducible levels (tests, demos); random when null.</param>
    public (GameSession Session, PlayerSlot Player) Create(string playerName, int? seed = null)
    {
        var name = CleanName(playerName);
        GameSession session;
        lock (_createGate)
        {
            session = new GameSession(Guid.NewGuid(), NewJoinCode(), catalog, levels, options, seed ?? Random.Shared.Next(), patterns);
            _sessions[session.Id] = session;
        }
        return (session, session.AddPlayer(name));
    }

    /// <summary>Joins by code (NET-3: one session, two players).</summary>
    public (GameSession Session, PlayerSlot Player) Join(string joinCode, string playerName)
    {
        var name = CleanName(playerName);
        var code = joinCode?.Trim().ToUpperInvariant() ?? "";
        var session = _sessions.Values.FirstOrDefault(s => s.JoinCode == code)
                      ?? throw new GameException(GameErrorCode.InvalidJoinCode, $"No session has the code '{code}'.");
        return (session, session.AddPlayer(name));
    }

    public GameSession Get(Guid sessionId) =>
        _sessions.GetValueOrDefault(sessionId)
        ?? throw new GameException(GameErrorCode.SessionNotFound, $"Session {sessionId} does not exist.");

    /// <summary>The session and the player whose token this is.</summary>
    public (GameSession Session, PlayerSlot Player) Authenticate(Guid sessionId, string? token)
    {
        var session = Get(sessionId);
        var player = string.IsNullOrEmpty(token) ? null : session.FindByToken(token);
        return player is null
            ? throw new GameException(GameErrorCode.InvalidPlayerToken, "Missing or wrong player token for this session.")
            : (session, player);
    }

    private string NewJoinCode()
    {
        while (true)
        {
            var code = new string(Enumerable.Range(0, 6).Select(_ => CodeAlphabet[Random.Shared.Next(CodeAlphabet.Length)]).ToArray());
            if (_sessions.Values.All(s => s.JoinCode != code))
            {
                return code;
            }
        }
    }

    private static string CleanName(string? name)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length is 0 or > MaxNameLength)
        {
            throw new GameException(GameErrorCode.InvalidRequest, $"Player name must be 1-{MaxNameLength} characters.");
        }
        return trimmed;
    }
}
