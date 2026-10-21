using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CastleEscape.Contracts;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.Content;

/// <summary>
/// All static game content: characters, items, combos, obstacles, zombie types and levels.
/// Singleton: loaded once from <c>content/*.json</c> on first use of <see cref="Instance"/>,
/// and shared (read-only) by every session, generator and endpoint.
/// Not thread-safe: the server reads <see cref="Instance"/> once at startup, before any other thread.
/// </summary>
[DesignPattern("Singleton", "Singleton")]
[DesignPattern("Facade", "Subsystem")]
public sealed class ContentCatalog
{
    private static ContentCatalog? _instance;

    /// <summary>The one shared catalog, loaded on first access.</summary>
    public static ContentCatalog Instance => _instance ??= Create();

    private static ContentCatalog Create() => new(ContentLoader.ReadDirectory(ContentLoader.DefaultDirectory));

    // Private: nobody outside can make a second catalog.
    private ContentCatalog(ContentData data) => Data = data;

    public ContentData Data { get; }

    public CharacterDefinition? FindCharacter(string id) => Data.Characters.FirstOrDefault(c => c.Id == id);

    public CharacterDefinition GetCharacter(string id) =>
        FindCharacter(id) ?? throw new KeyNotFoundException($"Unknown character '{id}'.");

    public Consumable GetConsumable(string id) =>
        Data.Consumables.FirstOrDefault(c => c.Id == id) ?? throw new KeyNotFoundException($"Unknown consumable '{id}'.");

    public Obstacle GetObstacle(string id) =>
        Data.Obstacles.FirstOrDefault(o => o.Id == id) ?? throw new KeyNotFoundException($"Unknown obstacle '{id}'.");

    public ZombieDefinition GetZombie(string id) =>
        Data.Zombies.FirstOrDefault(z => z.Id == id) ?? throw new KeyNotFoundException($"Unknown zombie '{id}'.");

    /// <summary>The level at a 1-based position in the sequence.</summary>
    public LevelDefinition GetLevel(int index) =>
        Data.Levels.FirstOrDefault(l => l.Index == index) ?? throw new KeyNotFoundException($"Unknown level index {index}.");

    public string ComputeContentHash()
    {
        var json = JsonSerializer.Serialize(Data);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
