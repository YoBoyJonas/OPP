# Castle Escape backend audit and refactoring proposal

The backend implements the twelve P1 design patterns described in the repository's [course context](requirements/course_context.md). They participate in gameplay or backend communication. A separate demonstration API also exists, but playing the game does not depend on calling it. The main architectural problem is that the game library combines domain rules, application orchestration, infrastructure and course tooling.

This is the historical audit recorded before restructuring. The structural correction is now implemented; see [ARCHITECTURE](ARCHITECTURE.md) and [CODE_WALKTHROUGH](CODE_WALKTHROUGH.md) for the current layout. Source links below follow the moved files, while findings and baseline test counts describe the earlier implementation. The restart-undo and address-demo qualifications remain separate known limitations.

## Current implementation

Castle Escape is an authoritative, in-memory, two-player cooperative game server. The backend calculates movement, damage, pickups, powers, door state and level completion. A client supplies input and renders the state it receives.

| Capability | Current behavior |
|---|---|
| Lobby | Create a session, share its join code, join as player two, select characters and leave. Names are limited to 1–24 characters; a third player is rejected. |
| Starting | When both players have selected characters, the next simulation tick loads level one. No separate start request or hub connection is required. |
| Characters | Warrior: 5 lives, speed 3.5; Scout: 3 lives, speed 6; Swimmer: 4 lives, speed 4.5 and a swimming speed modifier. Speeds are tiles per second. |
| Simulation | Configured for 20 ticks per second. Movement steps between tiles with continuous progress. Send a direction on key down and `None` on key up. |
| Levels | Ten configured levels, Dungeon then Crypt. Procedural generation includes validation and retries. Preset maps can replace generation through configuration. Default grid settings are 24 × 16. |
| Obstacles | Walls block movement; pits require Jump; water requires Swim. The Swimmer character's speed bonus does not itself grant the Swim power. |
| Items and powers | Health restores lives up to the character maximum; rewards add score; power items grant temporary Jump, Sprint or Swim. Powers stack and combine into JumpDash and FastSwim. |
| Enemies | Zombies use interchangeable Greedy, BFS, A* or Predictive movement. Contact causes damage. Crypt factories change some enemy and terrain behavior. |
| Cooperation | Both levers must be active together. The default `Latch` door remains open afterwards. Both players must stand on exit tiles together to complete the level. |
| Ending | Completing the final level wins; either player reaching zero lives loses. Explicitly leaving aborts the session. A hub disconnect records connectivity but does not automatically end the game. |
| Restart | Restores the level from its pristine prototype and restores level-start lives and score. Active powers are cleared. |
| Networking | REST for lobby and inspection; SignalR for input and pushed updates. REST input and buffered polling also support a client without SignalR. |
| Storage | JSON files contain static definitions. Live sessions and polling messages are held in process memory. There is no database or saved-game persistence. |
| Client | A small HTML/JavaScript canvas playground exists under `Server/wwwroot/playground`. It is development tooling; there is no separate finished client application. |

The session lifecycle is `WaitingForPlayers → CharacterSelect → LoadingLevel → Playing → LevelComplete`, followed by another level or `Victory`. `Defeat` and `Aborted` are terminal outcomes. These phases currently use an enum and a switch, not a completed P2 State implementation.

Behavior and ambiguous game requirements are further described in [DECISIONS](DECISIONS.md). For exact network payloads, use [FRONTEND_INTEGRATION](FRONTEND_INTEGRATION.md), [Contracts](../src/CastleEscape.Contracts), and [openapi.json](openapi.json).

## Verified pattern inventory

The following implementations have both runtime callers and corresponding tests under [Core.Tests/Patterns](../tests/CastleEscape.Core.Tests/Patterns). The count is twelve explicitly targeted P1 patterns; incidental similarities to other patterns do not establish completion of the P2 course requirements.

| Pattern | Implementation and actual use | Course requirement and qualification |
|---|---|---|
| Singleton | [ContentCatalog](../src/CastleEscape.Infrastructure/Content/ContentCatalog.cs) uses `Lazy<ContentCatalog>` with `ExecutionAndPublication`. Startup, sessions and generation share the content instance. | Concurrent access tests exist. The guarantee concerns initialization; it does not automatically make every referenced object immutable. |
| Factory Method | [ItemSpawner](../src/CastleEscape.Domain/Items/ItemSpawner.cs) delegates `CreateItem` to Health, Reward and Power spawners. Both level builders use these spawners. | Three concrete product classes: `HealthItem`, `RewardItem`, `PowerItem`. Their effects run during item collection. |
| Abstract Factory | [Theme factories](../src/CastleEscape.Domain/Generation/Themes/IThemeFactory.cs) create consistent Dungeon or Crypt families during level construction. | Two concrete factories, each providing wall, water, pit and zombie products. Differences include traversal speed and enemy behavior. |
| Builder | [LevelDirector](../src/CastleEscape.Domain/Generation/LevelDirector.cs) executes ordered construction steps through `ILevelBuilder`. `LevelProvider` selects the implementation. | Two concrete builders: procedural and preset. Tests cover construction order, retry behavior and generated results. |
| Prototype | [LevelState](../src/CastleEscape.Domain/World/LevelState.cs) provides deep and shallow cloning. `GameWorld.BeginLevel` and restart use the configured clone method. | Deep/shallow comparison, identity/address demonstration and switching exist. Deep is the playable default; shallow deliberately shares mutable state and breaks restart restoration. |
| Strategy | [ChaseStrategies](../src/CastleEscape.Domain/AI/ChaseStrategies.cs) implement `IZombieMovementStrategy`; the session calls each zombie's `Strategy.NextStep`. | Four concrete strategies: Greedy, BFS, A*, Predictive. Tests compare routes and runtime switching. |
| Decorator | [Abilities](../src/CastleEscape.Domain/Powers/Abilities.cs) wraps character abilities with power and combo decorators. `PowerManager` rebuilds the chain as powers change. Movement reads these abilities. | At least three nested decoration levels are tested. Stack levels produce repeated wrappers; expiration removes their effects. |
| Command | [GameCommands](../src/CastleEscape.Domain/Commands/GameCommands.cs) encapsulates input, steps and restart. `CommandProcessor` queues, executes and records commands. | Undo is used in real movement to cancel the later step when two players claim one tile. Restart undo is incomplete: active powers are not restored. |
| Observer | [GameEventPublisher](../src/CastleEscape.Domain/Events/GameEventPublisher.cs) notifies client-message, statistics, log and sound-cue observers attached by `GameSession`. | Attach/detach and event delivery are tested; [Observer documentation](patterns/Observer.md) includes the sequence diagram. Sound cues are messages for a client, not backend audio playback. |
| Adapter | [JSON](../src/CastleEscape.Infrastructure/Serialization/JsonMessageSerializerAdapter.cs) and [XML](../src/CastleEscape.Infrastructure/Serialization/XmlMessageSerializerAdapter.cs) adapters expose `IMessageSerializer`. Gameplay state and polling endpoints use them. | Tests check round trips and different interface/adaptee member counts. SignalR itself uses its configured JSON protocol; it does not pass through these adapters. |
| Bridge | [ClientNotifiers](../src/CastleEscape.Application/Messaging/ClientNotifiers.cs) separates state/event delivery policy from `IClientChannel`. The hosted game loop dispatches through these objects. | Two refined abstractions (`StateNotifier`, `EventNotifier`) and two runtime implementations (SignalR, polling). Both channels are enabled by default. |
| Facade | [GameFacade](../src/CastleEscape.Application/Sessions/GameFacade.cs) coordinates the registry, sessions, scheduler and content. REST gameplay, the hub and the hosted loop use it. | Multiple actual clients and at least three subsystems. Content, preview and development endpoints also have direct service dependencies, so the facade is not literally the entry point for every endpoint. |

The annotations in `Game/Patterns` describe implementations; they do not implement the patterns. Likewise, the classes in `Game/PatternDemos` exercise production classes rather than supplying the main gameplay implementations.

Two qualifications matter for the defence. First, [RestartLevelCommand](../src/CastleEscape.Domain/Commands/GameCommands.cs) saves the previous level, elapsed time, lives, score and tiles, but not the active power timers and stacks cleared by restart. Its `Undo` therefore does not fully satisfy the command interface's promise to reverse execution exactly. Existing restart tests do not cover that lost state. Second, [ObjectAddress](../tools/CastleEscape.PatternDemos/Demos/ObjectAddress.cs) attempts a no-GC region, but reads addresses even if that region cannot be established. Stable address comparisons are not guaranteed in that fallback. Neither issue means the underlying pattern is absent.

Passing tests and having pattern write-ups do not by themselves establish every grading deliverable. In particular, the course asks for MagicDraw diagrams and live defence changes; these require a separate submission review. The other eleven patterns are described in [P2_ROADMAP](P2_ROADMAP.md) as future work.

## Navigating the current solution

| Project or folder | Responsibility | Clean Architecture equivalent |
|---|---|---|
| `CastleEscape.Server` | Executable ASP.NET Core host, REST endpoints, SignalR hub, timer, DI registrations and configuration | Presentation plus composition root and some infrastructure |
| `CastleEscape.Game` | World model, rules, generation, sessions, commands, messaging, content loading and demos | Domain, Application and some Infrastructure combined |
| `CastleEscape.Contracts` | Requests, responses, realtime messages and shared enums, plus development and pattern-demo contracts | Public transport contracts, currently mixed with domain vocabulary |
| `CastleEscape.PatternDemos` | Console entry point that runs the twelve demonstrations | Course tooling; most implementation still resides in `Game/PatternDemos` |
| `CastleEscape.Game.Tests` | Game rules, content, generation and pattern tests | Core behavior verification |
| `CastleEscape.Server.Tests` | HTTP, SignalR, OpenAPI and development endpoint tests | Host integration verification |
| `content/` | Character, item, enemy, obstacle and level definitions plus preset maps | Static runtime assets |
| `docs/` | Integration instructions, course evidence and design decisions | Repository documentation |

The `.sln` groups projects under `src` and `tests`; those solution folders are organizational. A project is a build/dependency boundary, not necessarily a separate running service. `Server`, `Game` and `Contracts` participate in the same backend process. The console demo runner is a separate executable.

In Visual Studio, set **CastleEscape.Server** as the startup project and use its `http` launch profile. The configured address is `http://localhost:5035`. Open `/scalar` for API exploration or `/playground` for the browser test client when `DevTools:Enabled` is true. Use Test Explorer for the two test projects. Choose **CastleEscape.PatternDemos** as the startup project only when demonstrating patterns in the console.

Read these files in order:

1. [Server/Program.cs](../src/CastleEscape.Server/Program.cs): which services are constructed and which endpoints are mapped.
2. [SessionEndpoints](../src/CastleEscape.Server/Endpoints/SessionEndpoints.cs): how a frontend creates, joins and selects a character.
3. [GameFacade](../src/CastleEscape.Application/Sessions/GameFacade.cs): the application operations exposed to gameplay callers.
4. [SessionRegistry](../src/CastleEscape.Application/Sessions/SessionRegistry.cs): session lookup and player-token checks.
5. [GameSession](../src/CastleEscape.Application/Sessions/GameSession.cs): `Tick`, `TickPlaying` and `LoadLevel` explain the simulation.
6. [GameWorld](../src/CastleEscape.Domain/Sessions/GameWorld.cs) and [World](../src/CastleEscape.Domain/World): the state and rules those operations manipulate.

For a movement request, follow this actual path:

```text
POST /api/sessions/{id}/input or GameHub.SetDirection
  → GameFacade.SubmitDirection
  → SessionRegistry.Authenticate
  → GameSession.SubmitDirection
  → CommandProcessor.Enqueue(SetDirectionCommand)

GameLoopService timer
  → GameFacade.Tick → GameLoopScheduler.TickAll → GameSession.Tick
  → TickPlaying → CommandProcessor.ExecutePending
  → PlayerMovement → movement rules and StartStepCommand
  → zombies, pickups, damage, levers and completion
  → snapshots and outgoing messages
  → StateNotifier / EventNotifier → SignalR / polling
```

The HTTP input response is `202 Accepted`; it acknowledges queued input, not completed movement. Place breakpoints in `GameFacade.SubmitDirection`, `CommandProcessor.ExecutePending` and `PlayerMovement.Move` to see that separation. The core simulation can also run without ASP.NET through the test helpers or console demos.

## API specification by purpose

`{id}` below means the session GUID. Player-changing REST operations use `X-Player-Token`; the hub receives `sessionId` and `playerToken` as connection query parameters. Session/state reads and diagnostics currently do not require that token.

| Purpose | Current routes or methods |
|---|---|
| Create and join | `POST /api/sessions`, `POST /api/sessions/join` |
| Browse lobby state | `GET /api/sessions`, `GET /api/sessions/{id}` |
| Choose character and leave | `PUT /api/sessions/{id}/players/me/character`, `DELETE /api/sessions/{id}/players/me` |
| Gameplay through HTTP | `POST /api/sessions/{id}/input`, `POST /api/sessions/{id}/restart` |
| Read current game | `GET /api/sessions/{id}/state`, `/level`, `/hud` |
| Receive buffered messages | `GET /api/sessions/{id}/messages?afterSeq=...`; message bodies support JSON or XML |
| Gameplay through SignalR | `/hubs/game`; client calls `SetDirection`, `RequestRestart`, `Ping` |
| Server push messages | `SessionUpdated`, `LevelStarted`, `StateUpdated`, `GameEvent`, `Error`, `Pong` |
| Static game content | `GET /api/content` and `/characters`, `/consumables`, `/powers`, `/combos`, `/obstacles`, `/zombies`, `/levels`, `/levels/{index}` |
| Map inspection | `GET /api/levels/presets`, `GET /api/levels/{index}/preview` |
| Protocol reference | `GET /api/realtime/protocol`, `GET /api/realtime/examples` |
| Operational inspection | `GET /health`, `GET /api/diagnostics/loop`, `GET /api/diagnostics/sessions/{id}/commands`, `/events` |
| Course demonstrations | `GET /api/patterns`, `GET /api/patterns/{pattern}`, twelve `POST /api/patterns/{key}/demo` routes |
| Development helpers | `/api/dev/quickstart`, per-session undo, power grants, strategy changes and level skipping; `/api/dev/patterns/prototype-clone-mode` |
| API explorer | `/scalar`, `/openapi/v1.json` |

The exported development OpenAPI document currently lists 50 HTTP operations. It includes eight development operations and fourteen pattern operations. Hub methods are described separately by the realtime protocol endpoint and integration guide.

A normal frontend needs only content, lobby operations and either SignalR or HTTP input/polling. It never needs to request a pattern or run a demo. Displaying gameplay effects such as power stacking is a natural demonstration of Decorator; asking the player to run a Decorator endpoint is unnecessary.

Pattern endpoints are currently mapped unconditionally in [Program.cs](../src/CastleEscape.Server/Program.cs). `DevTools:Enabled` gates the development endpoints and playground only. Thus turning off development tools does **not** remove the pattern API. That API also includes demonstrations such as creating up to 512 threads for Singleton and inspecting managed object addresses for Prototype, which should be isolated from the normal game host.

Keeping console demonstrations is appropriate: the repository's course requirements explicitly ask for a working `main()` demonstration. They do not require a public demonstration HTTP API.

## Architectural findings

The current design already has useful boundaries: the game project does not reference ASP.NET Core, requests become queued commands, and clients receive snapshots. The confusion comes from the responsibilities inside those boundaries.

| Finding | Consequence |
|---|---|
| `Game` contains domain rules, sessions, file IO, JSON/XML serialization and demos | Its name does not tell a reader whether a file is gameplay, orchestration, infrastructure or course tooling. |
| `Game` depends on `Contracts` for both enums and network messages | Core game code is coupled to transport shapes. Merely renaming `Game` to `Domain` would not produce a clean dependency boundary. |
| `GameSession` handles lifecycle, simulation, observer wiring, snapshots and outbox messages | It is the best place to understand the game, but it also spans domain and application responsibilities. |
| Some content endpoints return `Game.Content` objects directly | `Contracts` does not describe the entire public API; changing a content model can change a response. |
| Response fields expose concrete class names, such as obstacle `Variant` | The frontend can become coupled to implementation names intended for course inspection. Stable content IDs are a better rendering contract. |
| Demo classes and pattern response models are in runtime projects | The separate console runner does not actually isolate the course tooling. |
| `PatternOptions` selects shallow cloning for live sessions | An intentionally incorrect comparison mode is part of runtime game configuration. Keep comparison switching in course tooling. |
| All sessions and polling buffers remain in dictionaries | There is no persistence, cross-instance coordination or session/buffer expiry. Server restart loses games; finished sessions remain retained. |

The last item is a current scope limitation, not a reason to add a database for the course. A future deployment needing several server instances would need an explicit session ownership and routing design.

## Proposed project structure

Use familiar layer names while keeping folders inside each layer organized around game concepts. Preserve the existing REST and SignalR gameplay behavior during the move.

```text
src/
  CastleEscape.Domain/
    World/             entities, grid, movement, collisions, exits
    AI/                zombie strategies
    Items/             products and spawners
    Powers/            decorators, timers and combinations
    Generation/        builders, director, theme factories, validation
    Sessions/          game state, simulation and gameplay commands
    Events/            domain events and publisher
    Content/           definitions and content abstractions
  CastleEscape.Application/
    Sessions/          GameFacade, session orchestration, scheduler
    Messaging/         notification policies and channel interfaces
    Snapshots/         mapping domain state to application results
    Abstractions/      session store and content access interfaces
  CastleEscape.Infrastructure/
    Content/           JSON loading, preset-file access, ContentCatalog
    Sessions/          in-memory session storage
    Serialization/     JSON and XML adapters
    Messaging/         polling buffer implementation
  CastleEscape.Contracts/
    Sessions/          public request and response DTOs
    Gameplay/          state, layout and HUD DTOs
    Realtime/          public hub messages
    Content/           explicit content response DTOs
  CastleEscape.Server/
    Endpoints/         HTTP mapping
    Hubs/              SignalR entry points
    Realtime/          hosted clock and SignalR channel
    Configuration/     options binding and dependency registration
    Development/       optional playground and development routes
tools/
  CastleEscape.PatternDemos/   console demonstrations and their results
tests/
  ...                         tests following the resulting boundaries
content/
docs/
```

Dependency rules would be:

- **Domain** depends on no other solution project. Move domain enums such as direction and power kind here and explicitly map them to wire enums at the boundary.
- **Application** depends on Domain. It owns use-case coordination and interfaces required by that coordination. For a strict separation, use application result/message types and map public DTOs at the server boundary.
- **Infrastructure** depends on Application and Domain, implements their interfaces, and may use Contracts for transport serialization. Keep SignalR-specific delivery in Server so Infrastructure does not depend on the host.
- **Contracts** remains independent and contains public transport data only.
- **Server** composes the implementation and maps requests, results and transport messages. Domain access, when needed for mappings, remains an outward-to-inward dependency.
- **PatternDemos** references the implementations it demonstrates. No runtime project references the demo runner. A course-only metadata attribute can remain in the core if useful; reflection catalogs and demonstration responses belong with tooling.

This requires responsibility changes as well as file moves. Split `GameSession` so the simulation and domain events stay in Domain while lobby orchestration, connection metadata, wire snapshots and outgoing delivery stay outside it. Split `SessionRegistry` so token/session operations belong to Application and dictionary storage belongs to Infrastructure. Separate pure preset construction from reading preset files. Preserve the Singleton requirement by exposing the actual infrastructure content singleton through a content interface; simply replacing it with an unrelated DI lifetime would weaken the course demonstration.

For this course project, no generic repository framework, mediator package or extra database is needed to establish these boundaries. Keep interfaces where they isolate storage, IO or a meaningful implementation choice.

## Refactoring sequence and acceptance criteria

1. **Isolate course tooling.** Move demo implementations, reflection catalog and demo result types into the tooling project. Remove `/api/patterns` from the normal host; retain the console entry point. If HTTP demonstrations are still desired, use a separate course host. Update tests, OpenAPI and defence instructions together.
2. **Separate infrastructure.** Extract JSON/preset loading, serializer adapters and polling storage behind interfaces. Give content endpoints explicit response DTOs. Keep JSON/XML state and polling behavior covered by existing integration tests.
3. **Separate Domain and Application.** Extract simulation rules from session orchestration, remove wire DTO dependencies from Domain, and group registration code outside `Program.cs`. Keep the facade as the cohesive game-use-case entry point.
4. **Complete the identified behavior gaps.** Make restart undo restore all state it changes, including powers, or explicitly narrow what undo promises. Keep shallow-clone experiments outside normal play. Make prototype address evidence clearly conditional on a successful no-GC region.
5. **Make navigation match the result.** Group runtime projects, tools, tests and solution documentation clearly in Visual Studio. Update source links, pattern participant discovery and course diagrams after namespaces move.

Acceptance means that a client can still create/join/select/play/restart through the existing gameplay protocol; all twelve patterns still have runtime use and console demonstrations; Domain has no ASP.NET, file IO, demo or transport-contract dependency; and the normal game API contains no pattern-demo operations. Existing gameplay tests should continue to pass, supplemented by focused tests for restart undo completeness and the chosen project boundaries.

## Validation of the current baseline

On 2026-10-11, `dotnet test CastleEscape.sln --nologo --verbosity minimal` completed successfully using the installed Windows .NET SDK 10.0.202: **160 game tests and 56 server tests, 216 total; zero failures or skips**. Server tests include two real SignalR test clients, REST gameplay, JSON/XML responses, polling and the exported OpenAPI consistency check.

The build reported two existing warnings in server tests: a possible null value passed to `Enumerable.Max` and an xUnit assertion style warning. The README's previous count of 215 tests was stale. These results validate the existing test coverage, not complete undo semantics, production scalability or every course submission requirement.
