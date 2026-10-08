using CastleEscape.Contracts;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Events;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Observer requirement: show how it works (the doc has the sequence diagram).</summary>
public sealed class ObserverDemo : IPatternDemo
{
    public string Key => "observer";

    /// <summary>Ana (1) next to a coin; Ben (2) right above a zombie (Z).</summary>
    public static readonly string[] Map =
    [
        "##########",
        "#1r.....2#",
        "#.......Z#",
        "##########",
    ];

    /// <summary>A demo observer: remembers what it was told.</summary>
    private sealed class Recorder : IGameEventObserver
    {
        public List<string> Seen { get; } = [];
        public void OnEvent(GameEvent gameEvent) => Seen.Add(gameEvent is SoundCue cue ? $"SoundCue({cue.Cue})" : gameEvent.Type);
    }

    public PatternDemoResponse Run(DemoOptions options)
    {
        var (session, ana, ben) = DemoWorld.PlayingSession(Map);
        session.DrainOutbox();
        var recorder = new Recorder();
        var trace = new DemoTrace("Observer");

        session.Attach(recorder);
        trace.Line("Subject: the session's GameEventPublisher. Observers: ClientNotification, SessionStatistics, EventLog, SoundCue, "
                   + "plus this demo's Recorder (attached now).");

        session.SubmitDirection(ana.PlayerId, Direction.Right);
        DemoWorld.RunUntil(session, () => recorder.Seen.Contains(nameof(ItemCollected)));
        session.SubmitDirection(ana.PlayerId, Direction.None);
        DemoWorld.RunUntil(session, () => recorder.Seen.Contains(nameof(LifeLost)));
        trace.Line($"Ana picks up the coin, then the zombie catches Ben. The Recorder saw, in order: {string.Join(", ", recorder.Seen)}.")
            .Line("  Each cue arrives before its event here: the SoundCue observer (attached before the Recorder) publishes it while "
                  + "the event is still being delivered. The clients, notified first, get ItemCollected and then SoundCue.");

        var seenWhileAttached = recorder.Seen.Count;
        session.Detach(recorder);
        DemoWorld.RunUntil(session, () => false, maxTicks: 40);
        var logSize = session.RecentEvents().Count;
        trace.Line($"Recorder detached; 40 more ticks: it still has {recorder.Seen.Count} events (was {seenWhileAttached}), "
                   + $"while the EventLog observer kept going ({logSize} entries).");

        var gameEventMessages = session.DrainOutbox().Count(m => m.Method == ClientMethods.GameEvent);
        var hud = session.Snapshot.Hud.Players;
        var anaStats = hud.Single(p => p.PlayerId == ana.PlayerId).Stats;
        var benStats = hud.Single(p => p.PlayerId == ben.PlayerId).Stats;
        var cues = session.RecentEvents().Where(e => e.Type == nameof(SoundCue)).Select(e => e.Data["cue"]).ToArray();
        trace.Line($"ClientNotification sent {gameEventMessages} GameEvent messages to the clients.")
            .Line($"SessionStatistics: Ana {Format(anaStats)}; Ben {Format(benStats)}.")
            .Line($"SoundCue turned events into cues: {string.Join(", ", cues)}.");

        trace.Evidence("recorderSaw", recorder.Seen)
            .Evidence("recorderCountAfterDetach", recorder.Seen.Count)
            .Evidence("eventLogEntries", logSize)
            .Evidence("gameEventMessages", gameEventMessages)
            .Evidence("soundCues", cues)
            .Evidence("statsAna", anaStats)
            .Evidence("statsBen", benStats);
        return trace.Done("One Publish per event; each observer does its own job and none of them knows about the others.");
    }

    private static string Format(Dictionary<string, int> stats) =>
        string.Join(", ", stats.Where(kv => kv.Key is nameof(ItemCollected) or nameof(LifeLost) or nameof(PowerGained)).Select(kv => $"{kv.Key} {kv.Value}"));
}
