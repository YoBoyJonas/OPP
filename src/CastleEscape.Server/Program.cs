using CastleEscape.Server.Development;
using CastleEscape.Domain.Content;
using CastleEscape.Server.Configuration;
using CastleEscape.Server.Endpoints;
using CastleEscape.Server.Hubs;
using CastleEscape.Server.OpenApi;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCastleEscape(builder.Configuration);

// Azure Linux/container hosting sets PORT; Azure App Service and local dev don't.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

// Load and validate content now, so a broken content file stops startup instead of the first request.
app.Services.GetRequiredService<IContentCatalog>();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

app.MapOpenApi(); // /openapi/v1.json
app.MapScalarApiReference(options => options
    .WithTitle("Castle Escape API")
    .AddPreferredSecuritySchemes(OpenApiSetup.PlayerTokenScheme)
    .EnablePersistentAuthentication()); // /scalar
app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();

app.MapDiagnosticsEndpoints();
app.MapContentEndpoints();
app.MapLevelEndpoints();
app.MapSessionEndpoints();
app.MapGameplayEndpoints();
app.MapRealtimeEndpoints();
if (app.Services.GetRequiredService<IOptions<DevToolsOptions>>().Value.Enabled)
{
    app.MapDevEndpoints();
    app.UseStaticFiles(); // wwwroot holds only the playground, a dev tool
    app.MapGet("/playground", () => Results.Redirect("/playground/index.html")).ExcludeFromDescription();
}
app.MapHub<GameHub>(RealtimeEndpoints.HubPath);

app.Run();

return 0;
