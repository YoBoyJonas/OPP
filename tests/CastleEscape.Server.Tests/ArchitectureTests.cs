using CastleEscape.Application.Sessions;
using CastleEscape.Domain.Sessions;
using CastleEscape.Infrastructure.Content;

namespace CastleEscape.Server.Tests;

public class ArchitectureTests
{
    [Fact]
    public void RuntimeAssemblies_DoNotReferenceCourseTooling()
    {
        foreach (var assembly in new[] { typeof(Program).Assembly, typeof(GameFacade).Assembly,
                     typeof(GameWorld).Assembly, typeof(ContentCatalog).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name == "CastleEscape.PatternDemos");
    }
}
