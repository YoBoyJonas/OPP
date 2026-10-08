using System.Collections.Concurrent;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.Messaging;

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

/// <summary>One message kept by the polling channel.</summary>
public sealed record BufferedMessage(long Seq, string Method, IServerMessage Message);

/// <summary>
/// Keeps the last messages of each session in memory, for clients that can't hold a SignalR connection.
/// They read them with <c>GET /api/sessions/{id}/messages?afterSeq=</c>, serialized as JSON or XML (Adapter).
/// </summary>
[DesignPattern("Bridge", "ConcreteImplementor")]
public sealed class PollingBufferChannel(int capacity) : IClientChannel
{
    private readonly ConcurrentDictionary<Guid, Queue<BufferedMessage>> _buffers = new();

    public string Name => "Polling";

    public int Capacity => capacity;

    public Task SendAsync(Guid sessionId, string method, IServerMessage message)
    {
        var buffer = _buffers.GetOrAdd(sessionId, _ => new Queue<BufferedMessage>());
        lock (buffer)
        {
            buffer.Enqueue(new BufferedMessage(message.Seq, method, message));
            while (buffer.Count > capacity)
            {
                buffer.Dequeue();
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>Messages with a sequence number above <paramref name="afterSeq"/>, oldest first.</summary>
    public IReadOnlyList<BufferedMessage> Read(Guid sessionId, long afterSeq)
    {
        if (!_buffers.TryGetValue(sessionId, out var buffer))
        {
            return [];
        }
        lock (buffer)
        {
            return buffer.Where(m => m.Seq > afterSeq).ToList();
        }
    }
}
