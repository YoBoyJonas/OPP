using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.PatternDemos;
using CastleEscape.Game.Powers;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Tests.Patterns;

public class DecoratorTests
{
    private static ContentCatalog Catalog => ContentCatalog.Instance;
    private static readonly Tile Floor = new(TerrainKind.Floor);
    private static readonly Tile Water = new(TerrainKind.Water, ThemeFactories.For(LevelTheme.Dungeon, Catalog).CreateWater());
    private static readonly Tile Pit = new(TerrainKind.Pit, ThemeFactories.For(LevelTheme.Dungeon, Catalog).CreatePit());

    private static PowerGrant Grant(PowerType type) => Catalog.Consumables.First(c => c.Grant?.Power == type).Grant!;
    private static PowerCombo Combo(PowerType type) => Catalog.Combos.Single(c => c.Granted == type);

    private static PowerManager Powers(string character, params PowerType[] pickups)
    {
        var powers = new PowerManager(Catalog.GetCharacter(character));
        foreach (var pickup in pickups)
        {
            powers.Gain(Grant(pickup), 10, maxLevel: 3);
            powers.RefreshCombos(Catalog.Combos);
        }
        return powers;
    }

    [Fact]
    public void AtLeastThreeDecorationLevels()
    {
        var abilities = Powers("scout", PowerType.Sprint, PowerType.Sprint, PowerType.Jump).Abilities;

        // Sprint, Sprint, Jump and the JumpDash combo, each wrapping the previous layer.
        Assert.True(AbilityChain.Depth(abilities) >= 3);
        Assert.Equal("Scout + Jump + Sprint + Sprint + JumpDash", abilities.Describe());
        Assert.IsType<JumpDashDecorator>(abilities);
        Assert.IsType<SprintDecorator>(((AbilityDecorator)abilities).Inner);
    }

    [Fact]
    public void Character_WithoutPowers_CannotEnterWaterOrPits()
    {
        var abilities = new CharacterAbilities(Catalog.GetCharacter("scout"));

        Assert.True(abilities.CanEnter(TerrainKind.Floor));
        Assert.False(abilities.CanEnter(TerrainKind.Water));
        Assert.False(abilities.CanEnter(TerrainKind.Pit));
        Assert.Equal(6.0, abilities.SpeedOn(Floor));
    }

    [Fact]
    public void EachDecorator_ChangesOneThing()
    {
        var scout = new CharacterAbilities(Catalog.GetCharacter("scout"));

        var jump = new JumpDecorator(scout, Grant(PowerType.Jump).Modifiers);
        var swim = new SwimDecorator(scout, Grant(PowerType.Swim).Modifiers);
        var sprint = new SprintDecorator(scout, Grant(PowerType.Sprint).Modifiers);

        Assert.True(jump.CanEnter(TerrainKind.Pit));
        Assert.False(jump.CanEnter(TerrainKind.Water));
        Assert.Equal(scout.SpeedOn(Floor), jump.SpeedOn(Floor));
        Assert.True(swim.CanEnter(TerrainKind.Water));
        Assert.False(swim.CanEnter(TerrainKind.Pit));
        Assert.Equal(scout.SpeedOn(Floor) * 1.5, sprint.SpeedOn(Floor), 6);
        Assert.False(sprint.CanEnter(TerrainKind.Water));
    }

    [Fact]
    public void StackedPower_IsOneDecoratorPerLevel_Pwr3()
    {
        var once = Powers("warrior", PowerType.Sprint).Abilities;
        var twice = Powers("warrior", PowerType.Sprint, PowerType.Sprint).Abilities;

        Assert.Equal(1, AbilityChain.Depth(once));
        Assert.Equal(2, AbilityChain.Depth(twice));
        Assert.Equal(3.5 * 1.5 * 1.5, twice.MoveSpeed, 6);
    }

    [Fact]
    public void Speeds_MatchTheRulesOfThePrototype()
    {
        // Swimmer with Sprint + Swim (so FastSwim) and Jump (so JumpDash): the richest chain.
        var swimmer = Catalog.GetCharacter("swimmer");
        var abilities = Powers("swimmer", PowerType.Sprint, PowerType.Swim, PowerType.Jump).Abilities;
        var sprint = Grant(PowerType.Sprint).Modifiers.MoveSpeedMultiplier;
        var jumpDash = Combo(PowerType.JumpDash).Modifiers;
        var land = swimmer.BaseMoveSpeed * sprint * jumpDash.MoveSpeedMultiplier;

        Assert.Equal(land, abilities.SpeedOn(Floor), 6);
        // FastSwim ignores the water penalty and the swim multipliers: land speed x its bonus.
        Assert.Equal(land * Combo(PowerType.FastSwim).Modifiers.SwimSpeedMultiplier, abilities.SpeedOn(Water), 6);
        // Pits: land speed x jump force x Jump item x JumpDash jump bonus (C9).
        Assert.Equal(land * swimmer.BaseJumpForce * Grant(PowerType.Jump).Modifiers.JumpForceMultiplier * jumpDash.JumpForceMultiplier,
            abilities.SpeedOn(Pit), 6);
    }

    [Fact]
    public void WithoutFastSwim_WaterIsSlowed()
    {
        var abilities = Powers("swimmer", PowerType.Swim).Abilities;

        var expected = 4.5 * Water.Obstacle!.MoveSpeedMultiplier * 1.5 /* swimmer */ * Grant(PowerType.Swim).Modifiers.SwimSpeedMultiplier;
        Assert.Equal(expected, abilities.SpeedOn(Water), 6);
    }

    [Fact]
    public void ExpiredPowers_UnwrapTheChain()
    {
        var powers = Powers("scout", PowerType.Jump, PowerType.Sprint);
        Assert.Equal(3, AbilityChain.Depth(powers.Abilities));

        powers.Tick(11);
        powers.RefreshCombos(Catalog.Combos);

        Assert.Equal(0, AbilityChain.Depth(powers.Abilities));
        Assert.False(powers.Abilities.CanEnter(TerrainKind.Pit));
    }

    [Fact]
    public void Demo_ReachesSixLayers()
    {
        var result = new DecoratorDemo().Run(DemoOptions.None);

        Assert.Equal(6, result.Evidence["maxDepth"]);
    }
}
