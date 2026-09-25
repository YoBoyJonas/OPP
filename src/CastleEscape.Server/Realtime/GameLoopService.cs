using System.Diagnostics;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
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
/// Ticks every active session at <c>Game:TickRate</c> Hz and pushes their messages to the SignalR group.
/// A session whose tick throws is logged and aborted; the others keep running.
/// </summary>
public sealed class GameLoopService(
    SessionRegistry registry,
    IHubContext<GameHub, IGameClient> hub,
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
                    sends.Add(Send(session.Id, message));
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

    private Task Send(Guid sessionId, OutgoingMessage message)
    {
        var group = hub.Clients.Group(GameHub.GroupName(sessionId));
        return message.Payload switch
        {
            SessionUpdatedMessage m => group.SessionUpdated(m),
            LevelStartedMessage m => group.LevelStarted(m),
            TickStateMessage m => group.StateUpdated(m),
            GameEventMessage m => group.GameEvent(m),
            ErrorMessage m => group.Error(m),
            _ => Task.CompletedTask,
        };
    }
}
