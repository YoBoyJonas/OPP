namespace CastleEscape.Contracts.Content;

/// <summary>What the server's content catalog holds, returned by <c>GET /api/content</c>.</summary>
/// <param name="ContentHash">SHA-256 of the whole catalog; changes whenever any content file changes.</param>
/// <param name="Characters">Number of playable characters.</param>
/// <param name="Consumables">Number of item types (Health, Reward, Power).</param>
/// <param name="Combos">Number of super-power combos.</param>
/// <param name="Obstacles">Number of obstacle types.</param>
/// <param name="Zombies">Number of zombie types.</param>
/// <param name="Levels">Number of levels in a run.</param>
public sealed record ContentSummaryResponse(
    string ContentHash,
    int Characters,
    int Consumables,
    int Combos,
    int Obstacles,
    int Zombies,
    int Levels);

/// <summary>A base power or super power and how to get it.</summary>
/// <param name="Power">The power.</param>
/// <param name="IsSuperPower">True for JumpDash and FastSwim, which come from combos.</param>
/// <param name="DurationSeconds">How long a pickup lasts (base powers only).</param>
/// <param name="GrantedByItems">Ids of items that grant this base power.</param>
/// <param name="RequiredPowers">For a super power: the base powers that must be active together.</param>
public sealed record PowerSummary(
    PowerType Power,
    bool IsSuperPower,
    double? DurationSeconds,
    string[] GrantedByItems,
    PowerType[] RequiredPowers);
