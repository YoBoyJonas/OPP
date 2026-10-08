using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation.Themes;

/// <summary>
/// Creates one consistent family of level parts for a theme: walls, water, pits and zombies.
/// A level is built with exactly one factory, so a Dungeon level can never get a Crypt pit.
/// </summary>
[DesignPattern("Abstract Factory", "AbstractFactory")]
public interface IThemeFactory
{
    LevelTheme Theme { get; }

    Wall CreateWall();
    Water CreateWater();
    Pit CreatePit();
    ZombieEntity CreateZombie(string id, ZombieDefinition definition, GridPos spawn);
}

/// <summary>The generic obstacles from <c>content/obstacles.json</c> that themed variants start from.</summary>
public sealed record ObstacleTemplates(Wall Wall, Water Water, Pit Pit)
{
    public static ObstacleTemplates From(ContentCatalog catalog) =>
        new(catalog.Obstacles.OfType<Wall>().First(), catalog.Obstacles.OfType<Water>().First(), catalog.Obstacles.OfType<Pit>().First());
}

public static class ThemeFactories
{
    /// <summary>The factory for a theme. The only place that maps themes to factories.</summary>
    public static IThemeFactory For(LevelTheme theme, ContentCatalog catalog)
    {
        var templates = ObstacleTemplates.From(catalog);
        return theme switch
        {
            LevelTheme.Dungeon => new DungeonThemeFactory(templates),
            LevelTheme.Crypt => new CryptThemeFactory(templates),
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "No factory for this theme."),
        };
    }

    public static IThemeFactory For(LevelDefinition definition, ContentCatalog catalog) => For(definition.Theme, catalog);

    /// <summary>Creates the obstacle of a kind picked from a level's obstacle table.</summary>
    public static Obstacle CreateObstacle(this IThemeFactory factory, ObstacleKind kind) => kind switch
    {
        ObstacleKind.Wall => factory.CreateWall(),
        ObstacleKind.Water => factory.CreateWater(),
        ObstacleKind.Pit => factory.CreatePit(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
