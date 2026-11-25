using CastleEscape.Game.Configuration;
using CastleEscape.Game.Tests.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class IteratorTests
{
    private static List<T> Drain<T>(IGameIterator<T> it)
    {
        var result = new List<T>();
        while (it.HasNext())
        {
            result.Add(it.Next());
        }
        return result;
    }

    [Fact]
    public void OneInterface_WalksThreeDataStructures()
    {
        // 1. 2D grid: radius 1 around a corner is clipped to the 3 in-bounds cells of the "plus" shape.
        var cells = Drain<GridPos>(new GridRadiusIterator(new Grid(5, 5), new GridPos(0, 0), 1));
        Assert.Equal([new(0, 0), new(1, 0), new(0, 1)], cells);

        // 2. List
        Assert.Equal(["a", "b"], Drain<string>(new ListIterator<string>(["a", "b"])));

        // 3. Hash set
        Assert.Equal([1, 2, 3], Drain<int>(new SetIterator<int>([3, 1, 2])).Order());

        var empty = new ListIterator<int>([]);
        Assert.False(empty.HasNext());
        Assert.Throws<InvalidOperationException>(() => empty.Next());
    }

    [Fact]
    public void ViewRadius_HidesFarTilesAndEntities()
    {
        var game = TestGame.Start([
            "####################",
            "#1.2.............Z.#",
            "####################",
        ], new GameOptions { LevelTransitionSeconds = 0, ViewRadius = 3 });

        var state = game.State;
        Assert.Empty(state.Zombies);
        Assert.Equal(2, state.Players.Length);
        Assert.Equal("#......", state.VisibleRows![1][..7]); // within 3 tiles of a player: terrain (players are sent as entities)
        Assert.Equal(' ', state.VisibleRows[1][17]);       // far tile: fog

        var full = TestGame.Start(["#####", "#1.2#", "#####"]);
        Assert.Null(full.State.VisibleRows);                    // radius 0 = whole map, as before
    }
}
