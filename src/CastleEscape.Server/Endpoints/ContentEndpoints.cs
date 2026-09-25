using CastleEscape.Contracts.Content;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace CastleEscape.Server.Endpoints;

/// <summary>Read-only access to the static content catalog.</summary>
public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/content").WithTags(ApiTags.Content);

        group.MapGet("", GetSummary)
            .WithName("GetContentSummary")
            .WithSummary("Content summary")
            .WithDescription("Counts of every content type and a hash that changes whenever a content file changes.");

        group.MapGet("/characters", (ContentCatalog c) => c.Characters)
            .WithName("GetCharacters")
            .WithSummary("Playable characters")
            .WithDescription("At least 3 characters that differ in lives, move speed and jump parameter (PLR-1).");

        group.MapGet("/consumables", (ContentCatalog c) => c.Consumables)
            .WithName("GetConsumables")
            .WithSummary("Item types")
            .WithDescription("Health (+lives), Reward (+score) and Power (temporary power) items (ITM-1). "
                             + "The `type` field tells the kind; Power items carry a `grant`.");

        group.MapGet("/powers", GetPowers)
            .WithName("GetPowers")
            .WithSummary("Base powers and super powers")
            .WithDescription("Jump, Sprint and Swim with their duration and the items that grant them; "
                             + "JumpDash and FastSwim with the base powers they need (PWR-1, PWR-2).");

        group.MapGet("/combos", (ContentCatalog c) => c.Combos)
            .WithName("GetCombos")
            .WithSummary("Super-power combos")
            .WithDescription("Two different active base powers create a super power (PWR-2).");

        group.MapGet("/obstacles", (ContentCatalog c) => c.Obstacles)
            .WithName("GetObstacles")
            .WithSummary("Obstacle types")
            .WithDescription("Wall (never passable), Water (needs Swim), Pit (needs Jump) (MOV-2).");

        group.MapGet("/zombies", (ContentCatalog c) => c.Zombies)
            .WithName("GetZombies")
            .WithSummary("Zombie types")
            .WithDescription("Speed, contact damage and the default movement strategy of each zombie type.");

        group.MapGet("/levels", (ContentCatalog c) => c.Levels)
            .WithName("GetLevelDefinitions")
            .WithSummary("Level definitions")
            .WithDescription("The fixed sequence of 10 levels: theme, obstacle/zombie/item counts and spawn tables.");

        group.MapGet("/levels/{index:int}", GetLevel)
            .WithName("GetLevelDefinition")
            .WithSummary("One level definition")
            .WithDescription("The level at a 1-based position in the sequence.");

        return app;
    }

    private static ContentSummaryResponse GetSummary(ContentCatalog catalog) => new(
        catalog.ComputeContentHash(),
        catalog.Characters.Count,
        catalog.Consumables.Count,
        catalog.Combos.Count,
        catalog.Obstacles.Count,
        catalog.Zombies.Count,
        catalog.Levels.Count);

    private static List<PowerSummary> GetPowers(ContentCatalog catalog, IOptions<GameOptions> options)
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

        return basePowers.Concat(superPowers).ToList();
    }

    private static Results<Ok<LevelDefinition>, ProblemHttpResult> GetLevel(int index, ContentCatalog catalog)
    {
        var level = catalog.Levels.FirstOrDefault(l => l.Index == index);
        return level is null
            ? TypedResults.Problem($"There is no level {index}; levels run 1..{catalog.Levels.Count}.",
                statusCode: StatusCodes.Status404NotFound, title: "Level not found")
            : TypedResults.Ok(level);
    }
}
