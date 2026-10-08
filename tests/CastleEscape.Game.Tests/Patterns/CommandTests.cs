using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Commands;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.Tests.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class CommandTests
{
    private static ContentCatalog Catalog => ContentCatalog.Instance;

    /// <summary>A world on the command demo map with both players placed, outside any session.</summary>
    private static (GameWorld World, PlayerEntity Ana, PlayerEntity Ben) World()
    {
        var ana = new PlayerSlot(Guid.NewGuid(), "t1", "Ana", 1) { CharacterId = "scout" };
        var ben = new PlayerSlot(Guid.NewGuid(), "t2", "Ben", 2) { CharacterId = "scout" };
        var level = new RowsLevelProvider(Catalog, CommandDemo.Map).CreateLevel(1, 0);
        ana.Entity = new PlayerEntity(ana.PlayerId, "Ana", Catalog.GetCharacter("scout"), level.StartTiles[0]);
        ben.Entity = new PlayerEntity(ben.PlayerId, "Ben", Catalog.GetCharacter("scout"), level.StartTiles[1]);
        var world = new GameWorld([ana, ben], new GameOptions(), new PatternOptions());
        world.BeginLevel(level);
        return (world, ana.Entity, ben.Entity);
    }

    [Fact]
    public void SetDirection_UndoRestoresThePreviousKey()
    {
        var (world, ana, _) = World();
        ana.HeldDirection = Direction.Up;
        var command = new SetDirectionCommand(7, ana.PlayerId, Direction.Right);

        Assert.True(command.Execute(world));
        Assert.Equal(Direction.Right, ana.HeldDirection);
        Assert.Equal(7, ana.DirectionSequence);

        command.Undo(world);
        Assert.Equal(Direction.Up, ana.HeldDirection);
        Assert.Equal(0, ana.DirectionSequence);
    }

    [Fact]
    public void StartStep_UndoCancelsTheStep()
    {
        var (world, ana, _) = World();
        var command = new StartStepCommand(1, ana.PlayerId, Direction.Right);

        Assert.True(command.Execute(world));
        Assert.Equal(new GridPos(2, 1), ana.NextTile);

        command.Undo(world);
        Assert.False(ana.IsMoving);
        Assert.Equal(new GridPos(1, 1), ana.Tile);
    }

    [Fact]
    public void RestartLevel_UndoBringsBackTheLevelBeingPlayed()
    {
        var (world, ana, ben) = World();
        var played = world.Level!;
        played.RemoveItem(played.Items[0]);
        ben.AddScore(10);
        ben.ReturnTo(new GridPos(5, 1));

        var restart = new RestartLevelCommand(1, ana.PlayerId);
        Assert.True(restart.Execute(world));
        Assert.NotSame(played, world.Level);
        Assert.Single(world.Level!.Items);
        Assert.Equal(0, ben.Score);
        Assert.Equal(new GridPos(3, 1), ben.Tile);

        restart.Undo(world);
        Assert.Same(played, world.Level);
        Assert.Empty(world.Level.Items);
        Assert.Equal(10, ben.Score);
        Assert.Equal(new GridPos(5, 1), ben.Tile);
    }

    [Fact]
    public void GivePower_UndoRestoresThePreviousPowerState()
    {
        var (world, ana, _) = World();
        var sprint = Catalog.Consumables.First(c => c.Grant?.Power == PowerType.Sprint).Grant!;
        ana.Powers.Gain(sprint, 5, maxLevel: 3);

        var command = new GivePowerCommand(1, ana.PlayerId, sprint, 10, 3, Catalog.Combos);
        command.Execute(world);
        Assert.Equal(2, ana.Powers.Get(PowerType.Sprint)!.Level);

        command.Undo(world);
        Assert.Equal(1, ana.Powers.Get(PowerType.Sprint)!.Level);
        Assert.Equal(5, ana.Powers.Get(PowerType.Sprint)!.RemainingSeconds);

        var jump = new GivePowerCommand(2, ana.PlayerId, Catalog.Consumables.First(c => c.Grant?.Power == PowerType.Jump).Grant!, 10, 3, Catalog.Combos);
        jump.Execute(world);
        Assert.Contains(ana.Powers.Combos, c => c.Granted == PowerType.JumpDash);
        jump.Undo(world);
        Assert.Null(ana.Powers.Get(PowerType.Jump));
        Assert.Empty(ana.Powers.Combos);
    }

    [Fact]
    public void Processor_RunsQueuedCommandsInSequenceOrder()
    {
        var (world, ana, _) = World();
        var processor = new CommandProcessor();
        processor.Enqueue(new SetDirectionCommand(2, ana.PlayerId, Direction.Down));
        processor.Enqueue(new SetDirectionCommand(1, ana.PlayerId, Direction.Up));

        processor.ExecutePending(world, tick: 1);

        Assert.Equal(Direction.Down, ana.HeldDirection); // sequence 2 ran last
        Assert.Equal([1L, 2L], processor.History().Select(r => r.Sequence));
    }

    [Fact]
    public void Processor_KeepsABoundedHistory_AndUndoesPerPlayer()
    {
        var (world, ana, ben) = World();
        var processor = new CommandProcessor();
        for (var i = 1; i <= CommandProcessor.HistoryLimit + 20; i++)
        {
            processor.Execute(new SetDirectionCommand(i, i % 2 == 0 ? ana.PlayerId : ben.PlayerId, Direction.Up), world, tick: i);
        }

        Assert.Equal(CommandProcessor.HistoryLimit, processor.History().Count);

        var undone = processor.UndoLast(world, ana.PlayerId);
        Assert.Equal(ana.PlayerId, undone!.PlayerId);
        Assert.Equal(CommandProcessor.HistoryLimit + 20, undone.Sequence);
        Assert.Contains(world.Events, e => e.Type == GameEventTypes.CommandUndone);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TileConflict_TheLaterInputIsUndone_D7(bool anaFirst)
    {
        var game = TestGame.Start(["#####", "#1.2#", "#####"]);
        if (anaFirst)
        {
            game.Hold(game.P1, Direction.Right);
            game.Hold(game.P2, Direction.Left);
        }
        else
        {
            game.Hold(game.P2, Direction.Left);
            game.Hold(game.P1, Direction.Right);
        }

        game.RunSeconds(1);

        var winner = anaFirst ? game.P1 : game.P2;
        Assert.Equal(new GridPos(2, 1), game.TileOf(winner));
        var undone = Assert.Single(game.Session.CommandHistory(), r => r.Undone);
        Assert.Equal(anaFirst ? game.P2.PlayerId : game.P1.PlayerId, undone.PlayerId);
        Assert.Single(game.EventsOf(GameEventTypes.CommandUndone));
    }

    [Fact]
    public void Session_UndoOnlyWhilePlaying()
    {
        var session = new GameSession(Guid.NewGuid(), "X", Catalog, new RowsLevelProvider(Catalog, CommandDemo.Map), new GameOptions(), 1);
        session.AddPlayer("Ana");

        var ex = Assert.Throws<GameException>(() => session.UndoLastCommand());

        Assert.Equal(GameErrorCode.WrongPhase, ex.Code);
    }

    [Fact]
    public void Demo_UndoesAConflictAndARestart()
    {
        var result = new CommandDemo().Run(DemoOptions.None);

        Assert.Equal(1, result.Evidence["undoneByConflict"]);
        Assert.Equal("RestartLevel", result.Evidence["undoneRestart"]);
    }
}
