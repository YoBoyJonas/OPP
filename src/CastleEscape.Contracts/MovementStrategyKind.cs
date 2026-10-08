namespace CastleEscape.Contracts;

/// <summary>How a zombie chooses its next step towards the nearest player.</summary>
public enum MovementStrategyKind
{
    /// <summary>Step that most reduces Manhattan distance; can get stuck behind walls.</summary>
    Greedy,

    /// <summary>First step of a breadth-first shortest path.</summary>
    Bfs,

    /// <summary>First step of an A* shortest path (Manhattan heuristic).</summary>
    AStar,

    /// <summary>Heads for the tile the player is moving towards, a few tiles ahead.</summary>
    Predictive
}
