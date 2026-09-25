using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CastleEscapeClient.Models;

public class ContentCatalog
{
    public List<CharacterDefinition> Characters { get; set; } = new();
    public List<Consumable> Consumables { get; set; } = new();
    public List<PowerCombo> Combos { get; set; } = new();
    public List<Obstacle> Obstacles { get; set; } = new();
    public List<ZombieDefinition> Zombies { get; set; } = new();
    public List<Level> Levels { get; set; } = new();

    public List<string> Validate()
    {
        var errors = new List<string>();

        CheckIds(Characters, c => c.Id, "Character", errors);
        CheckIds(Consumables, c => c.Id, "Consumable", errors);
        CheckIds(Combos, c => c.Id, "PowerCombo", errors);
        CheckIds(Obstacles, o => o.Id, "Obstacle", errors);
        CheckIds(Zombies, z => z.Id, "ZombieDefinition", errors);
        CheckIds(Levels, l => l.Id, "Level", errors);

        foreach (var obstacle in Obstacles)
        {
            if (obstacle.MinSize > obstacle.MaxSize)
            {
                errors.Add($"Obstacle '{obstacle.Id}' has MinSize greater than MaxSize.");
            }
        }

        var obstacleIds = Obstacles.Select(o => o.Id).ToHashSet();
        var zombieIds = Zombies.Select(z => z.Id).ToHashSet();
        var consumableIds = Consumables.Select(c => c.Id).ToHashSet();

        foreach (var level in Levels)
        {
            foreach (var obstacle in level.ObstacleTable)
            {
                if (!obstacleIds.Contains(obstacle.Id))
                {
                    errors.Add($"Level '{level.Id}' references unknown obstacle '{obstacle.Id}'.");
                }
            }
            foreach (var zombie in level.ZombieTable)
            {
                if (!zombieIds.Contains(zombie.Id))
                {
                    errors.Add($"Level '{level.Id}' references unknown zombie '{zombie.Id}'.");
                }
            }
            foreach (var consumable in level.ConsumableTable)
            {
                if (!consumableIds.Contains(consumable.Id))
                {
                    errors.Add($"Level '{level.Id}' references unknown consumable '{consumable.Id}'.");
                }
            }
        }

        return errors;
    }

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

    private static void CheckIds<T>(List<T> items, Func<T, string> idSelector, string typeName, List<string> errors)
    {
        var seen = new HashSet<string>();
        foreach (var item in items)
        {
            var id = idSelector(item);
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add($"{typeName} has a missing Id.");
                continue;
            }
            if (!seen.Add(id))
            {
                errors.Add($"{typeName} has a duplicate Id '{id}'.");
            }
        }
    }
}
