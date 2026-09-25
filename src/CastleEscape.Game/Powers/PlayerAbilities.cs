using CastleEscape.Contracts;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Powers;

/// <summary>
/// What a player can do right now: base character stats changed by active powers and combos.
/// </summary>
public static class PlayerAbilities
{
    /// <summary>MOV-2: water needs Swim, pits need Jump; everything else is decided by the terrain alone.</summary>
    public static bool CanTraverse(PlayerEntity player, TerrainKind terrain) => terrain switch
    {
        TerrainKind.Water => player.HasPower(PowerType.Swim),
        TerrainKind.Pit => player.HasPower(PowerType.Jump),
        _ => true,
    };

    /// <summary>Tiles per second while entering a tile.</summary>
    public static double SpeedOn(PlayerEntity player, Tile tile)
    {
        var character = player.Character;
        var baseMods = character.BaseModifiers;
        var speed = character.BaseMoveSpeed * baseMods.MoveSpeedMultiplier;

        if (player.GetPower(PowerType.Sprint) is { } sprint)
        {
            speed *= Math.Pow(sprint.Grant.Modifiers.MoveSpeedMultiplier, sprint.Level);
        }
        var jumpDash = player.Combos.FirstOrDefault(c => c.Granted == PowerType.JumpDash);
        if (jumpDash is not null)
        {
            speed *= jumpDash.Modifiers.MoveSpeedMultiplier;
        }

        switch (tile.Terrain)
        {
            case TerrainKind.Water:
                var fastSwim = player.Combos.FirstOrDefault(c => c.Granted == PowerType.FastSwim);
                if (fastSwim is not null)
                {
                    // FastSwim removes the water penalty and adds its own bonus.
                    speed *= fastSwim.Modifiers.SwimSpeedMultiplier;
                }
                else
                {
                    speed *= tile.Obstacle?.MoveSpeedMultiplier ?? 1.0;
                    speed *= baseMods.SwimSpeedMultiplier;
                    if (player.GetPower(PowerType.Swim) is { } swim)
                    {
                        speed *= Math.Pow(swim.Grant.Modifiers.SwimSpeedMultiplier, swim.Level);
                    }
                }
                break;

            case TerrainKind.Pit:
                // Decision C9: the jump parameter is the speed factor over pits.
                speed *= character.BaseJumpForce * baseMods.JumpForceMultiplier;
                if (player.GetPower(PowerType.Jump) is { } jump)
                {
                    speed *= Math.Pow(jump.Grant.Modifiers.JumpForceMultiplier, jump.Level);
                }
                if (jumpDash is not null)
                {
                    speed *= jumpDash.Modifiers.JumpForceMultiplier;
                }
                break;
        }

        return speed;
    }
}
