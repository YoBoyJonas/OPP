using CasteEscapeServer.Models;

namespace CasteEscapeServer.Game;

// ponytail: fixed/deterministic layouts, not a randomized generator with a
// reachability solver. Good enough for the communication + core-rules pass;
// swap in a real generator if random levels are ever required.
public static class LevelBuilder
{
    private const int Width = 22;
    private const int Height = 16;

    public static LevelState Build(int levelIndex)
    {
        var grid = new CellType[Width, Height];
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var isBorder = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                grid[x, y] = isBorder ? CellType.Wall : CellType.Empty;
            }
        }

        // Obstacle band crossing the room; alternates Water/Pit by level so both
        // power gates get exercised. One gap column keeps the room guaranteed-
        // reachable without a full pathfinding solver.
        var obstacleType = levelIndex % 2 == 1 ? CellType.Water : CellType.Pit;
        var midY = Height / 2;
        var gapX = Width - 3;
        for (var x = 2; x < Width - 2; x++)
        {
            if (x == gapX)
            {
                continue;
            }
            grid[x, midY] = obstacleType;
        }

        var level = new LevelState
        {
            Index = levelIndex,
            Width = Width,
            Height = Height,
            Grid = grid,
            PlayerSpawns = new[] { (1, 1), (1, Height - 2) },
            ExitCells = new List<(int X, int Y)> { (Width - 2, 1), (Width - 2, 2) }
        };

        level.Levers.Add(new LeverState { Id = "lever-1", X = 2, Y = Height - 2 });
        level.Levers.Add(new LeverState { Id = "lever-2", X = Width - 3, Y = Height - 2 });

        var zombieCount = levelIndex; // grows with level, per spec
        for (var i = 0; i < zombieCount; i++)
        {
            var x = 3 + i % (Width - 6);
            const int y = Height - 3;
            level.Zombies.Add(new ZombieState
            {
                Id = $"zombie-{i}",
                X = x,
                Y = y,
                HomeX = x,
                HomeY = y,
                Speed = 2.0,
                ContactDamage = 1
            });
        }

        var itemCount = 3 + levelIndex / 3;
        for (var i = 0; i < itemCount; i++)
        {
            var x = 3 + i * 2 % (Width - 6);
            const int y = 3;
            var kind = (ItemKind)(i % 3);
            var item = new ItemState { Id = $"item-{i}", X = x, Y = y, Kind = kind };
            if (kind == ItemKind.Power)
            {
                item.GrantedPower = obstacleType == CellType.Water ? PowerType.Swim : PowerType.Jump;
                item.GrantDurationSeconds = 10;
            }
            else if (kind == ItemKind.Reward)
            {
                item.ScoreValue = 10;
            }
            else
            {
                item.HealthValue = 1;
            }
            level.Items.Add(item);
        }

        return level;
    }
}
