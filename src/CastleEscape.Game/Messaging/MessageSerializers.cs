namespace CastleEscape.Game.Messaging;

/// <summary>Picks a serializer from a <c>?format=</c> value or an <c>Accept</c> header. JSON is the default.</summary>
public static class MessageSerializers
{
    public static readonly IMessageSerializer Json = new JsonMessageSerializerAdapter();
    public static readonly IMessageSerializer Xml = new XmlMessageSerializerAdapter();

    public static IMessageSerializer Choose(string? format, string? accept = null)
    {
        if (!string.IsNullOrWhiteSpace(format))
        {
            return format.Trim().ToLowerInvariant() switch
            {
                "xml" => Xml,
                "json" => Json,
                _ => throw new ArgumentException($"Unknown format '{format}'. Use json or xml.", nameof(format)),
            };
        }
        return accept?.Contains("xml", StringComparison.OrdinalIgnoreCase) == true ? Xml : Json;
    }
}
