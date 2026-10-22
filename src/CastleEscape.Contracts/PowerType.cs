namespace CastleEscape.Contracts;

/// <summary>
/// Base powers (Jump, Sprint, Swim) come from items; super powers (JumpDash, FastSwim) appear
/// while two different base powers are active. <c>None</c> marks terrain no power can cross.
/// </summary>
public enum PowerType
{
    None,
    Jump,
    Sprint,
    Swim,
    JumpDash,
    FastSwim
}
