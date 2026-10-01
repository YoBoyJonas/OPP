using CastleEscape.Contracts;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.World;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Facade: a whole game played through <see cref="GameFacade"/> alone, like the hub and REST do.</summary>
public sealed class FacadeDemo : IPatternDemo
{
    public string Key => "facade";

    /// <summary>Two levers (L), the door (D) and three exit tiles (E) below it.</summary>
    public static readonly string[] Map =
    [
        "#######",
        "#L1.2L#",
        "###D###",
        "##EEE##",
        "#######",
    ];

    public PatternDemoResponse Run(DemoOptions options)
    {
        var trace = new DemoTrace("Facade");

        // Composition root (what Program.cs does once): the subsystems, then the facade in front of them.
        var catalog = DemoWorld.Catalog;
        var registry = new SessionRegistry(catalog, new RowsLevelProvider(catalog, Map),
            new GameOptions { MaxLevel = 1, LevelTransitionSeconds = 0 });
        var game = new GameFacade(registry, new GameLoopScheduler(registry), catalog);
        var calls = 0;
        var delivered = new Dictionary<string, int>();

        void Tick()
        {
            foreach (var m in game.Tick(0.05))
            {
                delivered[m.Message.Method] = delivered.GetValueOrDefault(m.Message.Method) + 1;
            }
        }

        // Everything below uses the facade only.
        var ana = game.CreateSession("Ana");
        var ben = game.JoinSession(game.GetSession(ana.SessionId).JoinCode, "Ben");
        game.SelectCharacter(ana.SessionId, ana.PlayerToken, "scout");
        game.SelectCharacter(ana.SessionId, ben.PlayerToken, "warrior");
        calls += 4;
        Tick();
        trace.Line($"CreateSession, JoinSession(code {game.GetSession(ana.SessionId).JoinCode}), SelectCharacter x2 -> phase "
                   + $"{game.GetSession(ana.SessionId).Phase}, level {game.GetState(ana.SessionId).LevelIndex}.");

        void Walk(string token, Guid playerId, params GridPos[] route)
        {
            foreach (var target in route)
            {
                calls += WalkTo(game, ana.SessionId, token, playerId, target, Tick);
            }
        }

        Walk(ana.PlayerToken, ana.PlayerId, new GridPos(1, 1));
        Walk(ben.PlayerToken, ben.PlayerId, new GridPos(5, 1));
        var state = game.GetState(ana.SessionId);
        trace.Line($"Ana and Ben walk onto the levers (SubmitDirection, Tick): door open = {state.DoorOpen}.");

        Walk(ana.PlayerToken, ana.PlayerId, new GridPos(3, 1), new GridPos(3, 3), new GridPos(2, 3));
        Walk(ben.PlayerToken, ben.PlayerId, new GridPos(3, 1), new GridPos(3, 3));
        for (var i = 0; i < 5; i++) Tick();
        var hud = game.GetHud(ana.SessionId);
        trace.Line($"Both walk through the door onto the exit tiles -> phase {hud.Phase} after {hud.Tick} ticks.");
        trace.Line($"{calls} lobby and input calls to the facade (plus state reads); it delivered {delivered.Values.Sum()} messages "
                   + $"({string.Join(", ", delivered.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key} {kv.Value}"))}).");

        trace.Evidence("finalPhase", hud.Phase.ToString())
            .Evidence("facadeCalls", calls)
            .Evidence("delivered", delivered)
            .Evidence("facadeOperations", typeof(GameFacade).GetMethods()
                .Where(m => m.DeclaringType == typeof(GameFacade) && !m.IsSpecialName).Select(m => m.Name).Distinct().ToArray());
        return trace.Done("The client code never touched a session, registry, command or level: one interface for the whole game.");
    }

    /// <summary>A tiny bot: holds a direction until the player stands on <paramref name="target"/>. Returns the facade calls it made.</summary>
    private static int WalkTo(GameFacade game, Guid sessionId, string token, Guid playerId, GridPos target, Action tick)
    {
        var rows = game.GetLevelLayout(sessionId).Rows;
        var grid = new Grid(rows[0].Length, rows.Length);
        var calls = 1;
        var held = Direction.None;
        for (var i = 0; i < 400; i++)
        {
            var state = game.GetState(sessionId);
            var me = state.Players.Single(p => p.PlayerId == playerId);
            var here = new GridPos(me.TileX, me.TileY);
            if (!me.IsMoving && here == target)
            {
                game.SubmitDirection(sessionId, token, Direction.None);
                tick();
                return calls + 1;
            }
            // While stepping, plan from the tile being entered: the player steps on at once while a key is held.
            var from = me.IsMoving ? here.Step(me.Facing) : here;
            var other = state.Players.Single(p => p.PlayerId != playerId);
            var path = from == target ? [] : PathFinder.ShortestPath(grid, from, target, p =>
                rows[p.Y][p.X] != '#' && (rows[p.Y][p.X] != 'D' || state.DoorOpen) && !(p.X == other.TileX && p.Y == other.TileY));
            var next = path is { Count: > 0 } ? from.DirectionTo(path[0]) : Direction.None;
            if (next != held)
            {
                game.SubmitDirection(sessionId, token, next);
                held = next;
                calls++;
            }
            tick();
        }
        var last = game.GetState(sessionId);
        throw new InvalidOperationException($"The bot could not reach {target}: phase {last.Phase}, players "
            + string.Join(" ", last.Players.Select(p => $"{p.Name}({p.TileX},{p.TileY}) moving={p.IsMoving}")));
    }
}
