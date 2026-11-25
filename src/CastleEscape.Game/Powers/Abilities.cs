using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Powers;

/// <summary>What a player can do right now: which terrain they may enter and how fast they move over it.</summary>
public interface IAbilities
{
    /// <summary>Tiles per second on floor.</summary>
    double MoveSpeed { get; }

    /// <summary>MOV-2: whether the player may enter this terrain (walls and closed doors are checked elsewhere).</summary>
    bool CanEnter(TerrainKind terrain);

    /// <summary>Tiles per second while stepping onto <paramref name="tile"/>.</summary>
    double SpeedOn(Tile tile);
}

/// <summary>A character with no powers: floor only; water and pits are closed.</summary>
public sealed class CharacterAbilities(Character character) : IAbilities
{
    private StatModifiers Mods => character.BaseModifiers;

    public double MoveSpeed => character.BaseMoveSpeed * Mods.MoveSpeedMultiplier;

    public bool CanEnter(TerrainKind terrain) => terrain is not (TerrainKind.Water or TerrainKind.Pit);

    public double SpeedOn(Tile tile) => tile.Terrain switch
    {
        // The rates for water and pits apply once a decorator lets the player in.
        TerrainKind.Water => MoveSpeed * (tile.Obstacle?.MoveSpeedMultiplier ?? 1.0) * Mods.SwimSpeedMultiplier,
        // Decision C9: the jump parameter is the speed factor over pits.
        TerrainKind.Pit => MoveSpeed * (tile.Obstacle?.MoveSpeedMultiplier ?? 1.0) * character.BaseJumpForce * Mods.JumpForceMultiplier,
        _ => MoveSpeed,
    };
}

/// <summary>Wraps another <see cref="IAbilities"/> and passes everything through; subclasses change one thing.</summary>
public abstract class AbilityDecorator(IAbilities inner) : IAbilities
{
    /// <summary>The wrapped abilities (the next layer inwards).</summary>
    public IAbilities Inner { get; } = inner;

    public virtual double MoveSpeed => Inner.MoveSpeed;
    public virtual bool CanEnter(TerrainKind terrain) => Inner.CanEnter(terrain);
    public virtual double SpeedOn(Tile tile) => Inner.SpeedOn(tile);
}

/// <summary>Jump (PWR-1): pits become passable, and faster by the item's jump multiplier.</summary>
public sealed class Jump(IAbilities inner, StatModifiers modifiers) : AbilityDecorator(inner)
{
    public override bool CanEnter(TerrainKind terrain) => terrain == TerrainKind.Pit || Inner.CanEnter(terrain);
    public override double SpeedOn(Tile tile) =>
        Inner.SpeedOn(tile) * (tile.Terrain == TerrainKind.Pit ? modifiers.JumpForceMultiplier : 1.0);
}

/// <summary>Sprint: faster everywhere.</summary>
public sealed class Sprint(IAbilities inner, StatModifiers modifiers) : AbilityDecorator(inner)
{
    public override double MoveSpeed => Inner.MoveSpeed * modifiers.MoveSpeedMultiplier;
    public override double SpeedOn(Tile tile) => Inner.SpeedOn(tile) * modifiers.MoveSpeedMultiplier;
}

/// <summary>Swim: water becomes passable, and faster by the item's swim multiplier.</summary>
public sealed class Swim(IAbilities inner, StatModifiers modifiers) : AbilityDecorator(inner)
{
    public override bool CanEnter(TerrainKind terrain) => terrain == TerrainKind.Water || Inner.CanEnter(terrain);
    public override double SpeedOn(Tile tile) =>
        Inner.SpeedOn(tile) * (tile.Terrain == TerrainKind.Water ? modifiers.SwimSpeedMultiplier : 1.0);
}

/// <summary>JumpDash combo (Jump + Sprint, PWR-2): faster everywhere, and even faster over pits.</summary>
public sealed class JumpDash(IAbilities inner, StatModifiers modifiers) : AbilityDecorator(inner)
{
    public override double MoveSpeed => Inner.MoveSpeed * modifiers.MoveSpeedMultiplier;
    public override double SpeedOn(Tile tile) =>
        Inner.SpeedOn(tile) * modifiers.MoveSpeedMultiplier * (tile.Terrain == TerrainKind.Pit ? modifiers.JumpForceMultiplier : 1.0);
}

/// <summary>
/// FastSwim combo (Sprint + Swim, PWR-2): water no longer slows the player down. Water speed is the
/// land speed times the combo's bonus; what the inner chain says about water is replaced, not multiplied.
/// </summary>
public sealed class FastSwim(IAbilities inner, StatModifiers modifiers) : AbilityDecorator(inner)
{
    public override double SpeedOn(Tile tile) =>
        tile.Terrain == TerrainKind.Water ? Inner.MoveSpeed * modifiers.SwimSpeedMultiplier : Inner.SpeedOn(tile);
}
