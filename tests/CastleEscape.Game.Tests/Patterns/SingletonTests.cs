using CastleEscape.Game.Content;
using CastleEscape.Game.PatternDemos;

namespace CastleEscape.Game.Tests.Patterns;

public class SingletonTests
{
    [Fact]
    public void Instance_IsThreadSafe_OneInstanceOneCreation()
    {
        var result = new SingletonDemo().Run(new DemoOptions(new Dictionary<string, string> { ["threads"] = "64", ["naive"] = "false" }));

        Assert.Equal(1, result.Evidence["distinctInstances"]);
        Assert.Equal(1, ContentCatalog.InstanceCreations);
    }

    [Fact]
    public void Instance_AlwaysTheSameObject()
    {
        Assert.Same(ContentCatalog.Instance, ContentCatalog.Instance);
    }

    [Fact]
    public void NaiveHolder_RaceCreatesMoreThanOne()
    {
        var result = new SingletonDemo().Run(new DemoOptions(new Dictionary<string, string> { ["threads"] = "32" }));

        Assert.True((int)result.Evidence["naiveDistinctInstances"]! > 1, "the lock-free holder should lose the race");
        Assert.True((int)result.Evidence["naiveConstructorCalls"]! > 1);
    }

    [Fact]
    public void Constructor_IsPrivate()
    {
        Assert.Empty(typeof(ContentCatalog).GetConstructors()); // public constructors only
    }
}
