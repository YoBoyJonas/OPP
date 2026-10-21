using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CastleEscape.Server.Endpoints;

/// <summary>Lobby: create, join, pick a character, leave (join-session activity diagram, PLR-1, NET-3).</summary>
public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions").WithTags("Sessions");

        group.MapPost("", Create);

        group.MapPost("/join", Join);

        group.MapGet("", List);

        group.MapGet("/{sessionId:guid}", Get);

        group.MapPut("/{sessionId:guid}/players/me/character", SelectCharacter)
            .RequirePlayerToken();

        group.MapDelete("/{sessionId:guid}/players/me", Leave)
            .RequirePlayerToken();

        return app;
    }

    private static Created<CreateSessionResponse> Create(CreateSessionRequest request, GameFacade game)
    {
        var created = game.CreateSession(request.PlayerName);
        return TypedResults.Created($"/api/sessions/{created.SessionId}", created);
    }

    private static Ok<JoinSessionResponse> Join(JoinSessionRequest request, GameFacade game) =>
        TypedResults.Ok(game.JoinSession(request.JoinCode, request.PlayerName));

    private static Ok<IReadOnlyList<SessionSummary>> List(GameFacade game) => TypedResults.Ok(game.ListSessions());

    private static Ok<SessionDto> Get(Guid sessionId, GameFacade game) => TypedResults.Ok(game.GetSession(sessionId));

    private static Ok<SessionDto> SelectCharacter(Guid sessionId, SelectCharacterRequest request, HttpContext http, GameFacade game) =>
        TypedResults.Ok(game.SelectCharacter(sessionId, http.PlayerToken(), request.CharacterId));

    private static NoContent Leave(Guid sessionId, HttpContext http, GameFacade game)
    {
        game.LeaveSession(sessionId, http.PlayerToken());
        return TypedResults.NoContent();
    }

    /// <summary>The X-Player-Token header, read directly so OpenAPI shows it only as the security scheme.</summary>
    public static string? PlayerToken(this HttpContext http) =>
        http.Request.Headers[OpenApiSetup.PlayerTokenHeader].FirstOrDefault();
}
