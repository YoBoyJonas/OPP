using CastleEscape.Application.Content;
using CastleEscape.Contracts.Content;
using CastleEscape.Application.Configuration;
using CastleEscape.Domain.Configuration;
using CastleEscape.Domain.Content;
using CastleEscape.Infrastructure.Content;
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

        group.MapGet("", (ContentQueries c) => c.Summary())
            .WithName("GetContentSummary")
            .WithSummary("Content summary")
            .WithDescription("Counts of every content type and a hash that changes whenever a content file changes.");

        group.MapGet("/characters", (ContentQueries c) => c.Characters())
            .WithName("GetCharacters")
            .WithSummary("Playable characters")
            .WithDescription("At least 3 characters that differ in lives, move speed and jump parameter (PLR-1).");

        group.MapGet("/consumables", (ContentQueries c) => c.Consumables())
            .WithName("GetConsumables")
            .WithSummary("Item types")
            .WithDescription("Health (+lives), Reward (+score) and Power (temporary power) items (ITM-1). "
                             + "The `type` field tells the kind; Power items carry a `grant`.");

        group.MapGet("/powers", (ContentQueries c) => c.Powers())
            .WithName("GetPowers")
            .WithSummary("Base powers and super powers")
            .WithDescription("Jump, Sprint and Swim with their duration and the items that grant them; "
                             + "JumpDash and FastSwim with the base powers they need (PWR-1, PWR-2).");

        group.MapGet("/combos", (ContentQueries c) => c.Combos())
            .WithName("GetCombos")
            .WithSummary("Super-power combos")
            .WithDescription("Two different active base powers create a super power (PWR-2).");

        group.MapGet("/obstacles", (ContentQueries c) => c.Obstacles())
            .WithName("GetObstacles")
            .WithSummary("Obstacle types")
            .WithDescription("Wall (never passable), Water (needs Swim), Pit (needs Jump) (MOV-2).");

        group.MapGet("/zombies", (ContentQueries c) => c.Zombies())
            .WithName("GetZombies")
            .WithSummary("Zombie types")
            .WithDescription("Speed, contact damage and the default movement strategy of each zombie type.");

        group.MapGet("/levels", (ContentQueries c) => c.Levels())
            .WithName("GetLevelDefinitions")
            .WithSummary("Level definitions")
            .WithDescription("The fixed sequence of 10 levels: theme, obstacle/zombie/item counts and spawn tables.");

        group.MapGet("/levels/{index:int}", GetLevel)
            .WithName("GetLevelDefinition")
            .WithSummary("One level definition")
            .WithDescription("The level at a 1-based position in the sequence.");

        return app;
    }

    private static Results<Ok<LevelDefinitionDto>, ProblemHttpResult> GetLevel(int index, ContentQueries catalog)
    {
        var level = catalog.FindLevel(index);
        return level is null
            ? TypedResults.Problem($"There is no level {index}; levels run 1..{catalog.Levels().Length}.",
                statusCode: StatusCodes.Status404NotFound, title: "Level not found")
            : TypedResults.Ok(level);
    }
}
