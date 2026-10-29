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
            catalog.ComputeContentHash(),
            catalog.Characters.Count,
            catalog.Consumables.Count,
            catalog.Combos.Count,
            catalog.Obstacles.Count,
            catalog.Zombies.Count,
            catalog.Levels.Count));
    }

    [HttpGet("characters", Name = "GetCharacters")]
    public ActionResult<IReadOnlyList<CharacterDefinition>> GetCharacters()
    {
        return Ok(catalog.Characters);
    }

    [HttpGet("consumables", Name = "GetConsumables")]
    public ActionResult<IReadOnlyList<Consumable>> GetConsumables()
    {
        return Ok(catalog.Consumables);
    }

    [HttpGet("powers", Name = "GetPowers")]
    public ActionResult<List<PowerSummary>> GetPowers()
    {
        var basePowers = catalog.Consumables
            .Where(c => c.Grant is not null)
            .GroupBy(c => c.Grant!.Power)
            .OrderBy(g => g.Key)
            .Select(g => new PowerSummary(
                g.Key,
                IsSuperPower: false,
                g.First().Grant!.DurationSeconds ?? options.Value.PowerDurationSeconds,
                g.Select(c => c.Id).ToArray(),
                []));

        var superPowers = catalog.Combos.Select(combo => new PowerSummary(
            combo.Granted, IsSuperPower: true, DurationSeconds: null, [], combo.RequiredPowers.ToArray()));

        return Ok(basePowers.Concat(superPowers).ToList());
    }

    [HttpGet("combos", Name = "GetCombos")]
    public ActionResult<IReadOnlyList<PowerCombo>> GetCombos()
    {
        return Ok(catalog.Combos);
    }

    [HttpGet("obstacles", Name = "GetObstacles")]
    public ActionResult<IReadOnlyList<Obstacle>> GetObstacles()
    {
        return Ok(catalog.Obstacles);
    }

    [HttpGet("zombies", Name = "GetZombies")]
    public ActionResult<IReadOnlyList<ZombieDefinition>> GetZombies()
    {
        return Ok(catalog.Zombies);
    }

    [HttpGet("levels", Name = "GetLevelDefinitions")]
    public ActionResult<IReadOnlyList<LevelDefinition>> GetLevels()
    {
        return Ok(catalog.Levels);
    }

    [HttpGet("levels/{index:int}", Name = "GetLevelDefinition")]
    public ActionResult<LevelDefinition> GetLevel([FromRoute] int index)
    {
        var level = catalog.Levels.FirstOrDefault(l => l.Index == index);
        if (level is null)
        {
            return Problem($"There is no level {index}; levels run 1..{catalog.Levels.Count}.",
                statusCode: StatusCodes.Status404NotFound, title: "Level not found");
        }
        return Ok(level);
    }
}
