using System.Text.Json.Serialization;

namespace CastleEscape.Game.Content;

/// <summary>
/// An item that can lie on the map (ITM-1). The JSON field <c>type</c> picks the subclass.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Health), nameof(ConsumableKind.Health))]
[JsonDerivedType(typeof(Reward), nameof(ConsumableKind.Reward))]
[JsonDerivedType(typeof(Power), nameof(ConsumableKind.Power))]
public abstract class Consumable
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    // Read-only: written to JSON output, ignored on input (the "type" field picks the subclass).
    public abstract ConsumableKind Kind { get; }

    /// <summary>The power a Power item grants; null for the other kinds.</summary>
    public PowerGrant? Grant { get; init; }

    /// <summary>Lives restored by a Health item.</summary>
    public int HealthValue { get; init; }

    /// <summary>Points added by a Reward item.</summary>
    public int ScoreValue { get; init; }

    /// <summary>Default weight when a level table does not give one.</summary>
    public double SpawnWeight { get; init; } = 1.0;

    public string IconPath { get; init; } = string.Empty;
}
