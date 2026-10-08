using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Messaging;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Sessions;

namespace CastleEscape.Game.Tests.Patterns;

public class BridgeTests
{
    private static readonly Guid Session = Guid.NewGuid();

    private static OutgoingMessage State(long seq, long tick) =>
        new(ClientMethods.StateUpdated, new TickStateMessage(Session, seq, tick, SessionPhase.Playing, 1, false, [], [], [], []));

    private static OutgoingMessage Event(long seq, long tick) =>
        new(ClientMethods.GameEvent, new GameEventMessage(Session, seq, tick, GameEventTypes.LifeLost, null, "ouch", []));

    [Fact]
    public void AnyNotifierWorksOverAnyChannel()
    {
        var counting = new BridgeDemo.CountingChannel();
        var polling = new PollingBufferChannel(10);

        var notifiers = ClientNotifiers.For([counting, polling]);

        Assert.Equal(4, notifiers.Count);
        Assert.Equal(2, notifiers.Count(n => n.Channel == counting));
        Assert.Equal(2, notifiers.OfType<StateNotifier>().Count());
    }

    [Fact]
    public void Notifiers_OnlySendTheirOwnMessages()
    {
        var channel = new BridgeDemo.CountingChannel();
        var state = new StateNotifier(channel);
        var events = new EventNotifier(channel);

        Assert.NotNull(state.Notify(Session, State(1, 1)));
        Assert.Null(state.Notify(Session, Event(2, 1)));
        Assert.NotNull(events.Notify(Session, Event(2, 1)));
        Assert.Null(events.Notify(Session, State(1, 1)));
        Assert.Equal(new Dictionary<string, int> { [ClientMethods.StateUpdated] = 1, [ClientMethods.GameEvent] = 1 }, channel.Received);
    }

    [Fact]
    public void StateNotifier_ThinsStates_ButNeverEvents()
    {
        var channel = new BridgeDemo.CountingChannel();
        var notifiers = new ClientNotifier[] { new StateNotifier(channel, everyNthTick: 5), new EventNotifier(channel) };

        for (var tick = 1; tick <= 20; tick++)
        {
            ClientNotifiers.Dispatch(notifiers, Session, State(tick * 2, tick));
            ClientNotifiers.Dispatch(notifiers, Session, Event(tick * 2 + 1, tick));
        }

        Assert.Equal(4, channel.Received[ClientMethods.StateUpdated]); // ticks 5, 10, 15, 20
        Assert.Equal(20, channel.Received[ClientMethods.GameEvent]);
    }

    [Fact]
    public async Task PollingChannel_KeepsTheLastMessages_AndReadsAfterASeq()
    {
        var polling = new PollingBufferChannel(capacity: 3);

        for (var seq = 1; seq <= 5; seq++)
        {
            await polling.SendAsync(Session, ClientMethods.GameEvent, (IServerMessage)Event(seq, seq).Payload);
        }

        Assert.Equal([3L, 4L, 5L], polling.Read(Session, 0).Select(m => m.Seq));
        Assert.Equal([5L], polling.Read(Session, 4).Select(m => m.Seq));
        Assert.Empty(polling.Read(Guid.NewGuid(), 0));
    }

    [Fact]
    public void Demo_PollingGetsFewerStatesThanTheFullChannel()
    {
        var result = new BridgeDemo().Run(DemoOptions.None);

        var counting = Assert.IsType<Dictionary<string, int>>(result.Evidence["countingChannel"]);
        Assert.True((int)result.Evidence["pollingStateUpdates"]! < counting[ClientMethods.StateUpdated]);
    }
}
