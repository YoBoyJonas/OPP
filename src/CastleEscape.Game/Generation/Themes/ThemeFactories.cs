using CastleEscape.Contracts;
using CastleEscape.Game.Content;

namespace CastleEscape.Game.Generation.Themes;

public static class ThemeFactories
{
    public static IThemeFactory For(LevelTheme theme, ContentCatalog catalog)
    {
        var wall = catalog.Obstacles.OfType<Wall>().First();
        var water = catalog.Obstacles.OfType<Water>().First();
        var pit = catalog.Obstacles.OfType<Pit>().First();

        return theme switch
        {
            LevelTheme.Dungeon => new DungeonThemeFactory(wall, water, pit),
            LevelTheme.Crypt => new CryptThemeFactory(wall, water, pit),
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "No factory for this theme."),
        };
    }
}
