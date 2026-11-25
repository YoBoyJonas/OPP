using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Messaging;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests;

/// <summary>Small real-game setups shared by the pattern tests (the "tutorial" preset with two players).</summary>
public static class TestWorld
{
    public static ContentCatalog Catalog => ContentCatalog.Instance;

    /// <summary>Two levers, a door and exits: enough to win a one-level game.</summary>
    public static readonly string[] LeverDoorMap =
    [
        "#######",
        "#L1.2L#",
        "###D###",
        "##EEE##",
        "#######",
    ];

    /// <summary>A zombie with a wall between it and the players.</summary>
    public static readonly string[] WallMap =
    [
        "############",
        "#Z....#....#",
        "#.....#.1..#",
        "#.....#....#",
        "#..........#",
        "#.......2..#",
        "############",
    ];

    public static LevelState TutorialLevel() =>
        new LevelProvider(Catalog, new GameOptions(), new GenerationOptions()).BuildPreset(Catalog.GetLevel(1), "tutorial", seed: 42);

    public static (PlayerSlot P1, PlayerSlot P2) TwoPlayers(LevelState level)
    {
        var p1 = new PlayerSlot(Guid.NewGuid(), "test-token-1", "Ana", 1) { CharacterId = "scout" };
        var p2 = new PlayerSlot(Guid.NewGuid(), "test-token-2", "Ben", 2) { CharacterId = "swimmer" };
        p1.Entity = new PlayerEntity(p1.PlayerId, p1.Name, Catalog.GetCharacter("scout"), level.StartTiles[0]);
        p2.Entity = new PlayerEntity(p2.PlayerId, p2.Name, Catalog.GetCharacter("swimmer"), level.StartTiles[1]);
        return (p1, p2);
    }

    /// <summary>A realistic per-tick state message built by the real snapshot mapper.</summary>
    public static TickStateMessage SampleState()
    {
        var level = TutorialLevel();
        var (p1, p2) = TwoPlayers(level);
        return SnapshotMapper.ToTickState(Guid.NewGuid(), seq: 7, tick: 120, SessionPhase.Playing, level, [p1, p2]);
    }

    /// <summary>A Bridge implementor that only counts what it receives.</summary>
    public sealed class CountingChannel : IClientChannel
    {
        public Dictionary<string, int> Received { get; } = [];
        public string Name => "Counting";

        public Task SendAsync(Guid sessionId, string method, IServerMessage message)
        {
            Received[method] = Received.GetValueOrDefault(method) + 1;
            return Task.CompletedTask;
        }
    }
}
