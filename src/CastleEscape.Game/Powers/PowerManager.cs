using CastleEscape.Contracts;
using CastleEscape.Game.Content;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Powers;

/// <summary>
/// A player's active powers (PWR-1, PWR-3) and combos (PWR-2), and the ability chain they produce.
/// Whenever the set changes, <see cref="Abilities"/> is rebuilt: the character, then one decorator per
/// stack level of Jump, Sprint and Swim, then one per active combo.
/// </summary>
public sealed class PowerManager
{
    private static readonly PowerType[] BaseOrder = [PowerType.Jump, PowerType.Sprint, PowerType.Swim];

    private readonly CharacterDefinition _character;
    private readonly Dictionary<PowerType, ActivePower> _powers = [];
    private readonly List<PowerCombo> _combos = [];

    public PowerManager(CharacterDefinition character)
    {
        _character = character;
        Abilities = new CharacterAbilities(character);
    }

    /// <summary>The current decorator chain. Movement rules only ever talk to this.</summary>
    public IAbilities Abilities { get; private set; }

    /// <summary>Base powers currently active (Jump, Sprint, Swim).</summary>
    public IReadOnlyCollection<ActivePower> Active => _powers.Values;

    /// <summary>Super powers currently active (JumpDash, FastSwim).</summary>
    public IReadOnlyList<PowerCombo> Combos => _combos;

    public ActivePower? Get(PowerType power) => _powers.GetValueOrDefault(power);

    public bool Has(PowerType power) => _powers.ContainsKey(power) || _combos.Any(c => c.Granted == power);

    /// <summary>Adds a power, or stacks it if already active (PWR-3). Returns the resulting power.</summary>
    public ActivePower Gain(PowerGrant grant, double durationSeconds, int maxLevel)
    {
        if (_powers.TryGetValue(grant.Power, out var existing))
        {
            existing.Stack(durationSeconds, maxLevel);
        }
        else
        {
            existing = new ActivePower(grant, durationSeconds);
            _powers[grant.Power] = existing;
        }
        Rebuild();
        return existing;
    }

    /// <summary>Counts timers down; returns the powers that just ran out.</summary>
    public IReadOnlyList<PowerType> Tick(double seconds)
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
        if (expired.Count > 0)
        {
            Rebuild();
        }
        return expired;
    }

    /// <summary>A combo is active while all its required base powers are. Returns the combos that just started.</summary>
    public IReadOnlyList<PowerCombo> RefreshCombos(IReadOnlyList<PowerCombo> allCombos)
    {
        var active = allCombos.Where(c => c.RequiredPowers.All(_powers.ContainsKey)).ToList();
        var started = active.Where(c => !_combos.Contains(c)).ToList();
        if (started.Count > 0 || active.Count != _combos.Count)
        {
            _combos.Clear();
            _combos.AddRange(active);
            Rebuild();
        }
        return started;
    }

    public void Clear()
    {
        _powers.Clear();
        _combos.Clear();
        Rebuild();
    }

    private void Rebuild()
    {
        IAbilities abilities = new CharacterAbilities(_character);
        foreach (var type in BaseOrder)
        {
            if (_powers.TryGetValue(type, out var power))
            {
                for (var level = 0; level < power.Level; level++)
                {
                    abilities = Decorate(abilities, power.Grant);
                }
            }
        }
        foreach (var combo in _combos)
        {
            abilities = Decorate(abilities, combo);
        }
        Abilities = abilities;
    }

    private static IAbilities Decorate(IAbilities inner, PowerGrant grant) => grant.Power switch
    {
        PowerType.Jump => new JumpDecorator(inner, grant.Modifiers),
        PowerType.Sprint => new SprintDecorator(inner, grant.Modifiers),
        PowerType.Swim => new SwimDecorator(inner, grant.Modifiers),
        _ => throw new ArgumentOutOfRangeException(nameof(grant), grant.Power, "Not a base power."),
    };

    private static IAbilities Decorate(IAbilities inner, PowerCombo combo) => combo.Granted switch
    {
        PowerType.JumpDash => new JumpDashDecorator(inner, combo.Modifiers),
        PowerType.FastSwim => new FastSwimDecorator(inner, combo.Modifiers),
        _ => throw new ArgumentOutOfRangeException(nameof(combo), combo.Granted, "Not a combo."),
    };
}
