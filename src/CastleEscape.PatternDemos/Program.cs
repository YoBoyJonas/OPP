using CastleEscape.Game.PatternDemos;

// Runs every design pattern demo, or one by key with options:
//   dotnet run --project src/CastleEscape.PatternDemos
//   dotnet run --project src/CastleEscape.PatternDemos -- prototype --mode Shallow
var demos = PatternDemoCatalog.All;
var key = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
var options = DemoOptions.FromArgs(args.Skip(key is null ? 0 : 1));

if (key is not null)
{
    demos = demos.Where(d => d.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
    if (demos.Count == 0)
    {
        Console.WriteLine($"Unknown demo '{key}'. Available: {string.Join(", ", PatternDemoCatalog.All.Select(d => d.Key))}");
        return 1;
    }
}

Console.WriteLine($"Castle Escape - design pattern demos ({demos.Count})");
foreach (var demo in demos)
{
    var result = demo.Run(options);
    Console.WriteLine();
    Console.WriteLine($"=== {result.Pattern} ===");
    foreach (var line in result.Trace)
    {
        Console.WriteLine($"  {line}");
    }
    foreach (var (name, value) in result.Evidence)
    {
        Console.WriteLine($"  [{name}] {Format(value)}");
    }
    Console.WriteLine($"  => {result.Summary}");
}
return 0;

static string Format(object? value) => value switch
{
    null => "null",
    string s => s,
    System.Collections.IEnumerable items => string.Join(", ", items.Cast<object?>().Select(Format)),
    _ => value.ToString() ?? "",
};
