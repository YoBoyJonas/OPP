using System.Text.Json.Serialization;
using CastleEscape.Contracts;

namespace CastleEscape.Game.Content;

/// <summary>A terrain obstacle. The JSON field <c>type</c> picks the subclass.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Wall), nameof(ObstacleKind.Wall))]
[JsonDerivedType(typeof(Water), nameof(ObstacleKind.Water))]
[JsonDerivedType(typeof(Pit), nameof(ObstacleKind.Pit))]
public abstract class Obstacle
{
    protected Obstacle()
    {
    }

    /// <summary>Copies a template's values; themed variants (Abstract Factory products) start from the JSON template.</summary>
    protected Obstacle(Obstacle template)
    {
        Id = template.Id;
        Name = template.Name;
        TraversalRequirements = template.TraversalRequirements;
        BlocksZombies = template.BlocksZombies;
        MinSize = template.MinSize;
        MaxSize = template.MaxSize;
        MoveSpeedMultiplier = template.MoveSpeedMultiplier;
    }

    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public abstract ObstacleKind Kind { get; }

    /// <summary>The power needed to enter this tile. <c>None</c> means no power allows it (walls).</summary>
    public PowerType TraversalRequirements { get; init; }

    public bool BlocksZombies { get; init; }

    /// <summary>Smallest and largest patch the generator places, in tiles.</summary>
    public int MinSize { get; init; } = 1;
    public int MaxSize { get; init; } = 1;

    /// <summary>Speed multiplier while crossing this tile (e.g. water is slower).</summary>
    public double MoveSpeedMultiplier { get; init; } = 1.0;
}
