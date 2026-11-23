using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Generation.Themes;

/// <summary>Concrete factory for levels 1-5.</summary>
public sealed class DungeonThemeFactory(Wall wallTemplate, Water waterTemplate, Pit pitTemplate) : IThemeFactory
{
    public Wall CreateWall() => new StoneWall(wallTemplate);
    public Water CreateWater() => new MurkyWater(waterTemplate);
    public Pit CreatePit() => new SpikePit(pitTemplate);
    public ZombieEntity CreateZombie(string id, ZombieDefinition definition, GridPos spawn) => new DungeonZombie(id, definition, spawn);
}

public sealed class StoneWall : Wall
{
    public StoneWall(Wall template) : base(template)
    {
        Id = "stone-wall";
        Name = "Stone wall";
    }
}

public sealed class MurkyWater : Water
{
    public MurkyWater(Water template) : base(template)
    {
        Id = "murky-water";
        Name = "Murky water";
    }
}

public sealed class SpikePit : Pit
{
    public SpikePit(Pit template) : base(template)
    {
        Id = "spike-pit";
        Name = "Spike pit";
    }
}

public sealed class DungeonZombie(string id, ZombieDefinition definition, GridPos spawn) : ZombieEntity(id, definition, spawn);
