using CastleEscape.Contracts;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.Tests.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class PrototypeTests
{
    [Fact]
    public void DeepClone_CopiesEveryMutablePart_AndSharesImmutableOnes()
    {
        var original = DemoWorld.TutorialLevel();

        var clone = original.Clone(CloneMode.Deep);

        Assert.NotSame(original, clone);
        Assert.NotSame(original.Grid, clone.Grid);
        Assert.NotSame(original.Items, clone.Items);
        Assert.All(original.Items.Zip(clone.Items), p => Assert.NotSame(p.First, p.Second));
        Assert.All(original.Levers.Zip(clone.Levers), p => Assert.NotSame(p.First, p.Second));
        Assert.NotSame(original.Door, clone.Door);
        Assert.Same(original.Definition, clone.Definition);                                             // read-only content
        Assert.Same(original.Grid.GetTile(new GridPos(0, 0)), clone.Grid.GetTile(new GridPos(0, 0))); // immutable tile
        Assert.Equal(SnapshotMapper.ToRows(original), SnapshotMapper.ToRows(clone));                   // same content
        Assert.Equal(original.Items.Select(i => i.GetType()), clone.Items.Select(i => i.GetType()));   // same product classes
    }

    [Fact]
    public void ShallowClone_SharesEverythingButTheLevelObject()
    {
        var original = DemoWorld.TutorialLevel();

        var clone = original.Clone(CloneMode.Shallow);

        Assert.NotSame(original, clone);
        Assert.Same(original.Grid, clone.Grid);
        Assert.Same(original.Items, clone.Items);
        Assert.Same(original.Levers[0], clone.Levers[0]);
    }

    [Theory]
    [InlineData(CloneMode.Deep, false)]
    [InlineData(CloneMode.Shallow, true)]
    public void PlayingOnTheClone_ChangesTheOriginalOnlyWhenShallow(CloneMode mode, bool originalChanges)
    {
        var original = DemoWorld.TutorialLevel();
        var clone = original.Clone(mode);

        clone.RemoveItem(clone.Items[0]);
        clone.Levers[0].IsActive = true;
        clone.Grid.SetTile(new GridPos(5, 5), TerrainKind.Wall);

        Assert.Equal(originalChanges, original.Items.Count == clone.Items.Count);
        Assert.Equal(originalChanges, original.Levers[0].IsActive);
        Assert.Equal(originalChanges, original.Grid.GetTerrain(new GridPos(5, 5)) == TerrainKind.Wall);
    }

    [Fact]
    public void DeepClone_EntitiesMoveIndependently()
    {
        var original = DemoWorld.TutorialLevel();
        var zombie = new DungeonZombie("z", DemoWorld.Catalog.Zombies[0], new GridPos(3, 5));
        original.AddZombie(zombie);

        var clone = original.DeepClone();
        clone.Zombies[0].BeginStep(Direction.Right);
        clone.Zombies[0].Advance(1, out _);

        Assert.Equal(new GridPos(4, 5), clone.Zombies[0].Tile);
        Assert.Equal(new GridPos(3, 5), original.Zombies[0].Tile);
        Assert.IsType<DungeonZombie>(clone.Zombies[0]);
    }

    [Theory]
    [InlineData(CloneMode.Deep, 1)]
    [InlineData(CloneMode.Shallow, 0)]
    public void SessionRestart_UsesTheConfiguredCloneMode(CloneMode mode, int itemsAfterRestart)
    {
        var patterns = new PatternOptions { PrototypeCloneMode = mode };
        var game = TestGame.Start(new RowsLevelProvider(TestGame.Catalog, ["#########", "#1.r...2#", "#########"]),
            patterns: patterns);
        game.WalkTo(game.P1, new GridPos(3, 1));
        Assert.Empty(game.State.Items);

        game.Session.RequestRestart(game.P2.PlayerId);
        game.Tick();

        Assert.Equal(itemsAfterRestart, game.State.Items.Length);
    }

    [Fact]
    public void Addresses_AreEqualForTheSameObjectOnly()
    {
        var a = new object();
        var b = new object();

        var (sameA, aVsB) = ObjectAddress.WhileGcPaused(() => (ObjectAddress.Of(a) == ObjectAddress.Of(a), ObjectAddress.Of(a) == ObjectAddress.Of(b)));

        Assert.True(sameA);
        Assert.False(aVsB);
    }

    [Fact]
    public void Demo_ShowsShallowRestartBug()
    {
        var result = new PrototypeDemo().Run(DemoOptions.None);

        var modes = Assert.IsType<Dictionary<string, object?>>(result.Evidence["modes"]);
        Assert.True((bool)((Dictionary<string, object?>)modes["Deep"]!)["restartRestoresLevel"]!);
        Assert.False((bool)((Dictionary<string, object?>)modes["Shallow"]!)["restartRestoresLevel"]!);
    }
}
