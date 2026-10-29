using CastleEscape.Contracts;
using CastleEscape.Contracts.Gameplay;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Messaging;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CastleEscape.Server.Controllers;

/// <summary>
/// REST mirror of the hub, so everything can be tried from Scalar: send input, restart, and read
/// the state, layout and HUD. Reads use the session's latest snapshot and never lock the world.
/// </summary>
[ApiController]
[Route("api/sessions/{sessionId:guid}")]
[Tags(ApiTags.Gameplay)]
public sealed class GameplayController(GameFacade game, PollingBufferChannel polling, IOptions<RealtimeOptions> realtime)
    : ControllerBase
{
    [HttpPost("input", Name = "SubmitDirection")]
    public IActionResult SubmitInput([FromRoute] Guid sessionId, [FromBody] DirectionRequest request)
    {
        game.SubmitDirection(sessionId, Request.PlayerToken(), request.Direction);
        return Accepted();
    }

    [HttpPost("restart", Name = "RestartLevel")]
    public IActionResult Restart([FromRoute] Guid sessionId)
    {
        game.RequestRestart(sessionId, Request.PlayerToken());
        return Accepted();
    }

    // JSON or XML through the Adapter (IMessageSerializer): ?format= wins over the Accept header.
    [HttpGet("state", Name = "GetGameState")]
    public IActionResult GetState([FromRoute] Guid sessionId, [FromQuery] string? format)
    {
        var state = game.GetState(sessionId);
        var serializer = ChooseSerializer(format);
        return Content(serializer.Serialize(state), serializer.ContentType, System.Text.Encoding.UTF8);
    }

    [HttpGet("messages", Name = "PollMessages")]
    public ActionResult<PolledMessagesResponse> GetMessages([FromRoute] Guid sessionId, [FromQuery] long? afterSeq,
        [FromQuery] string? format)
    {
        game.GetSession(sessionId); // 404 for an unknown session
        if (!realtime.Value.EnabledChannels.Contains(RealtimeChannel.Polling))
        {
            throw new GameException(GameErrorCode.InvalidRequest, "The polling channel is disabled (Realtime:EnabledChannels).");
        }
        var serializer = ChooseSerializer(format);
        var after = afterSeq ?? 0;
        var messages = polling.Read(sessionId, after)
            .Select(m => new PolledMessageDto(m.Seq, m.Method, serializer.Serialize(m.Message)))
            .ToArray();
        return Ok(new PolledMessagesResponse(sessionId, serializer.ContentType,
            messages.Length > 0 ? messages[^1].Seq : after, messages));
    }

    [HttpGet("level", Name = "GetLevelLayout")]
    public ActionResult<LevelLayoutResponse> GetLevel([FromRoute] Guid sessionId)
    {
        var layout = game.GetLevelLayout(sessionId);
        return Ok(layout);
    }

    [HttpGet("hud", Name = "GetHud")]
    public ActionResult<HudResponse> GetHud([FromRoute] Guid sessionId)
    {
        var hud = game.GetHud(sessionId);
        return Ok(hud);
    }

    private IMessageSerializer ChooseSerializer(string? format)
    {
        try
        {
            return MessageSerializers.Choose(format, Request.Headers.Accept);
        }
        catch (ArgumentException ex)
        {
            throw new GameException(GameErrorCode.InvalidRequest, ex.Message);
        }
    }
}
