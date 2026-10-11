# Castle Escape game backend

A two-player cooperative dungeon escape for KTU T120B516 Object-Oriented Design. The server owns the game simulation; clients send input and render state. Twelve P1 design patterns support gameplay and communication. Course demonstrations run in a separate console tool.

## Run in Visual Studio

Open `CastleEscape.sln`, set **CastleEscape.Server** as the startup project and select the `http` profile. Requires the .NET 10 SDK. Runtime projects are grouped under `src`, demonstrations under `tools`, tests under `tests`, and navigation documents under `Documentation`.

| Address | Purpose |
|---|---|
| `http://localhost:5035/scalar` | Browse and call the gameplay API |
| `http://localhost:5035/playground` | Development browser client; quickstart opens a two-player game |
| `http://localhost:5035/openapi/v1.json` | HTTP API specification |
| `/hubs/game` | SignalR hub for input and server updates |

From a terminal at the repository root:

```bash
dotnet run --project src/CastleEscape.Server
dotnet test CastleEscape.sln
dotnet run --project tools/CastleEscape.PatternDemos
dotnet run --project tools/CastleEscape.PatternDemos -- prototype --mode Shallow
```

## Solution layout

```text
src/
  CastleEscape.Domain/          Game rules, entities, simulation and generation
  CastleEscape.Application/     Session operations, orchestration and DTO mapping
  CastleEscape.Infrastructure/  Files, in-memory storage and serializer adapters
  CastleEscape.Contracts/       Public requests, responses and realtime messages
  CastleEscape.Server/          ASP.NET Core, REST, SignalR and dependency registration
tools/
  CastleEscape.PatternDemos/    Console demonstrations and course catalog
tests/
  CastleEscape.Core.Tests/      Rules, generation, layers and pattern demonstrations
  CastleEscape.Server.Tests/    HTTP, SignalR and OpenAPI integration
content/                       Static JSON definitions and preset maps
docs/                          Navigation, API and course documentation
```

New to the codebase? Start with [Understanding Castle Escape](docs/PROJECT_GUIDE.md), a guided explanation of the projects, game flow, patterns and debugging. [CODE_WALKTHROUGH](docs/CODE_WALKTHROUGH.md) is the shorter navigation reference. For dependencies and placement decisions, read [ARCHITECTURE](docs/ARCHITECTURE.md). The old `CastleEscape.Game` project has been replaced by the three core layers. They are libraries in the same backend process, not separate services.

## Gameplay

Both players join a session and choose Warrior, Scout or Swimmer. The next tick starts level one. Zombies chase players; items restore lives, award points or grant temporary powers. Jump crosses pits, Swim crosses water, and Sprint increases speed. Powers stack and combine. Both players activate levers together, then reach exit tiles together. Clearing ten levels wins; either player losing all lives ends the game.

The server runs at 20 ticks per second by default. REST handles the lobby; SignalR carries input and updates, with REST input/polling as an alternative. Sessions are in memory. The included playground is a development client, not a separate finished frontend.

## Design patterns

| Layer | Patterns |
|---|---|
| Domain | Factory Method, Abstract Factory, Builder, Prototype, Strategy, Decorator, Command, Observer publisher |
| Application | Facade, Observer reactions, Bridge notification policies and channel interface |
| Infrastructure | Singleton content catalog, Adapter serializers, Bridge polling channel |
| Server | Bridge SignalR channel |

[Pattern documentation](docs/patterns/README.md) links each implementation, diagram and console demonstration. The server exposes no pattern-demo API. Deep/shallow comparison belongs in the console tool; live sessions use deep cloning.

## Documentation

| Document | Purpose |
|---|---|
| [PROJECT_GUIDE](docs/PROJECT_GUIDE.md) | Start here: beginner walkthrough of the structure, implementation, twelve patterns and Visual Studio debugging |
| [CODE_WALKTHROUGH](docs/CODE_WALKTHROUGH.md) | Reading order, request trace and manual gameplay recipe |
| [ARCHITECTURE](docs/ARCHITECTURE.md) | Layer boundaries, tick loop and implementation placement |
| [FRONTEND_INTEGRATION](docs/FRONTEND_INTEGRATION.md) | Client calls, messages, input and rendering |
| [USE_CASE_MAPPING](docs/USE_CASE_MAPPING.md) | Requirements mapped to code and tests |
| [DEFENCE_NOTES](docs/DEFENCE_NOTES.md) | Course demonstrations and live-change preparation |
| [DECISIONS](docs/DECISIONS.md) | Game-rule interpretations |
| [P2_ROADMAP](docs/P2_ROADMAP.md) | Placement plan for the remaining eleven patterns |
| [BACKEND_AUDIT](docs/BACKEND_AUDIT.md) | Historical audit behind the structural correction and known limitations |

## Configuration

Settings are in `src/CastleEscape.Server/appsettings.json` and validated at startup. Development overrides enable the playground and helper endpoints.

| Setting | Default | Meaning |
|---|---|---|
| `Game:TickRate` | 20 | Simulation steps per second |
| `Game:PowerDurationSeconds` | 10 | Default power duration |
| `Game:DoorMode` | `Latch` | Door stays open after simultaneous lever activation |
| `Game:MaxLevel` | 10 | Levels per run |
| `Generation:PresetLevel` | unset | Optional `tutorial` or `arena` map |
| `Realtime:EnabledChannels` | SignalR, Polling | Enabled delivery channels |
| `DevTools:Enabled` | false; true in Development | Gameplay development shortcuts and playground |
| `Cors:AllowedOrigins` | Local development origins | Browser clients allowed to call the host |

CI builds and tests the solution, then publishes `CastleEscape.Server` to Azure on pushes to `main`.
