using CastleEscape.Contracts;
using CastleEscape.Contracts.Dev;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;

namespace CastleEscape.Game.Sessions;

/// <summary>
/// Developer and defence shortcuts: skip the lobby, undo, switch zombie strategies, give powers, skip
/// levels, flip the Prototype clone mode at runtime. Served under <c>/api/dev</c> only when
/// <c>DevTools:Enabled</c>; every action goes through the same session code the game uses.
/// </summary>
public sealed class DevActions(SessionRegistry registry, ContentCatalog catalog, PatternOptions patterns)
{
    /// <summary>A session with two players who have picked characters; the first level loads on the next tick.</summary>
    /// <param name="hubUrl">Builds a player's hub URL from (sessionId, token).</param>
    public DevQuickstartResponse Quickstart(DevQuickstartRequest request, Func<Guid, string, string> hubUrl)
    {
        var characters = new[] { request.Character1 ?? "scout", request.Character2 ?? "warrior" };
        foreach (var id in characters.Where(id => catalog.FindCharacter(id) is null))
        {
            throw new GameException(GameErrorCode.UnknownCharacter,
                $"Unknown character '{id}'. Choose one of: {string.Join(", ", catalog.Characters.Select(c => c.Id))}.");
        }

        var (session, ana) = registry.Create("Ana", request.Seed);
        var (_, ben) = registry.Join(session.JoinCode, "Ben");
        session.SelectCharacter(ana.PlayerId, characters[0]);
        session.SelectCharacter(ben.PlayerId, characters[1]);

        return new DevQuickstartResponse(session.Id, session.JoinCode,
            new[] { ana, ben }.Select(p => new DevPlayerDto(p.PlayerId, p.Slot, p.Name, p.CharacterId!, p.Token, hubUrl(session.Id, p.Token))).ToArray());
    }

    public UndoResponse UndoLastCommand(Guid sessionId, Guid? playerId) => new(registry.Get(sessionId).UndoLastCommand(playerId));

    public void SetZombieStrategy(Guid sessionId, MovementStrategyKind strategy) => registry.Get(sessionId).SetZombieStrategy(strategy);

    public void GivePower(Guid sessionId, Guid playerId, PowerType power, double? durationSeconds) =>
        registry.Get(sessionId).GivePower(playerId, power, durationSeconds);

    public void SkipLevel(Guid sessionId) => registry.Get(sessionId).SkipLevel();

    public CloneModeDto GetCloneMode() => new(patterns.PrototypeCloneMode.ToString());

    /// <summary>Takes effect on the next level start or restart of every session.</summary>
    public CloneModeDto SetCloneMode(string mode)
    {
        if (!Enum.TryParse<CloneMode>(mode, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new GameException(GameErrorCode.InvalidRequest, $"Clone mode must be one of: {string.Join(", ", Enum.GetNames<CloneMode>())}.");
        }
        patterns.PrototypeCloneMode = parsed;
        return GetCloneMode();
    }
}
