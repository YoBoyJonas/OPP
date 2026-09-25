namespace CasteEscapeServer.Models;

public class LevelState
{
    public int Index { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public CellType[,] Grid { get; set; } = new CellType[0, 0];
    public (int X, int Y)[] PlayerSpawns { get; set; } = new (int, int)[2];
    public List<(int X, int Y)> ExitCells { get; set; } = new();
    public List<LeverState> Levers { get; set; } = new();
    public List<ItemState> Items { get; set; } = new();
    public List<ZombieState> Zombies { get; set; } = new();

    public CellType CellAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return CellType.Wall;
        }
        return Grid[x, y];
    }
}
