namespace CastleEscape.Domain.Content;

/// <summary>Game-content rules, independent of JSON and catalog lifetime.</summary>
public sealed class ContentValidator(IContentCatalog catalog)
{
    private static readonly PowerType[] BasePowers = [PowerType.Jump, PowerType.Sprint, PowerType.Swim];
    private IReadOnlyList<CharacterDefinition> Characters => catalog.Characters;
    private IReadOnlyList<Consumable> Consumables => catalog.Consumables;
    private IReadOnlyList<PowerCombo> Combos => catalog.Combos;
    private IReadOnlyList<Obstacle> Obstacles => catalog.Obstacles;
    private IReadOnlyList<ZombieDefinition> Zombies => catalog.Zombies;
    private IReadOnlyList<LevelDefinition> Levels => catalog.Levels;

    public List<string> Validate()
    {
        var errors = new List<string>();

        CheckIds(Characters, c => c.Id, "Character", errors);
        CheckIds(Consumables, c => c.Id, "Consumable", errors);
        CheckIds(Combos, c => c.Id, "PowerCombo", errors);
        CheckIds(Obstacles, o => o.Id, "Obstacle", errors);
        CheckIds(Zombies, z => z.Id, "ZombieDefinition", errors);
        CheckIds(Levels, l => l.Id, "Level", errors);

        ValidateCharacters(errors);
        ValidateConsumables(errors);
        ValidateCombos(errors);
        ValidateObstacles(errors);
        ValidateZombies(errors);
        ValidateLevels(errors);

        return errors;
    }

    private void ValidateCharacters(List<string> errors)
    {
        // Only enforced once content exists; an empty catalog is valid (unit tests build partial ones).
        if (Characters.Count is > 0 and < 3)
        {
            errors.Add($"PLR-1 needs at least 3 characters, found {Characters.Count}.");
        }
        foreach (var character in Characters)
        {
            if (character.MaxHealth <= 0) errors.Add($"Character '{character.Id}' needs MaxHealth > 0.");
            if (character.BaseMoveSpeed <= 0) errors.Add($"Character '{character.Id}' needs BaseMoveSpeed > 0.");
            if (character.BaseJumpForce <= 0) errors.Add($"Character '{character.Id}' needs BaseJumpForce > 0.");
        }
    }

    private void ValidateConsumables(List<string> errors)
    {
        foreach (var item in Consumables)
        {
            switch (item.Kind)
            {
                case ConsumableKind.Power when item.Grant is null:
                    errors.Add($"Power item '{item.Id}' has no Grant.");
                    break;
                case ConsumableKind.Health when item.HealthValue <= 0:
                    errors.Add($"Health item '{item.Id}' needs HealthValue > 0.");
                    break;
                case ConsumableKind.Reward when item.ScoreValue <= 0:
                    errors.Add($"Reward item '{item.Id}' needs ScoreValue > 0.");
                    break;
            }
            if (item.Kind != ConsumableKind.Power && item.Grant is not null)
            {
                errors.Add($"Item '{item.Id}' is not a Power item but has a Grant.");
            }
            if (item.Grant?.DurationSeconds is <= 0)
            {
                errors.Add($"Power item '{item.Id}' needs DurationSeconds > 0.");
            }
        }

        if (Consumables.Count > 0)
        {
            var granted = Consumables.Select(c => c.Grant?.Power).OfType<PowerType>().ToHashSet();
            foreach (var power in BasePowers.Where(p => !granted.Contains(p)))
            {
                errors.Add($"PWR-1: no item grants the base power {power}.");
            }
        }
    }

    private void ValidateCombos(List<string> errors)
    {
        foreach (var combo in Combos)
        {
            var required = combo.RequiredPowers.Distinct().ToList();
            if (required.Count < 2 || required.Count != combo.RequiredPowers.Count)
            {
                errors.Add($"Combo '{combo.Id}' needs at least two different required powers (PWR-2).");
            }
            if (required.Any(p => !BasePowers.Contains(p)))
            {
                errors.Add($"Combo '{combo.Id}' may only require base powers (Jump, Sprint, Swim).");
            }
            if (BasePowers.Contains(combo.Granted) || combo.Granted == PowerType.None)
            {
                errors.Add($"Combo '{combo.Id}' must grant a super power, not {combo.Granted}.");
            }
        }
    }

    private void ValidateObstacles(List<string> errors)
    {
        foreach (var obstacle in Obstacles)
        {
            if (obstacle.MinSize < 1 || obstacle.MinSize > obstacle.MaxSize)
            {
                errors.Add($"Obstacle '{obstacle.Id}' needs 1 <= MinSize <= MaxSize.");
            }
            var needsPower = obstacle.TraversalRequirements != PowerType.None;
            if (obstacle.Kind == ObstacleKind.Wall && needsPower)
            {
                errors.Add($"Wall '{obstacle.Id}' must have TraversalRequirements None.");
            }
            if (obstacle.Kind != ObstacleKind.Wall && !needsPower)
            {
                errors.Add($"Obstacle '{obstacle.Id}' must name the power that crosses it (MOV-2).");
            }
        }
        if (Obstacles.Count > 0 && Obstacles.All(o => o.Kind != ObstacleKind.Wall))
        {
            errors.Add("At least one Wall obstacle is needed for level borders.");
        }
    }

    private void ValidateZombies(List<string> errors)
    {
        foreach (var zombie in Zombies)
        {
            if (zombie.Speed <= 0) errors.Add($"Zombie '{zombie.Id}' needs Speed > 0.");
            if (zombie.ContactDamage <= 0) errors.Add($"Zombie '{zombie.Id}' needs ContactDamage > 0 (ZMB-2).");
        }
    }

    private void ValidateLevels(List<string> errors)
    {
        var ordered = Levels.OrderBy(l => l.Index).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].Index != i + 1)
            {
                errors.Add($"Level indexes must run 1..{ordered.Count} without gaps; found {ordered[i].Index} at position {i + 1}.");
                break;
            }
        }
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].ZombieCount < ordered[i - 1].ZombieCount)
            {
                errors.Add($"ZMB-4: level {ordered[i].Index} has fewer zombies than level {ordered[i - 1].Index}.");
            }
        }

        foreach (var level in Levels)
        {
            if (level.RoomSize is { } size && (size.Width < 20 || size.Height < 15))
            {
                errors.Add($"LVL-2: level '{level.Id}' room {size.Width}x{size.Height} is smaller than 20x15.");
            }
            if (level.ZombieCount < 0 || level.PickupCount < 0 || level.ObstacleCount < 1)
            {
                errors.Add($"Level '{level.Id}' needs ObstacleCount >= 1 (GEN-3) and non-negative counts.");
            }

            CheckTable(level, level.ObstacleTable, "obstacle", Obstacles.Select(o => o.Id), errors);
            CheckTable(level, level.ZombieTable, "zombie", Zombies.Select(z => z.Id), errors);
            CheckTable(level, level.ConsumableTable, "consumable", Consumables.Select(c => c.Id), errors);

            var hasPowerObstacle = level.ObstacleTable
                .Select(e => Obstacles.FirstOrDefault(o => o.Id == e.Id))
                .Any(o => o is not null && o.TraversalRequirements != PowerType.None);
            if (!hasPowerObstacle)
            {
                errors.Add($"GEN-3: level '{level.Id}' has no obstacle that requires a power in its ObstacleTable.");
            }
            if (level.ZombieCount > 0 && level.ZombieTable.Count == 0)
            {
                errors.Add($"Level '{level.Id}' has zombies but an empty ZombieTable.");
            }
            if (level.PickupCount > 0 && level.ConsumableTable.Count == 0)
            {
                errors.Add($"Level '{level.Id}' has pickups but an empty ConsumableTable.");
            }
        }
    }

    private static void CheckTable(LevelDefinition level, IReadOnlyList<SpawnTableEntry> table, string what,
        IEnumerable<string> knownIds, List<string> errors)
    {
        var known = knownIds.ToHashSet();
        foreach (var entry in table)
        {
            if (!known.Contains(entry.Id))
            {
                errors.Add($"Level '{level.Id}' references unknown {what} '{entry.Id}'.");
            }
            if (entry.Weight <= 0)
            {
                errors.Add($"Level '{level.Id}' {what} '{entry.Id}' needs Weight > 0.");
            }
        }
    }

    private static void CheckIds<T>(IReadOnlyList<T> items, Func<T, string> idSelector, string typeName, List<string> errors)
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
