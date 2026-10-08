using CastleEscape.Game.Configuration;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.World;

/// <summary>Something that can copy itself, deeply or shallowly.</summary>
[DesignPattern("Prototype", "Prototype")]
public interface ILevelPrototype<out T>
{
    T Clone(CloneMode mode);
}
