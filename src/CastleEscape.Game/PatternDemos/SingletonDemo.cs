using System.Runtime.CompilerServices;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Game.Content;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Singleton: every read of ContentCatalog.Instance gives the same object, and nobody can construct another.</summary>
public sealed class SingletonDemo : IPatternDemo
{
    public string Key => "singleton";

    public PatternDemoResponse Run(DemoOptions options)
    {
        var first = ContentCatalog.Instance;
        var same = ReferenceEquals(first, ContentCatalog.Instance) && ReferenceEquals(first, DemoWorld.Catalog);
        var publicConstructors = typeof(ContentCatalog).GetConstructors().Length;
        var trace = new DemoTrace("Singleton")
            .Line($"ContentCatalog.Instance read twice and through DemoWorld.Catalog: same object = {same} (identity hash {RuntimeHelpers.GetHashCode(first)}).")
            .Line($"Public constructors on ContentCatalog: {publicConstructors}, so no code can create a second catalog.")
            .Evidence("sameInstance", same)
            .Evidence("identityHash", RuntimeHelpers.GetHashCode(first))
            .Evidence("publicConstructors", publicConstructors);

        return trace.Done(same && publicConstructors == 0
            ? "One shared ContentCatalog: created on first use, and nobody can construct another."
            : "More than one ContentCatalog is reachable.");
    }
}
