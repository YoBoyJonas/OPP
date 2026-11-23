using System.Net;
using System.Net.Http.Json;
using CastleEscape.Contracts;
using CastleEscape.Contracts.Dev;
using CastleEscape.Contracts.Gameplay;
using CastleEscape.Contracts.Realtime;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CastleEscape.Server.Tests;

/// <summary>Every level is the "arena" preset (two zombies).</summary>
public sealed class ArenaFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Generation:PresetLevel", "arena");
        builder.UseSetting("Game:LevelTransitionSeconds", "0.1");
        builder.UseSetting("DevTools:Enabled", "true");
    }
}

public sealed class NoDevToolsFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) =>
        builder.UseSetting("DevTools:Enabled", "false");
}

public class DevEndpointTests(ArenaFactory factory) : IClassFixture<ArenaFactory>
{
    private async Task<(HttpClient Http, DevQuickstartResponse Game)> Quickstart()
    {
        var http = factory.CreateClient();
        var response = await http.PostAsJsonAsync("/api/dev/quickstart", new DevQuickstartRequest("swimmer", "scout"), Api.Json, Api.Ct);
        response.EnsureSuccessStatusCode();
        var game = (await response.Content.ReadFromJsonAsync<DevQuickstartResponse>(Api.Json, Api.Ct))!;
        await Api.Eventually(async () => (await http.GetAsync($"/api/sessions/{game.SessionId}/state", Api.Ct)).IsSuccessStatusCode ? "ok" : null,
            TimeSpan.FromSeconds(5));
        return (http, game);
    }

    private static Task<TickStateMessage?> State(HttpClient http, Guid sessionId) =>
        http.GetFromJsonAsync<TickStateMessage>($"/api/sessions/{sessionId}/state", Api.Json, Api.Ct);

    [Fact]
    public async Task Quickstart_ReturnsTwoReadyPlayers()
    {
        var (http, game) = await Quickstart();

        Assert.Equal(["swimmer", "scout"], game.Players.Select(p => p.CharacterId));
        Assert.All(game.Players, p => Assert.Contains($"playerToken={p.PlayerToken}", p.HubUrl));
        Assert.Equal(SessionPhase.Playing, (await State(http, game.SessionId))!.Phase);
    }

    [Fact]
    public async Task ZombieStrategy_CanBeSwitched_AndUndone()
    {
        var (http, game) = await Quickstart();
        var original = (await State(http, game.SessionId))!.Zombies.Select(z => z.Strategy).ToArray();
        Assert.NotEmpty(original);

        (await http.PutAsJsonAsync($"/api/dev/sessions/{game.SessionId}/zombies/strategy",
            new ZombieStrategyRequest(MovementStrategyKind.Predictive), Api.Json, Api.Ct)).EnsureSuccessStatusCode();
        await Api.Eventually(async () => (await State(http, game.SessionId))!.Zombies.All(z => z.Strategy == MovementStrategyKind.Predictive) ? "ok" : null,
            TimeSpan.FromSeconds(5));

        var undo = await (await http.PostAsync($"/api/dev/sessions/{game.SessionId}/undo", null, Api.Ct))
            .Content.ReadFromJsonAsync<UndoResponse>(Api.Json, Api.Ct);
        Assert.Equal("SetZombieStrategy Predictive", undo!.Undone);
        Assert.Equal(original, (await State(http, game.SessionId))!.Zombies.Select(z => z.Strategy).ToArray());
    }

    [Fact]
    public async Task GivePower_ShowsInTheHud()
    {
        var (http, game) = await Quickstart();
        var ana = game.Players[0];

        (await http.PostAsJsonAsync($"/api/dev/sessions/{game.SessionId}/players/{ana.PlayerId}/powers",
            new GivePowerRequest(PowerType.Swim, 30), Api.Json, Api.Ct)).EnsureSuccessStatusCode();

        await Api.Eventually(async () =>
        {
            var hud = await http.GetFromJsonAsync<HudResponse>($"/api/sessions/{game.SessionId}/hud", Api.Json, Api.Ct);
            return hud!.Players.Single(p => p.PlayerId == ana.PlayerId).Powers.Any(p => p.Power == PowerType.Swim) ? "ok" : null;
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SkipLevel_LoadsTheNextLevel()
    {
        var (http, game) = await Quickstart();

        (await http.PostAsync($"/api/dev/sessions/{game.SessionId}/skip-level", null, Api.Ct)).EnsureSuccessStatusCode();

        await Api.Eventually(async () => (await State(http, game.SessionId))!.LevelIndex == 2 ? "ok" : null, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CloneMode_CanBeFlippedAtRuntime()
    {
        var http = factory.CreateClient();
        const string url = "/api/dev/patterns/prototype-clone-mode";

        var shallow = await (await http.PutAsJsonAsync(url, new CloneModeDto("shallow"), Api.Json, Api.Ct)).Content.ReadFromJsonAsync<CloneModeDto>(Api.Json, Api.Ct);
        var deep = await (await http.PutAsJsonAsync(url, new CloneModeDto("Deep"), Api.Json, Api.Ct)).Content.ReadFromJsonAsync<CloneModeDto>(Api.Json, Api.Ct);
        var bad = await http.PutAsJsonAsync(url, new CloneModeDto("medium"), Api.Json, Api.Ct);

        Assert.Equal("Shallow", shallow!.Mode);
        Assert.Equal("Deep", deep!.Mode);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }
}

public class DevToolsDisabledTests(NoDevToolsFactory factory) : IClassFixture<NoDevToolsFactory>
{
    [Fact]
    public async Task DevEndpoints_DoNotExist()
    {
        var response = await factory.CreateClient().PostAsync("/api/dev/quickstart", null, Api.Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
