using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class FacadeTests
{
    private static ContentCatalog Catalog => ContentCatalog.Instance;

    private static GameFacade Facade(ILevelProvider? levels = null)
    {
        var registry = new SessionRegistry(Catalog, levels ?? new RowsLevelProvider(Catalog, FacadeDemo.Map),
            new GameOptions { LevelTransitionSeconds = 0 });
        return new GameFacade(registry, new GameLoopScheduler(registry), Catalog);
    }

    /// <summary>A provider whose levels break the game: the session's tick throws.</summary>
    private sealed class BrokenLevels : ILevelProvider
    {
        public LevelState CreateLevel(int levelIndex, int seed) => throw new InvalidOperationException("boom");
    }

    private static (GameFacade Game, Contracts.Sessions.CreateSessionResponse Ana, Contracts.Sessions.JoinSessionResponse Ben) Started(
        GameFacade? game = null)
    {
        game ??= Facade();
        var ana = game.CreateSession("Ana");
        var ben = game.JoinSession(game.GetSession(ana.SessionId).JoinCode, "Ben");
        game.SelectCharacter(ana.SessionId, ana.PlayerToken, "scout");
        game.SelectCharacter(ana.SessionId, ben.PlayerToken, "scout");
        game.Tick(0.05);
        return (game, ana, ben);
    }

    [Fact]
    public void Lobby_ThroughTheFacade()
    {
        var (game, ana, _) = Started();

        Assert.Equal(SessionPhase.Playing, game.GetSession(ana.SessionId).Phase);
        Assert.Single(game.ListSessions());
        Assert.Equal(1, game.GetState(ana.SessionId).LevelIndex);
        Assert.Contains("scout", game.CharacterIds);
    }

    [Fact]
    public void Inputs_NeedTheRightToken()
    {
        var (game, ana, _) = Started();

        var ex = Assert.Throws<GameException>(() => game.SubmitDirection(ana.SessionId, "not-a-token", Direction.Left));
        Assert.Equal(GameErrorCode.InvalidPlayerToken, ex.Code);
        Assert.Equal(GameErrorCode.SessionNotFound,
            Assert.Throws<GameException>(() => game.GetHud(Guid.NewGuid())).Code);
    }

    [Fact]
    public void State_BeforeTheFirstLevel_IsAClearError()
    {
        var game = Facade();
        var ana = game.CreateSession("Ana");

        Assert.Equal(GameErrorCode.NoLevelLoaded, Assert.Throws<GameException>(() => game.GetState(ana.SessionId)).Code);
    }

    [Fact]
    public void Connect_ReturnsTheCatchUpMessages()
    {
        var (game, ana, _) = Started();

        var connection = game.Connect(ana.SessionId, ana.PlayerToken, "conn-1");

        Assert.Equal(ana.PlayerId, connection.PlayerId);
        Assert.NotNull(connection.LevelStarted);
        Assert.NotNull(connection.State);
        Assert.True(game.GetSession(ana.SessionId).Players.Single(p => p.PlayerId == ana.PlayerId).Connected);

        game.Disconnect(ana.SessionId, ana.PlayerToken, "conn-1");
        Assert.False(game.GetSession(ana.SessionId).Players.Single(p => p.PlayerId == ana.PlayerId).Connected);
    }

    [Fact]
    public void Tick_StopsABrokenSession_AndReportsIt()
    {
        var game = Facade(new BrokenLevels());
        var healthy = Facade();
        var ana = game.CreateSession("Ana");
        var ben = game.JoinSession(game.GetSession(ana.SessionId).JoinCode, "Ben");
        game.SelectCharacter(ana.SessionId, ana.PlayerToken, "scout");
        game.SelectCharacter(ana.SessionId, ben.PlayerToken, "scout");

        var messages = game.Tick(0.05); // loading the level throws inside the session's tick

        Assert.Equal(SessionPhase.Aborted, game.GetSession(ana.SessionId).Phase);
        Assert.Contains(messages, m => m.Message.Method == ClientMethods.Error);
        Assert.Empty(healthy.Tick(0.05)); // other facades/sessions are unaffected
    }

    [Fact]
    public void Demo_WinsTheGame()
    {
        var result = new FacadeDemo().Run(DemoOptions.None);

        Assert.Equal(nameof(SessionPhase.Victory), result.Evidence["finalPhase"]);
    }
}
