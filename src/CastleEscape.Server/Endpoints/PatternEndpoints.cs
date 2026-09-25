using CastleEscape.Contracts.Patterns;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Patterns;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CastleEscape.Server.Endpoints;

/// <summary>Course demos: the P1 patterns, their participants (by reflection) and one demo endpoint per pattern.</summary>
public static class PatternEndpoints
{
    public static IEndpointRouteBuilder MapPatternEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/patterns").WithTags(ApiTags.Patterns);

        group.MapGet("", () => Describe())
            .WithName("ListPatterns")
            .WithSummary("All P1 patterns")
            .WithDescription("Name, owner, the problem solved, the course requirement and how it is met, "
                             + "and the participants found in the code by the [DesignPattern] attribute.");

        group.MapGet("/{pattern}", GetPattern)
            .WithName("GetPattern")
            .WithSummary("One pattern")
            .WithDescription("Details and participants (type, role, source file) of one pattern, by key, e.g. `decorator`.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapDemo("singleton",
            (int threads = 64, bool naive = true) => Run("singleton", ("threads", threads), ("naive", naive)),
            "Singleton: thread safety",
            "Starts `threads` threads through a Barrier; all read ContentCatalog.Instance at once. Reports distinct "
            + "instances (1) and creations (1). With `naive=true` the same race against a lock-free holder makes several.");

        app.MapDemo("adapter",
            (string format = "xml") => Run("adapter", ("format", format)),
            "Adapter: JSON / XML serializers",
            "Serializes a real tick state through IMessageSerializer (`format=json|xml`), round-trips it, and counts "
            + "the members of the 3-member target against the adaptees JsonSerializer and DataContractSerializer.");

        app.MapDemo("factory-method",
            (string kind = "all") => Run("factory-method", ("kind", kind)),
            "Factory Method: item spawners",
            "Spawns items on the tutorial level through ItemSpawners.For(consumable) (`kind=health|reward|power|all`), "
            + "applies each to a player, and lists the product family (HealthItem, RewardItem, PowerItem).");

        return app;
    }

    /// <summary>Maps a demo endpoint; the handler turns its typed query parameters into demo options.</summary>
    public static RouteHandlerBuilder MapDemo(this IEndpointRouteBuilder group, string key, Delegate handler, string summary, string description) =>
        group.MapPost($"/api/patterns/{key}/demo", handler)
            .WithTags(ApiTags.Patterns)
            .WithName($"Demo{string.Concat(key.Split('-').Select(p => char.ToUpperInvariant(p[0]) + p[1..]))}")
            .WithSummary(summary)
            .WithDescription(description);

    /// <summary>Runs a demo by key with the given options.</summary>
    public static PatternDemoResponse Run(string key, params (string Name, object? Value)[] options) =>
        (PatternDemoCatalog.Find(key) ?? throw new InvalidOperationException($"No demo '{key}'."))
        .Run(new DemoOptions(options.Where(o => o.Value is not null)
            .ToDictionary(o => o.Name, o => Convert.ToString(o.Value, System.Globalization.CultureInfo.InvariantCulture)!)));

    private static PatternDto[] Describe() =>
        PatternCatalog.Describe(typeof(PatternCatalog).Assembly, typeof(PatternEndpoints).Assembly);

    private static Results<Ok<PatternDto>, ProblemHttpResult> GetPattern(string pattern)
    {
        var found = Describe().FirstOrDefault(p => p.Key.Equals(pattern, StringComparison.OrdinalIgnoreCase));
        return found is null
            ? TypedResults.Problem($"Unknown pattern '{pattern}'. Known: {string.Join(", ", PatternCatalog.Keys)}.",
                statusCode: StatusCodes.Status404NotFound, title: "Pattern not found")
            : TypedResults.Ok(found);
    }
}
