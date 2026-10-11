using System.Reflection;
using CastleEscape.Application.Sessions;
using CastleEscape.Contracts.Content;
using CastleEscape.Domain.Sessions;
using CastleEscape.Infrastructure.Content;
using CastleEscape.PatternDemos.Catalog;
using CastleEscape.PatternDemos.Demos;

namespace CastleEscape.Core.Tests.Architecture;

public class DependencyTests
{
    [Fact]
    public void Domain_HasNoDependencyOnOtherSolutionProjects() =>
        Assert.Empty(SolutionReferences(typeof(GameWorld).Assembly));

    [Fact]
    public void Application_DependsOnlyOnDomainAndContracts() =>
        Assert.Equal(["CastleEscape.Contracts", "CastleEscape.Domain"], SolutionReferences(typeof(GameFacade).Assembly));

    [Fact]
    public void Contracts_AreIndependentOfRuntimeProjects() =>
        Assert.Empty(SolutionReferences(typeof(CharacterDto).Assembly));

    [Fact]
    public void CourseCatalog_FindsAllTwelvePatternsAcrossRuntimeLayers()
    {
        var patterns = PatternCatalog.Describe(typeof(GameWorld).Assembly, typeof(GameFacade).Assembly, typeof(ContentCatalog).Assembly);
        Assert.Equal(12, patterns.Length);
        Assert.All(patterns, p => Assert.NotEmpty(p.Participants));
        Assert.All(patterns.GroupBy(p => p.Owner), group => Assert.Equal(3, group.Count()));
        Assert.Equal(patterns.Select(p => p.Key).Order(), PatternDemoCatalog.All.Select(d => d.Key).Order());
    }

    [Theory]
    [InlineData(typeof(Domain.Direction), typeof(Contracts.Direction))]
    [InlineData(typeof(Domain.PowerType), typeof(Contracts.PowerType))]
    [InlineData(typeof(Domain.LevelTheme), typeof(Contracts.LevelTheme))]
    [InlineData(typeof(Domain.MovementStrategyKind), typeof(Contracts.MovementStrategyKind))]
    [InlineData(typeof(Domain.SessionPhase), typeof(Contracts.SessionPhase))]
    public void BoundaryEnumConversions_PreserveNamesAndValues(Type domain, Type contract)
    {
        Assert.Equal(Enum.GetNames(domain), Enum.GetNames(contract));
        Assert.Equal(Enum.GetValues(domain).Cast<object>().Select(Convert.ToInt32),
            Enum.GetValues(contract).Cast<object>().Select(Convert.ToInt32));
    }

    private static string[] SolutionReferences(Assembly assembly) => assembly.GetReferencedAssemblies()
        .Select(a => a.Name!).Where(n => n.StartsWith("CastleEscape.", StringComparison.Ordinal)).Order().ToArray();
}
