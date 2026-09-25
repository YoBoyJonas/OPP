# Implementation plan

Branch `backend/p1` (from `main` @ `6e1eed7`). Never pushed by the agent. `dotnet build` +
`dotnet test` green at every commit. Target `net10.0` (already used; SDK 10.0.202).

## Final layout

```
CastleEscape.slnx
src/
  CastleEscape.Contracts/     DTOs, enums (Direction, PowerType, SessionPhase…), hub messages
  CastleEscape.Game/          no ASP.NET reference
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
| 0 | Audit | `REPO_AUDIT.md`, this file | `docs: repo audit and implementation plan` |
| 1 | Skeleton | `git mv` server → `src/CastleEscape.Server`, content models → `src/CastleEscape.Game/Content` (compile-fixed), screenshot → `docs/diagrams/`; slnx; empty projects + test projects; OpenAPI + Scalar + `x-tagGroups` transformer + `X-Player-Token` scheme; `/health`; CORS; camelCase + string enums for HTTP and SignalR; ProblemDetails; options classes with validation; workflow path fix; pin patched `Microsoft.OpenApi` | `chore: restructure into solution layout`, `feat(server): OpenAPI, Scalar, health, options` |
| 2 | Content & domain | JSON content (3 characters, 5 consumables incl. Reward, 3 powers, 2 combos, obstacles for 2 themes, 3 zombie types, 10 levels, 2 presets), loader, validation; `GridPos`, `Grid`, entities, `LevelState` | `feat(content)…`, `feat(world)…` |
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

## Open questions (need your answer before Phase 1)

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
