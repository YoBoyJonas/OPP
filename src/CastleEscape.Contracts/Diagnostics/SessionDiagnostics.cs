namespace CastleEscape.Contracts.Diagnostics;

/// <summary>A command in a session's history (Command pattern).</summary>
/// <param name="Sequence">Execution order key: the input sequence it came from.</param>
/// <param name="Name">e.g. "SetDirection Right", "StartStep Left", "RestartLevel".</param>
/// <param name="PlayerId">The player it was for, if any.</param>
/// <param name="Tick">Tick it ran in.</param>
/// <param name="Undone">Whether it has been undone (D7 conflict or an undo request).</param>
public sealed record CommandRecordDto(long Sequence, string Name, Guid? PlayerId, long Tick, bool Undone);
