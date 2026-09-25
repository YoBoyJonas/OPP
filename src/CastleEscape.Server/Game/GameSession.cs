using CasteEscapeServer.Hubs;
using CasteEscapeServer.Models;
using Microsoft.AspNetCore.SignalR;

namespace CasteEscapeServer.Game;

public class GameSession
{
    private const double TickSeconds = 0.15;
    private const double ContactDistance = 0.6;
    private const int MaxLevel = 10;

    private readonly object _gate = new();
    private readonly IHubContext<GameHub> _hub;
    private readonly Timer _timer;
    private LevelState _level;
    private bool _over;

    public string SessionId { get; }
    public PlayerState[] Players { get; } = new PlayerState[2];

    public GameSession(string sessionId, IHubContext<GameHub> hub)
    {
        SessionId = sessionId;
        _hub = hub;
        _level = LevelBuilder.Build(1);
        _timer = new Timer(_ => Tick(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void AddPlayer(int index, PlayerState player)
    {
        var spawn = _level.PlayerSpawns[index];
        player.X = spawn.X;
        player.Y = spawn.Y;
        Players[index] = player;
    }

    public void Start() => _timer.Change(TimeSpan.FromSeconds(TickSeconds), TimeSpan.FromSeconds(TickSeconds));

    public void Stop() => _timer.Change(Timeout.Infinite, Timeout.Infinite);

    public void SetInput(string connectionId, Direction direction)
    {
        lock (_gate)
        {
            var player = Players.FirstOrDefault(p => p?.ConnectionId == connectionId);
            if (player != null)
            {
                player.CurrentDirection = direction;
            }
        }
    }

    public GameStateSnapshot RunTick()
    {
        lock (_gate)
        {
            return TickLocked();
        }
    }

    private void Tick()
    {
        GameStateSnapshot snapshot;
        lock (_gate)
        {
            if (_over || Players[0] == null || Players[1] == null)
            {
                return;
            }
            snapshot = TickLocked();
        }

        _ = _hub.Clients.Group(SessionId).SendAsync("GameStateUpdate", snapshot);
        if (_over)
        {
            Stop();
        }
    }

    private GameStateSnapshot TickLocked()
    {
        MovePlayers();
        MoveZombies();
        UpdateLevers();
        var doorOpen = _level.Levers.Count > 0 && _level.Levers.All(l => l.IsActive);
        CollectItems();
        TickPowers();

        var isGameOver = Players.Any(p => p.Health <= 0);
        var isWin = false;
        if (!isGameOver && doorOpen && Players.All(IsOnExit))
        {
            if (_level.Index >= MaxLevel)
            {
                isWin = true;
            }
            else
            {
                AdvanceLevel();
            }
        }

        if (isGameOver || isWin)
        {
            _over = true;
        }

        return BuildSnapshot(doorOpen, isGameOver, isWin);
    }

    private void MovePlayers()
    {
        foreach (var player in Players)
        {
            var (dx, dy) = DirectionVector(player.CurrentDirection);
            if (dx == 0 && dy == 0)
            {
                continue;
            }

            var newX = player.X + dx * player.MoveSpeed * TickSeconds;
            var newY = player.Y + dy * player.MoveSpeed * TickSeconds;

            if (IsTraversable((int)Math.Round(newX), (int)Math.Round(newY), player))
            {
                player.X = newX;
                player.Y = newY;
            }
        }
    }

    private bool IsTraversable(int x, int y, PlayerState player) => _level.CellAt(x, y) switch
    {
        CellType.Wall => false,
        CellType.Water => player.HasPower(PowerType.Swim),
        CellType.Pit => player.HasPower(PowerType.Jump),
        _ => true
    };

    private static (int dx, int dy) DirectionVector(Direction direction) => direction switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        Direction.Right => (1, 0),
        _ => (0, 0)
    };

    private void MoveZombies()
    {
        foreach (var zombie in _level.Zombies)
        {
            var target = NearestPlayer(zombie);
            var dx = target.X - zombie.X;
            var dy = target.Y - zombie.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance <= ContactDistance)
            {
                target.Health = Math.Max(0, target.Health - zombie.ContactDamage);
                zombie.X = zombie.HomeX;
                zombie.Y = zombie.HomeY;
                continue;
            }

            zombie.X += dx / distance * zombie.Speed * TickSeconds;
            zombie.Y += dy / distance * zombie.Speed * TickSeconds;
        }
    }

    private PlayerState NearestPlayer(ZombieState zombie) => Players.OrderBy(p =>
    {
        var dx = p.X - zombie.X;
        var dy = p.Y - zombie.Y;
        return dx * dx + dy * dy;
    }).First();

    private void UpdateLevers()
    {
        foreach (var lever in _level.Levers)
        {
            lever.IsActive = Players.Any(p => (int)Math.Round(p.X) == lever.X && (int)Math.Round(p.Y) == lever.Y);
        }
    }

    private bool IsOnExit(PlayerState player) =>
        _level.ExitCells.Contains(((int)Math.Round(player.X), (int)Math.Round(player.Y)));

    private void CollectItems()
    {
        foreach (var player in Players)
        {
            var cellX = (int)Math.Round(player.X);
            var cellY = (int)Math.Round(player.Y);
            var item = _level.Items.FirstOrDefault(i => i.X == cellX && i.Y == cellY);
            if (item == null)
            {
                continue;
            }

            switch (item.Kind)
            {
                case ItemKind.Health:
                    player.Health = Math.Min(player.MaxHealth, player.Health + item.HealthValue);
                    break;
                case ItemKind.Power:
                    if (item.GrantedPower.HasValue)
                    {
                        player.ActivePowers[item.GrantedPower.Value] = item.GrantDurationSeconds;
                    }
                    break;
            }

            _level.Items.Remove(item);
        }
    }

    private void TickPowers()
    {
        foreach (var player in Players)
        {
            var expired = new List<PowerType>();
            foreach (var power in player.ActivePowers.Keys.ToList())
            {
                var remaining = player.ActivePowers[power] - TickSeconds;
                if (remaining <= 0)
                {
                    expired.Add(power);
                }
                else
                {
                    player.ActivePowers[power] = remaining;
                }
            }
            foreach (var power in expired)
            {
                player.ActivePowers.Remove(power);
            }
        }
    }

    private void AdvanceLevel()
    {
        _level = LevelBuilder.Build(_level.Index + 1);
        for (var i = 0; i < Players.Length; i++)
        {
            var spawn = _level.PlayerSpawns[i];
            Players[i].X = spawn.X;
            Players[i].Y = spawn.Y;
            Players[i].CurrentDirection = Direction.None;
        }
    }

    private GameStateSnapshot BuildSnapshot(bool doorOpen, bool isGameOver, bool isWin) => new()
    {
        LevelIndex = _level.Index,
        DoorOpen = doorOpen,
        IsGameOver = isGameOver,
        IsWin = isWin,
        Players = Players.Select(p => new PlayerSnapshot
        {
            ConnectionId = p.ConnectionId,
            Name = p.Name,
            X = p.X,
            Y = p.Y,
            Health = p.Health,
            MaxHealth = p.MaxHealth,
            Score = p.Score,
            ActivePowers = p.ActivePowers.Keys.ToList()
        }).ToList(),
        Zombies = _level.Zombies.Select(z => new ZombieSnapshot { Id = z.Id, X = z.X, Y = z.Y }).ToList(),
        Levers = _level.Levers.Select(l => new LeverSnapshot { Id = l.Id, X = l.X, Y = l.Y, IsActive = l.IsActive }).ToList(),
        Items = _level.Items.Select(i => new ItemSnapshot { Id = i.Id, X = i.X, Y = i.Y, Kind = i.Kind }).ToList()
    };
}
