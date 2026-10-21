using CastleEscape.Game.Configuration;
using CastleEscape.Game.Messaging;
using CastleEscape.Game.Sessions;
using Microsoft.Extensions.Options;

namespace CastleEscape.Server.Realtime;

/// <summary>
/// The server's clock: every 1/<c>Game:TickRate</c> s it asks the <see cref="GameFacade"/> to advance all sessions,
/// then hands their messages to the client notifiers (Bridge: one state and one event notifier per enabled
/// channel, <c>Realtime:EnabledChannels</c>).
/// </summary>
public sealed class GameLoopService(
    GameFacade game,
    IReadOnlyList<ClientNotifier> notifiers,
    IOptions<GameOptions> options,
    ILogger<GameLoopService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickSeconds = 1.0 / options.Value.TickRate;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(tickSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var sends = game.Tick(tickSeconds)
                .SelectMany(m => ClientNotifiers.Dispatch(notifiers, m.SessionId, m.Message))
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
