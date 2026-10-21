using CastleEscape.Game.Sessions;

namespace CastleEscape.Server.Endpoints;

public static class DiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => "Healthy").WithTags("Diagnostics");

        app.MapGet("/api/diagnostics/sessions/{sessionId:guid}/commands", (Guid sessionId, GameFacade game) => game.GetCommandHistory(sessionId))
            .WithTags("Diagnostics")
            .WithSummary("Command history of a session")
            .WithDescription("The last 100 commands (inputs, steps, restarts, given powers), oldest first, with the tick each ran "
                             + "in and whether it was undone (Command pattern). An undone StartStep is a D7 tile conflict.");

        return app;
    }
}
