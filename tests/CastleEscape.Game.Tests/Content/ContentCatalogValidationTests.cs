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
        var catalog = new ContentCatalog();
        catalog.Characters.Add(new CharacterDefinition { Id = "warrior" });
        catalog.Characters.Add(new CharacterDefinition { Id = "warrior" });

        var errors = catalog.Validate();

        Assert.Contains(errors, e => e.Contains("duplicate Id 'warrior'"));
    }

    [Fact]
    public void Validate_LevelReferencesUnknownZombie_ReportsError()
    {
        var catalog = new ContentCatalog();
        var level = new LevelDefinition { Id = "level-1" };
        level.ZombieTable.Add(new ZombieDefinition { Id = "ghoul" });
        catalog.Levels.Add(level);

        var errors = catalog.Validate();

        Assert.Contains(errors, e => e.Contains("unknown zombie 'ghoul'"));
    }

    [Fact]
    public void ValidateOrThrow_InvalidCatalog_Throws()
    {
        var catalog = new ContentCatalog();
        catalog.Obstacles.Add(new Wall { Id = "stone", MinSize = 3, MaxSize = 1 });

        Assert.Throws<InvalidOperationException>(catalog.ValidateOrThrow);
    }

    [Fact]
    public void ComputeContentHash_SameContent_SameHash()
    {
        var a = new ContentCatalog();
        a.Characters.Add(new CharacterDefinition { Id = "scout", MaxHealth = 3 });
        var b = new ContentCatalog();
        b.Characters.Add(new CharacterDefinition { Id = "scout", MaxHealth = 3 });

        Assert.Equal(a.ComputeContentHash(), b.ComputeContentHash());
    }
}
