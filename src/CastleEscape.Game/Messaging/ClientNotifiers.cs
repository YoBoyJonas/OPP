using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.Sessions;

namespace CastleEscape.Game.Messaging;

/// <summary>
/// Decides which of a session's outgoing messages to send and how often, then hands them to its channel.
/// Notifiers and channels vary independently: any notifier works over any channel.
/// </summary>
[DesignPattern("Bridge", "Abstraction")]
public abstract class ClientNotifier(IClientChannel channel)
{
    public IClientChannel Channel { get; } = channel;

    /// <summary>The client methods this notifier is responsible for.</summary>
    protected abstract IReadOnlySet<string> Methods { get; }

    /// <summary>Sends the message if it is this notifier's kind and passes its policy. Returns null if not sent.</summary>
    public Task? Notify(Guid sessionId, OutgoingMessage message) =>
        Methods.Contains(message.Method) && message.Payload is IServerMessage payload && ShouldSend(message.Method, payload)
            ? Channel.SendAsync(sessionId, message.Method, payload)
            : null;

    /// <summary>The notifier's own rule on top of the method filter. Default: send everything.</summary>
    protected virtual bool ShouldSend(string method, IServerMessage message) => true;

    public override string ToString() => $"{GetType().Name} over {Channel.Name}";
}

/// <summary>
/// The world as it looks: <c>LevelStarted</c> always, <c>StateUpdated</c> every <paramref name="everyNthTick"/>th tick.
/// States replace each other, so a slow channel can safely skip some.
/// </summary>
[DesignPattern("Bridge", "RefinedAbstraction")]
public sealed class StateNotifier(IClientChannel channel, int everyNthTick = 1) : ClientNotifier(channel)
{
    private static readonly HashSet<string> StateMethods = [ClientMethods.LevelStarted, ClientMethods.StateUpdated];

    public int EveryNthTick { get; } = Math.Max(1, everyNthTick);

    protected override IReadOnlySet<string> Methods => StateMethods;

    protected override bool ShouldSend(string method, IServerMessage message) =>
        method != ClientMethods.StateUpdated || message.Tick % EveryNthTick == 0;
}

/// <summary>
/// What happened: game events, lobby changes and errors. Every one is sent; skipping an event would lose it.
/// </summary>
[DesignPattern("Bridge", "RefinedAbstraction")]
public sealed class EventNotifier(IClientChannel channel) : ClientNotifier(channel)
{
    private static readonly HashSet<string> EventMethods = [ClientMethods.GameEvent, ClientMethods.SessionUpdated, ClientMethods.Error];

    protected override IReadOnlySet<string> Methods => EventMethods;
}

/// <summary>Builds the notifiers for a set of channels: a state and an event notifier per channel.</summary>
public static class ClientNotifiers
{
    /// <param name="stateEveryNthTick">Per channel; channels not listed get every state.</param>
    public static IReadOnlyList<ClientNotifier> For(IEnumerable<IClientChannel> channels, IReadOnlyDictionary<string, int>? stateEveryNthTick = null) =>
        channels.SelectMany(channel => new ClientNotifier[]
        {
            new StateNotifier(channel, stateEveryNthTick?.GetValueOrDefault(channel.Name, 1) ?? 1),
            new EventNotifier(channel),
        }).ToList();

    /// <summary>Gives a message to every notifier; returns the sends that started.</summary>
    public static List<Task> Dispatch(IEnumerable<ClientNotifier> notifiers, Guid sessionId, OutgoingMessage message) =>
        notifiers.Select(n => n.Notify(sessionId, message)).OfType<Task>().ToList();
}
