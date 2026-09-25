using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.Tests.Sessions;

namespace CastleEscape.Game.Tests.Patterns;

public class ObserverTests
{
    private sealed class Recorder : IGameEventObserver
    {
        public List<GameEvent> Seen { get; } = [];
        public void OnEvent(GameEvent gameEvent) => Seen.Add(gameEvent);
    }

    private static readonly ItemCollected Pickup = new(Guid.NewGuid(), "picked up", "item-1", "gold-coin", ConsumableKind.Reward, 3, 10);

    [Fact]
    public void Publisher_NotifiesAttachedObserversOnly()
    {
        var publisher = new GameEventPublisher();
        var a = new Recorder();
        var b = new Recorder();
        publisher.Attach(a);
        publisher.Attach(b);
        publisher.Attach(a); // attaching twice has no effect

        publisher.Publish(Pickup);
        publisher.Detach(b);
        publisher.Publish(Pickup);

        Assert.Equal(2, a.Seen.Count);
        Assert.Single(b.Seen);
    }

    [Fact]
    public void Event_TypeAndDataComeFromTheRecord()
    {
        Assert.Equal(GameEventTypes.ItemCollected, Pickup.Type);
        Assert.Equal(new Dictionary<string, string>
        {
            ["itemId"] = "item-1", ["consumableId"] = "gold-coin", ["kind"] = "Reward", ["lives"] = "3", ["score"] = "10",
        }, Pickup.Data());
    }

    [Fact]
    public void EveryEventClass_HasAWireName()
    {
        var names = typeof(GameEventTypes).GetFields().Select(f => (string)f.GetValue(null)!).ToHashSet();
        var events = typeof(GameEvent).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameEvent)));

        Assert.All(events, t => Assert.Contains(t.Name, names));
    }

    [Fact]
    public void SoundCueObserver_PublishesACueForKnownEvents()
    {
        var publisher = new GameEventPublisher();
        var recorder = new Recorder();
        publisher.Attach(recorder);
        publisher.Attach(new SoundCueObserver(publisher));

        publisher.Publish(Pickup);
        publisher.Publish(new PlayerLeft(null, "bye"));

        var cue = Assert.Single(recorder.Seen.OfType<SoundCue>());
        Assert.Equal("pickup", cue.Cue);
        Assert.Equal(nameof(ItemCollected), cue.Source);
    }

    [Fact]
    public void StatisticsObserver_CountsPerPlayer_ButNotSoundCues()
    {
        var slot = new PlayerSlot(Pickup.PlayerId!.Value, "t", "Ana", 1);
        var stats = new SessionStatisticsObserver([slot]);

        stats.OnEvent(Pickup);
        stats.OnEvent(Pickup);
        stats.OnEvent(new SoundCue(slot.PlayerId, "cue", "pickup", "ItemCollected"));

        Assert.Equal(new Dictionary<string, int> { ["ItemCollected"] = 2 }, slot.Stats);
    }

    [Fact]
    public void EventLog_KeepsTheLastEntriesOnly()
    {
        var log = new EventLogObserver(() => 7, capacity: 3);

        for (var i = 0; i < 5; i++)
        {
            log.OnEvent(new LevelStarted(null, $"level {i}", i, LevelTheme.Dungeon));
        }

        Assert.Equal(["level 2", "level 3", "level 4"], log.Entries.Select(e => e.Message));
        Assert.All(log.Entries, e => Assert.Equal(7, e.Tick));
    }

    [Fact]
    public void Session_SendsEventsAndCues_InOrder()
    {
        var game = TestGame.Start(["#########", "#1r....2#", "#########"]);

        game.Hold(game.P1, Direction.Right);
        game.RunUntil(() => game.EventsOf(GameEventTypes.SoundCue).Any());

        var types = game.Messages.Select(m => m.Payload).OfType<GameEventMessage>().Select(e => e.Type).ToList();
        Assert.Equal(types.IndexOf(GameEventTypes.ItemCollected) + 1, types.IndexOf(GameEventTypes.SoundCue));
        Assert.Contains(game.Session.RecentEvents(), e => e.Type == GameEventTypes.ItemCollected);
    }

    [Fact]
    public void Demo_DetachedObserverStopsReceiving()
    {
        var result = new ObserverDemo().Run(DemoOptions.None);

        var seen = Assert.IsType<List<string>>(result.Evidence["recorderSaw"]);
        Assert.Equal(seen.Count, result.Evidence["recorderCountAfterDetach"]);
        Assert.Contains("SoundCue(pickup)", seen);
        Assert.True((int)result.Evidence["eventLogEntries"]! > seen.Count);
    }
}
