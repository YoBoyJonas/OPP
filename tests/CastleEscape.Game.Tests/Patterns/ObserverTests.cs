using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Sessions;

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
    public void StatisticsObserver_CountsPerPlayer_ButNotPlayerlessEvents()
    {
        var slot = new PlayerSlot(Pickup.PlayerId!.Value, "t", "Ana", 1);
        var stats = new SessionStatisticsObserver([slot]);

        stats.OnEvent(Pickup);
        stats.OnEvent(Pickup);
        stats.OnEvent(new PlayerLeft(null, "bye"));

        Assert.Equal(new Dictionary<string, int> { ["ItemCollected"] = 2 }, slot.Stats);
    }

    [Fact]
    public void Demo_DetachedObserverStopsReceiving()
    {
        var result = new ObserverDemo().Run(DemoOptions.None);

        var seen = Assert.IsType<List<string>>(result.Evidence["recorderSaw"]);
        Assert.Equal(seen.Count, result.Evidence["recorderCountAfterDetach"]);
        Assert.Contains(nameof(ItemCollected), seen);
        Assert.True((int)result.Evidence["clientEventsAfterDetach"]! > 0);
    }
}
