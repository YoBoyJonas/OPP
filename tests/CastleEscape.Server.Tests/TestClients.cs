using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.World;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace CastleEscape.Server.Tests;

/// <summary>Every level is the "tutorial" preset (no zombies); short pause between levels.</summary>
public sealed class TutorialFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Generation:PresetLevel", "tutorial");
        builder.UseSetting("Game:LevelTransitionSeconds", "0.1");
    }
}

public static class Api
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Creates a session, joins it, picks characters for both; returns both players' credentials.</summary>
    public static async Task<(Guid SessionId, (Guid Id, string Token) P1, (Guid Id, string Token) P2)> StartTwoPlayerGame(
        HttpClient http, string character = "scout")
    {
        var created = await (await http.PostAsJsonAsync("/api/sessions", new CreateSessionRequest("Ana"), Json, Ct))
            .Content.ReadFromJsonAsync<CreateSessionResponse>(Json, Ct);
        var joined = await (await http.PostAsJsonAsync("/api/sessions/join", new JoinSessionRequest(created!.JoinCode, "Ben"), Json, Ct))
            .Content.ReadFromJsonAsync<JoinSessionResponse>(Json, Ct);

        foreach (var token in new[] { created.PlayerToken, joined!.PlayerToken })
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"/api/sessions/{created.SessionId}/players/me/character")
            {
                Content = JsonContent.Create(new SelectCharacterRequest(character), options: Json),
            };
            request.Headers.Add("X-Player-Token", token);
            (await http.SendAsync(request, Ct)).EnsureSuccessStatusCode();
        }

        return (created.SessionId, (created.PlayerId, created.PlayerToken), (joined.PlayerId, joined.PlayerToken));
    }

    public static async Task<T> Eventually<T>(Func<Task<T?>> probe, TimeSpan timeout) where T : class
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await probe() is { } value) return value;
            await Task.Delay(25, Ct);
        }
        throw new TimeoutException("Condition not met in time.");
    }
}

/// <summary>A SignalR client that remembers the latest layout and state, and can walk its player to a tile.</summary>
public sealed class PlayerClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private volatile LevelStartedMessage? _level;
    private volatile TickStateMessage? _state;

    public PlayerClient(WebApplicationFactory<Program> factory, Guid sessionId, Guid playerId, string token)
    {
        PlayerId = playerId;
        _connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, $"hubs/game?sessionId={sessionId}&playerToken={token}"), o =>
            {
                o.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                o.Transports = HttpTransportType.LongPolling;
            })
            .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        _connection.On<LevelStartedMessage>(ClientMethods.LevelStarted, m => { _level = m; _state = m.State; LevelsStarted.Add(m.LevelIndex); });
        _connection.On<TickStateMessage>(ClientMethods.StateUpdated, m => _state = m);
        _connection.On<GameEventMessage>(ClientMethods.GameEvent, m => Events.Add(m));
        _connection.On<ErrorMessage>(ClientMethods.Error, m => Errors.Add(m));
    }

    public Guid PlayerId { get; }
    public ConcurrentBag<int> LevelsStarted { get; } = [];
    public ConcurrentBag<GameEventMessage> Events { get; } = [];
    public ConcurrentBag<ErrorMessage> Errors { get; } = [];
    public TickStateMessage? State => _state;
    public LevelStartedMessage? Level => _level;

    public Task StartAsync() => _connection.StartAsync(Api.Ct);

    public async Task WaitForLevel(int index, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!LevelsStarted.Contains(index) || _state?.LevelIndex != index)
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException($"Level {index} never started.");
            await Task.Delay(20, Api.Ct);
        }
    }

    /// <summary>Steers with held directions only, planning from the tile the player is heading to.</summary>
    public async Task WalkTo(GridPos target, TimeSpan timeout)
    {
        var rows = _level!.Rows;
        var grid = new Grid(_level.Width, _level.Height);
        Direction? last = null;
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var state = _state!;
            var me = state.Players.Single(p => p.PlayerId == PlayerId);
            var other = state.Players.Single(p => p.PlayerId != PlayerId);
            var tile = new GridPos(me.TileX, me.TileY);

            if (!me.IsMoving && tile == target)
            {
                await _connection.InvokeAsync(HubMethods.SetDirection, Direction.None, Api.Ct);
                return;
            }

            var from = me.IsMoving ? tile.Step(me.Facing) : tile;
            var path = PathFinder.ShortestPath(grid, from, target, p =>
                !(p.X == other.TileX && p.Y == other.TileY) && rows[p.Y][p.X] switch
                {
                    '#' or '~' or 'O' => false,
                    'D' => state.DoorOpen,
                    _ => true,
                });
            var direction = path is { Count: > 0 } ? from.DirectionTo(path[0]) : Direction.None;
            if (direction != last)
            {
                await _connection.InvokeAsync(HubMethods.SetDirection, direction, Api.Ct);
                last = direction;
            }
            await Task.Delay(10, Api.Ct);
        }

        throw new TimeoutException($"Did not reach {target}; at ({_state?.Players.Single(p => p.PlayerId == PlayerId).TileX}," +
                                   $"{_state?.Players.Single(p => p.PlayerId == PlayerId).TileY}).");
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
