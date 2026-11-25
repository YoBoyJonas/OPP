namespace CastleEscape.Game.World;

/// <summary>Iterator pattern: one way to walk three different data structures (2D grid, list, hash set).</summary>
public interface IGameIterator<out T>
{
    bool HasNext();
    T Next();
}

/// <summary>Walks the grid's 2D tile array: in-bounds cells within <c>radius</c> of <c>center</c> (a circle), row by row.</summary>
public sealed class GridRadiusIterator(Grid grid, GridPos center, int radius) : IGameIterator<GridPos>
{
    private int _x = center.X - radius;
    private int _y = center.Y - radius;

    public bool HasNext()
    {
        for (; _y <= center.Y + radius; _y++, _x = center.X - radius)
        {
            for (; _x <= center.X + radius; _x++)
            {
                int dx = _x - center.X, dy = _y - center.Y;
                if (dx * dx + dy * dy <= radius * radius && grid.InBounds(new GridPos(_x, _y)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public GridPos Next() => HasNext() ? new GridPos(_x++, _y) : throw new InvalidOperationException("No more cells.");
}

/// <summary>Walks a list by index (the level's zombies, items, levers).</summary>
public sealed class ListIterator<T>(IReadOnlyList<T> list) : IGameIterator<T>
{
    private int _index;

    public bool HasNext() => _index < list.Count;

    public T Next() => HasNext() ? list[_index++] : throw new InvalidOperationException("No more elements.");
}

/// <summary>Walks a hash set (no index, so it steps the set's own enumerator).</summary>
public sealed class SetIterator<T>(HashSet<T> set) : IGameIterator<T>
{
    private HashSet<T>.Enumerator _enumerator = set.GetEnumerator();
    private bool? _hasNext;

    public bool HasNext() => _hasNext ??= _enumerator.MoveNext();

    public T Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more elements.");
        }
        _hasNext = null;
        return _enumerator.Current;
    }
}
