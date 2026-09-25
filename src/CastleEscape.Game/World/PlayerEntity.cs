using CastleEscape.Contracts;
using CastleEscape.Game.Content;

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

    public bool IsDead => Lives <= 0;

    public void LoseLives(int amount) => Lives = Math.Max(0, Lives - amount);

    /// <summary>Health items restore lives up to the character's maximum (decision D14).</summary>
    public void GainLives(int amount) => Lives = Math.Min(MaxLives, Lives + amount);

    public void AddScore(int points) => Score += points;

    /// <summary>Puts the player on a new level's start tile, standing still.</summary>
    public void PlaceAt(GridPos start)
    {
        StartTile = start;
        HeldDirection = Direction.None;
        TeleportTo(start);
    }

    /// <summary>Restores lives and score to a checkpoint (restart level, D8).</summary>
    public void RestoreStats(int lives, int score)
    {
        Lives = lives;
        Score = score;
    }
}
