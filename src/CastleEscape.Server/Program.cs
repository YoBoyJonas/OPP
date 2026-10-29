using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Sessions;
using CastleEscape.Game.Messaging;
using CastleEscape.Server.Controllers;
using CastleEscape.Server.Errors;
using CastleEscape.Server.Hubs;
using CastleEscape.Server.OpenApi;
using CastleEscape.Server.Options;
using CastleEscape.Server.Realtime;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
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
// The dev controller is left out entirely when DevTools:Enabled is false, so /api/dev does not exist.
var devToolsEnabled = builder.Configuration.GetValue<bool>($"{DevToolsOptions.SectionName}:{nameof(DevToolsOptions.Enabled)}");
// Controllers read and write application/json only (no text/plain or text/json), as the Minimal API endpoints did.
// Output keeps application/*+json so ProblemDetails errors go out as application/problem+json.
builder.Services.AddControllers(options =>
    {
        options.Conventions.Add(new ApiDocumentationConvention()); // OpenAPI text lives in OpenApi/ApiDocumentation.cs
        options.OutputFormatters.RemoveType<StringOutputFormatter>();
        foreach (var json in options.InputFormatters.OfType<SystemTextJsonInputFormatter>())
        {
            json.SupportedMediaTypes.Remove("text/json");
            json.SupportedMediaTypes.Remove("application/*+json");
        }
        foreach (var json in options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>())
        {
            json.SupportedMediaTypes.Remove("text/json");
        }
    })
    .AddJsonOptions(options => JsonDefaults.Apply(options.JsonSerializerOptions))
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = GameExceptionHandler.InvalidModelState)
    .ConfigureApplicationPartManager(parts =>
    {
        if (!devToolsEnabled)
        {
            parts.FeatureProviders.Add(new WithoutController<DevController>());
        }
    });
builder.Services.AddSignalR().AddJsonProtocol(options => JsonDefaults.Apply(options.PayloadSerializerOptions));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GameExceptionHandler>();
builder.Services.AddCastleEscapeOpenApi();

var corsOrigins = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()?.AllowedOrigins ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials())); // SignalR sends credentials, which rules out AllowAnyOrigin.

// The Singleton owns its own lifetime; DI just hands out the same instance.
builder.Services.AddSingleton(_ => ContentCatalog.Instance);
builder.Services.AddSingleton<ILevelProvider>(sp => new LevelProvider(
    sp.GetRequiredService<ContentCatalog>(),
    sp.GetRequiredService<IOptions<GameOptions>>().Value,
    sp.GetRequiredService<IOptions<GenerationOptions>>().Value));
builder.Services.AddSingleton(sp => new SessionRegistry(
    sp.GetRequiredService<ContentCatalog>(),
    sp.GetRequiredService<ILevelProvider>(),
    sp.GetRequiredService<IOptions<GameOptions>>().Value,
    sp.GetRequiredService<IOptions<PatternOptions>>().Value));
builder.Services.AddSingleton<GameLoopStats>();
builder.Services.AddSingleton(sp =>
{
    var scheduler = new GameLoopScheduler(sp.GetRequiredService<SessionRegistry>());
    var stats = sp.GetRequiredService<GameLoopStats>();
    var logger = sp.GetRequiredService<ILogger<GameLoopScheduler>>();
    scheduler.SessionFailed += (session, ex) =>
    {
        stats.RecordFailure();
        logger.LogError(ex, "Tick failed for session {SessionId}; stopping it", session.Id);
    };
    return scheduler;
});
builder.Services.AddSingleton<GameFacade>();
builder.Services.AddSingleton(sp => new LevelPreviewer(
    sp.GetRequiredService<ContentCatalog>(),
    sp.GetRequiredService<IOptions<GameOptions>>().Value,
    sp.GetRequiredService<IOptions<GenerationOptions>>().Value));
builder.Services.AddSingleton(sp => new DevActions(
    sp.GetRequiredService<SessionRegistry>(),
    sp.GetRequiredService<ContentCatalog>(),
    sp.GetRequiredService<IOptions<PatternOptions>>().Value));

// Bridge: notifiers (what to send) over channels (how), both picked from Realtime settings.
builder.Services.AddSingleton(sp => new PollingBufferChannel(sp.GetRequiredService<IOptions<RealtimeOptions>>().Value.PollingBufferSize));
builder.Services.AddSingleton<SignalRClientChannel>();
builder.Services.AddSingleton<IReadOnlyList<ClientNotifier>>(sp =>
{
    var realtime = sp.GetRequiredService<IOptions<RealtimeOptions>>().Value;
    var channels = realtime.EnabledChannels.Distinct().Select(c => c switch
    {
        RealtimeChannel.SignalR => (IClientChannel)sp.GetRequiredService<SignalRClientChannel>(),
        _ => sp.GetRequiredService<PollingBufferChannel>(),
    });
    return ClientNotifiers.For(channels, new Dictionary<string, int> { ["Polling"] = realtime.PollingStateEveryNthTick });
});
builder.Services.AddHostedService<GameLoopService>();

// Azure Linux/container hosting sets PORT; Azure App Service and local dev don't.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

// Load and validate content now, so a broken content file stops startup instead of the first request.
app.Services.GetRequiredService<ContentCatalog>();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

app.MapOpenApi(); // /openapi/v1.json
app.MapScalarApiReference(options => options
    .WithTitle("Castle Escape API")
    .AddPreferredSecuritySchemes(OpenApiSetup.PlayerTokenScheme)
    .EnablePersistentAuthentication()); // /scalar
app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();

app.MapControllers();
if (app.Services.GetRequiredService<IOptions<DevToolsOptions>>().Value.Enabled)
{
    app.UseStaticFiles(); // wwwroot holds only the playground, a dev tool
    app.MapGet("/playground", () => Results.Redirect("/playground/index.html")).ExcludeFromDescription();
}
app.MapHub<GameHub>(RealtimeController.HubPath);

app.Run();

return 0;

static void AddValidatedOptions<TOptions>(IServiceCollection services, string section) where TOptions : class =>
    services.AddOptions<TOptions>()
        .BindConfiguration(section)
        .ValidateDataAnnotations()
        .ValidateOnStart();

/// <summary>Removes one controller from the app, as if it did not exist.</summary>
sealed class WithoutController<TController> : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) =>
        feature.Controllers.Remove(typeof(TController).GetTypeInfo());
}
