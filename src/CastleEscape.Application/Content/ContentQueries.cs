using CastleEscape.Contracts;
using CastleEscape.Contracts.Content;
using CastleEscape.Domain.Configuration;
using CastleEscape.Domain.Content;

namespace CastleEscape.Application.Content;

/// <summary>Public content views. Domain definitions never cross the API boundary directly.</summary>
public sealed class ContentQueries(IContentCatalog catalog, GameOptions options)
{
    public ContentSummaryResponse Summary() => new(catalog.ComputeContentHash(), catalog.Characters.Count,
        catalog.Consumables.Count, catalog.Combos.Count, catalog.Obstacles.Count, catalog.Zombies.Count, catalog.Levels.Count);
    public CharacterDto[] Characters() => catalog.Characters.Select(c => new CharacterDto(c.Id, c.Name, c.MaxHealth,
        c.BaseMoveSpeed, c.BaseJumpForce, Modifiers(c.BaseModifiers))).ToArray();
    public ConsumableDto[] Consumables() => catalog.Consumables.Select(c => new ConsumableDto(c.Kind.ToString(), c.Id,
        c.Name, c.Kind.ToString(), Grant(c.Grant), c.HealthValue, c.ScoreValue, c.SpawnWeight, c.IconPath)).ToArray();
    public PowerComboDto[] Combos() => catalog.Combos.Select(c => new PowerComboDto(c.Id, c.Name,
        c.RequiredPowers.Select(p => (PowerType)p).ToArray(), (PowerType)c.Granted, Modifiers(c.Modifiers))).ToArray();
    public ObstacleDto[] Obstacles() => catalog.Obstacles.Select(o => new ObstacleDto(o.Kind.ToString(), o.Id, o.Name,
        o.Kind.ToString(), (PowerType)o.TraversalRequirements, o.BlocksZombies, o.MinSize, o.MaxSize, o.MoveSpeedMultiplier)).ToArray();
    public ZombieDefinitionDto[] Zombies() => catalog.Zombies.Select(z => new ZombieDefinitionDto(z.Id, z.Name,
        z.Speed, z.ContactDamage, z.AttackCooldownSeconds, z.Size, (MovementStrategyKind)z.MovementStrategy)).ToArray();
    public LevelDefinitionDto[] Levels() => catalog.Levels.Select(Level).ToArray();
    public LevelDefinitionDto? FindLevel(int index) => catalog.Levels.FirstOrDefault(l => l.Index == index) is { } l ? Level(l) : null;
    public PowerSummary[] Powers() => catalog.Consumables.Where(c => c.Grant is not null).GroupBy(c => c.Grant!.Power)
        .OrderBy(g => g.Key).Select(g => new PowerSummary((PowerType)g.Key, false,
            g.First().Grant!.DurationSeconds ?? options.PowerDurationSeconds, g.Select(c => c.Id).ToArray(), []))
        .Concat(catalog.Combos.Select(c => new PowerSummary((PowerType)c.Granted, true, null, [],
            c.RequiredPowers.Select(p => (PowerType)p).ToArray()))).ToArray();

    private static StatModifiersDto Modifiers(StatModifiers m) => new(m.MoveSpeedMultiplier, m.JumpForceMultiplier, m.SwimSpeedMultiplier);
    private static PowerGrantDto? Grant(PowerGrant? g) => g is null ? null : new(g.Power.ToString(), (PowerType)g.Power, g.DurationSeconds, Modifiers(g.Modifiers));
    private static SpawnTableEntryDto[] Table(IReadOnlyList<SpawnTableEntry> entries) => entries.Select(e => new SpawnTableEntryDto(e.Id, e.Weight)).ToArray();
    private static LevelDefinitionDto Level(LevelDefinition l) => new(l.Id, l.Index, (LevelTheme)l.Theme,
        l.RoomSize is { } s ? new GridSizeDto(s.Width, s.Height) : null, l.ObstacleCount, Table(l.ObstacleTable),
        l.ZombieCount, Table(l.ZombieTable), l.PickupCount, Table(l.ConsumableTable), l.TimeLimitSeconds);
}
