using System.Reflection;
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
}
