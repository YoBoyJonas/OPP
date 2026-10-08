namespace CastleEscape.Contracts.Realtime;

/// <summary>How to talk to the game in real time (<c>GET /api/realtime/protocol</c>).</summary>
/// <param name="HubPath">SignalR hub path, e.g. /hubs/game.</param>
/// <param name="QueryParameters">Query parameters the hub URL needs.</param>
/// <param name="ServerToClient">Methods the server calls on the client.</param>
/// <param name="ClientToServer">Methods the client calls on the hub.</param>
/// <param name="EventTypes">Every <c>GameEvent.type</c> value.</param>
/// <param name="ErrorCodes">Every <c>Error.code</c> value (also the <c>code</c> of REST problem details).</param>
/// <param name="Rules">Ordering, catch-up and polling rules clients rely on.</param>
public sealed record RealtimeProtocolResponse(
    string HubPath,
    ProtocolParameterDto[] QueryParameters,
    ProtocolMethodDto[] ServerToClient,
    ProtocolMethodDto[] ClientToServer,
    string[] EventTypes,
    string[] ErrorCodes,
    string[] Rules);

public sealed record ProtocolParameterDto(string Name, string Description);

/// <summary>One hub method. <c>Payload</c> is the message type (server to client) or the argument list (client to server).</summary>
public sealed record ProtocolMethodDto(string Name, string Payload, string When);
