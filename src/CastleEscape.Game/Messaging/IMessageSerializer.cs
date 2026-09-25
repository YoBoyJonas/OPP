using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.Messaging;

/// <summary>
/// The one small serialization interface the game uses for messages (NET-2: JSON or XML).
/// Adapter pattern target: each implementation adapts one large .NET serializer API to these 3 members.
/// </summary>
[DesignPattern("Adapter", "Target")]
public interface IMessageSerializer
{
    /// <summary>MIME type of the output, e.g. "application/json".</summary>
    string ContentType { get; }

    string Serialize<T>(T value);

    T Deserialize<T>(string text);
}
