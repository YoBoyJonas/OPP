using CastleEscape.Domain.AI;
using CastleEscape.Domain.Commands;
using CastleEscape.Domain.Configuration;
using CastleEscape.Domain.Content;
using CastleEscape.Domain.Events;
using CastleEscape.Domain.Powers;
using CastleEscape.Domain.World;

namespace CastleEscape.Domain.Sessions;

public enum SimulationOutcome { Playing, LevelComplete, Defeat }

/// <summary>One deterministic gameplay step, independent of networking, storage and wall-clock time.</summary>
public sealed class GameSimulation(IContentCatalog catalog, GameOptions options)
{
    private readonly InteractionSettings _interaction = new(options.PowerDurationSeconds, options.MaxPowerStackLevel, options.RespawnPlayerOnHit);

    public SimulationOutcome Advance(GameWorld world, CommandProcessor commands, long tick, double seconds)
    {
        var level = world.Level!;
        var players = world.Players;
        var events = world.Events;
        world.LevelElapsed += seconds;
        PowerRules.TickPowers(players, catalog.Combos, seconds, events);
        PlayerMovement.Move(world, commands, tick, seconds);
        MoveZombies(level, players, seconds);
        InteractionResolver.CollectItems(level, players, catalog.Combos, _interaction, events);
        InteractionResolver.ResolveZombieContacts(level, players, _interaction, events);
        world.Exit!.UpdateLevers(players, events);
        if (players.FirstOrDefault(p => p.IsDead) is { } dead)
        {
            events.Add(new GameLost(dead.PlayerId, $"{dead.Name} has no lives left. Game over."));
            return SimulationOutcome.Defeat;
        }
        if (world.Exit.Update(players, events))
        {
            events.Add(new LevelCompleted(null, $"Level {level.Index} complete!", level.Index));
            return SimulationOutcome.LevelComplete;
        }
        return SimulationOutcome.Playing;
    }

    private static void MoveZombies(LevelState level, IReadOnlyList<PlayerEntity> players, double seconds)
    {
        var view = new WorldView(level, players);
        foreach (var zombie in level.Zombies)
        {
            if (!zombie.IsMoving) TryStartZombieStep(zombie, view);
            if (zombie.Advance(zombie.Speed * seconds, out var leftover) && TryStartZombieStep(zombie, view))
                zombie.Advance(Math.Min(leftover, 0.99), out _);
        }
    }

    private static bool TryStartZombieStep(ZombieEntity zombie, WorldView view)
    {
        var direction = zombie.Strategy.NextStep(zombie, view);
        if (direction == Direction.None || !view.CanZombieEnter(zombie.Tile.Step(direction))) return false;
        zombie.BeginStep(direction);
        return true;
    }
}
