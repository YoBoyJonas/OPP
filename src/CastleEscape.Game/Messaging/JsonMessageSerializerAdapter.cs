using System.Text.Json;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.Messaging;

/// <summary>
/// Adapts <see cref="JsonSerializer"/> (adaptee: a static class with dozens of overloads for strings, streams,
/// spans, nodes, type info…) to <see cref="IMessageSerializer"/>, using the game's JSON conventions.
/// </summary>
[DesignPattern("Adapter", "Adapter")]
public sealed class JsonMessageSerializerAdapter : IMessageSerializer
{
    private readonly JsonSerializerOptions _options = JsonDefaults.Create();

    public string ContentType => "application/json";

    // The runtime type, so an object-typed payload is written with all its properties.
    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, value?.GetType() ?? typeof(T), _options);

    public T Deserialize<T>(string text) =>
        JsonSerializer.Deserialize<T>(text, _options) ?? throw new FormatException("JSON text was 'null'.");
}
