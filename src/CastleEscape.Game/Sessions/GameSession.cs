using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.AI;
using CastleEscape.Game.Commands;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Powers;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Sessions;

/// <summary>
/// One two-player game: lobby, phases and messages. The playing field is a <see cref="GameWorld"/>,
/// which only <see cref="Tick"/> changes. Player inputs become commands, queued from any thread and
/// run at the start of the next tick. After every tick an immutable <see cref="Snapshot"/> is published for readers.
/// </summary>
public class GameSession
{
    private readonly object _gate = new();
    private readonly List<PlayerSlot> _slots = [];
    private readonly List<OutgoingMessage> _outbox = [];
    private readonly CommandProcessor _commands = new();
    private readonly GameWorld _world;
    private readonly GameEventPublisher _events = new();
    private readonly EventLogObserver _eventLog;
    private readonly ContentCatalog _catalog;
    private readonly ILevelProvider _levels;
    private readonly GameOptions _options;
    private readonly InteractionSettings _interaction;
    private readonly int _baseSeed;

    private long _messageSequence;
    private long _tick;
    private int _levelIndex;
    private int _announcedLevelVersion;
    private double _transitionLeft;
    private LevelStartedMessage? _lastLevelStarted;
    private TickStateMessage? _lastState;
    private volatile SessionSnapshot _snapshot;

    /// <param name="patterns">Read on every restart, so a runtime switch of the clone mode takes effect immediately.</param>
    public GameSession(Guid id, string joinCode, ContentCatalog catalog, ILevelProvider levels, GameOptions options, int baseSeed,
        PatternOptions? patterns = null)
    {
        Id = id;
        JoinCode = joinCode;
        _catalog = catalog;
        _levels = levels;
        _options = options;
        _baseSeed = baseSeed;
        _world = new GameWorld(_slots, options, patterns ?? new PatternOptions());

        // Everyone who reacts to game events, notified in this order at the end of each tick.
        _eventLog = new EventLogObserver(() => _tick);
        _events.Attach(new ClientNotificationObserver(e =>
            Send(ClientMethods.GameEvent, new GameEventMessage(Id, NextSeq(), _tick, e.Type, e.PlayerId, e.Message, e.Data()))));
        _events.Attach(new SessionStatisticsObserver(_slots));
        _events.Attach(_eventLog);
        _events.Attach(new SoundCueObserver(_events));
        _interaction = new InteractionSettings(options.PowerDurationSeconds, options.MaxPowerStackLevel, options.RespawnPlayerOnHit);
        _snapshot = BuildSnapshot();
    }

    public Guid Id { get; }
    public string JoinCode { get; }
    public SessionPhase Phase { get; private set; } = SessionPhase.WaitingForPlayers;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>Latest published state. Safe to read from any thread without locking.</summary>
    public SessionSnapshot Snapshot => _snapshot;

    public int MaxLevel => Math.Min(_options.MaxLevel, _catalog.Levels.Count);
    public double TickSeconds => 1.0 / _options.TickRate;
    public bool IsFinished => Phase is SessionPhase.Victory or SessionPhase.Defeat or SessionPhase.Aborted;

    // ---------------------------------------------------------------- lobby

    /// <summary>Adds a player (create or join). The second player moves the session to character select.</summary>
    public PlayerSlot AddPlayer(string name)
    {
        lock (_gate)
        {
            if (Phase != SessionPhase.WaitingForPlayers)
            {
                throw _slots.Count >= 2
                    ? new GameException(GameErrorCode.SessionFull, "This session already has two players.")
                    : new GameException(GameErrorCode.AlreadyStarted, "This session has already started.");
            }

            var slot = new PlayerSlot(Guid.NewGuid(), Guid.NewGuid().ToString("N"), name, _slots.Count + 1);
            _slots.Add(slot);
            Emit(new PlayerJoined(slot.PlayerId, $"{name} joined as player {slot.Slot}.", slot.Slot));
            if (_slots.Count == 2)
            {
                SetPhase(SessionPhase.CharacterSelect);
            }
            AfterLobbyChange();
            return slot;
        }
    }

    /// <summary>PLR-1. When both players have chosen, the first level loads on the next tick (NET-3).</summary>
    public void SelectCharacter(Guid playerId, string characterId)
    {
        lock (_gate)
        {
            if (Phase is not (SessionPhase.WaitingForPlayers or SessionPhase.CharacterSelect))
            {
                throw new GameException(GameErrorCode.WrongPhase, $"Characters can't be changed in phase {Phase}.");
            }
            if (_catalog.FindCharacter(characterId) is null)
            {
                throw new GameException(GameErrorCode.UnknownCharacter,
                    $"Unknown character '{characterId}'. Choose one of: {string.Join(", ", _catalog.Characters.Select(c => c.Id))}.");
            }

            GetSlot(playerId).CharacterId = characterId;
            if (_slots.Count == 2 && _slots.All(s => s.CharacterId is not null))
            {
                _levelIndex = 1;
                SetPhase(SessionPhase.LoadingLevel);
            }
            AfterLobbyChange();
        }
    }

    /// <summary>A player leaves: the session ends for both (D10 / Aborted).</summary>
    public void Leave(Guid playerId)
    {
        lock (_gate)
        {
            var slot = GetSlot(playerId);
            Emit(new PlayerLeft(playerId, $"{slot.Name} left the game."));
            if (!IsFinished)
            {
                SetPhase(SessionPhase.Aborted);
            }
            AfterLobbyChange();
        }
    }

    public void SetConnection(Guid playerId, string? connectionId)
    {
        lock (_gate)
        {
            GetSlot(playerId).ConnectionId = connectionId;
            AfterLobbyChange();
        }
    }

    /// <summary>Clears the player's connection, unless a newer connection has already replaced it.</summary>
    public void Disconnect(Guid playerId, string connectionId)
    {
        lock (_gate)
        {
            var slot = GetSlot(playerId);
            if (slot.ConnectionId == connectionId)
            {
                slot.ConnectionId = null;
                AfterLobbyChange();
            }
        }
    }

    /// <summary>Ends the session after an unexpected error in its tick, telling the clients why.</summary>
    public void Fail(string reason)
    {
        lock (_gate)
        {
            Send(ClientMethods.Error, new ErrorMessage(Id, NextSeq(), _tick, GameErrorCode.InvalidRequest, reason));
            SetPhase(SessionPhase.Aborted);
            _snapshot = BuildSnapshot();
        }
    }

    public PlayerSlot? FindByToken(string token)
    {
        lock (_gate)
        {
            return _slots.FirstOrDefault(s => s.Token == token);
        }
    }

    // ---------------------------------------------------------------- input (commands, queued)

    public void SubmitDirection(Guid playerId, Direction direction) =>
        _commands.Enqueue(new SetDirectionCommand(_commands.NextSequence(), playerId, direction));

    public void RequestRestart(Guid playerId) =>
        _commands.Enqueue(new RestartLevelCommand(_commands.NextSequence(), playerId));

    /// <summary>Dev tools: every zombie of the current level chases with <paramref name="kind"/>, from the next tick (undoable).</summary>
    public void SetZombieStrategy(MovementStrategyKind kind) =>
        _commands.Enqueue(new SetZombieStrategyCommand(_commands.NextSequence(), kind));

    /// <summary>Dev tools: completes the current level now, as if both players had reached the exit.</summary>
    public void SkipLevel()
    {
        lock (_gate)
        {
            if (Phase != SessionPhase.Playing)
            {
                throw new GameException(GameErrorCode.WrongPhase, $"Only a level being played can be skipped, not in phase {Phase}.");
            }
            var index = _world.Level!.Index;
            Emit(new LevelCompleted(null, $"Level {index} skipped (dev tools).", index));
            _transitionLeft = _options.LevelTransitionSeconds;
            SetPhase(SessionPhase.LevelComplete);
            FlushEvents();
            _snapshot = BuildSnapshot();
        }
    }

    /// <summary>Dev tools: gives a base power without an item, from the next tick.</summary>
    public void GivePower(Guid playerId, PowerType power, double? durationSeconds = null)
    {
        lock (_gate)
        {
            GetSlot(playerId);
        }
        var grant = _catalog.Consumables.Select(c => c.Grant).FirstOrDefault(g => g?.Power == power)
            ?? throw new GameException(GameErrorCode.InvalidRequest, $"No item grants {power}; only base powers can be given.");
        _commands.Enqueue(new GivePowerCommand(_commands.NextSequence(), playerId, grant,
            durationSeconds ?? grant.DurationSeconds ?? _options.PowerDurationSeconds, _options.MaxPowerStackLevel, _catalog.Combos));
    }

    /// <summary>Undoes the most recent command (optionally only one of a player's). Returns its name, or null if none.</summary>
    public string? UndoLastCommand(Guid? playerId = null)
    {
        lock (_gate)
        {
            if (Phase != SessionPhase.Playing)
            {
                throw new GameException(GameErrorCode.WrongPhase, $"Commands can only be undone while playing, not in phase {Phase}.");
            }
            var undone = _commands.UndoLast(_world, playerId);
            AnnounceLevelIfReplaced();
            FlushEvents();
            SendState();
            _snapshot = BuildSnapshot();
            return undone?.Name;
        }
    }

    /// <summary>The last <see cref="EventLogObserver.DefaultCapacity"/> game events, oldest first.</summary>
    public IReadOnlyList<EventLogEntry> RecentEvents()
    {
        lock (_gate)
        {
            return _eventLog.Entries;
        }
    }

    /// <summary>Lets extra observers listen to this session's events (tests, demos, future features).</summary>
    public void Attach(IGameEventObserver observer)
    {
        lock (_gate)
        {
            _events.Attach(observer);
        }
    }

    public void Detach(IGameEventObserver observer)
    {
        lock (_gate)
        {
            _events.Detach(observer);
        }
    }

    /// <summary>The last <see cref="CommandProcessor.HistoryLimit"/> commands, oldest first.</summary>
    public IReadOnlyList<CommandRecord> CommandHistory()
    {
        lock (_gate)
        {
            return _commands.History();
        }
    }

    /// <summary>Takes the messages produced since the last call, in order.</summary>
    public List<OutgoingMessage> DrainOutbox()
    {
        lock (_gate)
        {
            var messages = _outbox.ToList();
            _outbox.Clear();
            return messages;
        }
    }

    // ---------------------------------------------------------------- tick

    /// <summary>Advances the session by one fixed step. All phase behaviour goes through this switch (seam for P2 State).</summary>
    public void Tick(double seconds)
    {
        lock (_gate)
        {
            _tick++;
            switch (Phase)
            {
                case SessionPhase.LoadingLevel:
                    _commands.DiscardPending();
                    LoadLevel(_levelIndex);
                    break;

                case SessionPhase.Playing:
                    TickPlaying(seconds);
                    break;

                case SessionPhase.LevelComplete:
                    _commands.DiscardPending();
                    _transitionLeft -= seconds;
                    if (_transitionLeft <= 0)
                    {
                        AdvanceLevel();
                    }
                    break;

                default: // WaitingForPlayers, CharacterSelect, Victory, Defeat, Aborted: nothing moves.
                    _commands.DiscardPending();
                    break;
            }

            FlushEvents();
            _snapshot = BuildSnapshot();
        }
    }

    private void TickPlaying(double seconds)
    {
        _commands.ExecutePending(_world, _tick);                                          // 1. inputs (commands)
        AnnounceLevelIfReplaced();                                                        //    a restart replaces the level

        var level = _world.Level!;
        var players = _world.Players;
        var events = _world.Events;
        _world.LevelElapsed += seconds;
        PowerRules.TickPowers(players, _catalog.Combos, seconds, events);                 // 2. power timers, combos
        PlayerMovement.Move(_world, _commands, _tick, seconds);                           // 3. players
        MoveZombies(players, seconds);                                                    // 4. zombies
        InteractionResolver.CollectItems(level, players, _catalog.Combos, _interaction, events); // 5. interactions
        InteractionResolver.ResolveZombieContacts(level, players, _interaction, events);
        _world.Exit!.UpdateLevers(players, events);

        if (players.FirstOrDefault(p => p.IsDead) is { } dead)                           // 6. defeat / door / exit
        {
            Emit(new GameLost(dead.PlayerId, $"{dead.Name} has no lives left. Game over."));
            SetPhase(SessionPhase.Defeat);
        }
        else if (_world.Exit.Update(players, events))
        {
            Emit(new LevelCompleted(null, $"Level {level.Index} complete!", level.Index));
            _transitionLeft = _options.LevelTransitionSeconds;
            SetPhase(SessionPhase.LevelComplete);
        }

        SendState();                                                                      // 7. publish
    }

    /// <summary>ZMB-1: when a zombie stands on a tile, its strategy picks the next step towards the nearest player.</summary>
    private void MoveZombies(IReadOnlyList<PlayerEntity> players, double seconds)
    {
        var level = _world.Level!;
        var world = new WorldView(level, players);
        foreach (var zombie in level.Zombies)
        {
            if (!zombie.IsMoving)
            {
                TryStartZombieStep(zombie, world);
            }
            if (zombie.Advance(zombie.Speed * seconds, out var leftover) && TryStartZombieStep(zombie, world))
            {
                zombie.Advance(Math.Min(leftover, 0.99), out _);
            }
        }
    }

    private static bool TryStartZombieStep(ZombieEntity zombie, WorldView world)
    {
        var direction = zombie.Strategy.NextStep(zombie, world);
        if (direction == Direction.None || !world.CanZombieEnter(zombie.Tile.Step(direction)))
        {
            return false;
        }
        zombie.BeginStep(direction);
        return true;
    }

    // ---------------------------------------------------------------- levels

    private void LoadLevel(int index)
    {
        LevelState level;
        try
        {
            level = _levels.CreateLevel(index, unchecked(_baseSeed + index * 1009));
        }
        catch (LevelGenerationException ex)
        {
            Send(ClientMethods.Error, new ErrorMessage(Id, NextSeq(), _tick, GameErrorCode.LevelGenerationFailed, ex.Message));
            SetPhase(SessionPhase.Aborted);
            return;
        }

        foreach (var slot in _slots.Where(s => s.Entity is null))
        {
            slot.Entity = new PlayerEntity(slot.PlayerId, slot.Name, _catalog.GetCharacter(slot.CharacterId!), level.StartTiles[slot.Slot - 1]);
        }
        _world.BeginLevel(level);
        AnnounceLevelIfReplaced();
        Emit(new Events.LevelStarted(null, $"Level {index} ({level.Theme}) started.", index, level.Theme));
    }

    /// <summary>Sends LevelStarted when the world's level was replaced (new level, restart, undone restart).</summary>
    private void AnnounceLevelIfReplaced()
    {
        if (_world.LevelVersion == _announcedLevelVersion)
        {
            return;
        }
        _announcedLevelVersion = _world.LevelVersion;
        SetPhase(SessionPhase.Playing);

        var state = SnapshotMapper.ToTickState(Id, _messageSequence, _tick, Phase, _world.Level!, _slots);
        _lastState = state;
        _lastLevelStarted = SnapshotMapper.ToLevelStarted(Id, NextSeq(), _tick, _world.Level!, state);
        Send(ClientMethods.LevelStarted, _lastLevelStarted);
    }

    /// <summary>LVL-1, WIN-1: next level, or victory after the last one.</summary>
    private void AdvanceLevel()
    {
        if (_levelIndex >= MaxLevel)
        {
            Emit(new GameWon(null, $"All {MaxLevel} levels cleared. Victory!"));
            SetPhase(SessionPhase.Victory);
            return;
        }
        _levelIndex++;
        SetPhase(SessionPhase.LoadingLevel);
    }

    // ---------------------------------------------------------------- output

    private void SetPhase(SessionPhase phase)
    {
        if (Phase == phase)
        {
            return;
        }
        var previous = Phase;
        Phase = phase;
        Emit(new PhaseChanged(null, $"{previous} -> {phase}", previous, phase));
        Send(ClientMethods.SessionUpdated, new SessionUpdatedMessage(Id, NextSeq(), _tick, SessionDto()));
    }

    private void Emit(GameEvent e) => _world.Events.Add(e);

    /// <summary>Counts each event for the HUD statistics and sends it to the clients.</summary>
    private void FlushEvents()
    {
        var events = _world.Events.ToList();
        _world.Events.Clear();
        foreach (var e in events)
        {
            _events.Publish(e);
        }
    }

    private void SendState()
    {
        _lastState = SnapshotMapper.ToTickState(Id, NextSeq(), _tick, Phase, _world.Level!, _slots);
        Send(ClientMethods.StateUpdated, _lastState);
    }

    private void AfterLobbyChange()
    {
        FlushEvents();
        Send(ClientMethods.SessionUpdated, new SessionUpdatedMessage(Id, NextSeq(), _tick, SessionDto()));
        _snapshot = BuildSnapshot();
    }

    private void Send(string method, object payload) => _outbox.Add(new OutgoingMessage(method, payload));

    private long NextSeq() => ++_messageSequence;

    private SessionSnapshot BuildSnapshot() => new(
        SessionDto(),
        _lastLevelStarted,
        _lastState,
        _world.Level is null ? null : SnapshotMapper.ToLayout(Id, _world.Level),
        SnapshotMapper.ToHud(Id, Phase, _world.Level?.Index ?? 0, MaxLevel, _tick, _world.LevelElapsed, _world.Level, _slots));

    private Contracts.Sessions.SessionDto SessionDto() =>
        SnapshotMapper.ToSessionDto(Id, JoinCode, Phase, _world.Level?.Index ?? 0, MaxLevel, _slots);

    private PlayerSlot GetSlot(Guid playerId) =>
        _slots.FirstOrDefault(s => s.PlayerId == playerId)
        ?? throw new GameException(GameErrorCode.InvalidPlayerToken, "You are not a player in this session.");
}
