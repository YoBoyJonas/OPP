using CastleEscape.Game.Content;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Powers;

/// <summary>What a player can do right now: which terrain they may enter and how fast they move over it.</summary>
[DesignPattern("Decorator", "Component")]
public interface IAbilities
{
    /// <summary>Tiles per second on floor.</summary>
    double MoveSpeed { get; }

    /// <summary>MOV-2: whether the player may enter this terrain (walls and closed doors are checked elsewhere).</summary>
    bool CanEnter(TerrainKind terrain);

    /// <summary>Tiles per second while stepping onto <paramref name="tile"/>.</summary>
    double SpeedOn(Tile tile);

    /// <summary>The chain, innermost first, e.g. "Scout + Sprint + Sprint + Jump + JumpDash".</summary>
    string Describe();
}

/// <summary>A character with no powers: floor only; water and pits are closed.</summary>
[DesignPattern("Decorator", "ConcreteComponent")]
public sealed class CharacterAbilities(CharacterDefinition character) : IAbilities
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

    public string Describe() => character.Name;
}

/// <summary>Helpers for looking at a decorator chain.</summary>
public static class AbilityChain
{
    /// <summary>Number of decorators around the character (0 = no powers).</summary>
    public static int Depth(IAbilities abilities)
    {
        var depth = 0;
        while (abilities is AbilityDecorator decorator)
        {
            depth++;
            abilities = decorator.Inner;
        }
        return depth;
    }
}

/// <summary>Wraps another <see cref="IAbilities"/> and passes everything through; subclasses change one thing.</summary>
[DesignPattern("Decorator", "Decorator")]
public abstract class AbilityDecorator(IAbilities inner) : IAbilities
{
    /// <summary>The wrapped abilities (the next layer inwards).</summary>
    public IAbilities Inner { get; } = inner;

    public virtual double MoveSpeed => Inner.MoveSpeed;
    public virtual bool CanEnter(TerrainKind terrain) => Inner.CanEnter(terrain);
    public virtual double SpeedOn(Tile tile) => Inner.SpeedOn(tile);

    public string Describe() => $"{Inner.Describe()} + {Name}";

    protected abstract string Name { get; }
}

/// <summary>Jump (PWR-1): pits become passable, and faster by the item's jump multiplier.</summary>
[DesignPattern("Decorator", "ConcreteDecorator")]
public sealed class JumpDecorator(IAbilities inner, StatModifiers modifiers) : AbilityDecorator(inner)
{
    protected override string Name => "Jump";
    public override bool CanEnter(TerrainKind terrain) => terrain == TerrainKind.Pit || Inner.CanEnter(terrain);
    public override double SpeedOn(Tile tile) =>
        Inner.SpeedOn(tile) * (tile.Terrain == TerrainKind.Pit ? modifiers.JumpForceMultiplier : 1.0);
}

/// <summary>Sprint: faster everywhere.</summary>
[DesignPattern("Decorator", "ConcreteDecorator")]
public sealed class SprintDecorator(IAbilities inner, StatModifiers modifiers) : AbilityDecorator(inner)
{
    protected override string Name => "Sprint";
    public override double MoveSpeed => Inner.MoveSpeed * modifiers.MoveSpeedMultiplier;
    public override double SpeedOn(Tile tile) => Inner.SpeedOn(tile) * modifiers.MoveSpeedMultiplier;
}

/// <summary>Swim: water becomes passable, and faster by the item's swim multiplier.</summary>
[DesignPattern("Decorator", "ConcreteDecorator")]
public sealed class SwimDecorator(IAbilities inner, StatModifiers modifiers) : AbilityDecorator(inner)
{
    protected override string Name => "Swim";
    public override bool CanEnter(TerrainKind terrain) => terrain == TerrainKind.Water || Inner.CanEnter(terrain);
    public override double SpeedOn(Tile tile) =>
        Inner.SpeedOn(tile) * (tile.Terrain == TerrainKind.Water ? modifiers.SwimSpeedMultiplier : 1.0);
}

/// <summary>JumpDash combo (Jump + Sprint, PWR-2): faster everywhere, and even faster over pits.</summary>
[DesignPattern("Decorator", "ConcreteDecorator")]
public sealed class JumpDashDecorator(IAbilities inner, StatModifiers modifiers) : AbilityDecorator(inner)
{
    protected override string Name => "JumpDash";
    public override double MoveSpeed => Inner.MoveSpeed * modifiers.MoveSpeedMultiplier;
    public override double SpeedOn(Tile tile) =>
        Inner.SpeedOn(tile) * modifiers.MoveSpeedMultiplier * (tile.Terrain == TerrainKind.Pit ? modifiers.JumpForceMultiplier : 1.0);
}

/// <summary>
/// FastSwim combo (Sprint + Swim, PWR-2): water no longer slows the player down. Water speed is the
/// land speed times the combo's bonus; what the inner chain says about water is replaced, not multiplied.
/// </summary>
[DesignPattern("Decorator", "ConcreteDecorator")]
public sealed class FastSwimDecorator(IAbilities inner, StatModifiers modifiers) : AbilityDecorator(inner)
{
    protected override string Name => "FastSwim";
    public override double SpeedOn(Tile tile) =>
        tile.Terrain == TerrainKind.Water ? Inner.MoveSpeed * modifiers.SwimSpeedMultiplier : Inner.SpeedOn(tile);
}
