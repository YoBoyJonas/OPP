using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Sessions;

namespace CastleEscape.Game.Messaging;

/// <summary>
/// One real example of every server-to-client message, captured from a short scripted session on a
/// small map (so examples never drift from what the server really sends).
/// </summary>
public static class RealtimeExamples
{
    private static readonly string[] Map =
    [
        "##########",
        "#1r.....2#",
        "#.......Z#",
        "##########",
    ];

    public static Dictionary<string, IServerMessage> Capture(ContentCatalog catalog)
    {
        var session = new GameSession(Guid.NewGuid(), "EXMPL1", catalog, new RowsLevelProvider(catalog, Map),
            new GameOptions { LevelTransitionSeconds = 0 }, baseSeed: 1);
        var examples = new Dictionary<string, IServerMessage>();

        void Collect()
        {
            foreach (var message in session.DrainOutbox())
            {
                if (message.Payload is IServerMessage payload && (!examples.ContainsKey(message.Method) || message.Method == ClientMethods.GameEvent && IsBetterEvent(payload)))
                {
                    examples[message.Method] = payload;
                }
            }
        }

        var ana = session.AddPlayer("Ana");
        var ben = session.AddPlayer("Ben");
        session.SelectCharacter(ana.PlayerId, "scout");
        session.SelectCharacter(ben.PlayerId, "warrior");
        session.Tick(session.TickSeconds); // loads the level (inputs before this are discarded)
        Collect();
        session.SubmitDirection(ana.PlayerId, Direction.Right); // picks up the coin: an ItemCollected event
        for (var i = 0; i < 12; i++)
        {
            session.Tick(session.TickSeconds);
            Collect();
        }

        var tick = session.Snapshot.Hud.Tick;
        examples[ClientMethods.Error] = new ErrorMessage(session.Id, 0, tick, GameErrorCode.WrongPhase, "Characters can't be changed in phase Playing.");
        examples[ClientMethods.Pong] = new PongMessage(session.Id, session.Snapshot.State?.Seq ?? 0, tick, DateTimeOffset.UtcNow);
        return examples;
    }

    // Prefer a gameplay event over lobby noise as the GameEvent example.
    private static bool IsBetterEvent(IServerMessage message) => message is GameEventMessage { Type: GameEventTypes.ItemCollected };
}
