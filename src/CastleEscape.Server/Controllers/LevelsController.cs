using CastleEscape.Contracts.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Mvc;

namespace CastleEscape.Server.Controllers;

[ApiController]
[Route("api/levels")]
[Tags(ApiTags.Levels)]
public sealed class LevelsController(LevelPreviewer levels) : ControllerBase
{
    [HttpGet("presets", Name = "GetPresetNames")]
    public ActionResult<IReadOnlyList<string>> GetPresets()
    {
        var presets = levels.Presets();
        return Ok(presets);
    }

    [HttpGet("{index:int}/preview", Name = "PreviewLevel")]
    public ActionResult<LevelPreviewResponse> Preview([FromRoute] int index, [FromQuery] int? seed, [FromQuery] string? preset)
    {
        var preview = levels.Preview(index, seed ?? 1, preset);
        return Ok(preview);
    }
}
