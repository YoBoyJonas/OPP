using CastleEscape.Domain.Content;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CastleEscape.Domain;
using CastleEscape.Domain.Patterns;

namespace CastleEscape.Infrastructure.Content;

/// <summary>
/// All static game content: characters, items, combos, obstacles, zombie types and levels.
/// Singleton: loaded once from <c>content/*.json</c> on first use of <see cref="Instance"/>, validated,
/// and shared (read-only) by every session, generator and endpoint.
/// </summary>
[DesignPattern("Singleton", "Singleton")]
[DesignPattern("Facade", "Subsystem")]
public sealed class ContentCatalog : IContentCatalog
{
    // ExecutionAndPublication: if many threads ask at once, exactly one runs the factory; all get its result.
    private static readonly Lazy<ContentCatalog> LazyInstance = new(CreateInstance, LazyThreadSafetyMode.ExecutionAndPublication);
    private static int _instanceCreations;

    /// <summary>The one shared catalog. Thread-safe; loads and validates the content on first access.</summary>
    public static ContentCatalog Instance => LazyInstance.Value;

    /// <summary>How many times <see cref="Instance"/> has been created (always 0 or 1). Used by the demo.</summary>
    public static int InstanceCreations => Volatile.Read(ref _instanceCreations);

    private static ContentCatalog CreateInstance()
    {
        Interlocked.Increment(ref _instanceCreations);
        var catalog = new ContentCatalog(ContentLoader.ReadDirectory(ContentLoader.DefaultDirectory));
        catalog.ValidateOrThrow();
        return catalog;
    }

    // Private: nobody outside can make a second catalog.
    private ContentCatalog(ContentData data)
    {
        Characters = data.Characters;
        Consumables = data.Consumables;
        Combos = data.Combos;
        Obstacles = data.Obstacles;
        Zombies = data.Zombies;
        Levels = data.Levels;
    }

    /// <summary>A separate, unvalidated catalog for unit tests of the validation rules. Not for game code.</summary>
    internal static ContentCatalog FromData(ContentData data) => new(data);

    public IReadOnlyList<CharacterDefinition> Characters { get; }
    public IReadOnlyList<Consumable> Consumables { get; }
    public IReadOnlyList<PowerCombo> Combos { get; }
    public IReadOnlyList<Obstacle> Obstacles { get; }
    public IReadOnlyList<ZombieDefinition> Zombies { get; }
    public IReadOnlyList<LevelDefinition> Levels { get; }

    public CharacterDefinition? FindCharacter(string id) => Characters.FirstOrDefault(c => c.Id == id);

    public CharacterDefinition GetCharacter(string id) =>
        FindCharacter(id) ?? throw new KeyNotFoundException($"Unknown character '{id}'.");

    public Consumable GetConsumable(string id) =>
        Consumables.FirstOrDefault(c => c.Id == id) ?? throw new KeyNotFoundException($"Unknown consumable '{id}'.");

    public Obstacle GetObstacle(string id) =>
        Obstacles.FirstOrDefault(o => o.Id == id) ?? throw new KeyNotFoundException($"Unknown obstacle '{id}'.");

    public ZombieDefinition GetZombie(string id) =>
        Zombies.FirstOrDefault(z => z.Id == id) ?? throw new KeyNotFoundException($"Unknown zombie '{id}'.");

    /// <summary>The level at a 1-based position in the sequence.</summary>
    public LevelDefinition GetLevel(int index) =>
        Levels.FirstOrDefault(l => l.Index == index) ?? throw new KeyNotFoundException($"Unknown level index {index}.");

    public List<string> Validate() => new ContentValidator(this).Validate();

    public void ValidateOrThrow()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }
    }

    public string ComputeContentHash()
    {
        var json = JsonSerializer.Serialize(this);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

}
