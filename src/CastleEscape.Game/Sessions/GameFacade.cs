using CastleEscape.Contracts;
using CastleEscape.Contracts.Diagnostics;
using CastleEscape.Contracts.Gameplay;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.Content;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.Sessions;

/// <summary>What a newly connected hub client needs: who it is, and the messages that catch it up (layout and state once a level runs).</summary>
public sealed record ConnectResult(Guid SessionId, Guid PlayerId, SessionUpdatedMessage Session, LevelStartedMessage? LevelStarted, TickStateMessage? State);

/// <summary>
/// The one entry point to the game for every client: the SignalR hub, the REST endpoints and the console
/// demo. Each operation is one call here; behind it are the session registry (lookup, tokens), the
/// sessions and their command queues, the loop scheduler, and the content catalog.
/// Callers never see a <see cref="GameSession"/> or a <see cref="PlayerSlot"/>; they get contract DTOs.
/// </summary>
[DesignPattern("Facade", "Facade")]
public sealed class GameFacade(SessionRegistry registry, GameLoopScheduler scheduler, ContentCatalog catalog)
{
    // ---------------------------------------------------------------- lobby

    public CreateSessionResponse CreateSession(string playerName, int? seed = null)
    {
        var (session, player) = registry.Create(playerName, seed);
        return new CreateSessionResponse(session.Id, session.JoinCode, player.PlayerId, player.Token);
    }

    public JoinSessionResponse JoinSession(string joinCode, string playerName)
    {
        var (session, player) = registry.Join(joinCode, playerName);
        return new JoinSessionResponse(session.Id, player.PlayerId, player.Token);
    }

    public SessionDto SelectCharacter(Guid sessionId, string? playerToken, string characterId)
    {
        var (session, player) = registry.Authenticate(sessionId, playerToken);
        session.SelectCharacter(player.PlayerId, characterId);
        return session.Snapshot.Session;
    }

    public void LeaveSession(Guid sessionId, string? playerToken)
    {
        var (session, player) = registry.Authenticate(sessionId, playerToken);
        session.Leave(player.PlayerId);
    }

    public IReadOnlyList<SessionSummary> ListSessions() =>
        registry.All.Select(s => new SessionSummary(s.Id, s.JoinCode, s.Phase, s.Snapshot.Session.Players.Length)).ToList();

    public SessionDto GetSession(Guid sessionId) => registry.Get(sessionId).Snapshot.Session;

    /// <summary>The characters a player can pick (PLR-1).</summary>
    public IReadOnlyList<string> CharacterIds => catalog.Characters.Select(c => c.Id).ToList();

    // ---------------------------------------------------------------- input (queued as commands)

    public void SubmitDirection(Guid sessionId, string? playerToken, Direction direction)
    {
        var (session, player) = registry.Authenticate(sessionId, playerToken);
        session.SubmitDirection(player.PlayerId, direction);
    }

    public void RequestRestart(Guid sessionId, string? playerToken)
    {
        var (session, player) = registry.Authenticate(sessionId, playerToken);
        session.RequestRestart(player.PlayerId);
    }

    // ---------------------------------------------------------------- reads (snapshots, no locking)

    public TickStateMessage GetState(Guid sessionId) => registry.Get(sessionId).Snapshot.State ?? throw NoLevel();

    public LevelLayoutResponse GetLevelLayout(Guid sessionId) => registry.Get(sessionId).Snapshot.Layout ?? throw NoLevel();

    public HudResponse GetHud(Guid sessionId) => registry.Get(sessionId).Snapshot.Hud;

    // ---------------------------------------------------------------- hub connections

    /// <summary>Checks the token, records the connection, and returns the messages that bring the client up to date.</summary>
    public ConnectResult Connect(Guid sessionId, string? playerToken, string connectionId)
    {
        var (session, player) = registry.Authenticate(sessionId, playerToken);
        session.SetConnection(player.PlayerId, connectionId);

        var snapshot = session.Snapshot;
        return new ConnectResult(session.Id, player.PlayerId,
            new SessionUpdatedMessage(session.Id, snapshot.State?.Seq ?? 0, snapshot.Hud.Tick, snapshot.Session),
            snapshot.LevelStarted, snapshot.State);
    }

    /// <summary>Forgets a connection, unless the player has already reconnected on a newer one.</summary>
    public void Disconnect(Guid sessionId, string? playerToken, string connectionId)
    {
        try
        {
            var (session, player) = registry.Authenticate(sessionId, playerToken);
            session.Disconnect(player.PlayerId, connectionId);
        }
        catch (GameException)
        {
            // The session is gone or the token no longer matches: nothing to clean up.
        }
    }

    public PongMessage Ping(Guid sessionId, string? playerToken)
    {
        var (session, _) = registry.Authenticate(sessionId, playerToken);
        var snapshot = session.Snapshot;
        return new PongMessage(session.Id, snapshot.State?.Seq ?? 0, snapshot.Hud.Tick, DateTimeOffset.UtcNow);
    }

    // ---------------------------------------------------------------- diagnostics

    /// <summary>The session's recent commands, oldest first (Command pattern history).</summary>
    public IReadOnlyList<CommandRecordDto> GetCommandHistory(Guid sessionId) =>
        registry.Get(sessionId).CommandHistory().Select(c => new CommandRecordDto(c.Sequence, c.Name, c.PlayerId, c.Tick, c.Undone)).ToList();

    /// <summary>The session's recent game events, oldest first; optionally only those after <paramref name="afterTick"/>.</summary>
    public IReadOnlyList<EventLogEntryDto> GetEventLog(Guid sessionId, long afterTick = -1) =>
        registry.Get(sessionId).RecentEvents()
            .Where(e => e.Tick > afterTick)
            .Select(e => new EventLogEntryDto(e.Tick, e.At, e.Type, e.PlayerId, e.Message, new Dictionary<string, string>(e.Data)))
            .ToList();

    // ---------------------------------------------------------------- loop

    /// <summary>One fixed step for every session; returns the messages to deliver.</summary>
    public List<SessionMessage> Tick(double seconds) => scheduler.TickAll(seconds);

    private static GameException NoLevel() =>
        new(GameErrorCode.NoLevelLoaded, "No level has been loaded yet; both players must join and pick a character.");
}
