using CastleEscape.Game.Content;
using CastleEscape.Game.PatternDemos;

namespace CastleEscape.Game.Tests.Patterns;

public class SingletonTests
{
    [Fact]
    public void Instance_AlwaysTheSameObject()
    {
        Assert.Same(ContentCatalog.Instance, ContentCatalog.Instance);
    }

    [Fact]
    public void Constructor_IsPrivate()
    {
        Assert.Empty(typeof(ContentCatalog).GetConstructors()); // public constructors only
    }

    [Fact]
    public void Demo_ReportsOneSharedInstance()
    {
        var result = new SingletonDemo().Run(new DemoOptions(new Dictionary<string, string>()));

        Assert.Equal(true, result.Evidence["sameInstance"]);
        Assert.Equal(0, result.Evidence["publicConstructors"]);
    }
}
