namespace CastleEscape.Contracts.Diagnostics;

/// <summary>A command in a session's history (Command pattern).</summary>
/// <param name="Sequence">Execution order key: the input sequence it came from.</param>
/// <param name="Name">e.g. "SetDirection Right", "StartStep Left", "RestartLevel".</param>
/// <param name="PlayerId">The player it was for, if any.</param>
/// <param name="Tick">Tick it ran in.</param>
/// <param name="Undone">Whether it has been undone (D7 conflict or an undo request).</param>
public sealed record CommandRecordDto(long Sequence, string Name, Guid? PlayerId, long Tick, bool Undone);

/// <summary>A game event from a session's event log (Observer pattern: <c>EventLogObserver</c>).</summary>
/// <param name="Tick">Tick it was published in.</param>
/// <param name="At">Server time it was published.</param>
/// <param name="Type">Event type, as in <c>GameEventTypes</c>.</param>
/// <param name="PlayerId">The player it concerns, if any.</param>
/// <param name="Message">Human-readable text.</param>
/// <param name="Data">The event's own fields.</param>
public sealed record EventLogEntryDto(long Tick, DateTimeOffset At, string Type, Guid? PlayerId, string Message, Dictionary<string, string> Data);
