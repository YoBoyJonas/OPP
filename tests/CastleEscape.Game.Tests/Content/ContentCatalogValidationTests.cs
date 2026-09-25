using CastleEscape.Contracts;
using CastleEscape.Game.Content;

namespace CastleEscape.Game.Tests.Content;

public class ContentCatalogValidationTests
{
    [Fact]
    public void Validate_EmptyCatalog_HasNoErrors()
    {
        Assert.Empty(new ContentCatalog().Validate());
    }

    [Fact]
    public void Validate_DuplicateCharacterId_ReportsError()
    {
        var catalog = new ContentCatalog { Characters = [Character("warrior"), Character("warrior"), Character("scout")] };

        Assert.Contains(catalog.Validate(), e => e.Contains("duplicate Id 'warrior'"));
    }

    [Fact]
    public void Validate_FewerThanThreeCharacters_ReportsPlr1()
    {
        var catalog = new ContentCatalog { Characters = [Character("warrior"), Character("scout")] };

        Assert.Contains(catalog.Validate(), e => e.StartsWith("PLR-1"));
    }

    [Fact]
    public void Validate_LevelReferencesUnknownZombie_ReportsError()
    {
        var catalog = new ContentCatalog
        {
            Obstacles = [Water()],
            Levels = [Level(1, zombies: 1, zombieIds: ["ghoul"])],
        };

        Assert.Contains(catalog.Validate(), e => e.Contains("unknown zombie 'ghoul'"));
    }

    [Fact]
    public void Validate_ZombieCountDecreases_ReportsZmb4()
    {
        var catalog = new ContentCatalog
        {
            Obstacles = [Water()],
            Zombies = [new ZombieDefinition { Id = "z", Speed = 1 }],
            Levels = [Level(1, zombies: 3, zombieIds: ["z"]), Level(2, zombies: 2, zombieIds: ["z"])],
        };

        Assert.Contains(catalog.Validate(), e => e.StartsWith("ZMB-4"));
    }

    [Fact]
    public void Validate_LevelWithoutPowerObstacle_ReportsGen3()
    {
        var wall = new Wall { Id = "wall", TraversalRequirements = PowerType.None };
        var catalog = new ContentCatalog
        {
            Obstacles = [wall],
            Levels = [new LevelDefinition { Id = "l1", Index = 1, ObstacleCount = 1, ObstacleTable = [new SpawnTableEntry { Id = "wall" }] }],
        };

        Assert.Contains(catalog.Validate(), e => e.StartsWith("GEN-3"));
    }

    [Fact]
    public void Validate_ComboWithOnePower_ReportsError()
    {
        var catalog = new ContentCatalog
        {
            Combos = [new PowerCombo { Id = "bad", RequiredPowers = [PowerType.Jump], Granted = PowerType.JumpDash }],
        };

        Assert.Contains(catalog.Validate(), e => e.Contains("two different required powers"));
    }

    [Fact]
    public void Validate_PowerItemWithoutGrant_ReportsError()
    {
        var catalog = new ContentCatalog { Consumables = [new Power { Id = "empty" }] };

        Assert.Contains(catalog.Validate(), e => e.Contains("has no Grant"));
    }

    [Fact]
    public void Validate_RoomSmallerThan20x15_ReportsLvl2()
    {
        var level = new LevelDefinition
        {
            Id = "tiny", Index = 1, ObstacleCount = 1,
            RoomSize = new GridSize { Width = 10, Height = 10 },
            ObstacleTable = [new SpawnTableEntry { Id = "water" }],
        };
        var catalog = new ContentCatalog { Obstacles = [Water()], Levels = [level] };

        Assert.Contains(catalog.Validate(), e => e.StartsWith("LVL-2"));
    }

    [Fact]
    public void ValidateOrThrow_InvalidCatalog_Throws()
    {
        var catalog = new ContentCatalog { Obstacles = [new Wall { Id = "stone", MinSize = 3, MaxSize = 1 }] };

        Assert.Throws<InvalidOperationException>(catalog.ValidateOrThrow);
    }

    [Fact]
    public void ComputeContentHash_SameContent_SameHash()
    {
        var a = new ContentCatalog { Characters = [Character("scout")] };
        var b = new ContentCatalog { Characters = [Character("scout")] };

        Assert.Equal(a.ComputeContentHash(), b.ComputeContentHash());
    }

    private static CharacterDefinition Character(string id) =>
        new() { Id = id, Name = id, MaxHealth = 3, BaseMoveSpeed = 4 };

    private static Water Water() => new() { Id = "water", TraversalRequirements = PowerType.Swim, MinSize = 1, MaxSize = 2 };

    private static LevelDefinition Level(int index, int zombies, string[] zombieIds) => new()
    {
        Id = $"level-{index}",
        Index = index,
        ObstacleCount = 1,
        ObstacleTable = [new SpawnTableEntry { Id = "water" }],
        ZombieCount = zombies,
        ZombieTable = zombieIds.Select(id => new SpawnTableEntry { Id = id }).ToList(),
    };
}
