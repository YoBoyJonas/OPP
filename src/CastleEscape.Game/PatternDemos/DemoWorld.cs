using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Small real-game setups the demos share (the "tutorial" preset with two players).</summary>
public static class DemoWorld
{
    public static ContentCatalog Catalog => ContentCatalog.Instance;

    public static LevelState TutorialLevel() =>
        new LevelDirector(Catalog, new GenerationOptions()).Construct(PresetLevelBuilder.FromPreset(Catalog, "tutorial"), Catalog.GetLevel(1), seed: 42);

    /// <summary>A randomly generated level with the default options.</summary>
    public static LevelState Generate(LevelDefinition definition, int seed) =>
        new LevelDirector(Catalog, new GenerationOptions())
            .Construct(new ProceduralLevelBuilder(Catalog, new GameOptions(), new GenerationOptions()), definition, seed);

    public static (PlayerSlot P1, PlayerSlot P2) TwoPlayers(LevelState level)
    {
        var p1 = new PlayerSlot(Guid.NewGuid(), "demo-token-1", "Ana", 1) { CharacterId = "scout" };
        var p2 = new PlayerSlot(Guid.NewGuid(), "demo-token-2", "Ben", 2) { CharacterId = "swimmer" };
        p1.Entity = new PlayerEntity(p1.PlayerId, p1.Name, Catalog.GetCharacter("scout"), level.StartTiles[0]);
        p2.Entity = new PlayerEntity(p2.PlayerId, p2.Name, Catalog.GetCharacter("swimmer"), level.StartTiles[1]);
        return (p1, p2);
    }

    /// <summary>A real two-player session on a hand-made map, already playing level 1 (both players are scouts).</summary>
    public static (GameSession Session, PlayerSlot P1, PlayerSlot P2) PlayingSession(string[] rows, PatternOptions? patterns = null)
    {
        var session = new GameSession(Guid.NewGuid(), "DEMO01", Catalog, new RowsLevelProvider(Catalog, rows),
            new GameOptions { LevelTransitionSeconds = 0 }, baseSeed: 1, patterns);
        var p1 = session.AddPlayer("Ana");
        var p2 = session.AddPlayer("Ben");
        session.SelectCharacter(p1.PlayerId, "scout");
        session.SelectCharacter(p2.PlayerId, "scout");
        session.Tick(session.TickSeconds); // loads level 1
        return (session, p1, p2);
    }

    /// <summary>Ticks the session until <paramref name="done"/> holds (at most <paramref name="maxTicks"/> ticks).</summary>
    public static int RunUntil(GameSession session, Func<bool> done, int maxTicks = 400)
    {
        var ticks = 0;
        while (!done() && ticks < maxTicks)
        {
            session.Tick(session.TickSeconds);
            ticks++;
        }
        return ticks;
    }

    /// <summary>A realistic per-tick state message built by the real snapshot mapper.</summary>
    public static TickStateMessage SampleState()
    {
        var level = TutorialLevel();
        var (p1, p2) = TwoPlayers(level);
        return SnapshotMapper.ToTickState(Guid.NewGuid(), seq: 7, tick: 120, SessionPhase.Playing, level, [p1, p2]);
    }
}
