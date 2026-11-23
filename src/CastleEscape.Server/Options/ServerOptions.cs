using System.ComponentModel.DataAnnotations;

namespace CastleEscape.Server.Options;

/// <summary>Browser origins allowed to call the API and the hub, bound from the <c>Cors</c> section.</summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>Exact origins (scheme + host + port). SignalR needs credentials, so "*" is not allowed.</summary>
    [MinLength(1)]
    public string[] AllowedOrigins { get; set; } = [];
}

/// <summary>Developer-only endpoints and the playground page, bound from the <c>DevTools</c> section.</summary>
public sealed class DevToolsOptions
{
    public const string SectionName = "DevTools";

    public bool Enabled { get; set; }
}
