using CastleEscape.Domain.Configuration;
using CastleEscape.Domain.Patterns;

namespace CastleEscape.Domain.World;

/// <summary>Something that can copy itself, deeply or shallowly.</summary>
[DesignPattern("Prototype", "Prototype")]
public interface ILevelPrototype<out T>
{
    T Clone(CloneMode mode);
}
