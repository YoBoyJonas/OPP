namespace CastleEscape.Domain.Sessions;

/// <summary>Lives and score of each player when the level started; restart goes back to these (D8).</summary>
public sealed record LevelCheckpoint(IReadOnlyDictionary<Guid, (int Lives, int Score)> Players);
