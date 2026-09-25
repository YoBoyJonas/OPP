using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

/// <summary>
/// Where a session gets its levels. The session never calls the generator directly
/// (seam for the P2 Proxy: lazy, protection and logging providers).
/// </summary>
public interface ILevelProvider
{
    /// <summary>Creates level <paramref name="levelIndex"/> (1-based). The same seed gives the same level.</summary>
    /// <exception cref="LevelGenerationException">No valid level could be made.</exception>
    LevelState CreateLevel(int levelIndex, int seed);
}

/// <summary>Generation gave up after the configured number of attempts.</summary>
public class LevelGenerationException(int levelIndex, IReadOnlyList<string> errors)
    : Exception($"Could not generate level {levelIndex}: {string.Join("; ", errors)}")
{
    public int LevelIndex { get; } = levelIndex;
    public IReadOnlyList<string> Errors { get; } = errors;
}
