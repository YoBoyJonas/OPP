using CastleEscape.Game.Configuration;

namespace CastleEscape.Game.World;

/// <summary>Something that can copy itself, deeply or shallowly.</summary>
public interface ILevelPrototype<out T>
{
    T Clone(CloneMode mode);
}
