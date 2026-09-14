using CasteEscapeServer.Game;
using CasteEscapeServer.Models;
using Microsoft.AspNetCore.SignalR;

namespace CasteEscapeServer.Hubs;

public class GameHub : Hub
{
    private readonly SessionManager _sessions;

    public GameHub(SessionManager sessions)
    {
        _sessions = sessions;
    }

    public async Task<JoinResult> JoinSession(string playerName, string characterId)
    {
        var result = _sessions.Join(Context.ConnectionId, playerName, characterId);
        await Groups.AddToGroupAsync(Context.ConnectionId, result.SessionId);
        return result;
    }

    public void SendInput(PlayerInputMessage input)
    {
        _sessions.GetSession(Context.ConnectionId)?.SetInput(Context.ConnectionId, input.Direction);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var session = _sessions.Disconnect(Context.ConnectionId);
        var opponent = session?.Players.FirstOrDefault(p => p != null && p.ConnectionId != Context.ConnectionId);
        if (opponent != null)
        {
            await Clients.Client(opponent.ConnectionId).SendAsync("OpponentLeft");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
