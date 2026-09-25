namespace CastleEscape.Contracts.Sessions;

/// <summary>Body of <c>POST /api/sessions</c>.</summary>
/// <param name="PlayerName">Shown to the other player and in the HUD.</param>
public sealed record CreateSessionRequest(string PlayerName);

/// <summary>Result of creating a session. Share <c>JoinCode</c> with the second player; keep <c>PlayerToken</c> secret.</summary>
/// <param name="SessionId">Id used in every other URL and in the hub connection.</param>
/// <param name="JoinCode">Short code the second player enters (join-session activity diagram).</param>
/// <param name="PlayerId">Your player id.</param>
/// <param name="PlayerToken">Send as the X-Player-Token header and as the hub's playerToken query parameter.</param>
public sealed record CreateSessionResponse(Guid SessionId, string JoinCode, Guid PlayerId, string PlayerToken);

/// <summary>Body of <c>POST /api/sessions/join</c>.</summary>
/// <param name="JoinCode">The code from the first player.</param>
/// <param name="PlayerName">Shown to the other player and in the HUD.</param>
public sealed record JoinSessionRequest(string JoinCode, string PlayerName);

/// <summary>Result of joining a session.</summary>
public sealed record JoinSessionResponse(Guid SessionId, Guid PlayerId, string PlayerToken);

/// <summary>Body of <c>PUT /api/sessions/{id}/players/me/character</c>.</summary>
/// <param name="CharacterId">One of the ids from <c>GET /api/content/characters</c>.</param>
public sealed record SelectCharacterRequest(string CharacterId);

/// <summary>One row of <c>GET /api/sessions</c>.</summary>
public sealed record SessionSummary(Guid SessionId, string JoinCode, SessionPhase Phase, int PlayerCount);

/// <summary>A player as seen in the lobby.</summary>
/// <param name="PlayerId">Player id.</param>
/// <param name="Slot">1 or 2; decides the start tile.</param>
/// <param name="Name">Display name.</param>
/// <param name="CharacterId">Chosen character, or null while choosing.</param>
/// <param name="Connected">Whether the player has a live hub connection.</param>
public sealed record SessionPlayerDto(Guid PlayerId, int Slot, string Name, string? CharacterId, bool Connected);

/// <summary>Lobby view of a session.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="JoinCode">Code for the second player.</param>
/// <param name="Phase">Current lifecycle phase.</param>
/// <param name="LevelIndex">Current level (1-10), or 0 before the first level.</param>
/// <param name="MaxLevel">Number of levels in the run.</param>
/// <param name="Players">Joined players in slot order.</param>
public sealed record SessionDto(
    Guid SessionId,
    string JoinCode,
    SessionPhase Phase,
    int LevelIndex,
    int MaxLevel,
    IReadOnlyList<SessionPlayerDto> Players);

/// <summary>Body of <c>POST /api/sessions/{id}/input</c>: the direction key now held.</summary>
/// <param name="Direction">Up/Down/Left/Right on key down; None on key up.</param>
public sealed record DirectionRequest(Direction Direction);
