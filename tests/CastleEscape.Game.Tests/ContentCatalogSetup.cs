using System.Runtime.CompilerServices;
using CastleEscape.Game.Content;

namespace CastleEscape.Game.Tests;

/// <summary>
/// ContentCatalog.Instance is not thread-safe, and xUnit runs test classes in parallel. Like the server's
/// Program.cs, read it once on one thread before any test runs, so every test sees the same catalog.
/// </summary>
internal static class ContentCatalogSetup
{
    [ModuleInitializer]
    internal static void CreateCatalogOnce() => _ = ContentCatalog.Instance;
}
