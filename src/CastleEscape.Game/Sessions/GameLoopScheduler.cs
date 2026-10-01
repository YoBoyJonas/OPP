using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.Sessions;

/// <summary>A message ready to go to one session's clients.</summary>
public sealed record SessionMessage(Guid SessionId, OutgoingMessage Message);

/// <summary>
/// Advances every unfinished session by one fixed step and collects what they want to send. A session
/// whose tick throws is stopped (with an Error message to its clients); the others are not affected.
/// The host decides when to call it (a timer in the server, a loop in demos and tests).
/// </summary>
[DesignPattern("Facade", "Subsystem")]
public sealed class GameLoopScheduler(SessionRegistry registry)
{
    /// <summary>Called when a session's tick fails, before the session is stopped.</summary>
    public event Action<GameSession, Exception>? SessionFailed;

    public List<SessionMessage> TickAll(double seconds)
    {
        var messages = new List<SessionMessage>();
        foreach (var session in registry.All)
        {
            if (!session.IsFinished)
            {
                try
                {
                    session.Tick(seconds);
                }
                catch (Exception ex)
                {
                    SessionFailed?.Invoke(session, ex);
                    session.Fail("The server hit an error in this session and stopped it.");
                }
            }
            messages.AddRange(session.DrainOutbox().Select(m => new SessionMessage(session.Id, m)));
        }
        return messages;
    }
}
