using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation.Themes;

/// <summary>Concrete factory for levels 6-10, the harder part: slower water and pits, faster and smarter zombies.</summary>
public sealed class CryptThemeFactory(Wall wallTemplate, Water waterTemplate, Pit pitTemplate) : IThemeFactory
{
    public Wall CreateWall() => new BoneWall(wallTemplate);
    public Water CreateWater() => new PoisonWater(waterTemplate);
    public Pit CreatePit() => new AbyssPit(pitTemplate);
    public ZombieEntity CreateZombie(string id, ZombieDefinition definition, GridPos spawn) => new CryptZombie(id, definition, spawn);
}

public sealed class BoneWall : Wall
{
    public BoneWall(Wall template) : base(template)
    {
        Id = "bone-wall";
        Name = "Bone wall";
    }
}

/// <summary>Thick poisoned water: swimming is slower than in the Dungeon.</summary>
public sealed class PoisonWater : Water
{
    public const double SpeedMultiplier = 0.45;

    public PoisonWater(Water template) : base(template)
    {
        Id = "poison-water";
        Name = "Poison water";
        MoveSpeedMultiplier = SpeedMultiplier;
    }
}

/// <summary>A wider gap: jumping across takes longer.</summary>
public sealed class AbyssPit : Pit
{
    public const double SpeedMultiplier = 0.8;

    public AbyssPit(Pit template) : base(template)
    {
        Id = "abyss-pit";
        Name = "Abyss";
        MoveSpeedMultiplier = SpeedMultiplier;
    }
}

/// <summary>A faster zombie; simple Greedy chasers become path-finding (BFS) ones.</summary>
public sealed class CryptZombie(string id, ZombieDefinition definition, GridPos spawn) : ZombieEntity(id, definition, spawn)
{
    public const double SpeedBonus = 1.2;

    public override double Speed => Definition.Speed * SpeedBonus;

    public override MovementStrategyKind MovementStrategy =>
        Definition.MovementStrategy == MovementStrategyKind.Greedy ? MovementStrategyKind.Bfs : Definition.MovementStrategy;
}
