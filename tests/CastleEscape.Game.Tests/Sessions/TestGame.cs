using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Sessions;

/// <summary>Serves hand-written ASCII maps as levels (no validation, so maps can be tiny).</summary>
public sealed class RowsLevelProvider(ContentCatalog catalog, params string[][] levels) : ILevelProvider
{
    /// <summary>Use this level definition (and its first zombie type) instead of the level being loaded.</summary>
    public int? DefinitionIndex { get; init; }

    public LevelState CreateLevel(int levelIndex, int seed)
    {
        var rows = levels[Math.Min(levelIndex, levels.Length) - 1];
        return PresetLevelParser.Parse(rows, catalog.GetLevel(DefinitionIndex ?? levelIndex), catalog, seed);
    }
}

/// <summary>A session on a tiny map, ticked by hand at 20 Hz, with every message it sent recorded.</summary>
public sealed class TestGame
{
    public static readonly ContentCatalog Catalog = ContentLoader.LoadFromDirectory(ContentLoader.DefaultDirectory);

    private TestGame(GameSession session, PlayerSlot p1, PlayerSlot p2)
    {
        Session = session;
        P1 = p1;
        P2 = p2;
    }

    public GameSession Session { get; }
    public PlayerSlot P1 { get; }
    public PlayerSlot P2 { get; }
    public List<OutgoingMessage> Messages { get; } = [];

    public const double Dt = 0.05;

    public static TestGame Start(string[] rows, GameOptions? options = null, string character1 = "scout",
        string character2 = "scout", params string[][] moreLevels) =>
        Start(new RowsLevelProvider(Catalog, [rows, .. moreLevels]), options, character1, character2);

    public static TestGame Start(ILevelProvider levels, GameOptions? options = null, string character1 = "scout", string character2 = "scout")
    {
        var session = new GameSession(Guid.NewGuid(), "TEST01", Catalog, levels, options ?? new GameOptions { LevelTransitionSeconds = 0 }, baseSeed: 1);
        var p1 = session.AddPlayer("Ana");
        var p2 = session.AddPlayer("Ben");
        session.SelectCharacter(p1.PlayerId, character1);
        session.SelectCharacter(p2.PlayerId, character2);
        var game = new TestGame(session, p1, p2);
        game.Tick(); // loads level 1
        return game;
    }

    public TickStateMessage State => Session.Snapshot.State!;
    public PlayerStateDto Player(PlayerSlot slot) => State.Players.Single(p => p.PlayerId == slot.PlayerId);
    public GridPos TileOf(PlayerSlot slot) => new(Player(slot).TileX, Player(slot).TileY);
    public IEnumerable<GameEventMessage> Events => Messages.Select(m => m.Payload).OfType<GameEventMessage>();
    public IEnumerable<GameEventMessage> EventsOf(string type) => Events.Where(e => e.Type == type);

    public void Tick(int times = 1)
    {
        for (var i = 0; i < times; i++)
        {
            Session.Tick(Dt);
            Messages.AddRange(Session.DrainOutbox());
        }
    }

    public void RunSeconds(double seconds) => Tick((int)Math.Ceiling(seconds / Dt));

    public void Hold(PlayerSlot slot, Direction direction) => Session.SubmitDirection(slot.PlayerId, direction);

    /// <summary>Ticks until <paramref name="condition"/> holds; fails after <paramref name="maxSeconds"/>.</summary>
    public void RunUntil(Func<bool> condition, double maxSeconds = 10)
    {
        for (var t = 0.0; t < maxSeconds; t += Dt)
        {
            if (condition()) return;
            Tick();
        }
        Assert.Fail($"Condition not met within {maxSeconds}s.");
    }

    /// <summary>
    /// Steers a player to <paramref name="target"/> like a client would: it only ever sends held directions.
    /// It plans from the tile the player is heading to, so the new direction is in place before arrival.
    /// </summary>
    public void WalkTo(PlayerSlot slot, GridPos target, double maxSeconds = 15)
    {
        var rows = Session.Snapshot.Layout!.Rows;
        var last = (Direction?)null;
        for (var t = 0.0; t < maxSeconds; t += Dt)
        {
            var me = Player(slot);
            if (!me.IsMoving && me.TileX == target.X && me.TileY == target.Y)
            {
                Hold(slot, Direction.None);
                Tick();
                return;
            }

            var from = me.IsMoving ? new GridPos(me.TileX, me.TileY).Step(me.Facing) : new GridPos(me.TileX, me.TileY);
            var other = State.Players.Single(p => p.PlayerId != slot.PlayerId);
            var path = PathFinder.ShortestPath(GridFor(rows), from, target,
                p => Passable(rows, p) && !(p.X == other.TileX && p.Y == other.TileY));
            var direction = path is { Count: > 0 } ? from.DirectionTo(path[0]) : Direction.None;
            if (direction != last)
            {
                Hold(slot, direction);
                last = direction;
            }
            Tick();
        }
        Assert.Fail($"Player {slot.Slot} did not reach {target} within {maxSeconds}s (at {TileOf(slot)}).");
    }

    private bool Passable(IReadOnlyList<string> rows, GridPos p) => rows[p.Y][p.X] switch
    {
        '#' or '~' or 'O' => false,
        'D' => State.DoorOpen,
        _ => true,
    };

    private static Grid GridFor(IReadOnlyList<string> rows) => new(rows[0].Length, rows.Count);
}
