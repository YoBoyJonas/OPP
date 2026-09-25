namespace CastleEscape.Contracts.Diagnostics;

/// <summary>Liveness information returned by <c>GET /health</c>.</summary>
/// <param name="Status">Always "Healthy" when the server answers.</param>
/// <param name="Version">Server assembly version.</param>
/// <param name="Environment">ASP.NET Core environment name (Development, Production).</param>
/// <param name="StartedAtUtc">When the server process started.</param>
/// <param name="UptimeSeconds">Seconds since start.</param>
public sealed record HealthResponse(
    string Status,
    string Version,
    string Environment,
    DateTimeOffset StartedAtUtc,
    double UptimeSeconds);
