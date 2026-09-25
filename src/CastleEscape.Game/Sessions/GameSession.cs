using System.Collections.Concurrent;
using CastleEscape.Contracts;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Game.AI;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Content;
using CastleEscape.Game.Events;
using CastleEscape.Game.Generation;
using CastleEscape.Game.Powers;
using CastleEscape.Game.World;

namespace CastleEscape.Game.Sessions;

/// <summary>
/// One two-player game. Owns its world, which only <see cref="Tick"/> changes. Lobby calls and inputs from
/// the hub/REST are either short locked updates (join, pick character) or queued for the next tick.
/// After every tick an immutable <see cref="Snapshot"/> is published for readers.
/// </summary>
public class GameSession
{
    private readonly object _gate = new();
    private readonly ConcurrentQueue<PlayerInput> _inputs = new();
    private readonly List<PlayerSlot> _slots = [];
    private readonly List<OutgoingMessage> _outbox = [];
    private readonly List<PendingEvent> _events = [];
    private readonly ContentCatalog _catalog;
    private readonly ILevelProvider _levels;
    private readonly GameOptions _options;
    private readonly PatternOptions _patterns;
    private readonly InteractionSettings _interaction;
    private readonly int _baseSeed;

    private long _inputSequence;
    private long _messageSequence;
    private long _tick;
    private int _levelIndex;
    private double _levelElapsed;
    private double _transitionLeft;
    private LevelState? _level;
    private LevelState? _pristine; // the level as built; never played on (Prototype)
    private ExitMechanism? _exit;
    private LevelCheckpoint? _checkpoint;
    private LevelStartedMessage? _lastLevelStarted;
    private TickStateMessage? _lastState;
    private volatile SessionSnapshot _snapshot;

    /// <param name="patterns">Read on every restart, so a runtime switch of the clone mode takes effect immediately.</param>
    public GameSession(Guid id, string joinCode, ContentCatalog catalog, ILevelProvider levels, GameOptions options, int baseSeed,
        PatternOptions? patterns = null)
    {
        _patterns = patterns ?? new PatternOptions();
        Id = id;
        JoinCode = joinCode;
        _catalog = catalog;
        _levels = levels;
        _options = options;
        _baseSeed = baseSeed;
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
            Emit(PendingEvent.Of(GameEventTypes.PlayerJoined, slot.PlayerId, $"{name} joined as player {slot.Slot}."));
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
            Emit(PendingEvent.Of(GameEventTypes.PlayerLeft, playerId, $"{slot.Name} left the game."));
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

    // ---------------------------------------------------------------- input (queued)

    public void SubmitDirection(Guid playerId, Direction direction) =>
        _inputs.Enqueue(new PlayerInput(Interlocked.Increment(ref _inputSequence), playerId, PlayerInputKind.SetDirection, direction));

    public void RequestRestart(Guid playerId) =>
        _inputs.Enqueue(new PlayerInput(Interlocked.Increment(ref _inputSequence), playerId, PlayerInputKind.RequestRestart));

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
                    DiscardInputs();
                    LoadLevel(_levelIndex);
                    break;

                case SessionPhase.Playing:
                    TickPlaying(seconds);
                    break;

                case SessionPhase.LevelComplete:
                    DiscardInputs();
                    _transitionLeft -= seconds;
                    if (_transitionLeft <= 0)
                    {
                        AdvanceLevel();
                    }
                    break;

                default: // WaitingForPlayers, CharacterSelect, Victory, Defeat, Aborted: nothing moves.
                    DiscardInputs();
                    break;
            }

            FlushEvents();
            _snapshot = BuildSnapshot();
        }
    }

    private void TickPlaying(double seconds)
    {
        var level = _level!;
        var players = Players();
        _levelElapsed += seconds;

        ApplyInputs();                                                                   // 1. inputs
        if (Phase != SessionPhase.Playing) return;
        PowerRules.TickPowers(players, _catalog.Combos, seconds, _events);                // 2. power timers, combos
        MovePlayers(players, seconds);                                                    // 3. players
        MoveZombies(players, seconds);                                                    // 4. zombies
        InteractionResolver.CollectItems(level, players, _catalog.Combos, _interaction, _events); // 5. interactions
        InteractionResolver.ResolveZombieContacts(level, players, _interaction, _events);
        _exit!.UpdateLevers(players, _events);

        if (players.Any(p => p.IsDead))                                                   // 6. defeat / door / exit
        {
            var dead = players.First(p => p.IsDead);
            Emit(PendingEvent.Of(GameEventTypes.GameLost, dead.PlayerId, $"{dead.Name} has no lives left. Game over."));
            SetPhase(SessionPhase.Defeat);
        }
        else if (_exit.Update(players, _events))
        {
            Emit(PendingEvent.Of(GameEventTypes.LevelCompleted, null, $"Level {level.Index} complete!", new { level = level.Index }));
            _transitionLeft = _options.LevelTransitionSeconds;
            SetPhase(SessionPhase.LevelComplete);
        }

        SendState();                                                                      // 7. publish
    }

    private void ApplyInputs()
    {
        var inputs = new List<PlayerInput>();
        while (_inputs.TryDequeue(out var input))
        {
            inputs.Add(input);
        }

        foreach (var input in inputs.OrderBy(i => i.Sequence))
        {
            var player = _slots.FirstOrDefault(s => s.PlayerId == input.PlayerId)?.Entity;
            if (player is null)
            {
                continue;
            }

            switch (input.Kind)
            {
                case PlayerInputKind.SetDirection:
                    player.HeldDirection = input.Direction;
                    break;
                case PlayerInputKind.RequestRestart:
                    RestartLevel(player);
                    break;
            }
        }
    }

    private void DiscardInputs()
    {
        while (_inputs.TryDequeue(out _))
        {
        }
    }

    /// <summary>MOV-1/MOV-3: each player steps tile by tile in their held direction, independently.</summary>
    private void MovePlayers(IReadOnlyList<PlayerEntity> players, double seconds)
    {
        var level = _level!;
        foreach (var player in players)
        {
            var others = players.Where(p => p != player).ToList();
            var distance = 0.0;

            if (!player.IsMoving)
            {
                TryStartStep(player, others, level);
            }
            if (player.IsMoving)
            {
                distance = PlayerAbilities.SpeedOn(player, level.Grid.GetTile(player.NextTile!.Value)) * seconds;
            }

            // One arrival per tick at most; leftover distance carries into the next step.
            if (player.Advance(distance, out var leftover) && TryStartStep(player, others, level))
            {
                player.Advance(Math.Min(leftover, 0.99), out _);
            }
        }
    }

    private static bool TryStartStep(PlayerEntity player, IReadOnlyList<PlayerEntity> others, LevelState level)
    {
        if (player.HeldDirection == Direction.None)
        {
            return false;
        }
        var target = player.Tile.Step(player.HeldDirection);
        if (!MovementRules.CanPlayerEnter(player, target, level, others)) // COL-1: rejected, stays put
        {
            return false;
        }
        player.BeginStep(player.HeldDirection);
        return true;
    }

    /// <summary>ZMB-1: when a zombie stands on a tile, its strategy picks the next step towards the nearest player.</summary>
    private void MoveZombies(IReadOnlyList<PlayerEntity> players, double seconds)
    {
        var level = _level!;
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
        _checkpoint = new LevelCheckpoint(Players().ToDictionary(p => p.PlayerId, p => (p.Lives, p.Score)));
        _pristine = level;
        StartLevel(level.Clone(_patterns.PrototypeCloneMode));
        Emit(PendingEvent.Of(GameEventTypes.LevelStarted, null, $"Level {index} ({level.Theme}) started.", new { level = index }));
    }

    private void StartLevel(LevelState level)
    {
        _level = level;
        _exit = new ExitMechanism(level, _options.DoorMode);
        _levelElapsed = 0;
        foreach (var slot in _slots)
        {
            slot.Entity!.PlaceAt(level.StartTiles[slot.Slot - 1]);
            slot.Entity.ClearPowers();
        }
        SetPhase(SessionPhase.Playing);

        var state = SnapshotMapper.ToTickState(Id, _messageSequence, _tick, Phase, level, _slots);
        _lastState = state;
        _lastLevelStarted = SnapshotMapper.ToLevelStarted(Id, NextSeq(), _tick, level, state);
        Send(ClientMethods.LevelStarted, _lastLevelStarted);
    }

    /// <summary>
    /// D8: the same level again, as a fresh clone of the pristine level (Prototype); lives and score back
    /// to the level start. With <see cref="CloneMode.Shallow"/> the "fresh" copy shares its items, zombies and
    /// levers with the level just played, so the restart visibly fails (the defence switch).
    /// </summary>
    private void RestartLevel(PlayerEntity requestedBy)
    {
        var level = _pristine!.Clone(_patterns.PrototypeCloneMode);
        foreach (var player in Players())
        {
            var (lives, score) = _checkpoint!.Players[player.PlayerId];
            player.RestoreStats(lives, score);
        }
        StartLevel(level);
        Emit(PendingEvent.Of(GameEventTypes.LevelRestarted, requestedBy.PlayerId,
            $"{requestedBy.Name} restarted level {level.Index}.", new { level = level.Index }));
    }

    /// <summary>LVL-1, WIN-1: next level, or victory after the last one.</summary>
    private void AdvanceLevel()
    {
        if (_levelIndex >= MaxLevel)
        {
            Emit(PendingEvent.Of(GameEventTypes.GameWon, null, $"All {MaxLevel} levels cleared. Victory!"));
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
        Emit(PendingEvent.Of(GameEventTypes.PhaseChanged, null, $"{previous} -> {phase}", new { from = previous, to = phase }));
        Send(ClientMethods.SessionUpdated, new SessionUpdatedMessage(Id, NextSeq(), _tick, SessionDto()));
    }

    private void Emit(PendingEvent e) => _events.Add(e);

    /// <summary>Counts each event for the HUD statistics and sends it to the clients.</summary>
    private void FlushEvents()
    {
        foreach (var e in _events)
        {
            if (e.PlayerId is { } playerId && _slots.FirstOrDefault(s => s.PlayerId == playerId) is { } slot)
            {
                slot.Stats[e.Type] = slot.Stats.GetValueOrDefault(e.Type) + 1;
            }
            Send(ClientMethods.GameEvent, new GameEventMessage(Id, NextSeq(), _tick, e.Type, e.PlayerId, e.Message, new Dictionary<string, string>(e.Data)));
        }
        _events.Clear();
    }

    private void SendState()
    {
        _lastState = SnapshotMapper.ToTickState(Id, NextSeq(), _tick, Phase, _level!, _slots);
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
        _level is null ? null : SnapshotMapper.ToLayout(Id, _level),
        SnapshotMapper.ToHud(Id, Phase, _level?.Index ?? 0, MaxLevel, _tick, _levelElapsed, _level, _slots));

    private Contracts.Sessions.SessionDto SessionDto() =>
        SnapshotMapper.ToSessionDto(Id, JoinCode, Phase, _level?.Index ?? 0, MaxLevel, _slots);

    private List<PlayerEntity> Players() => _slots.Where(s => s.Entity is not null).Select(s => s.Entity!).ToList();

    private PlayerSlot GetSlot(Guid playerId) =>
        _slots.FirstOrDefault(s => s.PlayerId == playerId)
        ?? throw new GameException(GameErrorCode.InvalidPlayerToken, "You are not a player in this session.");
}
