using System.Reflection;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.Sessions;

namespace CastleEscape.Server.Tests;

public class FacadeRequirementTests
{
    [Fact]
    public void AtLeastTwoServerClassesAreFacadeClients()
    {
        static bool UsesFacade(MethodBase m) => m.GetParameters().Any(p => p.ParameterType == typeof(GameFacade));
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        var clients = typeof(Program).Assembly.GetTypes()
            .Where(t => t.GetConstructors(all).Any(UsesFacade) || t.GetMethods(all).Any(UsesFacade))
            .Select(t => t.Name)
            .ToList();

        Assert.Contains("GameHub", clients);
        Assert.Contains("GameLoopService", clients);
        Assert.Contains("SessionEndpoints", clients);
        Assert.Contains("GameplayEndpoints", clients);
    }

    /// <summary>
    /// The other half of the course requirement: at least 3 subsystem classes, counted from the same
    /// attributes <c>GET /api/patterns</c> reports, so the published participant list proves it too.
    /// </summary>
    [Fact]
    public void AtLeastThreeSubsystemClassesAreTagged()
    {
        var facade = PatternCatalog.Describe(typeof(GameFacade).Assembly).Single(p => p.Key == "facade");
        var subsystems = facade.Participants.Where(p => p.Role == "Subsystem").Select(p => p.Type).ToList();

        Assert.True(subsystems.Count >= 3, $"Facade needs >=3 tagged subsystems, found {subsystems.Count}: {string.Join(", ", subsystems)}");
        Assert.Single(facade.Participants.Where(p => p.Role == "Facade"));
    }
}
