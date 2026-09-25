using System.Text.Json;
using System.Text.Json.Serialization;

namespace CastleEscape.Game.Content;

/// <summary>Reads the content JSON files into a validated <see cref="ContentCatalog"/>.</summary>
public static class ContentLoader
{
    /// <summary>The <c>content</c> folder copied next to the running assembly.</summary>
    public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "content");

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // Lets "kind"/"power" appear anywhere in an object, not only first.
        AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static ContentCatalog LoadFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Content directory not found: {directory}");
        }

        var catalog = new ContentCatalog
        {
            Characters = Read<CharacterDefinition>(directory, "characters.json"),
            Consumables = Read<Consumable>(directory, "consumables.json"),
            Combos = Read<PowerCombo>(directory, "combos.json"),
            Obstacles = Read<Obstacle>(directory, "obstacles.json"),
            Zombies = Read<ZombieDefinition>(directory, "zombies.json"),
            Levels = Read<LevelDefinition>(directory, "levels.json"),
        };

        catalog.ValidateOrThrow();
        return catalog;
    }

    private static IReadOnlyList<T> Read<T>(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        using var stream = File.OpenRead(path);
        try
        {
            return JsonSerializer.Deserialize<List<T>>(stream, JsonOptions)
                   ?? throw new InvalidDataException($"{fileName} is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{fileName}: {ex.Message}", ex);
        }
    }
}
