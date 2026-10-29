using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation.Themes;

/// <summary>
/// Abstract Factory. Creates one family of level parts for a theme: a wall, water, a pit and zombies.
/// A level is built with exactly one factory, so a Dungeon level can never get a Crypt pit.
/// </summary>
public interface IThemeFactory
{
    Wall CreateWall();
    Water CreateWater();
    Pit CreatePit();
    ZombieEntity CreateZombie(string id, ZombieDefinition definition, GridPos spawn);
}
