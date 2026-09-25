# Repository audit (Phase 0)

Date: 2026-09-25 · Branch audited: `main` @ `6e1eed7` (identical to `origin/dev`)
SDKs installed: `10.0.202` only · BMAD: not installed in this repo (no `_bmad/`).

## 1. What exists

```
.github/workflows/main_castleescapeserver.yml   Azure App Service build+deploy on push to main
CasteEscapeServer/        (sic — "Caste") ASP.NET Core web app, net10.0
  Program.cs              AddSignalR, SessionManager singleton, PORT env var, MapHub("/gamehub")
  Hubs/GameHub.cs         untyped Hub: JoinSession(name, characterId), SendInput(PlayerInputMessage),
                          OnDisconnected → "OpponentLeft"
  Game/GameSession.cs     whole simulation in one class: System.Threading.Timer @ 0.15 s, lock,
                          continuous float movement, zombies, levers, items, powers, level advance
  Game/LevelBuilder.cs    static, fixed layout 22×16 (border walls, one water/pit band with a gap,
                          2 levers, 2 exit cells, N zombies = level index, items on one row)
  Game/SessionManager.cs  auto-matchmaking: first joiner waits, second joiner starts the session
  Game/Characters.cs      hard-coded warrior(5 hp, 3.5 t/s) / scout(3, 6.0) / swimmer(4, 4.5)
  Models/*.cs             CellType, Direction, PowerType, ItemKind(Power, Health), ItemState,
                          LeverState, ZombieState, PlayerState, LevelState, GameStateSnapshot,
                          JoinResult, PlayerInputMessage — mutable POCOs used both as runtime
                          state and as wire DTOs
  CasteEscapeServer.http  template leftover (/weatherforecast)
CastleEscapeClient/       console app, net10.0, Microsoft.AspNetCore.SignalR.Client 10.0.12
  Program.cs              keyboard → SendInput, prints GameStateUpdate JSON
  Models/*.cs             the team's content model from classDiagram.png: ContentCatalog,
                          Character, Consumable(+Health, Power), PowerGrant(+Jump, Sprint, Swim),
                          PowerCombo, Obstacle(+Wall, Water, Pit), Zombie, Level, GridSize,
                          StatModifiers, ConsumableKind, ObstacleKind, PowerType
Screenshot 2026-09-24 194854.png   = the class diagram (content model)
.DS_Store                           macOS junk, tracked
```

No `.sln`/`.slnx`, no tests, no `docs/`, no `content/` JSON, no frontend folder.
All commits are by one teammate (Jonas Armašauskas), 2026-09-14 … 09-24.

## 2. Build result

| Project | Result |
|---|---|
| `CasteEscapeServer` | ✅ builds. ⚠ `NU1903`: transitive `Microsoft.OpenApi 2.0.0` has a high-severity advisory (GHSA-v5pm-xwqc-g5wc) → pin a patched 2.x directly. |
| `CastleEscapeClient` | ❌ **3 errors**: `ContentCatalog`/`Level` reference `CharacterDefinition` and `ZombieDefinition`, but commit `4610da4` renamed the files *and classes* to `Character` / `Zombie`. |

The CI workflow only builds the server project, so CI is green despite the broken client.

## 3. Missing inputs (§2 of the brief)

- `docs/requirements/reikalavimai_zaidimui.txt` — **missing**
- `docs/requirements/course_context.md` — **missing**
- `docs/diagrams/*.png` — **missing**; only the class diagram exists (as the root screenshot).
  No use case or activity diagrams in the repo.

I will proceed on the brief's §3 rules unless these arrive; if they do, I diff them against §3
into `DECISIONS.md`.

## 4. Gaps vs. the game rules in the existing server

| Rule | Existing behaviour |
|---|---|
| LVL-2 | 22×16 fixed (≥ 20×15 OK, not 24×16 / configurable) |
| GEN-1..3 | no generation, one fixed layout per parity; no validation |
| MOV-1 | continuous float movement, rounding to tiles; can slide diagonally into walls between ticks |
| MOV-3 / D7 | players can overlap |
| ZMB-1 | zombies move in a straight line **through walls** |
| ZMB-4 | ✅ count = level index |
| DOOR-1 / D1 | door recomputed every tick (hold-while-active only) → levels are effectively unwinnable, exactly the problem D1 describes. There is no door tile; exit is just 2 cells |
| ITM-1 | no Reward item (`ScoreValue` exists but is never granted) |
| PWR-2/3 | no combos, re-pickup just resets the timer, Sprint has no effect |
| PLR-1 | character chosen on join, no select phase |
| NET-3 | auto-matchmaking only; no create/join by code, no REST |
| Restart | not implemented |
| Threading | `Timer` callbacks + lock; hub input mutates player state directly under the lock (fine for now, but no deterministic command point) |
| Session ended | `_over` stops the timer; no phases, no Victory/Defeat messages beyond flags |

## 5. What is reusable, and where it goes

| Existing | Planned (§4/§5) | Action |
|---|---|---|
| `CasteEscapeServer/` project | `src/CastleEscape.Server/` | `git mv` (keeps history, fixes "Caste" typo); Program rewritten with OpenAPI/Scalar/CORS/options; keep the `PORT` env handling (Azure) |
| `Hubs/GameHub.cs` | `Server/Hubs/GameHub.cs` : `Hub<IGameClient>` | rewrite; `JoinSession` → REST lobby; `SendInput` → `SetDirection`; path `/gamehub` → `/hubs/game` |
| `Game/GameSession.cs` | `Game/Sessions/GameSession.cs` + `World/*`, `AI/*`, hosted `GameLoopService` | split; keep method names where they still fit (`MovePlayers`, `MoveZombies`, `UpdateLevers`, `CollectItems`, `TickPowers`, `AdvanceLevel`) so the "before" is recognisable |
| `Game/LevelBuilder.cs` | Phase 3 naive `LevelGenerator` → Phase 4 `ProceduralLevelBuilder` | reuse its layout idea (border + obstacle band with a gap) as the naive generator; rename class to avoid clashing with the Builder pattern name |
| `Game/SessionManager.cs` | `Game/Sessions/SessionRegistry` | rewrite (create/join by code instead of auto-match) |
| `Game/Characters.cs` | `content/characters.json` | **keep the team's characters** (warrior/scout/swimmer + their numbers) instead of the brief's Knight/Ranger/Rogue example |
| `Models/Direction`, `PowerType` | `CastleEscape.Contracts` | move |
| `Models/CellType` | `Game/World/TerrainKind` (+ `Floor, Wall, Water, Pit, Door, Exit`) | rename/extend |
| `Models/*State` | `Game/World` entities (`PlayerEntity`, `ZombieEntity`, `ItemEntity`, `Lever`, `ExitDoor`) | replace |
| `Models/GameStateSnapshot`, `JoinResult`, `PlayerInputMessage` | `Contracts` records (`TickStateMessage`, `JoinSessionResponse`, …) | replace |
| `CastleEscapeClient/Models/*` | `src/CastleEscape.Game/Content/*` | `git mv` + fix: `Character`→`CharacterDefinition`, `Zombie`→`ZombieDefinition`, `Level`→`LevelDefinition` (names the diagram's catalog uses); add `Reward`, `ScoreValue`, `Theme`, `MovementStrategy`; JSON polymorphism for `Consumable`/`Obstacle`/`PowerGrant` subclasses; level tables become `{id, weight}` references (the existing `Validate()` already checks them by id) |
| `ContentCatalog.Validate/ValidateOrThrow/ComputeContentHash` | same | keep, extend, becomes the Singleton |
| `PowerCombo.RequiredPowers : PowerType` | `IReadOnlyList<PowerType>` | a single enum value can't express "Jump+Sprint" |
| `CastleEscapeClient/Program.cs` | — | see open question Q2 |
| `CasteEscapeServer.http` | `src/CastleEscape.Server/CastleEscape.http` | rewrite with real requests |
| `Screenshot 2026-09-24 194854.png` | `docs/diagrams/classDiagram.png` | `git mv` |
| `.DS_Store` | — | delete, add to `.gitignore` |
| `.github/workflows/main_castleescapeserver.yml` | same file | update the two project paths (see Q1) |

## 6. Risks

- **Auto-deploy**: every push to `main` deploys the server to Azure (`CastleEscapeServer` app).
  Moving the project without updating the workflow breaks the deploy once merged.
- **Frontend contract**: hub path, method names and payload shape all change. Only the console
  client consumes them today, but any frontend work started against `/gamehub` +
  `GameStateUpdate` will need to move to the new protocol (`docs/FRONTEND_INTEGRATION.md`).
- **Teammate ownership**: all current code is Jonas's. The restructure moves/rewrites most of it.
