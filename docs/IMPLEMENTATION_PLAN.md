# Implementation plan

Branch `backend/p1` (from `main` @ `6e1eed7`). Never pushed by the agent. `dotnet build` +
`dotnet test` green at every commit. Target `net10.0` (already used; SDK 10.0.202).

## Final layout

```
CastleEscape.sln
src/
  CastleEscape.Contracts/     DTOs, enums (Direction, PowerType, SessionPhase…), hub messages
  CastleEscape.Game/          no ASP.NET reference
    Configuration/            Game/Generation/Patterns/Realtime options (validated at startup)
    Content/                  ContentCatalog (Singleton), *Definition, StatModifiers, JSON loading
    World/                    GridPos, Direction helpers, Grid, TerrainKind, Entity, PlayerEntity,
                              ZombieEntity, Lever, ExitDoor, LevelState, WorldView, MovementRules,
                              InteractionResolver, ExitMechanism, SnapshotMapper
    Generation/               ILevelBuilder, Procedural/Preset builders, LevelDirector,
                              LevelValidator, ILevelProvider;  Themes/ (Abstract Factory)
    Items/                    ItemEntity + products, ItemSpawner + creators (Factory Method)
    Powers/                   IAbilities, decorators, PowerManager
    AI/                       IZombieMovementStrategy + 4 strategies
    Commands/                 IGameCommand, commands, CommandProcessor
    Events/                   GameEvent records, GameEventPublisher, observers
    Sessions/                 GameSession, SessionRegistry, GameFacade, DevActions, LevelCheckpoint
    Messaging/                IMessageSerializer + adapters, ClientNotifier hierarchy,
                              IClientChannel, PollingBufferChannel
    Patterns/                 DesignPatternAttribute, PatternCatalog (reflection)
    PatternDemos/             IPatternDemo, DemoResult, one runner per pattern
  CastleEscape.Server/        (moved from CasteEscapeServer/) Program, Endpoints/*, Hubs/GameHub,
                              GameLoopService, SignalRClientChannel, OpenApi/ transformers,
                              Options/, wwwroot/playground
  CastleEscape.PatternDemos/  console Main()
tests/
  CastleEscape.Game.Tests/    xUnit
  CastleEscape.Server.Tests/  xUnit + WebApplicationFactory + SignalR client
content/                      characters, consumables, combos, obstacles, zombies, levels (.json),
                              presets/*.txt — copied to output of Game project
docs/
```

## Phases and commits

| # | Phase | Contents | Commit(s) |
|---|---|---|---|
| 0 ✅ | Audit | `REPO_AUDIT.md`, this file | `docs: repo audit and implementation plan` |
| 1 ✅ | Skeleton | `git mv` server → `src/CastleEscape.Server`, content models → `src/CastleEscape.Game/Content` (compile-fixed), screenshot → `docs/diagrams/`; `.sln`; empty projects + test projects; OpenAPI + Scalar + `x-tagGroups` transformer + `X-Player-Token` scheme; `/health`; CORS; camelCase + string enums for HTTP and SignalR; ProblemDetails; options classes with validation; workflow path fix; pin patched `Microsoft.OpenApi` | `chore: restructure into solution layout`, `feat(server): OpenAPI, Scalar, health, options` |
| 2 ✅ | Content & domain | JSON content (3 characters, 5 consumables incl. Reward, 3 powers, 2 combos, obstacles for 2 themes, 3 zombie types, 10 levels, 2 presets), loader, validation; `GridPos`, `Grid`, entities, `LevelState` | `feat(content)…`, `feat(world)…` |
| 3 | Naive prototype | lobby (create/join/select/leave), `SessionPhase` switch, `GameLoopService` (`PeriodicTimer`, 20 Hz, per-session exception isolation, snapshot swap), input queue, step movement, straight-line chase with a `switch` on zombie type, items via `switch`, powers as a plain dictionary + `if`s, levers/door latch/exit, lives, win/lose, restart by regenerating, hub + REST mirror, naive single-class generator + naive JSON-only state endpoint. Integration test: 2 SignalR clients finish a preset level | several `feat(game)…`; tag **`p1-prototype-before-patterns`** |
| 4 | Patterns | one commit each, in dependency order: Singleton → Adapter → Factory Method → Abstract Factory → Builder → Prototype → Strategy → Decorator → Command → Observer → Bridge → Facade. Each: refactor, `[DesignPattern]` tags, demo runner, unit test for the course requirement, `docs/patterns/<Name>.md` with before/after Mermaid | `feat(patterns): apply <X> to <Y>` ×12 |
| 5 | Endpoints polish | all §6 groups, examples, Realtime protocol + example endpoints, Dev endpoints via `DevActions`, `docs/openapi.json` export (`Microsoft.Extensions.ApiDescription.Server`, fallback script) | `feat(api)…` |
| 6 | Docs & playground | README, ARCHITECTURE, FRONTEND_INTEGRATION, DECISIONS, USE_CASE_MAPPING, P2_ROADMAP, DEFENCE_NOTES, patterns/README, per-package Mermaid; `wwwroot/playground`; run the server and play two tabs in the browser; final DoD checklist | `docs…`, `feat(dev): playground` |

Pattern order rationale: Singleton and Adapter have no dependencies; the generation trio
(Factory Method → Abstract Factory → Builder) builds bottom-up; Prototype needs a finished
`LevelState`; Command needs the tick; Observer before Bridge, since Bridge consumes the
notifier observer; Facade last because it wraps the final subsystems.

## Notes on the plan

- The naive prototype is deliberately "clean but pattern-free" (switches, a dictionary of power
  timers, one generator class, one JSON serializer call), so each pattern commit has a clear before/after.
- `ContentCatalog` has no Singleton in Phase 2–3: it is registered in DI from a loader, and
  Phase 4 turns it into a `Lazy<T>` Singleton.
- Package APIs (Scalar 2.x, Microsoft.OpenApi 2.x extension types, `AddOpenApi` transformers) will be
  checked against the installed versions via context7 and compilation, not memory.
- Decisions D1–D10 go to `docs/DECISIONS.md` in Phase 1, plus these repo-specific ones: team
  characters kept, level/zombie data instances instead of `L1..Ln` / `Z1..Zn` subclasses,
  `{id, weight}` table entries, `PowerCombo.RequiredPowers` as a list, hub path change.

## Answers (2026-09-25)

1. Approved: move to `src/CastleEscape.Server`; CI builds the whole `.sln` and runs `dotnet test`. Done in Phase 1.
2. Approved: console `Program.cs` deleted; models moved to `Game/Content`. Done in Phase 1.
3. **Still open.** The reply contained both template options ("Team agreed" / "keep Jonas's
   structure and list what you'd rewrite first"). Phase 1 follows the conservative reading:
   Jonas's server files changed only in namespace and hub path. The rewrite list is below.
   **Confirm it before Phase 3.**
4. The inputs were in `~/Downloads`, not yet in the repo. I copied them into `docs/` (commit `810cb50`).
5. **Still open** (both template options were left in). Both options switch to `/hubs/game`, so
   the switch is done, and the migration is documented in `FRONTEND_INTEGRATION.md` either way.

## Changes to Jonas's server code, in order

| When | File | Change | Kept |
|---|---|---|---|
| Phase 1 ✅ | all | namespace `CasteEscapeServer` → `CastleEscape.Server` | everything else |
| Phase 1 ✅ | `Program.cs` | adds OpenAPI, Scalar, CORS, options; hub path `/hubs/game` | `SessionManager` registration, `PORT` handling |
| Phase 2 ✅ | `Models/Direction`, `PowerType` | moved to `CastleEscape.Contracts` (global using, so his files are unchanged) | values |
| Phase 2 ✅ / 3 | `Game/Characters.cs` | data is in `content/characters.json` (Phase 2); the class goes with `SessionManager` in Phase 3 | ids and numbers (warrior/scout/swimmer) |
| Phase 3 | `Game/SessionManager.cs` | replaced by `SessionRegistry`: create/join by code instead of auto-match | lock-per-registry idea |
| Phase 3 | `Hubs/GameHub.cs` | `Hub<IGameClient>`; lobby moves to REST; `SendInput` → `SetDirection`; connects with session id + token | disconnect handling (becomes `Aborted` + notification) |
| Phase 3 | `Game/GameSession.cs` | biggest change. The `Timer` is replaced by the shared `GameLoopService`, input goes through a queue, continuous movement becomes tile steps, and zombies path around walls. The door latches, and the session gets phases, restart and level-complete handling | method names and tick structure (`MovePlayers`, `MoveZombies`, `UpdateLevers`, `CollectItems`, `TickPowers`, `AdvanceLevel`) so the "before" UML stays recognisable |
| Phase 3 | `Game/LevelBuilder.cs` | becomes the naive single-class `LevelGenerator` (random but validated) plus a preset loader; Phase 4 splits it into the Builder pattern | layout idea: border walls, obstacle band with a guaranteed gap |
| Phase 3 | `Models/*State`, `GameStateSnapshot`, `JoinResult`, `PlayerInputMessage` | runtime state → `Game/World` entities; wire DTOs → `Contracts` records | field names where they still fit |

## Open questions (asked at Phase 0)

1. **Project move + CI.** OK to `git mv CasteEscapeServer → src/CastleEscape.Server` and update
   the Azure workflow's two paths on this branch? (Deploy only happens after you merge to `main`.)
2. **`CastleEscapeClient`.** Its `Models/` become the Game project's `Content/` (moved, compile-fixed).
   For its `Program.cs` console client, options are:
   (a) delete it, since the playground and integration tests replace it (recommended);
   (b) keep it and update it to the new protocol;
   (c) leave it untouched and broken.
3. **Teammate's code.** Everything in the repo is Jonas's work, and this plan rewrites most of
   the server and moves his models. Has the team agreed, or should changes to his files stay
   minimal?
4. **Missing inputs.** Requirements txt, course_context.md and the use case and activity diagrams
   aren't in the repo. Will you add them, or should I go on the brief alone?
5. **Protocol break.** Is anyone building a frontend against `/gamehub` + `GameStateUpdate`
   right now? If so, I can keep a thin compatibility alias for a while. Otherwise it's a clean switch to `/hubs/game`.
