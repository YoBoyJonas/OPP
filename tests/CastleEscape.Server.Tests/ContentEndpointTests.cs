using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CastleEscape.Server.Tests;

public class ContentEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Characters_AreTheTeamCharacters()
    {
        using var doc = await GetJson("/api/content/characters");

        Assert.Equal(["warrior", "scout", "swimmer"],
            doc.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()));
    }

    private async Task<JsonDocument> GetJson(string url)
    {
        var stream = await factory.CreateClient().GetStreamAsync(url, Ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: Ct);
    }
}
