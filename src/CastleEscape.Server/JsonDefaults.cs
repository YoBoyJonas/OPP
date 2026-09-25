using System.Text.Json;
using System.Text.Json.Serialization;

namespace CastleEscape.Server;

/// <summary>
/// One JSON convention for REST and SignalR: camelCase properties, enums as strings.
/// Dictionary keys are sent as-is, so HUD statistics keys match event type names (e.g. "ItemCollected").
/// </summary>
public static class JsonDefaults
{
    public static void Apply(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter());
    }
}
