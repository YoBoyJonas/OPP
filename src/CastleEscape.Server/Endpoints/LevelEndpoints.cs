using CastleEscape.Contracts.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CastleEscape.Server.Endpoints;

/// <summary>Look at levels without playing them: generated ones by seed, and the preset maps.</summary>
public static class LevelEndpoints
{
    public static IEndpointRouteBuilder MapLevelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/levels").WithTags(ApiTags.Levels);

        group.MapGet("/presets", (LevelPreviewer levels) => TypedResults.Ok(levels.Presets()))
            .WithName("GetPresetNames")
            .WithSummary("Preset map names")
            .WithDescription("Hand-made maps in content/presets. Use one with `preset=` on the preview, or for every level "
                             + "with `Generation:PresetLevel`.");

        group.MapGet("/{index:int}/preview", Preview)
            .WithName("PreviewLevel")
            .WithSummary("Build and show a level")
            .WithDescription("Builds level `index` (1-10) with the Builder pattern: the procedural builder for `seed` "
                             + "(validated and retried like in a game), or the preset builder for `preset`. Returns the map rows, "
                             + "the themed obstacles and zombies (Abstract Factory, Strategy) and the validation report. Same seed, same level.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static Ok<LevelPreviewResponse> Preview(int index, int? seed, string? preset, LevelPreviewer levels) =>
        TypedResults.Ok(levels.Preview(index, seed ?? 1, preset));
}
