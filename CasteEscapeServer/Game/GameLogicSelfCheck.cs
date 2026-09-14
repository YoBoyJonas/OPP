using CasteEscapeServer.Models;

namespace CasteEscapeServer.Game;

// ponytail: the one runnable check for this pass's non-trivial branchy logic
// (movement gating, zombie contact/respawn, door-needs-both-levers). Not a
// test project - an assert-and-print smoke test, run via `dotnet run -- selftest`.
public static class GameLogicSelfCheck
{
    public static bool Run()
    {
        var allPassed = true;
        allPassed &= RunObstacleGatingCheck();
        allPassed &= RunZombieContactCheck();
        allPassed &= RunDoorRequiresBothLeversCheck();
        return allPassed;
    }

    private static bool RunObstacleGatingCheck()
    {
        var session = NewSession();
        var player = session.Players[0];
        // Level 1's obstacle band (Water, odd level) sits at y = Height/2 = 8;
        // x=5 is inside the band (band spans x in [2, Width-3), gap column excluded).
        player.X = 5;
        player.Y = 7;
        player.CurrentDirection = Direction.Down;

        session.RunTick();
        var blockedWithoutSwim = player.Y == 7;

        player.ActivePowers[PowerType.Swim] = 10;
        session.RunTick();
        var crossedWithSwim = player.Y > 7;

        return Report("Water obstacle blocks without Swim, allows with Swim", blockedWithoutSwim && crossedWithSwim);
    }

    private static bool RunZombieContactCheck()
    {
        var session = NewSession();
        var player = session.Players[0];
        var startHealth = player.Health;
        // Level 1's single zombie spawns at (3, Height-3) = (3, 13).
        player.X = 3;
        player.Y = 13;

        var snapshot = session.RunTick();
        var tookDamage = player.Health == startHealth - 1;
        var zombieAtHome = snapshot.Zombies[0].X == 3 && snapshot.Zombies[0].Y == 13;

        return Report("Zombie contact deals damage and resets zombie to spawn", tookDamage && zombieAtHome);
    }

    private static bool RunDoorRequiresBothLeversCheck()
    {
        var session = NewSession();
        var player0 = session.Players[0];
        var player1 = session.Players[1];

        // Level 1's levers sit at (2, Height-2) and (Width-3, Height-2) = (2,14) and (19,14).
        player0.X = 2;
        player0.Y = 14;
        player1.X = 19;
        player1.Y = 14;
        var bothOnLevers = session.RunTick();

        player1.X = 10;
        player1.Y = 10;
        var onlyOneOnLever = session.RunTick();

        return Report("Door only open while both levers held simultaneously", bothOnLevers.DoorOpen && !onlyOneOnLever.DoorOpen);
    }

    private static GameSession NewSession()
    {
        var session = new GameSession("selftest", null!);
        session.AddPlayer(0, Characters.Create(Characters.WarriorId, "p0", "P0"));
        session.AddPlayer(1, Characters.Create(Characters.ScoutId, "p1", "P1"));
        return session;
    }

    private static bool Report(string name, bool passed)
    {
        Console.WriteLine($"[{(passed ? "PASS" : "FAIL")}] {name}");
        return passed;
    }
}
