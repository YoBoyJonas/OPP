using System.Diagnostics;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Messaging;
using CastleEscape.Game.Sessions;
using Microsoft.Extensions.Options;

namespace CastleEscape.Server.Realtime;

/// <summary>Timing numbers for the diagnostics endpoint. Updated by the loop only.</summary>
public sealed class GameLoopStats
{
    private readonly Queue<double> _recent = new();
    private readonly object _gate = new();

    public long Iterations { get; private set; }
    public double MaxMilliseconds { get; private set; }
    public long FailedSessionTicks { get; private set; }

    public double AverageMilliseconds
    {
        get
        {
            lock (_gate)
            {
                return _recent.Count == 0 ? 0 : _recent.Average();
            }
        }
    }

    public void Record(TimeSpan elapsed)
    {
        lock (_gate)
        {
            Iterations++;
            MaxMilliseconds = Math.Max(MaxMilliseconds, elapsed.TotalMilliseconds);
            _recent.Enqueue(elapsed.TotalMilliseconds);
            if (_recent.Count > 200)
            {
                _recent.Dequeue();
            }
        }
    }

    public void RecordFailure()
    {
        lock (_gate)
        {
            FailedSessionTicks++;
        }
    }
}

/// <summary>
/// Ticks every active session at <c>Game:TickRate</c> Hz and hands their messages to the client notifiers
/// (Bridge: one state and one event notifier per enabled channel, <c>Realtime:EnabledChannels</c>).
/// A session whose tick throws is logged and aborted; the others keep running.
/// </summary>
public sealed class GameLoopService(
    SessionRegistry registry,
    IReadOnlyList<ClientNotifier> notifiers,
    IOptions<GameOptions> options,
    GameLoopStats stats,
    ILogger<GameLoopService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickSeconds = 1.0 / options.Value.TickRate;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(tickSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var watch = Stopwatch.StartNew();
            var sends = new List<Task>();

            foreach (var session in registry.All)
            {
                if (!session.IsFinished)
                {
                    try
                    {
                        session.Tick(tickSeconds);
                    }
                    catch (Exception ex)
                    {
                        stats.RecordFailure();
                        logger.LogError(ex, "Tick failed for session {SessionId}; aborting it", session.Id);
                        session.Fail("The server hit an error in this session and stopped it.");
                    }
                }

                foreach (var message in session.DrainOutbox())
                {
                    sends.AddRange(ClientNotifiers.Dispatch(notifiers, session.Id, message));
                }
            }

            try
            {
                await Task.WhenAll(sends);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Sending messages to clients failed");
            }
            stats.Record(watch.Elapsed);
        }
    }
}
