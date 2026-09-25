using CastleEscape.Contracts;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.World;

/// <summary>
/// One generated level: terrain plus everything placed on it. Players are not part of it;
/// they belong to the session and move from level to level.
/// Collections are private and changed only through methods (seam for P2 Iterator/Composite/Visitor).
/// A freshly built level is kept as the prototype: every play and restart runs on a clone of it.
/// </summary>
[DesignPattern("Prototype", "ConcretePrototype")]
public class LevelState(LevelDefinition definition, int seed, Grid grid) : ILevelPrototype<LevelState>
{
    private readonly List<ItemEntity> _items = [];
    private readonly List<ZombieEntity> _zombies = [];
    private readonly List<Lever> _levers = [];
    private readonly List<GridPos> _exitTiles = [];
    private readonly List<GridPos> _startTiles = [];
    private readonly Dictionary<string, int> _idCounters = [];

    public LevelDefinition Definition { get; } = definition;
    public int Index => Definition.Index;
    public LevelTheme Theme => Definition.Theme;
    public int Seed { get; } = seed;
    public Grid Grid { get; } = grid;

    public ExitDoor? Door { get; private set; }

    public IReadOnlyList<ItemEntity> Items => _items;
    public IReadOnlyList<ZombieEntity> Zombies => _zombies;
    public IReadOnlyList<Lever> Levers => _levers;
    public IReadOnlyList<GridPos> ExitTiles => _exitTiles;

    /// <summary>Player 1's and player 2's start tiles, in that order.</summary>
    public IReadOnlyList<GridPos> StartTiles => _startTiles;

    /// <summary>A new entity id unique in this level, e.g. "item-3".</summary>
    public string NextEntityId(string prefix)
    {
        var next = _idCounters.GetValueOrDefault(prefix) + 1;
        _idCounters[prefix] = next;
        return $"{prefix}-{next}";
    }

    public void AddItem(ItemEntity item) => _items.Add(item);
    public bool RemoveItem(ItemEntity item) => _items.Remove(item);
    public ItemEntity? ItemAt(GridPos pos) => _items.FirstOrDefault(i => i.Tile == pos);

    public void AddZombie(ZombieEntity zombie) => _zombies.Add(zombie);

    public void AddLever(Lever lever) => _levers.Add(lever);
    public Lever? LeverAt(GridPos pos) => _levers.FirstOrDefault(l => l.Tile == pos);

    public void SetDoor(ExitDoor door) => Door = door;

    public void AddExitTile(GridPos pos) => _exitTiles.Add(pos);
    public bool IsExitTile(GridPos pos) => _exitTiles.Contains(pos);

    // ---------------------------------------------------------------- Prototype

    public LevelState Clone(CloneMode mode) => mode == CloneMode.Deep ? DeepClone() : ShallowClone();

    /// <summary>
    /// Copies only this object: the grid, the lists and the entities in them are shared with the original.
    /// Playing on it changes the original too (collected items stay collected after a restart).
    /// </summary>
    public LevelState ShallowClone() => (LevelState)MemberwiseClone();

    /// <summary>
    /// A fully independent level: its own grid array, lists and entity objects. Shared: the immutable
    /// tiles and content definitions, which nothing changes during play.
    /// </summary>
    public LevelState DeepClone()
    {
        var copy = new LevelState(Definition, Seed, Grid.Copy());
        copy._items.AddRange(_items.Select(i => (ItemEntity)i.CloneEntity()));
        copy._zombies.AddRange(_zombies.Select(z => (ZombieEntity)z.CloneEntity()));
        copy._levers.AddRange(_levers.Select(l => (Lever)l.CloneEntity()));
        copy._exitTiles.AddRange(_exitTiles);
        copy._startTiles.AddRange(_startTiles);
        foreach (var (prefix, count) in _idCounters)
        {
            copy._idCounters[prefix] = count;
        }
        copy.Door = (ExitDoor?)Door?.CloneEntity();
        return copy;
    }

    public void AddStartTile(GridPos pos)
    {
        if (_startTiles.Count == 2)
        {
            throw new InvalidOperationException("A level has exactly two start tiles.");
        }
        if (_startTiles.Contains(pos))
        {
            throw new InvalidOperationException($"Start tiles must be distinct (LVL-3); {pos} is used twice.");
        }
        _startTiles.Add(pos);
    }
}
