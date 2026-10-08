# Castle Escape — game server

A two-player co-op dungeon escape, built for KTU *T120B516 Object-Oriented Design* (fall 2026).
This repository holds the **authoritative server**: it simulates the whole game at 20 ticks per
second. Clients only send input and draw what the server tells them. Lobby, inspection and dev
tools are REST (with an OpenAPI/Scalar reference); play is real time over SignalR.

All 12 P1 design patterns are applied to real game problems, each with a runnable demo, a test
proving the course requirement, and a write-up with before/after diagrams ([docs/patterns](docs/patterns/README.md)).

## Quick start

Requires the .NET 10 SDK.

```bash
dotnet run --project src/CastleEscape.Server        # http://localhost:5035
```

| Open | What |
|---|---|
| http://localhost:5035/scalar | API reference; try every endpoint (Course → Patterns runs the pattern demos) |
| http://localhost:5035/playground | Browser playground: **Quickstart** gives two links, open each in its own tab and play with arrows/WASD (Development only) |
| http://localhost:5035/openapi/v1.json | OpenAPI document (also exported to [docs/openapi.json](docs/openapi.json)) |
| ws://localhost:5035/hubs/game | SignalR hub, see [FRONTEND_INTEGRATION](docs/FRONTEND_INTEGRATION.md) |

```bash
dotnet test                                                       # 215 tests: rules, generation, patterns, REST, SignalR
dotnet run --project src/CastleEscape.PatternDemos                # all 12 pattern demos in the console
dotnet run --project src/CastleEscape.PatternDemos -- prototype --mode Shallow
```

## The game in one paragraph

Ten levels on a ≥20×15 tile grid (Dungeon 1–5, Crypt 6–10). Two players each pick a character
(warrior 5 lives, scout 3 lives but fast, swimmer 4 lives, good in water). Each player must stand
on a lever at the same time to open the exit door, then both must stand on exit tiles. Zombies chase the
nearest player and cost a life on contact. Items give lives, score or temporary powers (Jump for
pits, Swim for water, Sprint). Two different powers together make a combo (JumpDash, FastSwim), and
the same power stacks. Any player losing all lives ends the game; clearing level 10 wins it.
Rule interpretations are recorded in [DECISIONS.md](docs/DECISIONS.md).

## Repository layout

```
src/
  CastleEscape.Contracts/     DTOs and message records shared with clients (no logic)
  CastleEscape.Game/          the game: content, world, rules, sessions, all 12 patterns, demos
  CastleEscape.Server/        ASP.NET Core host: REST endpoints, SignalR hub, game loop, playground
  CastleEscape.PatternDemos/  console runner for the pattern demos
tests/
  CastleEscape.Game.Tests/    rules, generation, and one test class per pattern
  CastleEscape.Server.Tests/  REST, SignalR (two real hub clients), polling, OpenAPI export
content/                      game content as JSON (characters, items, combos, obstacles, zombies, levels) + preset maps
docs/                         design documents (below)
```

## Design patterns (P1)

| Pattern | Where | Owner |
|---|---|---|
| [Singleton](docs/patterns/Singleton.md) | `ContentCatalog`: content loaded once, thread-safe | C |
| [Adapter](docs/patterns/Adapter.md) | `IMessageSerializer` over `JsonSerializer` / `DataContractSerializer` (JSON and XML) | C |
| [Bridge](docs/patterns/Bridge.md) | state/event notifiers × SignalR/polling channels | C |
| [Factory Method](docs/patterns/FactoryMethod.md) | `ItemSpawner.CreateItem` → Health/Reward/Power items | B |
| [Strategy](docs/patterns/Strategy.md) | zombie chase: Greedy, BFS, A*, Predictive | B |
| [Decorator](docs/patterns/Decorator.md) | power stacking: `IAbilities` wrapped by Jump/Sprint/Swim/combo decorators | B |
| [Abstract Factory](docs/patterns/AbstractFactory.md) | level themes: Dungeon/Crypt walls, water, pits, zombies | A |
| [Builder](docs/patterns/Builder.md) | `LevelDirector` + procedural and preset level builders | A |
| [Prototype](docs/patterns/Prototype.md) | restart = deep (or shallow, switchable) clone of the pristine level | A |
| [Command](docs/patterns/Command.md) | every input and step is an undoable command | D |
| [Observer](docs/patterns/Observer.md) | game events → client messages, HUD stats, log, sound cues | D |
| [Facade](docs/patterns/Facade.md) | `GameFacade`, the single entry point for hub, REST and loop | D |

Every participant carries `[DesignPattern("…", "role")]`; `GET /api/patterns` lists them from the code.
The tag `p1-prototype-before-patterns` marks the working game before any pattern was applied.

## Documentation

| Document | For |
|---|---|
| [CODE_WALKTHROUGH](docs/CODE_WALKTHROUGH.md) | **start here to read the code**: reading order, what each object means, one request traced end to end |
| [ARCHITECTURE](docs/ARCHITECTURE.md) | projects, the tick loop, threading, where each pattern sits |
| [FRONTEND_INTEGRATION](docs/FRONTEND_INTEGRATION.md) | building a client: lobby calls, hub connection, messages, input, rendering |
| [USE_CASE_MAPPING](docs/USE_CASE_MAPPING.md) | each use case and requirement → code, endpoint and test |
| [DECISIONS](docs/DECISIONS.md) | how ambiguous or conflicting requirements were interpreted |
| [DEFENCE_NOTES](docs/DEFENCE_NOTES.md) | demo script, switches, comparison answers, likely live changes |
| [P2_ROADMAP](docs/P2_ROADMAP.md) | where the 11 P2 patterns plug in (the seams are already there) |
| [IMPLEMENTATION_PLAN](docs/IMPLEMENTATION_PLAN.md), [REPO_AUDIT](docs/REPO_AUDIT.md) | how the work was planned and what was there before |

## Configuration

Settings live in `src/CastleEscape.Server/appsettings.json` and are validated at startup.

| Setting | Default | Meaning |
|---|---|---|
| `Game:TickRate` | 20 | simulation ticks per second |
| `Game:PowerDurationSeconds` | 10 | power duration when the item doesn't set one |
| `Game:DoorMode` | `Latch` | `Latch` (door stays open) or `HoldWhileActive` (D1) |
| `Game:MaxLevel` | 10 | levels in a run |
| `Generation:PresetLevel` | — | play a preset map (`tutorial`, `arena`) on every level |
| `Patterns:PrototypeCloneMode` | `Deep` | `Shallow` shows the shallow-copy restart bug (also switchable at runtime in dev tools) |
| `Realtime:EnabledChannels` | `SignalR, Polling` | delivery channels (Bridge) |
| `DevTools:Enabled` | `true` in Development only | `/api/dev/*` and `/playground` |
| `Cors:AllowedOrigins` | localhost dev ports | browser origins allowed to call the API and hub |

## Deployment

CI (`.github/workflows/main_castleescapeserver.yml`) builds the solution, runs the tests, and
publishes `src/CastleEscape.Server` to Azure on pushes to `main`. Dev tools are off outside
Development.
