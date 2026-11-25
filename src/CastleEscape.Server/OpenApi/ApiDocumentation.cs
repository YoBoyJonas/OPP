using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace CastleEscape.Server.OpenApi;

/// <summary>
/// The OpenAPI text of every endpoint, kept here so the controllers hold only routing and code.
/// Keyed by the route name in each action's <c>[HttpGet/Post/...(Name = "...")]</c>, which is also the operationId.
/// <see cref="ApiDocumentationConvention"/> applies an entry to its action at startup and fails if one is missing.
/// </summary>
public static class ApiDocumentation
{
    /// <param name="Summary">The one-line title in Scalar.</param>
    /// <param name="Description">The longer text under it (Markdown).</param>
    /// <param name="Success">The success status code.</param>
    /// <param name="SuccessType">The success body, when it is not the action's declared <c>ActionResult&lt;T&gt;</c>.</param>
    /// <param name="ContentTypes">The success content types, when not just JSON.</param>
    /// <param name="Errors">The error statuses; each is documented as a ProblemDetails body.</param>
    /// <param name="PlayerToken">The action acts as a player and needs the <c>X-Player-Token</c> header.</param>
    /// <param name="Tag">A tag that replaces the controller's one.</param>
    public sealed record Endpoint(
        string Summary,
        string? Description = null,
        int Success = Status200OK,
        Type? SuccessType = null,
        string[]? ContentTypes = null,
        int[]? Errors = null,
        bool PlayerToken = false,
        string? Tag = null);

    public static readonly IReadOnlyDictionary<string, Endpoint> Endpoints = new Dictionary<string, Endpoint>
    {
        // SessionsController
        ["CreateSession"] = new("Create a session",
            "Creates a two-player session with you as player 1. Share the joinCode with player 2. "
                + "Keep the playerToken: send it as X-Player-Token and as the hub's playerToken.",
            Success: Status201Created,
            SuccessType: typeof(CreateSessionResponse),
            Errors: [Status400BadRequest]),
        ["JoinSession"] = new("Join a session by code",
            "Joins as player 2. The session then waits for both players to pick a character.",
            Errors: [Status400BadRequest, Status404NotFound, Status409Conflict]),
        ["ListSessions"] = new("List sessions",
            "Every session on this server with its phase and player count."),
        ["GetSession"] = new("Get a session",
            "Phase, players, chosen characters and current level.",
            Errors: [Status404NotFound]),
        ["SelectCharacter"] = new("Pick your character",
            "PLR-1. Allowed until the game starts. When both players have picked, the first level loads.",
            Errors: [Status400BadRequest, Status401Unauthorized, Status409Conflict],
            PlayerToken: true),
        ["LeaveSession"] = new("Leave the session",
            "Ends the session for both players (phase Aborted); the other player is notified.",
            Success: Status204NoContent,
            Errors: [Status401Unauthorized],
            PlayerToken: true),

        // GameplayEndpoints
        ["SubmitDirection"] = new("Set the held direction",
            "Same as the hub's SetDirection. Send a direction on key down and None on key up. "
                + "Applied at the start of the next tick.",
            Success: Status202Accepted,
            Errors: [Status401Unauthorized, Status404NotFound],
            PlayerToken: true),
        ["RestartLevel"] = new("Restart the current level",
            "D8: the level returns to its generated layout; both players' lives and score return to the level start.",
            Success: Status202Accepted,
            Errors: [Status401Unauthorized, Status409Conflict],
            PlayerToken: true),
        ["GetGameState"] = new("Latest game state",
            "The latest per-tick state: players, zombies, items, levers, door. "
                + "JSON by default; `?format=xml` or `Accept: application/xml` for XML (NET-2, Adapter pattern).",
            SuccessType: typeof(TickStateMessage),
            ContentTypes: ["application/json", "application/xml"],
            Errors: [Status400BadRequest, Status404NotFound, Status409Conflict]),
        ["PollMessages"] = new("Read buffered messages (polling channel)",
            "The same messages SignalR pushes, kept in a per-session buffer for clients without a hub "
                + "connection (Bridge pattern: the polling channel). Pass the last `seq` you saw as `afterSeq`. "
                + "Each `body` is serialized as `format=json|xml`. StateUpdated is buffered every "
                + "`Realtime:PollingStateEveryNthTick` ticks; events are never skipped.",
            Errors: [Status400BadRequest, Status404NotFound],
            Tag: ApiTags.Realtime),
        ["GetLevelLayout"] = new("Static layout of the current level",
            "Rows of map legend characters, plus the legend itself.",
            Errors: [Status404NotFound, Status409Conflict]),
        ["GetHud"] = new("View game state (HUD table)",
            "Per player: lives, score, active powers with time left, combos, statistics. "
                + "Plus level, levers and door status.",
            Errors: [Status404NotFound]),

        // LevelsController
        ["GetPresetNames"] = new("Preset map names",
            "Hand-made maps in content/presets. Use one with `preset=` on the preview, or for every level "
                + "with `Generation:PresetLevel`."),
        ["PreviewLevel"] = new("Build and show a level",
            "Builds level `index` (1-10) with the Builder pattern: the procedural builder for `seed` "
                + "(validated and retried like in a game), or the preset builder for `preset`. Returns the map rows, "
                + "the themed obstacles and zombies (Abstract Factory, Strategy) and the validation report. Same seed, same level.",
            Errors: [Status400BadRequest, Status422UnprocessableEntity]),

        // ContentController
        ["GetContentSummary"] = new("Content summary",
            "Counts of every content type and a hash that changes whenever a content file changes."),
        ["GetCharacters"] = new("Playable characters",
            "At least 3 characters that differ in lives, move speed and jump parameter (PLR-1)."),
        ["GetConsumables"] = new("Item types",
            "Health (+lives), Reward (+score) and Power (temporary power) items (ITM-1). "
                + "The `type` field tells the kind; Power items carry a `grant`."),
        ["GetPowers"] = new("Base powers and super powers",
            "Jump, Sprint and Swim with their duration and the items that grant them; "
                + "JumpDash and FastSwim with the base powers they need (PWR-1, PWR-2)."),
        ["GetCombos"] = new("Super-power combos",
            "Two different active base powers create a super power (PWR-2)."),
        ["GetObstacles"] = new("Obstacle types",
            "Wall (never passable), Water (needs Swim), Pit (needs Jump) (MOV-2)."),
        ["GetZombies"] = new("Zombie types",
            "Speed, contact damage and the default movement strategy of each zombie type."),
        ["GetLevelDefinitions"] = new("Level definitions",
            "The fixed sequence of 10 levels: theme, obstacle/zombie/item counts and spawn tables."),
        ["GetLevelDefinition"] = new("One level definition",
            "The level at a 1-based position in the sequence.",
            Errors: [Status404NotFound]),

        // RealtimeController
        ["GetRealtimeProtocol"] = new("SignalR hub protocol",
            "Hub URL and query parameters, every server-to-client and client-to-server method with its payload "
                + "and when it happens, every event type and error code, and the ordering rules."),
        ["GetRealtimeExamples"] = new("Example payload of every server message",
            "Captured from a short real session on a small map, keyed by client method name. This is exactly "
                + "the JSON SignalR sends."),

        // DiagnosticsController
        ["GetHealth"] = new("Server health",
            "Liveness check. Returns 200 with version and uptime while the server is running."),
        ["GetLoopDiagnostics"] = new("Game loop timings",
            "Tick rate, session counts, and the average and slowest time to tick all sessions once."),
        ["GetCommandHistory"] = new("Command history of a session",
            "The last 100 commands (inputs, steps, restarts, given powers), oldest first, with the tick each ran "
                + "in and whether it was undone (Command pattern). An undone StartStep is a D7 tile conflict.",
            Errors: [Status404NotFound]),

        // DevController
        ["DevQuickstart"] = new("Two players, characters picked, level 1 starting",
            "Skips the lobby: creates a session with Ana and Ben, picks their characters and returns both "
                + "player tokens and hub URLs. Open each hub URL (or the playground) in its own tab.",
            Errors: [Status400BadRequest]),
        ["DevUndoLastCommand"] = new("Undo the last command",
            "Undoes the most recent command of the session, or of `playerId` (Command pattern). Undoing a "
                + "restart brings back the level as it was before it.",
            Errors: [Status404NotFound, Status409Conflict]),
        ["DevSetZombieStrategy"] = new("Switch every zombie's chase strategy",
            "Greedy, Bfs, AStar or Predictive (Strategy pattern). The tick state shows each zombie's `strategy`.",
            Success: Status202Accepted,
            Errors: [Status404NotFound]),
        ["DevGivePower"] = new("Give a player a power",
            "Jump, Sprint or Swim, as if an item had been picked up: stacks and starts combos (Decorator "
                + "pattern). Undoable.",
            Success: Status202Accepted,
            Errors: [Status400BadRequest, Status404NotFound]),
        ["DevSkipLevel"] = new("Complete the current level",
            "Ends the level as if both players had reached the exit; the next level loads after the usual pause.",
            Success: Status204NoContent,
            Errors: [Status404NotFound, Status409Conflict]),
        ["DevGetCloneMode"] = new("Current Prototype clone mode"),
        ["DevSetCloneMode"] = new("Switch deep/shallow level copies at runtime",
            "`Deep` or `Shallow` (Prototype pattern). Takes effect on the next level start or restart: with "
                + "Shallow, collected items stay gone after a restart.",
            Errors: [Status400BadRequest]),
    };
}
