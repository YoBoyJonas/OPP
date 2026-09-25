using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Sessions;
using Microsoft.AspNetCore.SignalR;

namespace CastleEscape.Server.Hubs;

/// <summary>
/// Real-time channel at <c>/hubs/game?sessionId=…&amp;playerToken=…</c>. Clients only send input (NET-1);
/// the server pushes lobby updates, level layouts, per-tick state and events.
/// </summary>
public class GameHub(SessionRegistry registry) : Hub<IGameClient>
{
    private const string SessionKey = "sessionId";
    private const string PlayerKey = "playerId";

    public static string GroupName(Guid sessionId) => $"session:{sessionId}";

    public override async Task OnConnectedAsync()
    {
        var query = Context.GetHttpContext()?.Request.Query;
        Guid.TryParse(query?["sessionId"], out var sessionId);
        var token = query?["playerToken"].ToString();

        GameSession session;
        PlayerSlot player;
        try
        {
            (session, player) = registry.Authenticate(sessionId, token);
        }
        catch (GameException ex)
        {
            await Clients.Caller.Error(new ErrorMessage(sessionId, 0, 0, ex.Code, ex.Message));
            Context.Abort();
            return;
        }

        Context.Items[SessionKey] = session.Id;
        Context.Items[PlayerKey] = player.PlayerId;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(session.Id));
        session.SetConnection(player.PlayerId, Context.ConnectionId);

        // Catch up a new or reconnecting client with the current lobby, layout and state.
        var snapshot = session.Snapshot;
        var tick = snapshot.Hud.Tick;
        await Clients.Caller.SessionUpdated(new SessionUpdatedMessage(session.Id, snapshot.State?.Seq ?? 0, tick, snapshot.Session));
        if (snapshot.LevelStarted is { } levelStarted)
        {
            await Clients.Caller.LevelStarted(levelStarted);
        }
        if (snapshot.State is { } state)
        {
            await Clients.Caller.StateUpdated(state);
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetPlayer(out var session, out var playerId))
        {
            session.Disconnect(playerId, Context.ConnectionId);
        }
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>The direction key now held: Up/Down/Left/Right on key down, None on key up.</summary>
    public void SetDirection(Contracts.Direction direction)
    {
        var (session, playerId) = RequirePlayer();
        session.SubmitDirection(playerId, direction);
    }

    /// <summary>Restart the current level (D8). Ask for confirmation on the client first.</summary>
    public void RequestRestart()
    {
        var (session, playerId) = RequirePlayer();
        session.RequestRestart(playerId);
    }

    /// <summary>Round-trip check; answered with <c>Pong</c>.</summary>
    public Task Ping()
    {
        var (session, _) = RequirePlayer();
        var snapshot = session.Snapshot;
        return Clients.Caller.Pong(new PongMessage(session.Id, snapshot.State?.Seq ?? 0, snapshot.Hud.Tick, DateTimeOffset.UtcNow));
    }

    private (GameSession Session, Guid PlayerId) RequirePlayer() =>
        TryGetPlayer(out var session, out var playerId)
            ? (session, playerId)
            : throw new HubException("Not joined to a session.");

    private bool TryGetPlayer(out GameSession session, out Guid playerId)
    {
        session = null!;
        playerId = Guid.Empty;
        if (Context.Items.TryGetValue(SessionKey, out var s) && Context.Items.TryGetValue(PlayerKey, out var p))
        {
            try
            {
                session = registry.Get((Guid)s!);
                playerId = (Guid)p!;
                return true;
            }
            catch (GameException)
            {
                return false;
            }
        }
        return false;
    }
}
