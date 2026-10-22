using CastleEscape.Game.Content;

namespace CastleEscape.Server.Endpoints;

/// <summary>The playable characters, for the lobby's character picker.</summary>
public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/content/characters", () => ContentCatalog.Instance.Data.Characters).WithTags("Content");
        return app;
    }
}
