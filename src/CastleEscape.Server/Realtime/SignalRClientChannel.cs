using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Messaging;
using CastleEscape.Game.Patterns;
using CastleEscape.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CastleEscape.Server.Realtime;

/// <summary>Pushes messages to the session's SignalR group; the method name is the client method.</summary>
[DesignPattern("Bridge", "ConcreteImplementor")]
public sealed class SignalRClientChannel(IHubContext<GameHub> hub) : IClientChannel
{
    public string Name => "SignalR";

    public Task SendAsync(Guid sessionId, string method, IServerMessage message) =>
        // object: SignalR serializes the runtime type, so every message keeps all its fields.
        hub.Clients.Group(GameHub.GroupName(sessionId)).SendAsync(method, (object)message);
}
