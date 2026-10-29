using CastleEscape.Server.OpenApi;

namespace CastleEscape.Server.Controllers;

public static class PlayerTokenExtensions
{
    /// <summary>The X-Player-Token header, read directly so OpenAPI shows it only as the security scheme.</summary>
    public static string? PlayerToken(this HttpRequest request) =>
        request.Headers[OpenApiSetup.PlayerTokenHeader].FirstOrDefault();
}
