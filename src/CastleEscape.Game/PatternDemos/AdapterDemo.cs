using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Messaging;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Adapter requirement: adapter and adaptee have different numbers of methods.</summary>
public sealed class AdapterDemo : IPatternDemo
{
    public string Key => "adapter";

    public PatternDemoResponse Run(DemoOptions options)
    {
        var format = options.Get("format", "xml");
        var serializer = MessageSerializers.Choose(format);
        var trace = new DemoTrace("Adapter");

        var state = DemoWorld.SampleState();
        var text = serializer.Serialize(state);
        var back = serializer.Deserialize<TickStateMessage>(text);
        var sameContent = MessageSerializers.Json.Serialize(back) == MessageSerializers.Json.Serialize(state);

        var counts = MemberCounts();
        trace.Line($"Client code calls IMessageSerializer.Serialize(state) -> {serializer.GetType().Name} ({serializer.ContentType}).")
            .Line($"Output: {text.Length} characters; Deserialize<TickStateMessage> round trip gives the same content: {sameContent}.")
            .Line($"Target IMessageSerializer: {counts["IMessageSerializer (target)"]} members.")
            .Line($"Adaptee System.Text.Json.JsonSerializer: {counts["JsonSerializer (adaptee)"]} public static methods.")
            .Line($"Adaptee DataContractSerializer: {counts["DataContractSerializer (adaptee)"]} public methods.")
            .Evidence("format", format)
            .Evidence("contentType", serializer.ContentType)
            .Evidence("roundTripEqual", sameContent)
            .Evidence("memberCounts", counts)
            .Evidence("output", text.Length > 1500 ? text[..1500] + "…" : text);

        return trace.Done($"{serializer.GetType().Name} hides a {Math.Max(counts["JsonSerializer (adaptee)"], counts["DataContractSerializer (adaptee)"])}-method API "
                          + $"behind {counts["IMessageSerializer (target)"]} members.");
    }

    /// <summary>Public member counts of the target and both adaptees, by reflection.</summary>
    public static Dictionary<string, int> MemberCounts() => new()
    {
        // Properties plus ordinary methods (a property's get_ method is not counted twice).
        ["IMessageSerializer (target)"] = typeof(IMessageSerializer).GetProperties().Length
                                          + typeof(IMessageSerializer).GetMethods().Count(m => !m.IsSpecialName),
        ["JsonSerializer (adaptee)"] = typeof(JsonSerializer).GetMethods(BindingFlags.Public | BindingFlags.Static).Length,
        ["DataContractSerializer (adaptee)"] = typeof(DataContractSerializer).GetMethods(BindingFlags.Public | BindingFlags.Instance).Length,
    };
}
