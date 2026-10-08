using CastleEscape.Contracts;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Messaging;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Bridge: notifiers (what to send) vary independently of channels (how to deliver).</summary>
public sealed class BridgeDemo : IPatternDemo
{
    public string Key => "bridge";

    /// <summary>A third channel, written for this demo in a few lines: it only counts what it receives.</summary>
    [DesignPattern("Bridge", "ConcreteImplementor")]
    public sealed class CountingChannel : IClientChannel
    {
        public Dictionary<string, int> Received { get; } = [];
        public string Name => "Counting";

        public Task SendAsync(Guid sessionId, string method, IServerMessage message)
        {
            Received[method] = Received.GetValueOrDefault(method) + 1;
            return Task.CompletedTask;
        }
    }

    public PatternDemoResponse Run(DemoOptions options)
    {
        var ticks = Math.Clamp(options.GetInt("ticks", 40), 1, 400);
        var everyNth = Math.Clamp(options.GetInt("pollingEvery", 5), 1, 100);
        var format = options.Get("format", "xml");
        var trace = new DemoTrace("Bridge");

        var polling = new PollingBufferChannel(capacity: 200);
        var counting = new CountingChannel();
        var notifiers = ClientNotifiers.For([counting, polling], new Dictionary<string, int> { [polling.Name] = everyNth });
        trace.Line($"Notifiers: {string.Join("; ", notifiers)}.");

        var (session, ana, _) = DemoWorld.PlayingSession(CommandDemo.Map);
        session.SubmitDirection(ana.PlayerId, Direction.Right);
        var sent = new Dictionary<string, int>();
        for (var i = 0; i < ticks; i++)
        {
            session.Tick(session.TickSeconds);
            foreach (var message in session.DrainOutbox())
            {
                foreach (var notifier in notifiers)
                {
                    if (notifier.Notify(session.Id, message) is not null)
                    {
                        sent[notifier.ToString()] = sent.GetValueOrDefault(notifier.ToString()) + 1;
                    }
                }
            }
        }

        trace.Line($"{ticks} ticks of a real session; messages each notifier passed to its channel:");
        foreach (var (notifier, count) in sent.OrderBy(kv => kv.Key))
        {
            trace.Line($"  {notifier,-36} {count,4}");
        }

        var buffered = polling.Read(session.Id, afterSeq: 0);
        var serializer = MessageSerializers.Choose(format);
        var sample = buffered.FirstOrDefault(m => m.Method == ClientMethods.GameEvent) ?? buffered[0];
        var body = serializer.Serialize(sample.Message);
        trace.Line($"Polling buffer: {buffered.Count} messages "
                   + $"({buffered.Count(m => m.Method == ClientMethods.StateUpdated)} StateUpdated: one every {everyNth} ticks).")
            .Line($"A poller reads them as {serializer.ContentType} (Adapter); message #{sample.Seq} ({sample.Method}): "
                  + (body.Length > 300 ? body[..300] + "…" : body));

        trace.Evidence("perNotifier", sent)
            .Evidence("countingChannel", counting.Received)
            .Evidence("pollingBuffered", buffered.Count)
            .Evidence("pollingStateUpdates", buffered.Count(m => m.Method == ClientMethods.StateUpdated));
        return trace.Done("2 notifiers x 3 channels (SignalR, Polling, and the demo's Counting) from 2 + 3 classes, not 6.");
    }
}
