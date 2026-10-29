using CastleEscape.Contracts.Dev;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Mvc;

namespace CastleEscape.Server.Controllers;

/// <summary>
/// Developer and defence shortcuts under <c>/api/dev</c>. Registered only when <c>DevTools:Enabled</c> is true
/// (Development by default), so they don't exist at all on a public deployment (see Program.cs).
/// </summary>
[ApiController]
[Route("api/dev")]
[Tags(ApiTags.Dev)]
public sealed class DevController(DevActions dev) : ControllerBase
{
    [HttpPost("quickstart", Name = "DevQuickstart")]
    public ActionResult<DevQuickstartResponse> Quickstart([FromBody] DevQuickstartRequest? request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var started = dev.Quickstart(request ?? new DevQuickstartRequest(),
            (sessionId, token) => $"{baseUrl}{RealtimeController.HubPath}?sessionId={sessionId}&playerToken={token}");
        return Ok(started);
    }

    [HttpPost("sessions/{sessionId:guid}/undo", Name = "DevUndoLastCommand")]
    public ActionResult<UndoResponse> UndoLastCommand([FromRoute] Guid sessionId, [FromQuery] Guid? playerId)
    {
        var undone = dev.UndoLastCommand(sessionId, playerId);
        return Ok(undone);
    }

    [HttpPut("sessions/{sessionId:guid}/zombies/strategy", Name = "DevSetZombieStrategy")]
    public IActionResult SetZombieStrategy([FromRoute] Guid sessionId, [FromBody] ZombieStrategyRequest request)
    {
        dev.SetZombieStrategy(sessionId, request.Strategy);
        return Accepted();
    }

    [HttpPost("sessions/{sessionId:guid}/players/{playerId:guid}/powers", Name = "DevGivePower")]
    public IActionResult GivePower([FromRoute] Guid sessionId, [FromRoute] Guid playerId, [FromBody] GivePowerRequest request)
    {
        dev.GivePower(sessionId, playerId, request.Power, request.DurationSeconds);
        return Accepted();
    }

    [HttpPost("sessions/{sessionId:guid}/skip-level", Name = "DevSkipLevel")]
    public IActionResult SkipLevel([FromRoute] Guid sessionId)
    {
        dev.SkipLevel(sessionId);
        return NoContent();
    }

    [HttpGet("patterns/prototype-clone-mode", Name = "DevGetCloneMode")]
    public ActionResult<CloneModeDto> GetCloneMode()
    {
        return Ok(dev.GetCloneMode());
    }

    [HttpPut("patterns/prototype-clone-mode", Name = "DevSetCloneMode")]
    public ActionResult<CloneModeDto> SetCloneMode([FromBody] CloneModeDto request)
    {
        return Ok(dev.SetCloneMode(request.Mode));
    }
}
