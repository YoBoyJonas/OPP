namespace CastleEscape.Contracts.Diagnostics;

/// <summary>Game loop timings (<c>GET /api/diagnostics/loop</c>).</summary>
/// <param name="TickRate">Configured ticks per second.</param>
/// <param name="Sessions">Sessions known to the server.</param>
/// <param name="ActiveSessions">Sessions still being ticked (not finished).</param>
/// <param name="LoopIterations">Loop iterations since start.</param>
/// <param name="AverageTickMilliseconds">Average time to tick all sessions once, over the last 200 iterations.</param>
/// <param name="MaxTickMilliseconds">Slowest iteration since start.</param>
/// <param name="FailedSessionTicks">Session ticks that threw and were isolated.</param>
public sealed record LoopDiagnosticsResponse(
    int TickRate,
    int Sessions,
    int ActiveSessions,
    long LoopIterations,
    double AverageTickMilliseconds,
    double MaxTickMilliseconds,
    long FailedSessionTicks);
