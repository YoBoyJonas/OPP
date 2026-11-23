using System.Globalization;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Patterns;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Mvc;

namespace CastleEscape.Server.Controllers;

/// <summary>Course demos: the P1 patterns, their participants (by reflection) and one demo endpoint per pattern.</summary>
[ApiController]
[Route("api/patterns")]
[Tags(ApiTags.Patterns)]
public sealed class PatternsController : ControllerBase
{
    [HttpGet(Name = "ListPatterns")]
    public ActionResult<PatternDto[]> List()
    {
        return Ok(Describe());
    }

    [HttpGet("{pattern}", Name = "GetPattern")]
    public ActionResult<PatternDto> GetPattern([FromRoute] string pattern)
    {
        var found = Describe().FirstOrDefault(p => p.Key.Equals(pattern, StringComparison.OrdinalIgnoreCase));
        if (found is null)
        {
            return Problem($"Unknown pattern '{pattern}'. Known: {string.Join(", ", PatternCatalog.Keys)}.",
                statusCode: StatusCodes.Status404NotFound, title: "Pattern not found");
        }
        return Ok(found);
    }

    [HttpPost("singleton/demo", Name = "DemoSingleton")]
    public ActionResult<PatternDemoResponse> DemoSingleton([FromQuery] int threads = 64, [FromQuery] bool naive = true)
    {
        return Ok(Run("singleton", ("threads", threads), ("naive", naive)));
    }

    [HttpPost("adapter/demo", Name = "DemoAdapter")]
    public ActionResult<PatternDemoResponse> DemoAdapter([FromQuery] string format = "xml")
    {
        return Ok(Run("adapter", ("format", format)));
    }

    [HttpPost("factory-method/demo", Name = "DemoFactoryMethod")]
    public ActionResult<PatternDemoResponse> DemoFactoryMethod([FromQuery] string kind = "all")
    {
        return Ok(Run("factory-method", ("kind", kind)));
    }

    [HttpPost("prototype/demo", Name = "DemoPrototype")]
    public ActionResult<PatternDemoResponse> DemoPrototype([FromQuery] string mode = "both")
    {
        return Ok(Run("prototype", ("mode", mode)));
    }

    [HttpPost("strategy/demo", Name = "DemoStrategy")]
    public ActionResult<PatternDemoResponse> DemoStrategy([FromQuery] string strategy = "all", [FromQuery] int steps = 12)
    {
        return Ok(Run("strategy", ("strategy", strategy), ("steps", steps)));
    }

    [HttpPost("decorator/demo", Name = "DemoDecorator")]
    public ActionResult<PatternDemoResponse> DemoDecorator([FromQuery] string character = "scout", [FromQuery] int sprints = 2)
    {
        return Ok(Run("decorator", ("character", character), ("sprints", sprints)));
    }

    [HttpPost("command/demo", Name = "DemoCommand")]
    public ActionResult<PatternDemoResponse> DemoCommand([FromQuery] bool undoRestart = true)
    {
        return Ok(Run("command", ("undoRestart", undoRestart)));
    }

    [HttpPost("observer/demo", Name = "DemoObserver")]
    public ActionResult<PatternDemoResponse> DemoObserver()
    {
        return Ok(Run("observer"));
    }

    [HttpPost("bridge/demo", Name = "DemoBridge")]
    public ActionResult<PatternDemoResponse> DemoBridge([FromQuery] int ticks = 40, [FromQuery] int pollingEvery = 5,
        [FromQuery] string format = "xml")
    {
        return Ok(Run("bridge", ("ticks", ticks), ("pollingEvery", pollingEvery), ("format", format)));
    }

    [HttpPost("facade/demo", Name = "DemoFacade")]
    public ActionResult<PatternDemoResponse> DemoFacade()
    {
        return Ok(Run("facade"));
    }

    /// <summary>Runs a demo by key with the given options.</summary>
    private static PatternDemoResponse Run(string key, params (string Name, object? Value)[] options)
    {
        var demo = PatternDemoCatalog.Find(key) ?? throw new InvalidOperationException($"No demo '{key}'.");
        var demoOptions = options
            .Where(o => o.Value is not null)
            .ToDictionary(o => o.Name, o => Convert.ToString(o.Value, CultureInfo.InvariantCulture)!);
        return demo.Run(new DemoOptions(demoOptions));
    }

    private static PatternDto[] Describe()
    {
        return PatternCatalog.Describe(typeof(PatternCatalog).Assembly, typeof(PatternsController).Assembly);
    }
}
