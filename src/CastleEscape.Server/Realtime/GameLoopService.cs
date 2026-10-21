using CastleEscape.Game.Configuration;
using CastleEscape.Game.Messaging;
using CastleEscape.Game.Sessions;

namespace CastleEscape.Server.Realtime;

/// <summary>
/// The server's clock: every 1/<c>Game:TickRate</c> s it asks the <see cref="GameFacade"/> to advance all sessions,
/// then hands their messages to the client notifiers (Bridge: one state and one event notifier per enabled
/// channel, <c>Realtime:EnabledChannels</c>).
/// </summary>
public sealed class GameLoopService(
    GameFacade game,
    SignalRClientChannel signalR,
    PollingBufferChannel polling,
    RealtimeOptions realtime,
    GameOptions options,
    ILogger<GameLoopService> logger) : BackgroundService
{
    private readonly IReadOnlyList<ClientNotifier> _notifiers = ClientNotifiers.For(
        realtime.EnabledChannels.Distinct().Select(c => c == RealtimeChannel.SignalR ? (IClientChannel)signalR : polling),
        new Dictionary<string, int> { ["Polling"] = realtime.PollingStateEveryNthTick });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickSeconds = 1.0 / options.TickRate;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(tickSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var sends = game.Tick(tickSeconds)
                .SelectMany(m => ClientNotifiers.Dispatch(_notifiers, m.SessionId, m.Message))
                .ToList();
            try
            {
                await Task.WhenAll(sends);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "A client send failed");
            }
        }
    }
}
