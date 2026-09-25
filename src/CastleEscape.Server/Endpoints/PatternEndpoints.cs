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

        app.MapDemo("abstract-factory",
            (string theme = "both", int seed = 7) => Run("abstract-factory", ("theme", theme), ("seed", seed)),
            "Abstract Factory: level themes",
            "Creates the product family of each theme factory (`theme=dungeon|crypt|both`): wall, water, pit and zombie with "
            + "their gameplay values. Then generates a real level of that theme and checks every placed part comes from the same family.");

        app.MapDemo("builder",
            (int level = 1, int seed = 7, string preset = "tutorial") => Run("builder", ("level", level), ("seed", seed), ("preset", preset)),
            "Builder: level construction",
            "LevelDirector builds level `level` with ProceduralLevelBuilder (random, `seed`) and PresetLevelBuilder "
            + "(the `preset` map: tutorial or arena), using the same step order. Returns both results as map rows.");

        app.MapDemo("prototype",
            (string mode = "both") => Run("prototype", ("mode", mode)),
            "Prototype: deep vs shallow level copies",
            "Clones the tutorial level (`mode=Deep|Shallow|both`) and reports, for the level and its parts, the memory "
            + "address and identity hash in the original and the clone. Then plays on the clone and restarts from the "
            + "pristine level: deep restores it, shallow does not. The game itself uses `Patterns:PrototypeCloneMode`.");

        app.MapDemo("strategy",
            (string strategy = "all", int steps = 12) => Run("strategy", ("strategy", strategy), ("steps", steps)),
            "Strategy: zombie chase algorithms",
            "Puts the same zombie behind a wall and swaps its strategy (`strategy=Greedy|Bfs|AStar|Predictive|all`), "
            + "running up to `steps` steps with each. Greedy gets stuck, BFS and A* go around, Predictive aims where the "
            + "player is heading. Also compares BFS and A* search cost.");

        app.MapDemo("decorator",
            (string character = "scout", int sprints = 2) => Run("decorator", ("character", character), ("sprints", sprints)),
            "Decorator: power stacking",
            "Gives a character (`character=warrior|scout|swimmer`) `sprints` Sprint pickups, then Jump and Swim. After each "
            + "pickup it prints the decorator chain, its depth and the floor/water/pit speeds. The combos JumpDash and FastSwim "
            + "are added as decorators too. Then all timers expire and the chain unwinds.");

        app.MapDemo("command",
            (bool undoRestart = true) => Run("command", ("undoRestart", undoRestart)),
            "Command: inputs with undo",
            "Plays a scripted two-player session. Both players step into the same tile, and the later StartStep command is "
            + "undone (D7). One player collects a coin, the other restarts the level, and UndoLastCommand() (`undoRestart`) "
            + "puts the pre-restart level, score and positions back. Returns the command history.");

        app.MapDemo("observer",
            () => Run("observer"),
            "Observer: game events",
            "Plays a scripted session with an extra Recorder observer attached: a coin pickup and a zombie hit. Shows what "
            + "each observer did (client messages, HUD statistics, event log, sound cues), then detaches the Recorder and "
            + "shows it no longer receives events.");

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
