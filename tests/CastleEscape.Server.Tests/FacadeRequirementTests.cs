using System.Reflection;
using CastleEscape.Domain.Patterns;
using CastleEscape.Application.Sessions;
using CastleEscape.Domain.Sessions;

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
        var subsystems = typeof(GameFacade).Assembly.GetTypes()
            .Where(t => t.GetCustomAttributes<DesignPatternAttribute>().Any(a => a.Pattern == "Facade" && a.Role == "Subsystem"));
        Assert.True(subsystems.Count() >= 3);
    }
}
