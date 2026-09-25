using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Sessions;

/// <summary>Game rules on tiny hand-made maps (legend: content/presets/README.md).</summary>
public class GameRulesTests
{
    [Fact]
    public void Movement_WallBlocks_Col1()
    {
        var game = TestGame.Start(["#####", "#1.2#", "#####"]);

        game.Hold(game.P1, Direction.Left);
        game.RunSeconds(1);

        Assert.Equal(new GridPos(1, 1), game.TileOf(game.P1));
    }

    [Fact]
    public void Movement_ConstantSpeedGridSteps_Mov1()
    {
        // Scout: 6 tiles/s, so ~0.5 s for 3 tiles.
        var game = TestGame.Start(["##########", "#1......2#", "##########"]);

        game.Hold(game.P1, Direction.Right);
        game.RunSeconds(0.5);

        Assert.InRange(game.TileOf(game.P1).X, 3, 4);
        Assert.Equal(1, game.TileOf(game.P1).Y);
    }

    [Fact]
    public void Movement_WaterBlocksWithoutSwim_Mov2()
    {
        var game = TestGame.Start(["########", "#1~...2#", "########"]);

        game.Hold(game.P1, Direction.Right);
        game.RunSeconds(1);

        Assert.Equal(new GridPos(1, 1), game.TileOf(game.P1));
    }

    [Fact]
    public void Movement_SwimCrossesWater_Mov2()
    {
        var game = TestGame.Start(["########", "#1w~..2#", "########"]);

        game.Hold(game.P1, Direction.Right);
        game.RunSeconds(3);

        Assert.Equal(new GridPos(5, 1), game.TileOf(game.P1)); // stops next to player 2 (D7)
        Assert.Single(game.EventsOf(GameEventTypes.ItemCollected));
    }

    [Fact]
    public void Movement_JumpCrossesPit_Mov2()
    {
        var game = TestGame.Start(["########", "#1jO..2#", "########"]);

        game.Hold(game.P1, Direction.Right);
        game.RunSeconds(3);

        Assert.Equal(new GridPos(5, 1), game.TileOf(game.P1));
    }

    [Fact]
    public void Movement_PitBlocksWithoutJump_Mov2()
    {
        var game = TestGame.Start(["########", "#1O...2#", "########"]);

        game.Hold(game.P1, Direction.Right);
        game.RunSeconds(1);

        Assert.Equal(new GridPos(1, 1), game.TileOf(game.P1));
    }

    [Fact]
    public void Movement_PlayersNeverShareATile_D7()
    {
        var game = TestGame.Start(["#####", "#1.2#", "#####"]);

        game.Hold(game.P1, Direction.Right);
        game.Hold(game.P2, Direction.Left);
        game.RunSeconds(1);

        Assert.Equal(new GridPos(2, 1), game.TileOf(game.P1)); // player 1 claims the tile first
        Assert.Equal(new GridPos(3, 1), game.TileOf(game.P2));
    }

    [Fact]
    public void Movement_PlayersMoveIndependently_Mov3()
    {
        var game = TestGame.Start(["#######", "#1...2#", "#.....#", "#######"]);

        game.Hold(game.P1, Direction.Down);
        game.Hold(game.P2, Direction.Down);
        game.RunSeconds(0.5);

        Assert.Equal(new GridPos(1, 2), game.TileOf(game.P1));
        Assert.Equal(new GridPos(5, 2), game.TileOf(game.P2));
    }

    [Fact]
    public void Items_HealthIsCappedAndRewardScores_Itm2()
    {
        var game = TestGame.Start(["#########", "#1hr...2#", "#########"]);

        game.Hold(game.P1, Direction.Right);
        game.RunSeconds(1);

        var p1 = game.Player(game.P1);
        Assert.Equal(3, p1.Lives); // scout max is 3 (D14)
        Assert.Equal(10, p1.Score);
        Assert.Empty(game.State.Items); // collected items leave the map
        Assert.Equal(2, game.Session.Snapshot.Hud.Players.Single(p => p.Slot == 1).Stats[GameEventTypes.ItemCollected]);
    }

    [Fact]
    public void Powers_ExpireAfterDuration_Pwr1()
    {
        var game = TestGame.Start(["#########", "#1s....2#", "#########"],
            new GameOptions { PowerDurationSeconds = 1, LevelTransitionSeconds = 0 });

        game.Hold(game.P1, Direction.Right);
        game.RunUntil(() => game.Player(game.P1).Powers.Count == 1);
        game.RunSeconds(1.2);

        Assert.Empty(game.Player(game.P1).Powers);
        Assert.Single(game.EventsOf(GameEventTypes.PowerExpired));
    }

    [Fact]
    public void Powers_SamePowerStacks_Pwr3()
    {
        var game = TestGame.Start(["##########", "#1ss....2#", "##########"]);

        game.Hold(game.P1, Direction.Right);
        game.RunUntil(() => game.Player(game.P1).Powers is [{ Level: 2 }]);

        var sprint = game.Player(game.P1).Powers.Single();
        Assert.Equal(PowerType.Sprint, sprint.Power);
        Assert.True(sprint.RemainingSeconds > 15, "second pickup extends the duration");
    }

    [Theory]
    [InlineData('j', 's', PowerType.JumpDash)]
    [InlineData('s', 'w', PowerType.FastSwim)]
    public void Powers_TwoDifferentPowersMakeACombo_Pwr2(char first, char second, PowerType combo)
    {
        var game = TestGame.Start(["##########", $"#1{first}{second}....2#", "##########"]);

        game.Hold(game.P1, Direction.Right);
        game.RunUntil(() => game.Player(game.P1).Combos.Count > 0);

        Assert.Equal([combo], game.Player(game.P1).Combos);
        Assert.Single(game.EventsOf(GameEventTypes.ComboActivated));
    }

    [Fact]
    public void Zombie_ContactCostsALifeAndResetsZombie_Zmb2_Zmb3()
    {
        var game = TestGame.Start(["##########", "#1....Z.2#", "##########"]);

        game.RunUntil(() => game.EventsOf(GameEventTypes.LifeLost).Any());

        Assert.Equal(2, game.Player(game.P2).Lives);
        var zombie = game.State.Zombies.Single();
        Assert.Equal((6, 1), (zombie.TileX, zombie.TileY)); // back on its spawn tile
    }

    private static readonly string[] WallBetween =
    [
        "##########",
        "#1..#..Z.#",
        "#...#....#",
        "#...#....#",
        "#........#",
        "#2.......#",
        "##########",
    ];

    [Fact]
    public void Zombie_BfsChasesAroundWalls_Zmb1()
    {
        // Level 6's first zombie type is the BFS "hunter". Both players are on the far side of the wall,
        // so any catch means it found the way around. (It retargets whichever player is nearest as it moves.)
        var levels = new RowsLevelProvider(TestGame.Catalog, WallBetween) { DefinitionIndex = 6 };
        var game = TestGame.Start(levels);

        game.RunUntil(() => game.EventsOf(GameEventTypes.LifeLost).Any(), maxSeconds: 15);
    }

    [Fact]
    public void Zombie_GreedyGetsStuckBehindWalls()
    {
        // Level 1's "shambler" is greedy: it walks left until the wall and stays there.
        var game = TestGame.Start(WallBetween);

        game.RunSeconds(5);

        Assert.Empty(game.EventsOf(GameEventTypes.LifeLost));
        Assert.Equal(5, game.State.Zombies.Single().TileX);
    }

    [Fact]
    public void Defeat_WhenAnyPlayerHasNoLives_Plr3()
    {
        var game = TestGame.Start(["##########", "#1....Z.2#", "##########"]);

        game.RunUntil(() => game.Session.Phase == SessionPhase.Defeat, maxSeconds: 30);

        Assert.Equal(0, game.Player(game.P2).Lives);
        Assert.Single(game.EventsOf(GameEventTypes.GameLost));
    }

    private static readonly string[] LeverRoom =
    [
        "#########",
        "#1L...L2#",
        "#.......#",
        "####D####",
        "###.EE.##",
        "#########",
    ];

    [Fact]
    public void Door_OpensWhenBothLeversActive_AndLatches_D1()
    {
        var game = TestGame.Start(LeverRoom);

        game.WalkTo(game.P1, new GridPos(2, 1));
        Assert.False(game.State.DoorOpen); // one lever is not enough
        game.WalkTo(game.P2, new GridPos(6, 1));
        game.Tick();
        Assert.True(game.State.DoorOpen);

        game.WalkTo(game.P1, new GridPos(3, 2));
        game.RunSeconds(0.5);
        Assert.True(game.State.DoorOpen); // latched
        Assert.Single(game.EventsOf(GameEventTypes.DoorOpened));
    }

    [Fact]
    public void Door_HoldWhileActive_ClosesWhenALeverIsReleased()
    {
        var game = TestGame.Start(LeverRoom, new GameOptions { DoorMode = DoorMode.HoldWhileActive, LevelTransitionSeconds = 0 });

        game.WalkTo(game.P1, new GridPos(2, 1));
        game.WalkTo(game.P2, new GridPos(6, 1));
        game.Tick();
        Assert.True(game.State.DoorOpen);

        game.WalkTo(game.P1, new GridPos(3, 2));

        Assert.False(game.State.DoorOpen);
    }

    [Fact]
    public void Exit_NeedsBothPlayers_Door2()
    {
        var game = TestGame.Start(LeverRoom, moreLevels: [LeverRoom]);
        game.WalkTo(game.P1, new GridPos(2, 1));
        game.WalkTo(game.P2, new GridPos(6, 1));

        game.WalkTo(game.P1, new GridPos(5, 4));
        game.RunSeconds(0.5);
        Assert.Equal(SessionPhase.Playing, game.Session.Phase); // one player on the exit is not enough

        game.WalkTo(game.P2, new GridPos(4, 4));
        game.RunUntil(() => game.Session.Snapshot.Session.LevelIndex == 2);

        Assert.Single(game.EventsOf(GameEventTypes.LevelCompleted));
        Assert.Equal(2, game.Messages.Select(m => m.Payload).OfType<LevelStartedMessage>().Count());
    }

    [Fact]
    public void Victory_AfterTheLastLevel_Win1()
    {
        var game = TestGame.Start(LeverRoom, new GameOptions { MaxLevel = 1, LevelTransitionSeconds = 0 });
        game.WalkTo(game.P1, new GridPos(2, 1));
        game.WalkTo(game.P2, new GridPos(6, 1));
        game.WalkTo(game.P1, new GridPos(5, 4));
        game.WalkTo(game.P2, new GridPos(4, 4));

        game.RunUntil(() => game.Session.Phase == SessionPhase.Victory);

        Assert.Single(game.EventsOf(GameEventTypes.GameWon));
    }

    [Fact]
    public void Restart_RestoresItemsAndScore_D8()
    {
        var game = TestGame.Start(["#########", "#1.r...2#", "#########"]);
        game.WalkTo(game.P1, new GridPos(3, 1));
        Assert.Equal(10, game.Player(game.P1).Score);
        Assert.Empty(game.State.Items);

        game.Session.RequestRestart(game.P2.PlayerId);
        game.Tick();

        Assert.Equal(0, game.Player(game.P1).Score);
        Assert.Single(game.State.Items);
        Assert.Equal(new GridPos(1, 1), game.TileOf(game.P1));
        Assert.Single(game.EventsOf(GameEventTypes.LevelRestarted));
    }

    [Fact]
    public void Leave_AbortsTheSession_D10()
    {
        var game = TestGame.Start(["#####", "#1.2#", "#####"]);

        game.Session.Leave(game.P2.PlayerId);

        Assert.Equal(SessionPhase.Aborted, game.Session.Phase);
    }
}
