using System.Globalization;
using CastleEscape.Contracts.Patterns;

namespace CastleEscape.Game.PatternDemos;

/// <summary>
/// A runnable demonstration of one pattern using the real game classes. Shared by the console app
/// (<c>CastleEscape.PatternDemos</c>) and the <c>/api/patterns/{key}/demo</c> endpoints, so both show the same output.
/// </summary>
public interface IPatternDemo
{
    /// <summary>Same key as in <see cref="Patterns.PatternCatalog"/>, e.g. "prototype".</summary>
    string Key { get; }

    /// <summary>Runs the demo. <paramref name="options"/> are named parameters, e.g. mode=Shallow.</summary>
    PatternDemoResponse Run(DemoOptions options);
}

/// <summary>Named demo parameters (query string or <c>--name value</c> on the console).</summary>
public sealed class DemoOptions(IReadOnlyDictionary<string, string> values)
{
    public static readonly DemoOptions None = new(new Dictionary<string, string>());

    public string Get(string name, string fallback) =>
        values.FirstOrDefault(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)).Value is { Length: > 0 } v ? v : fallback;

    public int GetInt(string name, int fallback) =>
        int.TryParse(Get(name, ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    public bool GetBool(string name, bool fallback) => bool.TryParse(Get(name, ""), out var v) ? v : fallback;

    public TEnum GetEnum<TEnum>(string name, TEnum fallback) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(Get(name, ""), ignoreCase: true, out var v) ? v : fallback;

    /// <summary>Parses console arguments of the form <c>--name value</c>.</summary>
    public static DemoOptions FromArgs(IEnumerable<string> args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? pending = null;
        foreach (var arg in args)
        {
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                pending = arg[2..];
                values[pending] = "true";
            }
            else if (pending is not null)
            {
                values[pending] = arg;
                pending = null;
            }
        }
        return new DemoOptions(values);
    }
}

/// <summary>Collects the trace lines and evidence of a demo run.</summary>
public sealed class DemoTrace(string pattern)
{
    private readonly List<string> _lines = [];
    private readonly Dictionary<string, object?> _evidence = [];

    public DemoTrace Line(string text)
    {
        _lines.Add(text);
        return this;
    }

    public DemoTrace Evidence(string key, object? value)
    {
        _evidence[key] = value;
        return this;
    }

    public PatternDemoResponse Done(string summary) => new(pattern, summary, _lines.ToArray(), _evidence);
}

/// <summary>All demos, in the order the console runs them. Add a new demo here.</summary>
public static class PatternDemoCatalog
{
    public static IReadOnlyList<IPatternDemo> All { get; } =
    [
        new SingletonDemo(),
        new AdapterDemo(),
        new FactoryMethodDemo(),
        new AbstractFactoryDemo(),
        new BuilderDemo(),
        new PrototypeDemo(),
        new StrategyDemo(),
        new DecoratorDemo(),
        new CommandDemo(),
        new ObserverDemo(),
        new BridgeDemo(),
    ];

    public static IPatternDemo? Find(string key) => All.FirstOrDefault(d => d.Key == key);
}
