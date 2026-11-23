using CastleEscape.Game.Configuration;
using CastleEscape.Game.Patterns;

namespace CastleEscape.Game.World;

[DesignPattern("Prototype", "Prototype")]
public interface ILevelPrototype<out T>
{
    T Clone(CloneMode mode);
}
