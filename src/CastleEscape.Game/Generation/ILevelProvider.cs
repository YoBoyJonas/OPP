using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation;

public interface ILevelProvider
{
    LevelState CreateLevel(int levelIndex, int seed);
}

public class LevelGenerationException(int levelIndex, IReadOnlyList<string> errors)
    : Exception($"Could not generate level {levelIndex}: {string.Join("; ", errors)}")
{
    public int LevelIndex { get; } = levelIndex;
    public IReadOnlyList<string> Errors { get; } = errors;
}
