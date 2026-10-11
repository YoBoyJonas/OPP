using CastleEscape.Domain.Content;
using CastleEscape.Domain.Generation;
using CastleEscape.Infrastructure.Content;

namespace CastleEscape.PatternDemos.Demos;

public static class PresetBuilders
{
    public static PresetLevelBuilder FromFile(IContentCatalog catalog, string name) =>
        new(catalog, new FilePresetMapSource().ReadRows(name));
}
