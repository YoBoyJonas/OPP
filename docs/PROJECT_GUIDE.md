# Understanding Castle Escape

Castle Escape is a backend for a two-player cooperative escape game. The server keeps the real game state and decides what happens: where players can move, what zombies do, which items are collected, and when a level ends. A client sends player actions and displays the results returned by the server.

The players choose characters, avoid zombies, collect items and use powers to cross obstacles. They cooperate to activate levers and reach the exits. The default run has ten levels; clearing them wins, while either player running out of lives ends the game in defeat.

The solution contains five runtime projects, but they run together inside **one server process**. You start `CastleEscape.Server`; it uses the other four projects as libraries. The twelve design patterns are techniques used inside that implementation. The separate console project demonstrates those techniques for the course.

Read this guide with the solution open beside it. Follow the linked files when a class is introduced; you do not need to read every method on the first pass. Start with sections 1–6 to understand the structure and execution, then use the remaining sections while exploring and debugging.

## Contents

1. [Open and run the solution](#1-open-and-run-the-solution)
2. [Understand the projects and their dependencies](#2-understand-the-projects-and-their-dependencies)
3. [Recognize the main game objects](#3-recognize-the-main-game-objects)
4. [Follow server startup](#4-follow-server-startup)
5. [Follow a game from the lobby to the first move](#5-follow-a-game-from-the-lobby-to-the-first-move)
6. [Understand the game loop](#6-understand-the-game-loop)
7. [Understand level creation and restart](#7-understand-level-creation-and-restart)
8. [Understand the API and client communication](#8-understand-the-api-and-client-communication)
9. [Find and explain the twelve design patterns](#9-find-and-explain-the-twelve-design-patterns)
10. [Read the C sharp conventions used here](#10-read-the-c-sharp-conventions-used-here)
11. [Learn the code through debugging and tests](#11-learn-the-code-through-debugging-and-tests)
12. [Know where to make a change](#12-know-where-to-make-a-change)
13. [Know the current boundaries](#13-know-the-current-boundaries)

## 1 Open and run the solution

Open `CastleEscape.sln`. In Solution Explorer, expand `src`, right-click **CastleEscape.Server**, and choose **Set as Startup Project**. Choose the `http` launch profile and start debugging. The projects target .NET 10, so the .NET 10 SDK and a Visual Studio installation that supports it are required.

Open these addresses manually; the launch profile has automatic browser launch disabled:

| Address | What you use it for |
|---|---|
| `http://localhost:5035/playground` | See and control the game in a small development browser client |
| `http://localhost:5035/scalar` | Browse the HTTP API and send requests manually |
| `http://localhost:5035/health` | Check that the host responds |
| `http://localhost:5035/openapi/v1.json` | Read the machine-readable HTTP API description |

In the playground, **Quickstart: Ana + Ben** creates both players and selects their characters. Use the player links it provides to open their views. Click the board and use arrow keys or WASD. The server still handles movement and game rules; the browser is displaying and controlling them.

The `http` profile selects the Development environment. Its settings enable the playground and development helper endpoints. See [launchSettings.json](../src/CastleEscape.Server/Properties/launchSettings.json) and [appsettings.Development.json](../src/CastleEscape.Server/appsettings.Development.json).

### What the solution folders mean

| Solution folder | Contents |
|---|---|
| `src` | Five projects used by the running backend |
| `tools` | `CastleEscape.PatternDemos`, a separate console executable |
| `tests` | Two automated test projects |
| `Documentation` | Links to documentation files, including this guide |

A **solution** groups projects. A **project** has a `.csproj` file that describes how to compile it and what it references. A regular folder inside a project groups related source files; it does not create another application or architectural layer.

Solution folders organize Visual Studio's view. For example, `Documentation` is a solution folder pointing to files physically stored in `docs`. The repository also contains a root `content` directory for JSON definitions and preset maps. It is not a C# project and may be easier to browse in File Explorer. `bin` and `obj` contain generated build output; read and edit the original source files instead.

## 2 Understand the projects and their dependencies

This structure should feel familiar if you know Clean Architecture:

| Project | The question it answers | Important folders |
|---|---|---|
| **Domain** | What are the game rules and objects? | `World`, `Sessions`, `Powers`, `AI`, `Commands`, `Generation`, `Items`, `Events`, `Content` |
| **Application** | How do we carry out a session operation? | `Sessions`, `Snapshots`, `Generation`, `Events`, `Messaging`, `Content`, `Development` |
| **Infrastructure** | How do we read files, store sessions and serialize messages? | `Content`, `Sessions`, `Serialization`, `Messaging` |
| **Contracts** | What data can a client send or receive? | `Sessions`, `Gameplay`, `Realtime`, `Content`, `Diagnostics`, `Dev` |
| **Server** | How can clients reach the backend, and how is it started? | `Endpoints`, `Hubs`, `Realtime`, `Configuration`, `OpenApi`, `Development`, `wwwroot` |

For example, deciding whether a player can cross water belongs in Domain. Finding the player's session and accepting their input belongs in Application. Turning an HTTP body into a request object belongs at the Server boundary. The shape of that request is declared in Contracts.

These are the actual project references. An arrow means “can directly use types from”:

```text
Server         -> Application, Infrastructure, Domain, Contracts
Infrastructure -> Application, Domain, Contracts
Application    -> Domain, Contracts
Domain         -> no other solution project
Contracts      -> no other solution project
```

**A project reference is not a request-flow diagram.** Infrastructure references Application so it can implement an interface declared there. At runtime, Application can call that interface and reach an Infrastructure object without referencing Infrastructure itself.

Here is a concrete example:

1. Application declares [ISessionStore](../src/CastleEscape.Application/Sessions/ISessionStore.cs), which describes how sessions can be stored and found.
2. Infrastructure implements it in [InMemorySessionStore](../src/CastleEscape.Infrastructure/Sessions/InMemorySessionStore.cs), using a concurrent dictionary.
3. Server registers that implementation when starting.
4. Application's `SessionRegistry` receives an `ISessionStore` and calls it. It does not construct or name `InMemorySessionStore`.

This is dependency inversion: higher-level code declares what it needs, while an outer layer provides the implementation. The same arrangement is used for `IContentCatalog` and `IPresetMapSource` in Domain.

There is one pragmatic choice compared with stricter Clean Architecture templates: **Application references Contracts** and returns its DTOs. There is no second set of almost identical application response objects. Domain still has no dependency on transport DTOs. [SnapshotMapper](../src/CastleEscape.Application/Snapshots/SnapshotMapper.cs) and [ContentQueries](../src/CastleEscape.Application/Content/ContentQueries.cs) perform the conversion at the Application boundary.

There is no database, Entity Framework context, or migration project. `InMemorySessionStore` stores live session objects for the lifetime of this process. The root `content` JSON files define game content; they are not saved player progress.

## 3 Recognize the main game objects

Several classes have similar names because they describe different parts of the same game. Keep these distinctions in mind:

| Object | Meaning and responsibility |
|---|---|
| [GameFacade](../src/CastleEscape.Application/Sessions/GameFacade.cs) | Public application operations such as create, join, choose a character, submit input and read state |
| [SessionRegistry](../src/CastleEscape.Application/Sessions/SessionRegistry.cs) | Finds sessions, creates join codes and checks player tokens, using `ISessionStore` |
| [GameSession](../src/CastleEscape.Application/Sessions/GameSession.cs) | Coordinates one two-player game: lobby, phase changes, command queue, observers, snapshots and outgoing messages |
| [GameWorld](../src/CastleEscape.Domain/Sessions/GameWorld.cs) | Holds the mutable playing state: current level, players, restart prototype, checkpoint and pending domain events |
| [GameSimulation](../src/CastleEscape.Domain/Sessions/GameSimulation.cs) | Advances game rules for one time step and reports whether play continues, the level is complete, or the players lost |
| [LevelState](../src/CastleEscape.Domain/World/LevelState.cs) | One map: grid, items, zombies, levers, door, starts and exits; players are kept separately because they continue between levels |
| [PlayerState](../src/CastleEscape.Domain/Sessions/PlayerState.cs) | A domain participant: identity, chosen character, statistics and an optional live entity |
| [PlayerSlot](../src/CastleEscape.Application/Sessions/PlayerSlot.cs) | Extends `PlayerState` with the session token and connection information |
| [PlayerEntity](../src/CastleEscape.Domain/World/PlayerEntity.cs) | The player on the map: position, lives, score, movement and powers |
| [SessionSnapshot](../src/CastleEscape.Application/Sessions/SessionTypes.cs) | Published DTO views of a session, which the API can read without modifying its world |

**Session coordinates, world holds state, simulation applies rules.** This distinction is useful whenever you are unsure which class to open.

Before a level loads, a player can have a slot and a character selection without having a `PlayerEntity`. Loading the first level creates that entity. The same participant then continues through later levels.

Also distinguish a **definition** from a **live instance**. A `CharacterDefinition` describes the Scout's starting attributes. A `PlayerEntity` is Ana playing that character with her current position and remaining lives. A consumable definition describes a kind of pickup; an `ItemEntity` is one actual pickup at one map position.

## 4 Follow server startup

Start reading at [Server/Program.cs](../src/CastleEscape.Server/Program.cs). It has top-level statements rather than an explicit `Main` method. The important steps are:

1. `WebApplication.CreateBuilder(args)` creates the host builder and configuration.
2. `AddCastleEscape(...)` registers the services and settings the application will use.
3. `builder.Build()` constructs the host.
4. Resolving `IContentCatalog` loads and validates the content immediately. Invalid content fails startup.
5. Middleware is configured for exceptions, status responses and CORS.
6. `Map...Endpoints()` connects URLs to handlers. `MapHub<GameHub>` exposes the SignalR hub.
7. If development tools are enabled, their endpoints and browser files are exposed too.
8. `app.Run()` starts the host, including its background game loop.

Open [ServiceRegistration.cs](../src/CastleEscape.Server/Configuration/ServiceRegistration.cs) next. This is the **composition root**: the place where concrete objects are chosen and wired together.

For example, this existing registration means “when a constructor asks for `ISessionStore`, supply an `InMemorySessionStore`”:

```csharp
services.AddSingleton<ISessionStore, InMemorySessionStore>();
```

Dependency injection, or **DI**, is the mechanism that supplies registered dependencies. You will see an endpoint parameter such as `GameFacade game`; ASP.NET Core obtains that service from DI. The client does not send a facade in its request.

`AddSingleton` means one service instance is shared within this host's service container. It does **not** mean the server supports only one game: the shared registry stores many separate `GameSession` objects. It also does not automatically constitute the course's Singleton pattern; `ContentCatalog` has its own explicit singleton implementation, explained later.

`AddHostedService<GameLoopService>()` registers work that runs in the background while the server runs. This is why zombies and power timers keep advancing even when no HTTP requests arrive.

Settings are bound from [appsettings.json](../src/CastleEscape.Server/appsettings.json) into option classes. For example, `Game:TickRate` becomes `GameOptions.TickRate`. Development settings override the base file. The launch profile controls local addresses and the environment; `appsettings` controls game and host behavior.

## 5 Follow a game from the lobby to the first move

### Create and join

Ana sends `POST /api/sessions` with `{"playerName":"Ana"}`.

[SessionEndpoints.Create](../src/CastleEscape.Server/Endpoints/SessionEndpoints.cs) calls `GameFacade.CreateSession`. The facade asks `SessionRegistry.Create` to create a session, put it in the store, and add Ana. The response contains four values with different purposes:

| Value | Use |
|---|---|
| `sessionId` | Identifies the game in URLs |
| `joinCode` | Lets Ben join the same game |
| `playerId` | Identifies Ana within game state and messages |
| `playerToken` | Authorizes actions as Ana; send it in `X-Player-Token` for protected HTTP actions |

Ben sends `POST /api/sessions/join` with the join code and his name. He receives his own player ID and token. With two players present, the phase becomes `CharacterSelect`.

### Select characters and load the level

Each player calls `PUT /api/sessions/{sessionId}/players/me/character` with their own token and a body such as `{"characterId":"scout"}`.

`SessionRegistry.Authenticate` finds the session and matches the token to a player. `GameSession.SelectCharacter` validates the selection and stores it. Once both players have selected, it changes the phase to `LoadingLevel`.

The **next tick** loads the level and changes the phase to `Playing`. There is no separate start endpoint, and a SignalR connection is not required to start playing through HTTP.

The main lifecycle is:

```text
WaitingForPlayers -> CharacterSelect -> LoadingLevel -> Playing
                                            ^             |
                                            |             v
                                            +------ LevelComplete

After the final level and transition: Victory
During play, if either player has no lives: Defeat
If someone leaves, or a session fails: Aborted
```

`GameSession.Tick` currently implements this with a `switch` on the phase. That is lifecycle logic; it is not yet an implementation of the GoF State pattern.

### Send the first movement input

Ana sends `POST /api/sessions/{sessionId}/input` with her token and `{"direction":"Right"}`.

```text
HTTP request
  GameplayEndpoints.SubmitInput          Server
    GameFacade.SubmitDirection           Application
      SessionRegistry.Authenticate
      GameSession.SubmitDirection
        CommandProcessor.Enqueue         Domain
HTTP response: 202 Accepted

Next playing tick
  CommandProcessor.ExecutePending
    SetDirectionCommand.Execute
  GameSimulation.Advance
    PlayerMovement.Move
      MovementRules.CanPlayerEnter
      StartStepCommand.Execute
      advance the player's position
```

The request queues an intention. `202 Accepted` does not promise that the player already moved. `SetDirectionCommand` changes the held direction; `PlayerMovement` later checks the rules and begins a permitted tile step.

Send `{"direction":"None"}` when the key is released. A direction is held across ticks, so one `Right` request is not a request to move exactly one tile. Releasing it stops further held movement; a step already underway can finish.

SignalR's `GameHub.SetDirection` calls the same facade operation. HTTP and SignalR therefore reach the same gameplay implementation.

## 6 Understand the game loop

A **tick** is one simulation step. At the default rate of 20 ticks per second, each step represents `0.05` seconds of game time. This is independent of how often the browser draws a frame.

[GameLoopService](../src/CastleEscape.Server/Realtime/GameLoopService.cs) owns the timer. It calls `GameFacade.Tick`, which delegates to [GameLoopScheduler.TickAll](../src/CastleEscape.Application/Sessions/GameLoopScheduler.cs). The scheduler advances unfinished sessions and collects their outgoing messages. A failure in one session stops that session while allowing the scheduler to continue with others.

For a session in `Playing`, the order is:

| Order | Code | Effect |
|---|---|---|
| 1 | `CommandProcessor.ExecutePending` | Applies queued inputs and other commands, ordered by sequence within the drained batch |
| 2 | `AnnounceLevelIfReplaced` | Detects changes such as a restart and queues the new level layout |
| 3 | `GameSimulation.Advance` | Advances powers, players, zombies, item pickups, damage, levers and exit checks |
| 4 | `GameSession.TickPlaying` | Converts the simulation result into session phase changes and queues the current state |
| 5 | `FlushEvents` | Publishes domain events to observers |
| 6 | `BuildSnapshot` | Publishes the latest collection of DTO views for API readers |
| 7 | Scheduler and host | Drain outgoing messages, then dispatch them through the configured channels |

Inside [GameSimulation.Advance](../src/CastleEscape.Domain/Sessions/GameSimulation.cs), order matters. Power timers update before movement. Item collection happens after movement. Zombie contact can cause defeat before the level-completion check.

### How movement works

[MovableEntity](../src/CastleEscape.Domain/World/MovableEntity.cs) stores a current `Tile`, an optional `NextTile`, and `Progress` between 0 and 1. It advances toward the next tile according to speed and elapsed time. Its interpolated render position lets a client show smooth movement between tiles.

[MovementRules](../src/CastleEscape.Domain/World/MovementRules.cs) checks map bounds, walls, closed doors, required abilities, and tiles occupied or claimed by another player. [PlayerMovement](../src/CastleEscape.Domain/Sessions/PlayerMovement.cs) uses those rules. If both stationary players try to claim the same free tile in one tick, it undoes the step associated with the later direction input.

[InteractionResolver](../src/CastleEscape.Domain/World/InteractionResolver.cs) handles pickup and zombie-contact consequences. [ExitMechanism](../src/CastleEscape.Domain/World/ExitMechanism.cs) handles levers, the door and the players reaching exit tiles. In the default `Latch` door mode, activating all levers together opens the door permanently for that attempt. Both players must then stand on exit tiles together to complete the level.

### Why snapshots and queues exist

HTTP requests and the background loop can execute concurrently. `GameSession` uses a lock around its tick and other session mutations. Input can be added to a thread-safe command queue and applied during the tick. Ordinary state reads use the latest published snapshot rather than walking the mutable world while it changes.

A **snapshot** here is a set of response/message objects representing the last published view. It is not a saved game, database record, or full undo checkpoint. Its arrays and dictionaries are treated as read-only by consumers, although they are not deeply immutable.

An **outbox** here is simply `GameSession`'s in-memory list of outgoing messages. The scheduler drains it after ticking. This is not a durable transactional outbox, and there is no external message broker.

An **event** says something happened, such as `ItemCollected`. A **command** asks for an action, such as `SetDirection`. A **snapshot** says what the state currently looks like. Those three objects serve different purposes even when they concern the same player.

## 7 Understand level creation and restart

The `content` folder supplies values and definitions:

| File | Defines |
|---|---|
| [characters.json](../content/characters.json) | Character IDs and base attributes |
| [consumables.json](../content/consumables.json) | Health, reward and power pickups |
| [combos.json](../content/combos.json) | Which powers combine and their modifiers |
| [obstacles.json](../content/obstacles.json) | Base obstacle properties |
| [zombies.json](../content/zombies.json) | Zombie definitions, including movement strategies |
| [levels.json](../content/levels.json) | Level progression, themes and generation parameters |
| [presets](../content/presets) | Handwritten maps such as `tutorial.txt` and `arena.txt` |

[ContentLoader](../src/CastleEscape.Infrastructure/Content/ContentLoader.cs) reads the JSON. [ContentCatalog](../src/CastleEscape.Infrastructure/Content/ContentCatalog.cs) provides shared access to the definitions. [ContentValidator](../src/CastleEscape.Domain/Content/ContentValidator.cs) checks their game constraints. The project copies the content into build output; change the repository originals, then rebuild/restart to reload them.

When `GameSession.LoadLevel` asks for a level:

1. Application's [LevelProvider](../src/CastleEscape.Application/Generation/LevelProvider.cs) chooses a procedural builder, or a preset builder if `Generation:PresetLevel` is configured.
2. For a preset, Infrastructure's `FilePresetMapSource` reads the rows. Domain's builder receives those rows without opening files itself.
3. Domain's [LevelDirector](../src/CastleEscape.Domain/Generation/LevelDirector.cs) calls construction steps in order: terrain, exit and door, starting tiles, levers, power obstacles, items, then zombies.
4. A theme factory supplies related obstacle and zombie variants. Item spawners create the correct kinds of pickups.
5. `LevelValidator` checks the assembled level. Procedural generation can retry with another seed up to the configured limit; a fixed preset gets one attempt.
6. `GameWorld.BeginLevel` keeps the built map as `Pristine`, records players' lives and scores in a checkpoint, and starts play on a clone.

The seed lets generation be reproduced with the same inputs. The level definition describes what should be generated; the resulting `LevelState` contains the actual generated objects.

### Restart is different from generating the next level

`RestartLevelCommand` restores the players' level-start lives and scores and starts on a fresh copy of the pristine map. It resets their positions and clears active powers. It does not generate a different random layout. A subsequent level goes through the provider and construction steps again.

Live server sessions use **deep cloning**. Mutable items, zombies, levers and the door are independent of the pristine map, so collecting an item does not remove it from the restart template. Immutable content definitions can be shared.

Shallow cloning remains available to console demos and tests to demonstrate why sharing mutable state breaks restart behavior. It is not a server API option.

## 8 Understand the API and client communication

This server uses **Minimal APIs**. There is no `Controllers` folder: the `MapGet`, `MapPost` and other registrations in `Server/Endpoints` serve that role. A handler reads a request, calls application code, and returns an HTTP result.

The contracts for those inputs and outputs live in `CastleEscape.Contracts`. A **DTO**, or data transfer object, carries data across a boundary. For example, `DirectionRequest` contains a direction, while `TickStateMessage` contains the changing game state. These objects are deliberately separate from live `PlayerEntity` and `LevelState` instances.

### Which endpoint group to open

| URL area | Purpose | Server source |
|---|---|---|
| `/api/content` | Read available characters, powers and other definitions | [ContentEndpoints](../src/CastleEscape.Server/Endpoints/ContentEndpoints.cs) |
| `/api/sessions` | Create, join, choose a character, inspect the lobby and leave | [SessionEndpoints](../src/CastleEscape.Server/Endpoints/SessionEndpoints.cs) |
| `/api/sessions/{sessionId}/input`, `/restart`, `/state`, `/level`, `/hud`, `/messages` | Control and observe an existing game | [GameplayEndpoints](../src/CastleEscape.Server/Endpoints/GameplayEndpoints.cs) |
| `/api/levels` | List presets and preview levels without starting a session | [LevelEndpoints](../src/CastleEscape.Server/Endpoints/LevelEndpoints.cs) |
| `/hubs/game` | Persistent SignalR connection for input and pushed messages | [GameHub](../src/CastleEscape.Server/Hubs/GameHub.cs) |
| `/api/realtime` | Protocol description and example messages | [RealtimeEndpoints](../src/CastleEscape.Server/Endpoints/RealtimeEndpoints.cs) |
| `/health`, `/api/diagnostics` | Host health, loop measurements, command history and event logs | [DiagnosticsEndpoints](../src/CastleEscape.Server/Endpoints/DiagnosticsEndpoints.cs) |
| `/api/dev` | Quickstart, undo, give powers, change zombie strategy and skip levels when enabled | [DevEndpoints](../src/CastleEscape.Server/Development/DevEndpoints.cs) |

The facade handles gameplay/session operations. Read-only content and preview endpoints use `ContentQueries` and `LevelPreviewer` directly. Development shortcuts use `DevActions`. An application layer can have several entry services; they do not all need to be methods on one facade.

### HTTP and SignalR serve complementary roles

HTTP is convenient for lobby operations and individual reads. SignalR maintains a connection so the server can push updates without the browser repeatedly requesting them. A hub client connects to `/hubs/game` with `sessionId` and `playerToken`, then invokes methods such as `SetDirection`.

| Server message | What the client learns |
|---|---|
| `SessionUpdated` | Lobby or session details changed |
| `LevelStarted` | A new or restarted level's layout and initial state |
| `StateUpdated` | Current moving game state |
| `GameEvent` | Something happened, such as a pickup, damage or a sound cue |
| `Error` | An operation or session encountered an error |
| `Pong` | Response to a connection round-trip check |

The client renders these messages. Receiving a sound cue means the client can play an appropriate sound; the server itself does not play audio.

For a client without SignalR, `/messages?afterSeq=...` reads a bounded polling buffer. Save the last sequence received and send it on the next poll. `seq` orders messages; `tick` identifies simulation steps. One tick can produce several messages. By default, polling retains state updates every fifth tick, while event notifications are not deliberately downsampled. Old messages still fall out of the finite buffer.

`GET /state` reads the latest state directly. It supports JSON or XML through serializer adapters, selected with `?format=xml` or the `Accept` header. Polling can also serialize message bodies in either format. This does not mean every endpoint or the SignalR connection switches to XML.

**There is no `/api/patterns` API.** Changing zombie strategy through a development helper changes the actual game. Running the separate `strategy` console demo demonstrates its implementation for the course. Those are different uses of the same game code.

For exact request/response schemas, use Scalar or [FRONTEND_INTEGRATION](FRONTEND_INTEGRATION.md). For expected application errors, read [GameExceptionHandler](../src/CastleEscape.Server/Endpoints/GameExceptionHandler.cs), which translates them into HTTP problem responses.

## 9 Find and explain the twelve design patterns

The pattern names describe object relationships and responsibilities. A `[DesignPattern(...)]` attribute labels participants for course tooling; the attribute itself does not implement the pattern. Look at the interfaces, inheritance, composition and calls beneath the label.

### 1 Facade gives callers a small set of game operations

[GameFacade](../src/CastleEscape.Application/Sessions/GameFacade.cs) hides coordination between the registry, sessions, scheduler and content catalog. An endpoint calls `SubmitDirection` without navigating those objects itself. To recognize the pattern, compare that one public call with the authentication and delegation inside it.

### 2 Singleton shares the content catalog

[ContentCatalog](../src/CastleEscape.Infrastructure/Content/ContentCatalog.cs) has a private constructor and a static `Lazy<ContentCatalog>` behind `Instance`. Its normal runtime path loads and validates one shared catalog. This is the course's explicit Singleton implementation. DI hands out that same instance through `IContentCatalog`; other `AddSingleton` registrations are service lifetime choices.

### 3 Builder constructs a level in ordered steps

[ILevelBuilder](../src/CastleEscape.Domain/Generation/ILevelBuilder.cs) defines construction operations. `ProceduralLevelBuilder` and `PresetLevelBuilder` implement them differently. `LevelDirector` runs the same sequence against either builder and retrieves a `LevelState`. This separates the construction process from how each step is performed.

### 4 Abstract Factory creates a consistent theme family

[IThemeFactory](../src/CastleEscape.Domain/Generation/Themes/IThemeFactory.cs) creates related walls, water, pits and zombies. [DungeonThemeFactory](../src/CastleEscape.Domain/Generation/Themes/DungeonTheme.cs) and [CryptThemeFactory](../src/CastleEscape.Domain/Generation/Themes/CryptTheme.cs) provide different families. A builder receives the chosen factory and uses its products throughout a level. Look for the several related creation methods, rather than just one object being created.

### 5 Factory Method lets subclasses choose the pickup class

[ItemSpawner](../src/CastleEscape.Domain/Items/ItemSpawner.cs) performs common spawning checks, calls the abstract `CreateItem`, then registers the result in the level. `HealthItemSpawner`, `RewardItemSpawner` and `PowerItemSpawner` override that method to construct their respective item classes. The factory method is the overridden creation operation; `ItemSpawners.For` is the helper that chooses a creator.

### 6 Prototype gives restart an independent copy of the map

[LevelState](../src/CastleEscape.Domain/World/LevelState.cs) implements [ILevelPrototype](../src/CastleEscape.Domain/World/ILevelPrototype.cs). `GameWorld` clones its pristine level for play and restart. Read `DeepClone` to see which mutable parts are copied, then compare `ShallowClone`, which uses `MemberwiseClone` and shares nested references.

### 7 Strategy changes how zombies choose their next move

A zombie holds an [IZombieMovementStrategy](../src/CastleEscape.Domain/AI/IZombieMovementStrategy.cs). [Greedy, BFS, A star and predictive strategies](../src/CastleEscape.Domain/AI/ChaseStrategies.cs) all implement the same operation. `GameSimulation` calls `zombie.Strategy.NextStep` without implementing those algorithms itself. For example, greedy movement can get stuck behind a wall where a path-searching strategy can find a way around it.

### 8 Decorator combines temporary abilities

In [Abilities.cs](../src/CastleEscape.Domain/Powers/Abilities.cs), `CharacterAbilities` is the base `IAbilities` object. `JumpDecorator`, `SprintDecorator` and `SwimDecorator` wrap another `IAbilities` and add behavior. Combo decorators add further effects. [PowerManager](../src/CastleEscape.Domain/Powers/PowerManager.cs) rebuilds the chain when stacks, timers or combos change.

For example, a conceptual chain is `SprintDecorator(JumpDecorator(CharacterAbilities))`. The player can cross pits and move faster. Movement rules call the outer `IAbilities` without needing a separate player subclass for every power combination. The name Swimmer alone does not grant water access: base abilities block water and pits until an appropriate power permits entry.

### 9 Command makes actions executable and undoable objects

[IGameCommand](../src/CastleEscape.Domain/Commands/IGameCommand.cs) defines execution and undo. [Concrete commands](../src/CastleEscape.Domain/Commands/GameCommands.cs) store the information needed to perform actions and restore relevant previous state. [CommandProcessor](../src/CastleEscape.Domain/Commands/CommandProcessor.cs) queues, executes and records them.

This supports real gameplay: direction input waits for a tick, restart becomes an action object, and a conflicting player step can be undone. Undo is specific to each command; it is not a universal rewind of the entire simulation.

### 10 Observer lets several systems react to game events

[GameEventPublisher](../src/CastleEscape.Domain/Events/GameEventPublisher.cs) maintains `IGameEventObserver` subscribers. Application attaches [observers](../src/CastleEscape.Application/Events/Observers.cs) for client notifications, statistics, recent-event logging and sound cues. When an item is collected, one event can trigger all those reactions. Pickup rules do not need to know each reaction's implementation. Publication is synchronous in this process.

### 11 Bridge separates notification policy from delivery channel

[ClientNotifier](../src/CastleEscape.Application/Messaging/ClientNotifiers.cs) decides what to send and how frequently. Its `StateNotifier` and `EventNotifier` subclasses use an [IClientChannel](../src/CastleEscape.Application/Messaging/ClientChannels.cs) to perform delivery. The concrete channels are [SignalRClientChannel](../src/CastleEscape.Server/Realtime/SignalRClientChannel.cs) and [PollingBufferChannel](../src/CastleEscape.Infrastructure/Messaging/PollingBufferChannel.cs).

These are two independently variable dimensions: notification policy and transport. State can be sent every tick through SignalR and every fifth tick through polling while events use their own policy. That separation is the Bridge.

### 12 Adapter gives JSON and XML serialization one interface

[IMessageSerializer](../src/CastleEscape.Infrastructure/Serialization/IMessageSerializer.cs) presents one serialization API. [JsonMessageSerializerAdapter](../src/CastleEscape.Infrastructure/Serialization/JsonMessageSerializerAdapter.cs) wraps `System.Text.Json`; [XmlMessageSerializerAdapter](../src/CastleEscape.Infrastructure/Serialization/XmlMessageSerializerAdapter.cs) wraps `DataContractSerializer`. Their underlying library APIs differ, but `GameplayEndpoints` can use either through the same interface.

Bridge concerns which messages are delivered over which channel. Adapter concerns how different serialization libraries are called. They solve different parts of communication.

### Run the demonstrations after reading the game usage

From a terminal at the repository root:

```bash
dotnet run --project tools/CastleEscape.PatternDemos
dotnet run --project tools/CastleEscape.PatternDemos -- strategy
dotnet run --project tools/CastleEscape.PatternDemos -- prototype --mode Shallow
```

The console project references the real implementation and sets up small scenarios. Runtime projects do not reference the console project. It can therefore contain course explanations and intentionally problematic shallow-clone scenarios without becoming part of the client API. Each pattern has a longer explanation, diagram and tests linked from the [pattern index](patterns/README.md).

## 10 Read the C sharp conventions used here

These constructs explain much of the code's compact appearance:

| Construct | How to read it here |
|---|---|
| `public class GameFacade(SessionRegistry registry, ...)` | A primary constructor declares dependencies on the class declaration. Those parameters are used inside its methods. |
| `record DirectionRequest(Direction Direction)` | A compact data type with a generated constructor and property; commonly used for contracts. A record is not necessarily deeply immutable. |
| `IContentCatalog` | An interface defines operations available to callers. Use Go To Implementation to find concrete behavior. |
| `=>` on a method | A short method whose result is the expression on the right. Inside LINQ calls, `x => ...` defines a function applied to each element. |
| `this IServiceCollection services` | The first parameter of an extension method; it permits `services.AddCastleEscape(...)` syntax. |
| `is not { } player`, `is { IsMoving: false }` | C# pattern matching: check a value and possibly bind a variable or inspect a property. This language feature is unrelated to the twelve design patterns. |
| `LevelState?`, `level!` | `?` allows null. `!` suppresses a compiler nullability warning; it does not check or fix null at runtime. |
| `[]`, `[a, b]` | Collection expressions used to create collections. |
| `lock (_gate)` | Serializes code entering the same lock so shared state is not mutated concurrently by those paths. |
| `using Direction = CastleEscape.Domain.Direction;` | A type alias disambiguates the domain enum from the identically named wire enum. |

Domain and Contracts each define some enums, such as `Direction`. This keeps Domain independent of the wire model. Application maps between them; tests check that the names and numeric values used by casts remain aligned. When investigating one, check its namespace.

In Visual Studio, use **Go To Definition**, **Go To Implementation**, and **Find All References** from the context menu. Definition shows what a type promises, implementation shows how it works, and references show where the behavior is actually used. A folder named `Patterns` mostly contains course metadata; most pattern implementations live in the feature folders that need them.

## 11 Learn the code through debugging and tests

### First pass through the code

Open these in order and focus on the named parts:

| Order | File | What to understand before continuing |
|---|---|---|
| 1 | [Program.cs](../src/CastleEscape.Server/Program.cs) | Which endpoint groups and background services the host starts |
| 2 | [ServiceRegistration.cs](../src/CastleEscape.Server/Configuration/ServiceRegistration.cs) | How interfaces receive implementations |
| 3 | [SessionEndpoints.cs](../src/CastleEscape.Server/Endpoints/SessionEndpoints.cs) | How a URL reaches an application method |
| 4 | [GameFacade.cs](../src/CastleEscape.Application/Sessions/GameFacade.cs) | The operations available to callers |
| 5 | [GameSession.cs](../src/CastleEscape.Application/Sessions/GameSession.cs) | `SelectCharacter`, `Tick`, `TickPlaying`, and `LoadLevel` |
| 6 | [GameSimulation.cs](../src/CastleEscape.Domain/Sessions/GameSimulation.cs) | The order of the actual gameplay rules |
| 7 | [GameWorld.cs](../src/CastleEscape.Domain/Sessions/GameWorld.cs) | Which state those rules operate on |
| 8 | [SnapshotMapper.cs](../src/CastleEscape.Application/Snapshots/SnapshotMapper.cs) | How internal objects become client data |

After that, follow one feature into a smaller class such as `MovementRules`, `PowerManager` or `ExitMechanism`. Reading every builder and serializer first is unnecessary to understand a move.

### Trace a movement request with breakpoints

Use Scalar for this exercise so HTTP handler breakpoints are hit. The playground sends ordinary movement through SignalR, so its entry breakpoint is `GameHub.SetDirection` instead.

1. Create Ana with `POST /api/sessions`, then join Ben with `POST /api/sessions/join`. Keep both tokens.
2. Select `scout` for each player using `PUT /api/sessions/{sessionId}/players/me/character` and the corresponding `X-Player-Token`.
3. Wait until `GET /api/sessions/{sessionId}/state` reports `Playing`.
4. Set a breakpoint in `GameplayEndpoints.SubmitInput` and submit `{"direction":"Right"}`. Inspect `sessionId` and `request.Direction`, then step into the facade and session enqueue operation.
5. Set a breakpoint in `SetDirectionCommand.Execute`. Continue execution. It will be reached from the background tick, not as a continuation of the HTTP handler's call stack.
6. Inspect the player in `PlayerMovement.Move`: `HeldDirection`, `Tile`, `NextTile`, `Progress` and `Abilities`. A blocked target correctly results in no step.
7. Continue, send `{"direction":"None"}`, and inspect `/state` again.

Disable frequently hit tick breakpoints when finished. Inspect the Call Stack window to distinguish the request path from the timer path. If you cannot comfortably observe movement on a randomly generated map, use the fixed tutorial preset:

```bash
dotnet run --project src/CastleEscape.Server -- --Generation:PresetLevel=tutorial
```

For debugging that preset directly in Visual Studio, temporarily set `Generation:PresetLevel` to `tutorial` in the Development settings and restart. That setting makes the provider use the preset for every generated level; remove it to return to procedural generation.

### Tests let you advance time yourself

`CastleEscape.Core.Tests` covers game rules, generation, pattern behavior and architectural dependencies. `CastleEscape.Server.Tests` covers HTTP and SignalR integration, API schemas and host behavior.

Start with [GameRulesTests](../tests/CastleEscape.Core.Tests/Sessions/GameRulesTests.cs), especially `Movement_WallBlocks_Col1` and `Movement_ConstantSpeedGridSteps_Mov1`. They use a small map and a [TestGame](../tests/CastleEscape.Core.Tests/Sessions/TestGame.cs) helper. Its `Tick()` calls `Session.Tick(0.05)` directly, so time advances only when the test asks it to. Debugging one test is often easier than following a live timer.

In Test Explorer, locate one test and choose **Debug**. Set a breakpoint in the rule it exercises. A `[Fact]` is one test case; a `[Theory]` runs a test with supplied data cases. Test names ending in identifiers such as `MOV1` or `D7` refer to requirements and decisions documented in [USE_CASE_MAPPING](USE_CASE_MAPPING.md) and [DECISIONS](DECISIONS.md).

Useful next tests are [CommandTests](../tests/CastleEscape.Core.Tests/Patterns/CommandTests.cs) for tile conflicts, [DecoratorTests](../tests/CastleEscape.Core.Tests/Patterns/DecoratorTests.cs) for abilities, and [PrototypeTests](../tests/CastleEscape.Core.Tests/Patterns/PrototypeTests.cs) for cloning. Each connects a design-pattern concept to an observable result.

Run the whole suite from the repository root with:

```bash
dotnet test CastleEscape.sln
```

### Common sources of confusion

| Symptom | What to check |
|---|---|
| Server runs but no browser opens | Open the URL manually; `launchBrowser` is false. |
| Playground returns 404 | Check `DevTools:Enabled` and the selected environment. |
| State says no level is loaded | Both players must join and choose characters, then a tick must load the level. |
| An action returns 401 | Send the token for that player in that session, rather than the join code or player ID. |
| Direction request succeeds but movement is absent | The request only queues input; check the phase, next tick and movement rules. |
| `Connected` is false while HTTP actions work | That flag describes the hub connection. HTTP-only play does not require one. |
| Edits to content do not appear | Edit the source `content` directory, rebuild and restart the process to reload the singleton. |

## 12 Know where to make a change

Choose a location by responsibility, then follow its callers and tests:

| Intended change | Start here |
|---|---|
| Tune a character's speed or lives | `content/characters.json` |
| Tune an existing pickup or power modifier | `content/consumables.json` and `content/combos.json` |
| Change whether a player may enter a tile | Domain `World/MovementRules.cs` and the relevant ability decorator |
| Change pickup or damage effects | Domain `World/InteractionResolver.cs` and `Items/Items.cs` |
| Change lever or exit behavior | Domain `World/ExitMechanism.cs` |
| Add a zombie movement algorithm | Domain `AI`, the strategy selection helper, relevant enums/content and tests |
| Change how a level is assembled | Domain `Generation`, especially the builder and `LevelDirector` |
| Change lobby or level-transition behavior | Application `Sessions/GameSession.cs` |
| Add an operation a client can call | A Contracts request/response if needed, an Application operation, then a Server endpoint or hub method |
| Add information to game-state output | Contracts message/DTO and Application `Snapshots/SnapshotMapper.cs`; update the client that displays it |
| Change file parsing or add a content source | Infrastructure `Content`; keep game validation in Domain |
| Add a reaction to a game event | Application `Events/Observers.cs`, then attach the observer to the session |
| Add a delivery channel | Implement `IClientChannel` and register it in Server; reuse notification policies |
| Change the development browser | Server `wwwroot/playground` |
| Add a course demonstration | `tools/CastleEscape.PatternDemos/Demos` and its catalog |

For example, changing the Scout's base speed is a content edit. Making Sprint calculate speed differently is a domain behavior edit. Displaying that speed in a new client field crosses the DTO/mapping boundary. All three concern speed, but they belong in different places because they have different responsibilities.

When you add behavior, inspect a nearby test that already demonstrates the expected style. When you change an API shape, also check the frontend integration documentation and OpenAPI snapshot tests. The existing architecture tests protect project dependencies and keep the console demo project out of runtime references.

## 13 Know the current boundaries

The current game implements the two-player lobby, character selection, generated/preset levels, movement and collision rules, zombies, pickups, temporary and combined powers, cooperative doors/exits, restart, victory/defeat, state delivery and development helpers.

Keep these limits in mind when explaining it:

- Sessions and polling buffers are in memory. Restarting the host loses them. There is no database-backed save/load or account system; player tokens identify participants in a session.
- The included playground is a development client. A finished client would still need its own presentation, input handling, connection handling and user experience around the API.
- Twelve course patterns are implemented. Comments mentioning a “P2 seam” describe a place for future work. A phase switch is not already State, and a restart checkpoint is not already a complete Memento implementation. [P2_ROADMAP](P2_ROADMAP.md) describes the later work.
- Restart works by restoring the level-start state described above. Undoing a restart restores the previous level, lives, score and tiles, but does not restore active power timers. It is not a complete rewind.
- Some preview and diagnostic fields expose class or pattern-related descriptions for coursework inspection. The gameplay API no longer has separate pattern demonstrations or a clone-mode switch.

You are ready to navigate the project when you can explain where an input enters, why it waits for a tick, which class applies its rules, and how the resulting state gets back to the client. For a course explanation, add one concrete pattern along that path: Command for input, Decorator for movement abilities, Observer for reactions, or Bridge for delivery.
