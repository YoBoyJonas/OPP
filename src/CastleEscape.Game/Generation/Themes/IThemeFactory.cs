using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation.Themes;

/// <summary>Abstract Factory: one family of level parts per theme.</summary>
public interface IThemeFactory
{
    Wall CreateWall();
    Water CreateWater();
    Pit CreatePit();
    ZombieEntity CreateZombie(string id, ZombieDefinition definition, GridPos spawn);
}
