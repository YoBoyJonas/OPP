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

    private readonly Dictionary<PowerType, ActivePower> _powers = [];
    private readonly List<PowerCombo> _combos = [];

    /// <summary>Base powers currently active (Jump, Sprint, Swim).</summary>
    public IReadOnlyCollection<ActivePower> Powers => _powers.Values;

    /// <summary>Super powers currently active (JumpDash, FastSwim).</summary>
    public IReadOnlyList<PowerCombo> Combos => _combos;

    public bool HasPower(PowerType power) => _powers.ContainsKey(power) || _combos.Any(c => c.Granted == power);

    public ActivePower? GetPower(PowerType power) => _powers.GetValueOrDefault(power);

    /// <summary>Adds a power, or stacks it if already active. Returns the resulting power.</summary>
    public ActivePower GainPower(PowerGrant grant, double durationSeconds, int maxLevel)
    {
        if (_powers.TryGetValue(grant.Power, out var existing))
        {
            existing.Stack(durationSeconds, maxLevel);
            return existing;
        }
        var power = new ActivePower(grant, durationSeconds);
        _powers[grant.Power] = power;
        return power;
    }

    /// <summary>Counts power timers down; returns the powers that just ran out.</summary>
    public List<PowerType> TickPowers(double seconds)
    {
        var expired = new List<PowerType>();
        foreach (var power in _powers.Values.ToList())
        {
            power.Tick(seconds);
            if (power.IsExpired)
            {
                _powers.Remove(power.Power);
                expired.Add(power.Power);
            }
        }
        return expired;
    }

    public bool RemovePower(PowerType power) => _powers.Remove(power);

    public void SetCombos(IEnumerable<PowerCombo> combos)
    {
        _combos.Clear();
        _combos.AddRange(combos);
    }

    public void ClearPowers()
    {
        _powers.Clear();
        _combos.Clear();
    }

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
