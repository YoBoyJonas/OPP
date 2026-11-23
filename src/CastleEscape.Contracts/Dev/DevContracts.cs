namespace CastleEscape.Contracts.Dev;

/// <summary>Start a ready-to-play two-player session in one call (<c>POST /api/dev/quickstart</c>).</summary>
/// <param name="Character1">Player 1's character; default scout.</param>
/// <param name="Character2">Player 2's character; default warrior.</param>
/// <param name="Seed">Fixed seed for reproducible levels; random when null.</param>
public sealed record DevQuickstartRequest(string? Character1 = null, string? Character2 = null, int? Seed = null);

/// <summary>A player created by the quickstart, with the token to act as them.</summary>
public sealed record DevPlayerDto(Guid PlayerId, int Slot, string Name, string CharacterId, string PlayerToken, string HubUrl);

/// <summary>The quickstarted session. Open the hub URL of each player in its own tab (or the playground).</summary>
public sealed record DevQuickstartResponse(Guid SessionId, string JoinCode, DevPlayerDto[] Players);

/// <summary>Switch every zombie of the current level to one chase strategy.</summary>
public sealed record ZombieStrategyRequest(MovementStrategyKind Strategy);

/// <summary>Give a base power (Jump, Sprint, Swim) without an item.</summary>
/// <param name="Power">The power.</param>
/// <param name="DurationSeconds">How long; default from the item or <c>Game:PowerDurationSeconds</c>.</param>
public sealed record GivePowerRequest(PowerType Power, double? DurationSeconds = null);

/// <summary>Result of an undo request: the command undone, or null if there was nothing to undo.</summary>
public sealed record UndoResponse(string? Undone);

/// <summary>The Prototype pattern's clone mode used on level start and restart: <c>Deep</c> or <c>Shallow</c>.</summary>
public sealed record CloneModeDto(string Mode);
