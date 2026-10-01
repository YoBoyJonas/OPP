using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CastleEscape.Server.Tests;

/// <summary>
/// Keeps <c>docs/openapi.json</c> (for frontend developers and code generators) in step with the code.
/// After changing an endpoint, regenerate it with: <c>UPDATE_OPENAPI=1 dotnet test --filter OpenApiExport</c>.
/// </summary>
public class OpenApiExportTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static string RepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CastleEscape.sln")))
        {
            dir = dir.Parent;
        }
        return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("Repository root not found."), relative);
    }

    [Fact]
    public async Task DocsOpenApiJson_IsUpToDate()
    {
        var live = await factory.CreateClient().GetStringAsync("/openapi/v1.json", Api.Ct);
        var document = JsonNode.Parse(live)!.AsObject();
        document.Remove("servers"); // the test host's address, not part of the contract
        var normalized = document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n";

        var path = RepoFile(Path.Combine("docs", "openapi.json"));
        if (Environment.GetEnvironmentVariable("UPDATE_OPENAPI") == "1")
        {
            await File.WriteAllTextAsync(path, normalized, Api.Ct);
        }

        Assert.True(File.Exists(path), "docs/openapi.json is missing. Run: UPDATE_OPENAPI=1 dotnet test --filter OpenApiExport");
        var saved = (await File.ReadAllTextAsync(path, Api.Ct)).ReplaceLineEndings("\n");
        Assert.True(saved == normalized, "docs/openapi.json is out of date. Run: UPDATE_OPENAPI=1 dotnet test --filter OpenApiExport");
    }
}
