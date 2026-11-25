using System.Xml.Linq;
using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.Messaging;

namespace CastleEscape.Game.Tests.Patterns;

public class AdapterTests
{
    public static TheoryData<string> Formats => new() { "json", "xml" };

    private static void AssertRoundTrip<T>(IMessageSerializer serializer, T message)
    {
        var back = serializer.Deserialize<T>(serializer.Serialize(message));
        // Compare by content: records with arrays are not equal by reference.
        Assert.Equal(MessageSerializers.Json.Serialize(message), MessageSerializers.Json.Serialize(back));
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void EveryServerMessage_RoundTrips(string format)
    {
        var serializer = MessageSerializers.Choose(format);
        var state = TestWorld.SampleState();
        var session = new SessionDto(state.SessionId, "ABC123", SessionPhase.Playing, 1, 10,
            [new SessionPlayerDto(Guid.NewGuid(), 1, "Ana", "scout", true), new SessionPlayerDto(Guid.NewGuid(), 2, "Ben", null, false)]);

        AssertRoundTrip(serializer, state);
        AssertRoundTrip(serializer, new LevelStartedMessage(state.SessionId, 1, 1, 1, LevelTheme.Dungeon, 24, 16, ["####", "#..#"], state));
        AssertRoundTrip(serializer, new SessionUpdatedMessage(state.SessionId, 2, 2, session));
        AssertRoundTrip(serializer, new GameEventMessage(state.SessionId, 3, 3, GameEventTypes.LifeLost, Guid.NewGuid(), "ouch",
            new Dictionary<string, string> { ["lives"] = "2" }));
        AssertRoundTrip(serializer, new ErrorMessage(state.SessionId, 4, 4, GameErrorCode.WrongPhase, "no"));
        AssertRoundTrip(serializer, new PongMessage(state.SessionId, 5, 5, DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void Xml_IsRealXml()
    {
        var xml = MessageSerializers.Xml.Serialize(TestWorld.SampleState());

        var root = XDocument.Parse(xml).Root!;
        Assert.Equal("TickStateMessage", root.Name.LocalName);
        Assert.Equal("urn:castle-escape", root.Name.NamespaceName);
    }

    [Fact]
    public void Choose_UsesFormatThenAcceptHeader()
    {
        Assert.IsType<XmlMessageSerializerAdapter>(MessageSerializers.Choose("xml"));
        Assert.IsType<XmlMessageSerializerAdapter>(MessageSerializers.Choose(null, "application/xml"));
        Assert.IsType<JsonMessageSerializerAdapter>(MessageSerializers.Choose(null, "*/*"));
        Assert.Throws<ArgumentException>(() => MessageSerializers.Choose("yaml"));
    }

    [Fact]
    public void AdapterAndAdapteeHaveDifferentMethodCounts()
    {
        // Properties plus ordinary methods (a property's get_ method is not counted twice).
        var target = typeof(IMessageSerializer).GetProperties().Length + typeof(IMessageSerializer).GetMethods().Count(m => !m.IsSpecialName);
        var json = typeof(System.Text.Json.JsonSerializer).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Length;
        var xml = typeof(System.Runtime.Serialization.DataContractSerializer).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Length;

        Assert.Equal(3, target);
        Assert.True(json > 3);
        Assert.True(xml > 3);
    }
}
