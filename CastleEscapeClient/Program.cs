using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

var url = args.Length > 0 ? args[0] : "https://castleescapeserver-fufhf7dvg5aja3d7.swedencentral-01.azurewebsites.net/gamehub";
Console.Clear();
Console.Write("Player name: ");
var name = Console.ReadLine() ?? "Player";
Console.Write("Character (warrior/scout/swimmer, default warrior): ");
var character = Console.ReadLine();
if (string.IsNullOrWhiteSpace(character)) character = "warrior";

var connection = new HubConnectionBuilder().WithUrl(url).WithAutomaticReconnect().Build();

const int W = 22, H = 16;
var frame = new object();

// Terrain mirrors the server's LevelBuilder (deterministic); snapshots only carry what moves.
char[,] Terrain(int level)
{
    var g = new char[W, H];
    for (var x = 0; x < W; x++)
        for (var y = 0; y < H; y++)
            g[x, y] = x == 0 || y == 0 || x == W - 1 || y == H - 1 ? '#' : '.';
    for (var x = 2; x < W - 2; x++)
        if (x != W - 3) g[x, H / 2] = level % 2 == 1 ? '~' : 'O';
    return g;
}

void Draw(JsonElement s)
{
    var level = s.GetProperty("levelIndex").GetInt32();
    var open = s.GetProperty("doorOpen").GetBoolean();
    var g = Terrain(level);
    g[W - 2, 1] = g[W - 2, 2] = open ? '_' : 'X';
    foreach (var i in s.GetProperty("items").EnumerateArray())
        g[i.GetProperty("x").GetInt32(), i.GetProperty("y").GetInt32()] = i.GetProperty("kind").GetInt32() == 0 ? '*' : '+';
    foreach (var l in s.GetProperty("levers").EnumerateArray())
        g[l.GetProperty("x").GetInt32(), l.GetProperty("y").GetInt32()] = l.GetProperty("isActive").GetBoolean() ? '/' : '\\';
    void Put(JsonElement e, char c)
    {
        var x = (int)Math.Round(e.GetProperty("x").GetDouble());
        var y = (int)Math.Round(e.GetProperty("y").GetDouble());
        if (x >= 0 && y >= 0 && x < W && y < H) g[x, y] = c;
    }
    foreach (var z in s.GetProperty("zombies").EnumerateArray()) Put(z, 'Z');
    var n = 1;
    var info = new System.Text.StringBuilder();
    foreach (var p in s.GetProperty("players").EnumerateArray())
    {
        Put(p, (char)('0' + n));
        info.AppendLine($"P{n} {p.GetProperty("name").GetString()}: HP {p.GetProperty("health")}/{p.GetProperty("maxHealth")} powers {p.GetProperty("activePowers")}");
        n++;
    }

    var sb = new System.Text.StringBuilder();
    for (var y = 0; y < H; y++)
    {
        for (var x = 0; x < W; x++) sb.Append(g[x, y]).Append(' ');
        sb.AppendLine();
    }
    sb.AppendLine($"Level {level}  door {(open ? "OPEN" : "closed")}");
    sb.Append(info);
    sb.AppendLine("# wall  ~ water  O pit  Z zombie  * power  + health  \\ / lever  X/_ exit");
    sb.AppendLine("Arrows move, Space stops, Esc quits.");
    if (s.GetProperty("isWin").GetBoolean()) sb.AppendLine("*** YOU WIN ***");
    else if (s.GetProperty("isGameOver").GetBoolean()) sb.AppendLine("*** GAME OVER ***");

    lock (frame)
    {
        Console.SetCursorPosition(0, 0);
        Console.Write(sb.ToString().Replace("\n", "\x1b[K\n"));
        Console.Write("\x1b[J");
    }
}

connection.On<JsonElement>("GameStateUpdate", Draw);
connection.On("OpponentLeft", () => { lock (frame) Console.WriteLine("*** opponent left ***"); });

await connection.StartAsync();
var join = await connection.InvokeAsync<object>("JoinSession", name, character);
Console.Clear();
Console.WriteLine("Joined. Waiting for opponent... (Esc to quit)");

while (true)
{
    var key = Console.ReadKey(intercept: true).Key;
    int? direction = key switch
    {
        ConsoleKey.UpArrow => 1,
        ConsoleKey.DownArrow => 2,
        ConsoleKey.LeftArrow => 3,
        ConsoleKey.RightArrow => 4,
        ConsoleKey.Spacebar => 0,
        ConsoleKey.Escape => -1,
        _ => null
    };

    if (direction == -1) break;
    if (direction == null) continue;

    await connection.SendAsync("SendInput", new { Direction = direction });
}

await connection.StopAsync();
