using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Sessions;
using Microsoft.AspNetCore.SignalR;

namespace CastleEscape.Server.Hubs;

/// <summary>
/// Real-time channel at <c>/hubs/game?sessionId=…&amp;playerToken=…</c>. Clients only send input (NET-1);
/// the server pushes lobby updates, level layouts, per-tick state and events. Every call goes through
/// the <see cref="GameFacade"/>.
/// </summary>
public class GameHub(GameFacade game) : Hub<IGameClient>
{
    private const string SessionKey = "sessionId";
    private const string TokenKey = "playerToken";

    public static string GroupName(Guid sessionId) => $"session:{sessionId}";

    public override async Task OnConnectedAsync()
    {
        var query = Context.GetHttpContext()?.Request.Query;
        Guid.TryParse(query?["sessionId"], out var sessionId);
        var token = query?["playerToken"].ToString();

        ConnectResult connection;
        try
        {
            connection = game.Connect(sessionId, token, Context.ConnectionId);
        }
        catch (GameException ex)
        {
            await Clients.Caller.Error(new ErrorMessage(sessionId, 0, 0, ex.Code, ex.Message));
            Context.Abort();
            return;
        }

        Context.Items[SessionKey] = connection.SessionId;
        Context.Items[TokenKey] = token;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(connection.SessionId));

        // Catch up a new or reconnecting client with the current lobby, layout and state.
        await Clients.Caller.SessionUpdated(connection.Session);
        if (connection.LevelStarted is { } levelStarted)
        {
            await Clients.Caller.LevelStarted(levelStarted);
        }
        if (connection.State is { } state)
        {
            await Clients.Caller.StateUpdated(state);
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetPlayer(out var sessionId, out var token))
        {
            game.Disconnect(sessionId, token, Context.ConnectionId);
        }
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>The direction key now held: Up/Down/Left/Right on key down, None on key up.</summary>
    public void SetDirection(Contracts.Direction direction)
    {
        var (sessionId, token) = RequirePlayer();
        game.SubmitDirection(sessionId, token, direction);
    }

    /// <summary>Restart the current level (D8). Ask for confirmation on the client first.</summary>
    public void RequestRestart()
    {
        var (sessionId, token) = RequirePlayer();
        game.RequestRestart(sessionId, token);
    }

    /// <summary>Round-trip check; answered with <c>Pong</c>.</summary>
    public Task Ping()
    {
        var (sessionId, token) = RequirePlayer();
        return Clients.Caller.Pong(game.Ping(sessionId, token));
    }

    private (Guid SessionId, string Token) RequirePlayer() =>
        TryGetPlayer(out var sessionId, out var token)
            ? (sessionId, token)
            : throw new HubException("Not joined to a session.");

    private bool TryGetPlayer(out Guid sessionId, out string token)
    {
        sessionId = Guid.Empty;
        token = "";
        if (Context.Items.TryGetValue(SessionKey, out var s) && Context.Items.TryGetValue(TokenKey, out var t) && s is Guid id && t is string tk)
        {
            sessionId = id;
            token = tk;
            return true;
        }
        return false;
    }
}
