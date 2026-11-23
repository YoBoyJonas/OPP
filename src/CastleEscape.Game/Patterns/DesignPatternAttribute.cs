using System.Runtime.CompilerServices;

namespace CastleEscape.Game.Patterns;

/// <summary>
/// Marks a class as a participant of a design pattern, e.g. <c>[DesignPattern("Decorator", "ConcreteDecorator")]</c>.
/// <c>GET /api/patterns</c> lists participants from these attributes, so the docs never drift from the code.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class DesignPatternAttribute(string pattern, string role, [CallerFilePath] string sourceFile = "") : Attribute
{
    /// <summary>Pattern name as used in <see cref="PatternCatalog"/>, e.g. "Abstract Factory".</summary>
    public string Pattern { get; } = pattern;

    /// <summary>The participant's role in the pattern, e.g. "ConcreteFactory".</summary>
    public string Role { get; } = role;

    /// <summary>Source file path relative to the repository (filled in by the compiler).</summary>
    public string SourceFile { get; } = ToRepoPath(sourceFile);

    private static string ToRepoPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        var index = normalized.IndexOf("/src/", StringComparison.Ordinal);
        return index >= 0 ? normalized[(index + 1)..] : normalized;
    }
}
