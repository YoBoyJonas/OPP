using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.Patterns;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation.Themes;

/// <summary>Levels 1-5: stone walls, murky water, spike pits and ordinary zombies.</summary>
[DesignPattern("Abstract Factory", "ConcreteFactory")]
public sealed class DungeonThemeFactory(ObstacleTemplates templates) : IThemeFactory
{
    public LevelTheme Theme => LevelTheme.Dungeon;

    public Wall CreateWall() => new StoneWall(templates.Wall);
    public Water CreateWater() => new MurkyWater(templates.Water);
    public Pit CreatePit() => new SpikePit(templates.Pit);
    public ZombieEntity CreateZombie(string id, ZombieDefinition definition, GridPos spawn) => new DungeonZombie(id, definition, spawn);
}

[DesignPattern("Abstract Factory", "ConcreteProduct")]
public sealed class StoneWall : Wall
{
    public StoneWall(Wall template) : base(template)
    {
        Id = "stone-wall";
        Name = "Stone wall";
    }
}

/// <summary>Water with the template's slowdown (0.6).</summary>
[DesignPattern("Abstract Factory", "ConcreteProduct")]
public sealed class MurkyWater : Water
{
    public MurkyWater(Water template) : base(template)
    {
        Id = "murky-water";
        Name = "Murky water";
    }
}

/// <summary>A pit crossed at the normal jump speed.</summary>
[DesignPattern("Abstract Factory", "ConcreteProduct")]
public sealed class SpikePit : Pit
{
    public SpikePit(Pit template) : base(template)
    {
        Id = "spike-pit";
        Name = "Spike pit";
    }
}

/// <summary>A zombie exactly as its type defines it.</summary>
[DesignPattern("Abstract Factory", "ConcreteProduct")]
public sealed class DungeonZombie(string id, ZombieDefinition definition, GridPos spawn) : ZombieEntity(id, definition, spawn);
