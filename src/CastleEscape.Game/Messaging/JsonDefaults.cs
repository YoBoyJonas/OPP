using System.Text.Json;
using System.Text.Json.Serialization;

namespace CastleEscape.Game.Messaging;

/// <summary>
/// One JSON convention for REST, SignalR and the JSON message adapter: camelCase properties, enums as strings.
/// Dictionary keys are sent as-is, so HUD statistics keys match event type names (e.g. "ItemCollected").
/// </summary>
public static class JsonDefaults
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Apply(options);
        return options;
    }

    public static void Apply(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter());
    }
}
