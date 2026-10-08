using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CastleEscape.Contracts;
using CastleEscape.Contracts.Content;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CastleEscape.Server.Tests;

public class ContentEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Summary_CountsEveryContentType()
    {
        var summary = await factory.CreateClient().GetFromJsonAsync<ContentSummaryResponse>("/api/content", Ct);

        Assert.NotNull(summary);
        Assert.Equal(64, summary.ContentHash.Length);
        Assert.Equal((3, 6, 2, 3, 4, 10),
            (summary.Characters, summary.Consumables, summary.Combos, summary.Obstacles, summary.Zombies, summary.Levels));
    }

    [Fact]
    public async Task Consumables_WriteTypeKindAndGrant()
    {
        using var doc = await GetJson("/api/content/consumables");

        var boots = doc.RootElement.EnumerateArray().Single(e => e.GetProperty("id").GetString() == "jump-boots");
        Assert.Equal("Power", boots.GetProperty("type").GetString());
        Assert.Equal("Power", boots.GetProperty("kind").GetString());
        Assert.Equal("Jump", boots.GetProperty("grant").GetProperty("power").GetString());
    }

    [Fact]
    public async Task Characters_AreTheTeamCharacters()
    {
        using var doc = await GetJson("/api/content/characters");

        Assert.Equal(["warrior", "scout", "swimmer"],
            doc.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Powers_ListBaseAndSuperPowers()
    {
        var powers = await factory.CreateClient().GetFromJsonAsync<List<PowerSummary>>("/api/content/powers",
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } }, Ct);

        Assert.NotNull(powers);
        Assert.Equal([PowerType.Jump, PowerType.Sprint, PowerType.Swim, PowerType.JumpDash, PowerType.FastSwim],
            powers.Select(p => p.Power));
        Assert.All(powers.Where(p => !p.IsSuperPower), p => Assert.Equal(10, p.DurationSeconds));
    }

    [Fact]
    public async Task Level_Unknown_Returns404Problem()
    {
        var response = await factory.CreateClient().GetAsync("/api/content/levels/99", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Level_Known_ReturnsTheme()
    {
        using var doc = await GetJson("/api/content/levels/7");

        Assert.Equal("Crypt", doc.RootElement.GetProperty("theme").GetString());
    }

    private async Task<JsonDocument> GetJson(string url)
    {
        var stream = await factory.CreateClient().GetStreamAsync(url, Ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: Ct);
    }
}
