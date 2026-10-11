using CastleEscape.Contracts.Realtime;
using CastleEscape.Domain.Patterns;

namespace CastleEscape.Application.Messaging;

/// <summary>
/// How a message physically reaches a session's clients. The notifiers decide <i>what</i> to send;
/// a channel only knows <i>how</i> to deliver it.
/// </summary>
[DesignPattern("Bridge", "Implementor")]
public interface IClientChannel
{
    /// <summary>Short name for logs and diagnostics, e.g. "SignalR".</summary>
    string Name { get; }

    Task SendAsync(Guid sessionId, string method, IServerMessage message);
}
