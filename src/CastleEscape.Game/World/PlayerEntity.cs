using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.Powers;

namespace CastleEscape.Game.World;

/// <summary>A player's character in the world. Lives across levels; placed on a start tile per level.</summary>
public class PlayerEntity(Guid playerId, string name, CharacterDefinition character, GridPos start)
    : MovableEntity(playerId.ToString("N"), start)
{
    public Guid PlayerId { get; } = playerId;
    public string Name { get; } = name;
    public CharacterDefinition Character { get; } = character;

    /// <summary>The start tile of the current level.</summary>
    public GridPos StartTile { get; private set; } = start;

    public int Lives { get; private set; } = character.MaxHealth;
    public int MaxLives => Character.MaxHealth;
    public int Score { get; private set; }

    /// <summary>The direction key currently held by the client (MOV-3: each player independently).</summary>
    public Direction HeldDirection { get; set; }

    /// <summary>
    /// The last direction pressed, kept until one step has been tried with it. A key tapped and released
    /// within one tick still moves the player one tile.
    /// </summary>
    public Direction TappedDirection { get; set; }

    /// <summary>Sequence of the input that set <see cref="HeldDirection"/>; the later of two conflicting steps loses (D7).</summary>
    public long DirectionSequence { get; set; }

    public bool IsDead => Lives <= 0;

    public void LoseLives(int amount) => Lives = Math.Max(0, Lives - amount);

    /// <summary>Health items restore lives up to the character's maximum (decision D14).</summary>
    public void GainLives(int amount) => Lives = Math.Min(MaxLives, Lives + amount);

    public void AddScore(int points) => Score += points;

    /// <summary>Active powers and combos, and the ability chain they make (Decorator).</summary>
    public PowerManager Powers { get; } = new(character);

    /// <summary>What the player can do right now (terrain and speed), with all active powers applied.</summary>
    public IAbilities Abilities => Powers.Abilities;

    /// <summary>Puts the player on a new level's start tile, standing still.</summary>
    public void PlaceAt(GridPos start)
    {
        StartTile = start;
        HeldDirection = Direction.None;
        TappedDirection = Direction.None;
        TeleportTo(start);
    }

    /// <summary>Moves back to a tile within the same level (undoing a restart), standing still.</summary>
    public void ReturnTo(GridPos tile)
    {
        HeldDirection = Direction.None;
        TeleportTo(tile);
    }

    /// <summary>Restores lives and score to a checkpoint (restart level, D8).</summary>
    public void RestoreStats(int lives, int score)
    {
        Lives = lives;
        Score = score;
    }
}
