using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.Items;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.World;

public class WorldModelTests
{
    [Fact]
    public void GridPos_Step_UsesScreenCoordinates()
    {
        var pos = new GridPos(5, 5);

        Assert.Equal(new GridPos(5, 4), pos.Step(Direction.Up));
        Assert.Equal(new GridPos(5, 6), pos.Step(Direction.Down));
        Assert.Equal(new GridPos(4, 5), pos.Step(Direction.Left));
        Assert.Equal(new GridPos(6, 5), pos.Step(Direction.Right));
        Assert.Equal(pos, pos.Step(Direction.None));
    }

    [Fact]
    public void GridPos_DirectionTo_NeighbourOnly()
    {
        Assert.Equal(Direction.Right, new GridPos(1, 1).DirectionTo(new GridPos(2, 1)));
        Assert.Equal(Direction.None, new GridPos(1, 1).DirectionTo(new GridPos(3, 1)));
    }

    [Fact]
    public void Grid_OutsideIsWall()
    {
        var grid = new Grid(20, 15);

        Assert.Equal(TerrainKind.Floor, grid.GetTerrain(new GridPos(0, 0)));
        Assert.Equal(TerrainKind.Wall, grid.GetTerrain(new GridPos(-1, 0)));
        Assert.Equal(TerrainKind.Wall, grid.GetTerrain(new GridPos(20, 0)));
    }

    [Fact]
    public void Grid_EachCellHasItsOwnTileObject()
    {
        // The P1 "before" for the P2 Flyweight: one object per cell.
        var grid = new Grid(20, 15);

        Assert.NotSame(grid.GetTile(new GridPos(0, 0)), grid.GetTile(new GridPos(1, 0)));
    }

    [Fact]
    public void Grid_Positions_AreRowMajor()
    {
        var grid = new Grid(3, 2);

        Assert.Equal(
            [new(0, 0), new(1, 0), new(2, 0), new(0, 1), new(1, 1), new(2, 1)],
            grid.Positions());
    }

    [Fact]
    public void MovableEntity_AdvancesAndArrives_WithLeftover()
    {
        var zombie = new ZombieEntity("z", new ZombieDefinition { Id = "z", Speed = 1 }, new GridPos(2, 2));
        zombie.BeginStep(Direction.Right);

        Assert.False(zombie.Advance(0.4, out _));
        Assert.Equal((2.4, 2.0), zombie.RenderPosition);

        Assert.True(zombie.Advance(0.8, out var leftover));
        Assert.Equal(new GridPos(3, 2), zombie.Tile);
        Assert.False(zombie.IsMoving);
        Assert.Equal(0.2, leftover, 6);
    }

    [Fact]
    public void Zombie_ResetToSpawn_CancelsStep()
    {
        var zombie = new ZombieEntity("z", new ZombieDefinition { Id = "z", Speed = 1 }, new GridPos(2, 2));
        zombie.BeginStep(Direction.Down);
        zombie.Advance(1, out _);
        zombie.BeginStep(Direction.Down);

        zombie.ResetToSpawn();

        Assert.Equal(new GridPos(2, 2), zombie.Tile);
        Assert.False(zombie.IsMoving);
    }

    [Fact]
    public void Player_LivesAreCappedAndFloored()
    {
        var player = new PlayerEntity(Guid.NewGuid(), "Ana",
            new CharacterDefinition { Id = "scout", MaxHealth = 3, BaseMoveSpeed = 6 }, new GridPos(1, 1));

        player.GainLives(5);
        Assert.Equal(3, player.Lives);

        player.LoseLives(10);
        Assert.Equal(0, player.Lives);
        Assert.True(player.IsDead);
    }

    [Fact]
    public void LevelState_FindsAndRemovesItems()
    {
        var level = new LevelState(new LevelDefinition { Id = "l", Index = 1 }, seed: 1, new Grid(20, 15));
        var item = ItemSpawners.Spawn(level, new Health { Id = "h", HealthValue = 1 }, new GridPos(4, 4));

        Assert.Same(item, level.ItemAt(new GridPos(4, 4)));
        Assert.True(level.RemoveItem(item));
        Assert.Null(level.ItemAt(new GridPos(4, 4)));
    }

    [Fact]
    public void LevelState_StartTilesMustBeDistinct_Lvl3()
    {
        var level = new LevelState(new LevelDefinition { Id = "l", Index = 1 }, seed: 1, new Grid(20, 15));
        level.AddStartTile(new GridPos(1, 1));

        Assert.Throws<InvalidOperationException>(() => level.AddStartTile(new GridPos(1, 1)));
    }
}
