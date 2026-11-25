using CastleEscape.Contracts.Content;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CastleEscape.Server.Controllers;

/// <summary>Read-only access to the static content catalog.</summary>
[ApiController]
[Route("api/content")]
[Tags(ApiTags.Content)]
public sealed class ContentController(ContentCatalog catalog, IOptions<GameOptions> options) : ControllerBase
{
    [HttpGet(Name = "GetContentSummary")]
    public ActionResult<ContentSummaryResponse> GetSummary()
    {
        return Ok(new ContentSummaryResponse(
            catalog.Data.Characters.Count,
            catalog.Data.Consumables.Count,
            catalog.Data.Combos.Count,
            catalog.Data.Obstacles.Count,
            catalog.Data.Zombies.Count,
            catalog.Data.Levels.Count));
    }

    [HttpGet("characters", Name = "GetCharacters")]
    public ActionResult<IReadOnlyList<Character>> GetCharacters()
    {
        return Ok(catalog.Data.Characters);
    }

    [HttpGet("consumables", Name = "GetConsumables")]
    public ActionResult<IReadOnlyList<Consumable>> GetConsumables()
    {
        return Ok(catalog.Data.Consumables);
    }

    [HttpGet("powers", Name = "GetPowers")]
    public ActionResult<List<PowerSummary>> GetPowers()
    {
        var basePowers = catalog.Data.Consumables
            .Where(c => c.Grant is not null)
            .GroupBy(c => c.Grant!.Power)
            .OrderBy(g => g.Key)
            .Select(g => new PowerSummary(
                g.Key,
                IsSuperPower: false,
                g.First().Grant!.DurationSeconds ?? options.Value.PowerDurationSeconds,
                g.Select(c => c.Id).ToArray(),
                []));

        var superPowers = catalog.Data.Combos.Select(combo => new PowerSummary(
            combo.Granted, IsSuperPower: true, DurationSeconds: null, [], combo.RequiredPowers.ToArray()));

        return Ok(basePowers.Concat(superPowers).ToList());
    }

    [HttpGet("combos", Name = "GetCombos")]
    public ActionResult<IReadOnlyList<PowerCombo>> GetCombos()
    {
        return Ok(catalog.Data.Combos);
    }

    [HttpGet("obstacles", Name = "GetObstacles")]
    public ActionResult<IReadOnlyList<Obstacle>> GetObstacles()
    {
        return Ok(catalog.Data.Obstacles);
    }

    [HttpGet("zombies", Name = "GetZombies")]
    public ActionResult<IReadOnlyList<ZombieDefinition>> GetZombies()
    {
        return Ok(catalog.Data.Zombies);
    }

    [HttpGet("levels", Name = "GetLevelDefinitions")]
    public ActionResult<IReadOnlyList<Level>> GetLevels()
    {
        return Ok(catalog.Data.Levels);
    }

    [HttpGet("levels/{index:int}", Name = "GetLevelDefinition")]
    public ActionResult<Level> GetLevel([FromRoute] int index)
    {
        var level = catalog.Data.Levels.FirstOrDefault(l => l.Index == index);
        if (level is null)
        {
            return Problem($"There is no level {index}; levels run 1..{catalog.Data.Levels.Count}.",
                statusCode: StatusCodes.Status404NotFound, title: "Level not found");
        }
        return Ok(level);
    }
}
