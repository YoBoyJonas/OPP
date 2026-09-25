using System.Globalization;

namespace CastleEscape.Game.Events;

/// <summary>
/// A gameplay fact noted during a tick; the session turns these into <c>GameEvent</c> messages at the end
/// of the tick. <see cref="Type"/> is one of <c>GameEventTypes</c>.
/// </summary>
public sealed record PendingEvent(string Type, Guid? PlayerId, string Message, IReadOnlyDictionary<string, string> Data)
{
    /// <summary>Creates an event; <paramref name="data"/> is an anonymous object, e.g. <c>new { power, level }</c>.</summary>
    public static PendingEvent Of(string type, Guid? playerId, string message, object? data = null) =>
        new(type, playerId, message, ToDictionary(data));

    private static Dictionary<string, string> ToDictionary(object? data) =>
        data?.GetType().GetProperties().ToDictionary(
            p => p.Name,
            p => Convert.ToString(p.GetValue(data), CultureInfo.InvariantCulture) ?? "") ?? [];
}
