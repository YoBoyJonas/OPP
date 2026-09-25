using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.Messaging;

/// <summary>
/// Adapts <see cref="DataContractSerializer"/> (adaptee: one instance per type, stream/writer/reader based,
/// with WriteObject/ReadObject/WriteStartObject/IsStartObject… overloads) to <see cref="IMessageSerializer"/>.
/// The protocol messages carry [DataContract] so they can be written as XML.
/// </summary>
[DesignPattern("Adapter", "Adapter")]
public sealed class XmlMessageSerializerAdapter : IMessageSerializer
{
    private static readonly XmlWriterSettings WriterSettings = new() { Indent = true, OmitXmlDeclaration = false };
    private readonly ConcurrentDictionary<Type, DataContractSerializer> _serializers = new();

    public string ContentType => "application/xml";

    public string Serialize<T>(T value)
    {
        var type = value?.GetType() ?? typeof(T);
        using var text = new Utf8StringWriter();
        using (var writer = XmlWriter.Create(text, WriterSettings))
        {
            SerializerFor(type).WriteObject(writer, value);
        }
        return text.ToString();
    }

    public T Deserialize<T>(string text)
    {
        using var reader = XmlReader.Create(new StringReader(text));
        return (T)(SerializerFor(typeof(T)).ReadObject(reader) ?? throw new FormatException("XML contained no object."));
    }

    private DataContractSerializer SerializerFor(Type type) => _serializers.GetOrAdd(type, t => new DataContractSerializer(t));

    /// <summary>StringWriter reports UTF-16; this makes the XML declaration say UTF-8, which is what HTTP sends.</summary>
    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
