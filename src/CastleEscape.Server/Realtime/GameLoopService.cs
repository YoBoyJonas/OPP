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

    public long FailedSends { get; private set; }

    public void RecordSendFailure()
    {
        lock (_gate)
        {
            FailedSends++;
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
/// The server's clock: every 1/<c>Game:TickRate</c> s it asks the <see cref="GameFacade"/> to advance all sessions,
/// then hands their messages to the client notifiers (Bridge: one state and one event notifier per enabled
/// channel, <c>Realtime:EnabledChannels</c>).
/// </summary>
public sealed class GameLoopService(
    GameFacade game,
    IReadOnlyList<ClientNotifier> notifiers,
    IOptions<GameOptions> options,
    GameLoopStats stats) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickSeconds = 1.0 / options.Value.TickRate;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(tickSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var watch = Stopwatch.StartNew();
            var sends = game.Tick(tickSeconds)
                .SelectMany(m => ClientNotifiers.Dispatch(notifiers, m.SessionId, m.Message))
                .ToList();
            try
            {
                await Task.WhenAll(sends);
            }
            catch (Exception)
            {
                stats.RecordSendFailure();
            }
            stats.Record(watch.Elapsed);
        }
    }
}
