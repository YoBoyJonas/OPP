using System.Net.Http.Json;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CastleEscape.Server.Tests;

public class PatternEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/api/patterns")]
    [InlineData("/api/patterns/prototype")]
    [InlineData("/api/dev/patterns/prototype-clone-mode")]
    public async Task CourseEndpoints_AreAbsentEvenInDevelopment(string path)
    {
        var http = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync(path, Api.Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.PutAsJsonAsync(path, new { mode = "Shallow" }, Api.Ct)).StatusCode);
    }

    [Theory]
    [InlineData("singleton")]
    [InlineData("adapter")]
    [InlineData("factory-method")]
    [InlineData("abstract-factory")]
    [InlineData("builder")]
    [InlineData("prototype")]
    [InlineData("strategy")]
    [InlineData("decorator")]
    [InlineData("command")]
    [InlineData("observer")]
    [InlineData("bridge")]
    [InlineData("facade")]
    public async Task DemoEndpoints_AreAbsent(string key) =>
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().PostAsync($"/api/patterns/{key}/demo", null, Api.Ct)).StatusCode);
}
