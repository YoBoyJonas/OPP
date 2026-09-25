using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CastleEscape.Contracts.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CastleEscape.Server.Tests;

public class SkeletonTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await factory.CreateClient().GetFromJsonAsync<HealthResponse>("/health", Ct);

        Assert.NotNull(response);
        Assert.Equal("Healthy", response.Status);
    }

    [Fact]
    public async Task OpenApi_HasTitleTagsAndTagGroups()
    {
        using var doc = await GetOpenApiDocument();
        var root = doc.RootElement;

        Assert.Equal("Castle Escape API", root.GetProperty("info").GetProperty("title").GetString());

        var tags = root.GetProperty("tags").EnumerateArray().Select(t => t.GetProperty("name").GetString()).ToList();
        Assert.Equal(
            ["Content", "Dev", "Diagnostics", "Gameplay", "Levels", "Patterns", "Realtime", "Sessions"],
            tags.Order());

        var groups = root.GetProperty("x-tagGroups").EnumerateArray()
            .ToDictionary(g => g.GetProperty("name").GetString()!,
                          g => g.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList());
        Assert.Equal(["Lobby", "Gameplay", "Content", "Course", "Ops"], groups.Keys);
        Assert.Equal(["Gameplay", "Realtime"], groups["Gameplay"]);
        Assert.Equal(["Diagnostics", "Dev"], groups["Ops"]);
    }

    [Fact]
    public async Task OpenApi_DeclaresPlayerTokenHeaderScheme()
    {
        using var doc = await GetOpenApiDocument();

        var scheme = doc.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("PlayerToken");
        Assert.Equal("apiKey", scheme.GetProperty("type").GetString());
        Assert.Equal("header", scheme.GetProperty("in").GetString());
        Assert.Equal("X-Player-Token", scheme.GetProperty("name").GetString());
    }

    [Fact]
    public async Task OpenApi_ListsHealthUnderDiagnostics()
    {
        using var doc = await GetOpenApiDocument();

        var get = doc.RootElement.GetProperty("paths").GetProperty("/health").GetProperty("get");
        Assert.Equal("GetHealth", get.GetProperty("operationId").GetString());
        Assert.Equal("Diagnostics", get.GetProperty("tags")[0].GetString());
    }

    [Fact]
    public async Task Scalar_ServesPage()
    {
        var response = await factory.CreateClient().GetAsync("/scalar", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Hub_IsMappedAtNewPath()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/hubs/game/negotiate?negotiateVersion=1", null, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cors_AllowsConfiguredOriginWithCredentials()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/hubs/game/negotiate");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await factory.CreateClient().SendAsync(request, Ct);

        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public void SignalRJson_WritesEnumsAsStrings()
    {
        var options = factory.Services.GetRequiredService<IOptions<Microsoft.AspNetCore.SignalR.JsonHubProtocolOptions>>();

        var json = JsonSerializer.Serialize(new { direction = CastleEscape.Server.Models.Direction.Up },
            options.Value.PayloadSerializerOptions);

        Assert.Equal("""{"direction":"Up"}""", json);
    }

    [Fact]
    public void InvalidOptions_FailStartup()
    {
        using var broken = factory.WithWebHostBuilder(b => b.UseSetting("Game:TickRate", "0"));

        var ex = Assert.ThrowsAny<Exception>(() => broken.CreateClient());

        Assert.Contains("TickRate", ex.ToString());
    }

    private async Task<JsonDocument> GetOpenApiDocument()
    {
        var stream = await factory.CreateClient().GetStreamAsync("/openapi/v1.json", Ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: Ct);
    }
}
