using System.ComponentModel.DataAnnotations;

namespace CastleEscape.Game.Configuration;

/// <summary>Ways a server message can reach the clients.</summary>
public enum RealtimeChannel
{
    /// <summary>Push over the SignalR hub.</summary>
    SignalR,

    /// <summary>Buffered per session, read with <c>GET /api/sessions/{id}/messages?afterSeq=</c>.</summary>
    Polling
}

/// <summary>Client delivery settings, bound from the <c>Realtime</c> configuration section.</summary>
public sealed class RealtimeOptions
{
    public const string SectionName = "Realtime";

    // Array, not List: the configuration binder replaces arrays but appends to pre-filled lists.
    [MinLength(1)]
    public RealtimeChannel[] EnabledChannels { get; set; } = [RealtimeChannel.SignalR, RealtimeChannel.Polling];

    /// <summary>How many recent messages the polling channel keeps per session.</summary>
    [Range(1, 10_000)]
    public int PollingBufferSize { get; set; } = 200;

    /// <summary>
    /// The polling channel gets every Nth tick's StateUpdated (events always). At 20 Hz a buffer of 200
    /// would otherwise hold only 10 seconds of messages, and polling clients don't need 20 states a second.
    /// </summary>
    [Range(1, 100)]
    public int PollingStateEveryNthTick { get; set; } = 5;
}
