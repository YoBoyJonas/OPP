using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

var url = args.Length > 0 ? args[0] : "http://localhost:5035/gamehub";
Console.Write("Player name: ");
var name = Console.ReadLine() ?? "Player";
Console.Write("Character (warrior/scout/swimmer, default warrior): ");
var character = Console.ReadLine();
if (string.IsNullOrWhiteSpace(character)) character = "warrior";

var connection = new HubConnectionBuilder().WithUrl(url).WithAutomaticReconnect().Build();

connection.On<object>("GameStateUpdate", snapshot =>
    Console.WriteLine(JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false })));
connection.On("OpponentLeft", () => Console.WriteLine("*** opponent left ***"));

await connection.StartAsync();
var join = await connection.InvokeAsync<object>("JoinSession", name, character);
Console.WriteLine(JsonSerializer.Serialize(join));
Console.WriteLine("Waiting for opponent... use arrow keys to move once the game starts, Esc to quit.");

while (true)
{
    var key = Console.ReadKey(intercept: true).Key;
    var direction = key switch
    {
        ConsoleKey.UpArrow => "Up",
        ConsoleKey.DownArrow => "Down",
        ConsoleKey.LeftArrow => "Left",
        ConsoleKey.RightArrow => "Right",
        ConsoleKey.Escape => "quit",
        _ => null
    };

    if (direction == "quit") break;
    if (direction == null) continue;

    await connection.SendAsync("SendInput", new { Direction = direction });
}

await connection.StopAsync();
