namespace CastleEscape.Game.Configuration;

/// <summary>Which copy the Prototype pattern makes of the pristine level.</summary>
public enum CloneMode
{
    Deep,
    Shallow
}

/// <summary>Switches the lecturer may ask to flip at the defence, bound from the <c>Patterns</c> section.</summary>
public sealed class PatternOptions
{
    public const string SectionName = "Patterns";

    public CloneMode PrototypeCloneMode { get; set; } = CloneMode.Deep;
}
