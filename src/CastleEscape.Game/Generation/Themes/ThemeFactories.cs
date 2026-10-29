using CastleEscape.Contracts;
using CastleEscape.Game.Content;

namespace CastleEscape.Game.Generation.Themes;

/// <summary>Picks the concrete factory for a level's theme. The only place that maps themes to factories.</summary>
public static class ThemeFactories
{
    public static IThemeFactory For(LevelTheme theme, ContentCatalog catalog)
    {
        // The generic obstacles from content/obstacles.json; every themed variant starts from these values.
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
