using CasteEscapeServer.Game;
using CasteEscapeServer.Hubs;

if (args.Contains("selftest"))
{
    var passed = GameLogicSelfCheck.Run();
    return passed ? 0 : 1;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<SessionManager>();

// Azure Linux/container hosting sets PORT; Azure App Service and local dev don't.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

app.MapHub<GameHub>("/gamehub");

app.Run();

return 0;
