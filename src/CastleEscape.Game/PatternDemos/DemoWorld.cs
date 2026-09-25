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

    /// <summary>A realistic per-tick state message built by the real snapshot mapper.</summary>
    public static TickStateMessage SampleState()
    {
        var level = TutorialLevel();
        var (p1, p2) = TwoPlayers(level);
        return SnapshotMapper.ToTickState(Guid.NewGuid(), seq: 7, tick: 120, SessionPhase.Playing, level, [p1, p2]);
    }
}
