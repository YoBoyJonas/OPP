using System.Net;
using System.Net.Http.Json;
using CastleEscape.Contracts;
using CastleEscape.Contracts.Content;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CastleEscape.Server.Tests;

public class LevelEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private Task<LevelPreviewResponse?> Preview(string query) =>
        factory.CreateClient().GetFromJsonAsync<LevelPreviewResponse>($"/api/levels/{query}", Api.Json, Api.Ct);

    [Fact]
    public async Task Presets_AreListed()
    {
        var presets = await factory.CreateClient().GetFromJsonAsync<string[]>("/api/levels/presets", Api.Json, Api.Ct);

        Assert.Contains("tutorial", presets!);
        Assert.Contains("arena", presets!);
    }

    [Fact]
    public async Task GeneratedPreview_IsValid_Themed_AndRepeatable()
    {
        var a = await Preview("8/preview?seed=3");
        var b = await Preview("8/preview?seed=3");

        Assert.True(a!.Valid);
        Assert.Equal(LevelTheme.Crypt, a.Theme);
        Assert.Equal("ProceduralLevelBuilder", a.Builder);
        Assert.All(a.Zombies, z => Assert.Equal("CryptZombie", z.Variant));
        Assert.Equal(a.Rows, b!.Rows);
    }

    [Fact]
    public async Task PresetPreview_UsesThePresetBuilder()
    {
        var level = await Preview("1/preview?preset=tutorial");

        Assert.Equal("PresetLevelBuilder", level!.Builder);
        Assert.True(level.Valid);
        Assert.Equal(24, level.Width);
    }

    [Theory]
    [InlineData("99/preview")]
    [InlineData("1/preview?preset=nope")]
    public async Task BadRequests_Are400(string query)
    {
        var response = await factory.CreateClient().GetAsync($"/api/levels/{query}", Api.Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
