namespace CastleEscape.Domain.Configuration;

/// <summary>Which copy the Prototype pattern makes of the pristine level.</summary>
public enum CloneMode
{
    Deep,
    Shallow
}

/// <summary>Clone policy for in-process course comparisons. The normal host uses the default deep copy.</summary>
public sealed class PatternOptions
{
    public CloneMode PrototypeCloneMode { get; set; } = CloneMode.Deep;
}
