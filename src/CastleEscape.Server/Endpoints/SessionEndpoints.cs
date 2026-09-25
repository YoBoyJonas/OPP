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
        var group = app.MapGroup("/api/sessions").WithTags(ApiTags.Sessions);

        group.MapPost("", Create)
            .WithName("CreateSession")
            .WithSummary("Create a session")
            .WithDescription("Creates a two-player session with you as player 1. Share the joinCode with player 2. "
                             + "Keep the playerToken: send it as X-Player-Token and as the hub's playerToken.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/join", Join)
            .WithName("JoinSession")
            .WithSummary("Join a session by code")
            .WithDescription("Joins as player 2. The session then waits for both players to pick a character.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("", List)
            .WithName("ListSessions")
            .WithSummary("List sessions")
            .WithDescription("Every session on this server with its phase and player count.");

        group.MapGet("/{sessionId:guid}", Get)
            .WithName("GetSession")
            .WithSummary("Get a session")
            .WithDescription("Phase, players, chosen characters and current level.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{sessionId:guid}/players/me/character", SelectCharacter)
            .WithName("SelectCharacter")
            .WithSummary("Pick your character")
            .WithDescription("PLR-1. Allowed until the game starts. When both players have picked, the first level loads.")
            .RequirePlayerToken()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{sessionId:guid}/players/me", Leave)
            .WithName("LeaveSession")
            .WithSummary("Leave the session")
            .WithDescription("Ends the session for both players (phase Aborted); the other player is notified.")
            .RequirePlayerToken()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static Created<CreateSessionResponse> Create(CreateSessionRequest request, SessionRegistry registry)
    {
        var (session, player) = registry.Create(request.PlayerName);
        return TypedResults.Created($"/api/sessions/{session.Id}",
            new CreateSessionResponse(session.Id, session.JoinCode, player.PlayerId, player.Token));
    }

    private static Ok<JoinSessionResponse> Join(JoinSessionRequest request, SessionRegistry registry)
    {
        var (session, player) = registry.Join(request.JoinCode, request.PlayerName);
        return TypedResults.Ok(new JoinSessionResponse(session.Id, player.PlayerId, player.Token));
    }

    private static Ok<List<SessionSummary>> List(SessionRegistry registry) =>
        TypedResults.Ok(registry.All
            .Select(s => new SessionSummary(s.Id, s.JoinCode, s.Phase, s.Snapshot.Session.Players.Length))
            .ToList());

    private static Ok<SessionDto> Get(Guid sessionId, SessionRegistry registry) =>
        TypedResults.Ok(registry.Get(sessionId).Snapshot.Session);

    private static Ok<SessionDto> SelectCharacter(Guid sessionId, SelectCharacterRequest request, HttpContext http, SessionRegistry registry)
    {
        var (session, player) = registry.Authenticate(sessionId, http.PlayerToken());
        session.SelectCharacter(player.PlayerId, request.CharacterId);
        return TypedResults.Ok(session.Snapshot.Session);
    }

    private static NoContent Leave(Guid sessionId, HttpContext http, SessionRegistry registry)
    {
        var (session, player) = registry.Authenticate(sessionId, http.PlayerToken());
        session.Leave(player.PlayerId);
        return TypedResults.NoContent();
    }

    /// <summary>The X-Player-Token header, read directly so OpenAPI shows it only as the security scheme.</summary>
    public static string? PlayerToken(this HttpContext http) =>
        http.Request.Headers[OpenApiSetup.PlayerTokenHeader].FirstOrDefault();
}
