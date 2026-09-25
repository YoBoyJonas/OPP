using CastleEscape.Game.Configuration;
using CastleEscape.Server;
using CastleEscape.Server.Endpoints;
using CastleEscape.Server.Game;
using CastleEscape.Server.Hubs;
using CastleEscape.Server.OpenApi;
using CastleEscape.Server.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration, validated at startup so a bad appsettings value fails fast.
AddValidatedOptions<GameOptions>(builder.Services, GameOptions.SectionName);
AddValidatedOptions<GenerationOptions>(builder.Services, GenerationOptions.SectionName);
AddValidatedOptions<PatternOptions>(builder.Services, PatternOptions.SectionName);
AddValidatedOptions<RealtimeOptions>(builder.Services, RealtimeOptions.SectionName);
AddValidatedOptions<CorsSettings>(builder.Services, CorsSettings.SectionName);
AddValidatedOptions<DevToolsOptions>(builder.Services, DevToolsOptions.SectionName);

builder.Services.ConfigureHttpJsonOptions(options => JsonDefaults.Apply(options.SerializerOptions));
builder.Services.AddSignalR().AddJsonProtocol(options => JsonDefaults.Apply(options.PayloadSerializerOptions));
builder.Services.AddProblemDetails();
builder.Services.AddCastleEscapeOpenApi();

var corsOrigins = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()?.AllowedOrigins ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials())); // SignalR sends credentials, which rules out AllowAnyOrigin.

builder.Services.AddSingleton<SessionManager>();

// Azure Linux/container hosting sets PORT; Azure App Service and local dev don't.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

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
app.MapHub<GameHub>("/hubs/game");

app.Run();

return 0;

static void AddValidatedOptions<TOptions>(IServiceCollection services, string section) where TOptions : class =>
    services.AddOptions<TOptions>()
        .BindConfiguration(section)
        .ValidateDataAnnotations()
        .ValidateOnStart();
