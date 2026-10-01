using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CastleEscape.Contracts;
using CastleEscape.Contracts.Gameplay;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.World;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CastleEscape.Server.Tests;

public class LobbyTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static CancellationToken Ct => Api.Ct;

    [Fact]
    public async Task CreateJoinSelect_StartsLevelOne()
    {
        var http = factory.CreateClient();
        var (sessionId, _, _) = await Api.StartTwoPlayerGame(http);

        var session = await Api.Eventually(async () =>
        {
            var s = await http.GetFromJsonAsync<SessionDto>($"/api/sessions/{sessionId}", Api.Json, Ct);
            return s!.Phase == SessionPhase.Playing ? s : null;
        }, TimeSpan.FromSeconds(5));

        Assert.Equal(1, session.LevelIndex);
        Assert.All(session.Players, p => Assert.Equal("scout", p.CharacterId));

        var state = await http.GetFromJsonAsync<TickStateMessage>($"/api/sessions/{sessionId}/state", Api.Json, Ct);
        Assert.Equal(2, state!.Players.Length);
        var layout = await http.GetFromJsonAsync<LevelLayoutResponse>($"/api/sessions/{sessionId}/level", Api.Json, Ct);
        Assert.Equal((24, 16), (layout!.Width, layout.Height));
        var hud = await http.GetFromJsonAsync<HudResponse>($"/api/sessions/{sessionId}/hud", Api.Json, Ct);
        Assert.Equal(2, hud!.LeversTotal);
    }

    [Fact]
    public async Task RestInput_MovesThePlayer()
    {
        var http = factory.CreateClient();
        var (sessionId, p1, _) = await Api.StartTwoPlayerGame(http);
        var before = await Api.Eventually(() => http.GetFromJsonAsync<TickStateMessage>($"/api/sessions/{sessionId}/state", Api.Json, Ct)
            .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : null), TimeSpan.FromSeconds(5));
        var start = before.Players.Single(p => p.PlayerId == p1.Id);

        // Try every direction; at least one must be open from a start tile.
        foreach (var direction in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{sessionId}/input")
            {
                Content = JsonContent.Create(new DirectionRequest(direction), options: Api.Json),
            };
            request.Headers.Add("X-Player-Token", p1.Token);
            Assert.Equal(HttpStatusCode.Accepted, (await http.SendAsync(request, Ct)).StatusCode);
            await Task.Delay(300, Ct);

            var now = (await http.GetFromJsonAsync<TickStateMessage>($"/api/sessions/{sessionId}/state", Api.Json, Ct))!
                .Players.Single(p => p.PlayerId == p1.Id);
            if ((now.TileX, now.TileY) != (start.TileX, start.TileY) || now.IsMoving)
            {
                return;
            }
        }
        Assert.Fail("Player 1 never moved.");
    }

    [Fact]
    public async Task Join_WrongCode_Is404WithCode()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/sessions/join", new JoinSessionRequest("ZZZZZZ", "Ben"), Api.Json, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal("InvalidJoinCode", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Join_ThirdPlayer_Is409()
    {
        var http = factory.CreateClient();
        var created = await (await http.PostAsJsonAsync("/api/sessions", new CreateSessionRequest("Ana"), Api.Json, Ct))
            .Content.ReadFromJsonAsync<CreateSessionResponse>(Api.Json, Ct);
        await http.PostAsJsonAsync("/api/sessions/join", new JoinSessionRequest(created!.JoinCode, "Ben"), Api.Json, Ct);

        var third = await http.PostAsJsonAsync("/api/sessions/join", new JoinSessionRequest(created.JoinCode, "Cid"), Api.Json, Ct);

        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
    }

    [Fact]
    public async Task SelectCharacter_WithoutToken_Is401()
    {
        var http = factory.CreateClient();
        var created = await (await http.PostAsJsonAsync("/api/sessions", new CreateSessionRequest("Ana"), Api.Json, Ct))
            .Content.ReadFromJsonAsync<CreateSessionResponse>(Api.Json, Ct);

        var response = await http.PutAsJsonAsync($"/api/sessions/{created!.SessionId}/players/me/character",
            new SelectCharacterRequest("scout"), Api.Json, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task State_BeforeAnyLevel_Is409()
    {
        var http = factory.CreateClient();
        var created = await (await http.PostAsJsonAsync("/api/sessions", new CreateSessionRequest("Ana"), Api.Json, Ct))
            .Content.ReadFromJsonAsync<CreateSessionResponse>(Api.Json, Ct);

        var response = await http.GetAsync($"/api/sessions/{created!.SessionId}/state", Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Hub_WrongToken_SendsErrorAndCloses()
    {
        var http = factory.CreateClient();
        var created = await (await http.PostAsJsonAsync("/api/sessions", new CreateSessionRequest("Ana"), Api.Json, Ct))
            .Content.ReadFromJsonAsync<CreateSessionResponse>(Api.Json, Ct);
        await using var client = new PlayerClient(factory, created!.SessionId, created.PlayerId, "not-the-token");

        try
        {
            await client.StartAsync();
        }
        catch
        {
            // The server may close the connection before the start handshake completes.
        }
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (client.Errors.IsEmpty && DateTime.UtcNow < deadline) await Task.Delay(20, Ct);

        Assert.Contains(client.Errors, e => e.Code == GameErrorCode.InvalidPlayerToken);
    }
}

public class TwoPlayerRunTests(TutorialFactory factory) : IClassFixture<TutorialFactory>
{
    [Fact]
    public async Task TwoSignalRClients_PullLevers_ReachExit_NextLevelStarts()
    {
        var http = factory.CreateClient();
        var (sessionId, p1, p2) = await Api.StartTwoPlayerGame(http);
        await using var ana = new PlayerClient(factory, sessionId, p1.Id, p1.Token);
        await using var ben = new PlayerClient(factory, sessionId, p2.Id, p2.Token);
        await ana.StartAsync();
        await ben.StartAsync();
        await ana.WaitForLevel(1, TimeSpan.FromSeconds(5));
        await ben.WaitForLevel(1, TimeSpan.FromSeconds(5));

        // tutorial.txt: levers at (3,3) and (20,3); door (11,12); exit tiles (11,13) and (12,13).
        await Task.WhenAll(
            ana.WalkTo(new GridPos(3, 3), TimeSpan.FromSeconds(10)),
            ben.WalkTo(new GridPos(20, 3), TimeSpan.FromSeconds(10)));
        await Api.Eventually(() => Task.FromResult(ana.State!.DoorOpen ? ana.State : null), TimeSpan.FromSeconds(3));

        await ana.WalkTo(new GridPos(12, 13), TimeSpan.FromSeconds(15));
        await ben.WalkTo(new GridPos(11, 13), TimeSpan.FromSeconds(15));

        await ana.WaitForLevel(2, TimeSpan.FromSeconds(5));
        Assert.Contains(ana.Events, e => e.Type == GameEventTypes.DoorOpened);
        Assert.Contains(ben.Events, e => e.Type == GameEventTypes.LevelCompleted);
        Assert.Empty(ana.Errors);
    }
}

public class StateFormatTests(TutorialFactory factory) : IClassFixture<TutorialFactory>
{
    [Fact]
    public async Task State_AsXml_ViaQueryAndAcceptHeader()
    {
        var http = factory.CreateClient();
        var (sessionId, _, _) = await Api.StartTwoPlayerGame(http);
        await Api.Eventually(async () => (await http.GetAsync($"/api/sessions/{sessionId}/state", Api.Ct)).IsSuccessStatusCode ? "ok" : null,
            TimeSpan.FromSeconds(5));

        var byQuery = await http.GetAsync($"/api/sessions/{sessionId}/state?format=xml", Api.Ct);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/sessions/{sessionId}/state");
        request.Headers.Add("Accept", "application/xml");
        var byHeader = await http.SendAsync(request, Api.Ct);

        foreach (var response in new[] { byQuery, byHeader })
        {
            Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
            var xml = System.Xml.Linq.XDocument.Parse(await response.Content.ReadAsStringAsync(Api.Ct));
            Assert.Equal("TickStateMessage", xml.Root!.Name.LocalName);
        }

        var bad = await http.GetAsync($"/api/sessions/{sessionId}/state?format=yaml", Api.Ct);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }
}

public class PollingChannelTests(TutorialFactory factory) : IClassFixture<TutorialFactory>
{
    [Fact]
    public async Task Messages_ArePolledInOrder_AfterTheGivenSeq()
    {
        var http = factory.CreateClient();
        var (sessionId, _, _) = await Api.StartTwoPlayerGame(http);

        var first = await Api.Eventually(async () =>
        {
            var page = await http.GetFromJsonAsync<PolledMessagesResponse>($"/api/sessions/{sessionId}/messages", Api.Json, Api.Ct);
            return page!.Messages.Any(m => m.Method == ClientMethods.StateUpdated) ? page : null;
        }, TimeSpan.FromSeconds(5));

        Assert.Equal("application/json", first.ContentType);
        Assert.Contains(first.Messages, m => m.Method == ClientMethods.LevelStarted);
        Assert.Equal(first.Messages.Select(m => m.Seq).Order(), first.Messages.Select(m => m.Seq));
        Assert.Equal(first.Messages[^1].Seq, first.LatestSeq);

        var next = await http.GetFromJsonAsync<PolledMessagesResponse>(
            $"/api/sessions/{sessionId}/messages?afterSeq={first.LatestSeq}&format=xml", Api.Json, Api.Ct);
        Assert.All(next!.Messages, m => Assert.True(m.Seq > first.LatestSeq));
        Assert.Equal("application/xml", next.ContentType);
        Assert.All(next.Messages, m => Assert.StartsWith("<?xml", m.Body));
    }
}
