using System.Net.Http.Json;
using System.Text.Json;
using CastleEscape.Contracts.Realtime;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CastleEscape.Server.Tests;

public class RealtimeEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Protocol_ListsEveryMethodAndEventType()
    {
        var protocol = await factory.CreateClient().GetFromJsonAsync<RealtimeProtocolResponse>("/api/realtime/protocol", Api.Json, Api.Ct);

        Assert.Equal("/hubs/game", protocol!.HubPath);
        Assert.Equal(typeof(ClientMethods).GetFields().Length, protocol.ServerToClient.Length);
        Assert.Equal(typeof(HubMethods).GetFields().Length, protocol.ClientToServer.Length);
        Assert.Contains(GameEventTypes.SoundCue, protocol.EventTypes);
    }

    [Fact]
    public async Task Examples_CoverEveryServerMessage()
    {
        var examples = await factory.CreateClient().GetFromJsonAsync<Dictionary<string, JsonElement>>("/api/realtime/examples", Api.Json, Api.Ct);

        Assert.Equal(typeof(ClientMethods).GetFields().Select(f => (string)f.GetValue(null)!).Order(), examples!.Keys.Order());
        Assert.Equal("ItemCollected", examples[ClientMethods.GameEvent].GetProperty("type").GetString());
        Assert.True(examples[ClientMethods.StateUpdated].GetProperty("players").GetArrayLength() == 2);
    }
}
