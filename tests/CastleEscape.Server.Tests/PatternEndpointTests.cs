using System.Net.Http.Json;
using CastleEscape.Contracts.Patterns;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CastleEscape.Server.Tests;

public class PatternEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task ListsTwelvePatterns_EachOwnedOncePerStudentTriple()
    {
        var patterns = await factory.CreateClient().GetFromJsonAsync<PatternDto[]>("/api/patterns", Api.Json, Api.Ct);

        Assert.NotNull(patterns);
        Assert.Equal(12, patterns.Length);
        Assert.Equal(12, patterns.Select(p => p.Name).Distinct().Count());
        Assert.All(patterns.GroupBy(p => p.Owner), g => Assert.Equal(3, g.Count()));
    }

    [Fact]
    public async Task UnknownPattern_Is404()
    {
        var response = await factory.CreateClient().GetAsync("/api/patterns/visitor", Api.Ct);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
