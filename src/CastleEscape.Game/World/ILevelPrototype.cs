using CastleEscape.Game.Configuration;

namespace CastleEscape.Game.World;

public interface ILevelPrototype<out T>
{
    T Clone(CloneMode mode);
}
