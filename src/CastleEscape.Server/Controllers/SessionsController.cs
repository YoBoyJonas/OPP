using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Mvc;

namespace CastleEscape.Server.Controllers;

/// <summary>Lobby: create, join, pick a character, leave (join-session activity diagram, PLR-1, NET-3).</summary>
[ApiController]
[Route("api/sessions")]
[Tags(ApiTags.Sessions)]
public sealed class SessionsController(GameFacade game) : ControllerBase
{
    [HttpPost(Name = "CreateSession")]
    public ActionResult<CreateSessionResponse> Create([FromBody] CreateSessionRequest request)
    {
        var created = game.CreateSession(request.PlayerName);
        return Created($"/api/sessions/{created.SessionId}", created);
    }

    [HttpPost("join", Name = "JoinSession")]
    public ActionResult<JoinSessionResponse> Join([FromBody] JoinSessionRequest request)
    {
        var joined = game.JoinSession(request.JoinCode, request.PlayerName);
        return Ok(joined);
    }

    [HttpGet(Name = "ListSessions")]
    public ActionResult<IReadOnlyList<SessionSummary>> List()
    {
        var sessions = game.ListSessions();
        return Ok(sessions);
    }

    [HttpGet("{sessionId:guid}", Name = "GetSession")]
    public ActionResult<SessionDto> Get([FromRoute] Guid sessionId)
    {
        var session = game.GetSession(sessionId);
        return Ok(session);
    }

    [HttpPut("{sessionId:guid}/players/me/character", Name = "SelectCharacter")]
    public ActionResult<SessionDto> SelectCharacter([FromRoute] Guid sessionId, [FromBody] SelectCharacterRequest request)
    {
        var session = game.SelectCharacter(sessionId, Request.PlayerToken(), request.CharacterId);
        return Ok(session);
    }

    [HttpDelete("{sessionId:guid}/players/me", Name = "LeaveSession")]
    public IActionResult Leave([FromRoute] Guid sessionId)
    {
        game.LeaveSession(sessionId, Request.PlayerToken());
        return NoContent();
    }
}
