namespace CastleEscape.Server.OpenApi;

/// <summary>
/// Every OpenAPI tag and the Scalar sidebar group it belongs to (<c>x-tagGroups</c>).
/// Endpoints use these constants in <c>.WithTags(...)</c> so names never drift.
/// </summary>
public static class ApiTags
{
    public const string Sessions = "Sessions";
    public const string Gameplay = "Gameplay";
    public const string Realtime = "Realtime";
    public const string Content = "Content";
    public const string Levels = "Levels";
    public const string Patterns = "Patterns";
    public const string Diagnostics = "Diagnostics";
    public const string Dev = "Dev";

    public sealed record TagInfo(string Name, string Description);

    public sealed record TagGroup(string Name, IReadOnlyList<TagInfo> Tags);

    public static readonly IReadOnlyList<TagGroup> Groups =
    [
        new("Lobby",
        [
            new(Sessions, "Create, join and leave a two-player session; pick a character."),
        ]),
        new("Gameplay",
        [
            new(Gameplay, "REST mirror of the hub: send input, restart, read state, HUD and level layout."),
            new(Realtime, "SignalR protocol description and example payloads for every hub message."),
        ]),
        new("Content",
        [
            new(Content, "Static game content: characters, items, powers, combos, obstacles, zombies, levels."),
            new(Levels, "Preview generated or preset levels with their validation report."),
        ]),
        new("Course",
        [
            new(Patterns, "The P1 design patterns: participants, requirements and runnable demos."),
        ]),
        new("Ops",
        [
            new(Diagnostics, "Health, game loop timings, command history and event log."),
            new(Dev, "Developer shortcuts. Available only when DevTools:Enabled is true."),
        ]),
    ];
}
