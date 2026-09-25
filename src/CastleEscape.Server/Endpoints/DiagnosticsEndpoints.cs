using System.Reflection;
using CastleEscape.Contracts.Diagnostics;
using CastleEscape.Server.OpenApi;

namespace CastleEscape.Server.Endpoints;

public static class DiagnosticsEndpoints
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;

    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags(ApiTags.Diagnostics);

        group.MapGet("/health", GetHealth)
            .WithName("GetHealth")
            .WithSummary("Server health")
            .WithDescription("Liveness check. Returns 200 with version and uptime while the server is running.");

        return app;
    }

    private static HealthResponse GetHealth(IHostEnvironment environment)
    {
        var version = typeof(DiagnosticsEndpoints).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        return new HealthResponse(
            "Healthy",
            version,
            environment.EnvironmentName,
            StartedAt,
            Math.Round((DateTimeOffset.UtcNow - StartedAt).TotalSeconds, 1));
    }
}
