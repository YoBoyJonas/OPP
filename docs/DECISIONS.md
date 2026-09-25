# Decisions

Where the requirements (`requirements/reikalavimai_zaidimui.txt`), the UML drafts
(`diagrams/`) and the build brief disagree or leave gaps, this file records what the server
does and why. Configurable decisions name their setting.

## Game rules

| ID | Decision | Source of conflict | Setting |
|---|---|---|---|
| D1 | **Door latches.** Once both levers are active in the same tick, the door opens and stays open for the rest of the level. | Requirements: "durys atviros tik kol abi svirtys aktyvios tuo pačiu kadru". Taken literally, the players must leave the levers to reach the exit, so no level can be won. The pull-lever diagram ("other player has opened lever → Open exit door") and the open-door diagram (a one-off "Update door state") already read as a latch. | `Game:DoorMode = Latch \| HoldWhileActive` (default `Latch`) |
| D2 | **Levers activate while a player stands on them.** There is no "pull" input. | Pull-lever diagram: "Presses button to pull lever". Requirements (collisions): "Ant svirties langelio svirtis aktyvi". | — |
| D3 | **Powers start on pickup.** "Use active power" is carried out automatically: tiles that need a power can be crossed while it is active. | Use-active-power diagram: "Presses activate power button". Requirements: "laikina galia… galioja fiksuotą laiką". The team should update the use case and activity diagrams. | — |
| D4 | **Three item kinds**: Power, Reward (score), Health (+1 life). `Reward` and `Consumable.ScoreValue` are added to the content model. | Requirements list 3 kinds; the class diagram and the collect-item diagram show only Health and Power. | — |
| D5 | **No soft-locks.** The critical path (start → both levers → door → exit) must be walkable without powers. Power obstacles guard shortcuts and reward pockets, and each level has at least one (GEN-3). | Powers expire, so a mandatory power gate can make a level unwinnable. | `Generation:RequirePowerOnCriticalPath` (default `false`) |
| D6 | **No player respawn on hit.** Only the zombie resets. | Lose-life diagram has "Respawn player"; the requirements only reset the zombie ("zombis iškart perkeliamas į savo starto langelį"). | `Game:RespawnPlayerOnHit` (default `false`) |
| D7 | Two players can't occupy the same tile, or step into the tile the other is on or stepping into. If both start a step into the same tile in the same tick, the step from the **later direction input** is undone (Command `Undo()`), so the earlier key press wins. | Not specified. Makes the Command pattern's `Undo()` part of normal play. | — |
| D8 | **Restart level**: either player may request it. The level goes back to its generated layout (a fresh deep copy of the pristine level: Prototype) and both players' lives, score and powers go back to their level-start values. The confirmation dialog is client-side. | Restart diagram shows the dialog in the "Castle escape" lane; the server can't show dialogs. | — |
| D9 | Score is per player; the HUD shows both players. | Not specified. | — |
| D10 | Defeat ends the session in a terminal `Defeat` phase; clients return to the menu. | Lose-game diagram: "Return to main menu", "Show you lose message". | — |
| D11 | **Levels start automatically**: when both players have chosen characters, and after each completed level. "Start level" is a server event (`LevelStarted`); the loading screen is client-side. A generation failure is retried with new seeds (`Generation:MaxAttempts`), then reported as an error. | Start-level diagram: "Player presses start level". Requirements: "Įveikus lygį automatiškai užkraunamas kitas". | — |
| D12 | `LevelDefinition.TimeLimitSeconds` is kept but not enforced. | In the class diagram, no requirement. | — |
| D14 | Health items restore lives **up to the character's maximum** (as the first prototype did). | Requirements say only "+1 gyvybė". An uncapped counter would make the character choice (PLR-1) meaningless after a few potions. | — |
| D15 | **Powers do not carry over between levels**: each level (and each restart) starts with no active powers. | Not specified. The checkpoint (D8) then only needs lives and score. A carried-over Swim would also let a player skip the next level's power obstacles. | — |
| D16 | **"Nearest player" means straight-line distance** from the zombie, re-checked on every tile (ZMB-1). A zombie may switch targets as it moves. | "mažiausio atstumo žaidėjo" does not say which distance. Straight-line is simplest and is the same for every strategy. Path distance is a possible Strategy variant. | — |
| D17 | A level completes when both players stand still on exit tiles; next comes a short `LevelComplete` pause (`Game:LevelTransitionSeconds`, default 2 s), then the next level loads automatically. | "Perėjimas įvyksta tik kai abu žaidėjai stovi ant išėjimo langelių" + clients need a moment to show "level complete". | `Game:LevelTransitionSeconds` |
| D13 | The zombie section is duplicated under "Žemėlapio ir objektų generavimas"; the duplicate is ignored. The collision section's "PRI-2 + PRI-3" refers to the zombie rules (brief ZMB-2, ZMB-3). | Requirements text. | — |

## Content model

| ID | Decision | Why |
|---|---|---|
| C1 | Class names follow the class diagram: `CharacterDefinition`, `ZombieDefinition`; the diagram's `Level` becomes `LevelDefinition`. The diagram's package `GameModels` becomes namespace `CastleEscape.Game.Content`. | The diagram's `ContentCatalog` already refers to `CharacterDefinition` and `ZombieDefinition`. Commit `4610da4` had renamed the classes but not their uses, which broke the build. `LevelDefinition` avoids a clash with the runtime `LevelState`. |
| C2 | The diagram's `L1..Ln` and `Z1..Zn` subclasses become **data instances** in `content/*.json`: 10 level definitions and 2–4 zombie types. | Levels and zombie types differ only in data and in their movement strategy. A subclass per level adds no behaviour, and adding a level would mean changing code. |
| C3 | `Consumable` (Health, Power, Reward), `Obstacle` (Wall, Water, Pit) and `PowerGrant` (Jump, Sprint, Swim) keep their subclasses from the diagram and load from JSON with a `type` discriminator (their read-only `kind`/`power` fields are still written on output). | The Abstract Factory products (e.g. `StoneWall : Wall`) extend these classes. |
| C4 | Level tables (`ObstacleTable`, `ZombieTable`, `ConsumableTable`) reference content by id with a spawn weight, instead of embedding copies. | `ContentCatalog.Validate()` already checks these tables by id. Embedded copies would duplicate content and drift. |
| C5 | `PowerCombo.RequiredPowers` becomes a list of powers. | One `PowerType` value can't express "Jump + Sprint". |
| C6 | **The team's characters are kept**: warrior (5 lives, 3.5 tiles/s), scout (3, 6.0), swimmer (4, 4.5). | PLR-1 check: ≥ 3 characters ✔. Lives differ (5/3/4) ✔. Base speed differs ✔. The jump parameter was missing from `Characters.cs`; `content/characters.json` gives each character a distinct `BaseJumpForce` (Phase 2), which completes PLR-1. |

| C7 | `content/obstacles.json` holds one generic `wall`, `water` and `pit`. Themed variants (e.g. Dungeon's murky water, Crypt's poison water) are **classes** made by the Abstract Factory in Phase 4, not extra JSON rows. | If the level tables named themed obstacles directly, nothing would be left for the factory to decide, and mixing families would only be caught by validation. |
| C8 | Optional content values fall back to config: `PowerGrant.DurationSeconds` (null → `Game:PowerDurationSeconds`) and `LevelDefinition.RoomSize` (null → `Game:GridWidth`/`GridHeight`). The shipped content leaves both unset, so the config settings from the brief work as the brief describes. | One source of truth per number, and content can still override per item or level. |
| C9 | **Jump parameter** (`BaseJumpForce × JumpForceMultiplier`) is the speed multiplier while crossing a pit with Jump active. | The requirements name a jump parameter but give it no meaning on a top-down grid. As a pit speed it has a visible effect and works with Jump Boots and JumpDash. |
| C10 | Added `Obstacle.MoveSpeedMultiplier` (water 0.6) and `ZombieDefinition.MovementStrategy`. | Water must slow swimmers (FastSwim removes the penalty), and each zombie type needs a default Strategy (ZMB-1). |

## Project and protocol

| ID | Decision | Why |
|---|---|---|
| P1 | `CasteEscapeServer` → `src/CastleEscape.Server` (`git mv`; fixes the typo). The console client's `Program.cs` is deleted; its models moved to `CastleEscape.Game/Content`. | Approved 2026-09-25. History is kept through the renames. |
| P2 | Classic `CastleEscape.sln`, not `.slnx`. | Works in every IDE version the team may use. |
| P3 | The hub moves from `/gamehub` to `/hubs/game`, and its methods change (see `FRONTEND_INTEGRATION.md`). No compatibility alias. | There is no frontend yet, and the old console client is removed. |
| P4 | CI builds the whole solution and runs `dotnet test` before publishing. | Approved 2026-09-25. The broken client build had gone unnoticed because CI built only the server. |
| P5 | `Microsoft.OpenApi` is pinned to 2.12.2. | GHSA-v5pm-xwqc-g5wc affects versions ≤ 2.7.4. `Microsoft.AspNetCore.OpenApi` 10 requires 2.x. |
| P6 | Scalar and the OpenAPI document are served in every environment, Azure included. The Dev endpoints and the playground need `DevTools:Enabled` (true only in `appsettings.Development.json`). | The API reference is useful at the defence; the dev shortcuts must not be exposed on a public deployment by default. |
| P7 | Options that the game rules read (`Game`, `Generation`, `Patterns`, `Realtime`) live in `CastleEscape.Game.Configuration`; host-only options (`Cors`, `DevTools`) live in the Server project. | The Game project must not depend on ASP.NET Core. |
| P8 | Tests use xUnit v3 with plain `Assert`. | Brief §10. No licensed assertion libraries. |
| P10 | **Threading**: each session has one lock. Only `Tick` changes the world. Hub/REST input is queued (sequence-numbered) and applied at the start of the next tick. Lobby calls (join, pick character, leave) take the lock briefly, because they must answer synchronously. REST reads use the immutable snapshot published after each tick and never take the lock. | Brief §4. The lobby exception keeps create/join responses immediate without a second queue. |
| P11 | `Generation:PresetLevel` (e.g. `tutorial`) makes every level a preset map. Used by integration tests, demos and the playground. | Deterministic multiplayer runs without changing code. |
| P12 | Wire DTOs in `CastleEscape.Contracts` use arrays and `Dictionary<,>` (not `IReadOnlyList`/`IReadOnlyDictionary`). Realtime and session messages carry `[DataContract]`/`[DataMember]`. | `DataContractSerializer` (XML adaptee, NET-2) can't construct positional records without them, and can't serialize interface-typed collections whose runtime type is compiler-generated. The JSON is unchanged. |
| P9 | Changes to Jonas's server code stay minimal until the rewrite list in `IMPLEMENTATION_PLAN.md` is confirmed. Phase 1 changed only namespaces and the hub path. | The team hasn't confirmed the rewrite scope yet (open question 3). |

## Course context notes

- "Simple graphics only (System.Drawing / swing level)" applies to the frontend. The dev
  playground is a debugging tool, not the graded client.
- "Game objects may be stored in a database" is optional; content stays in JSON behind
  `ContentCatalog` for now.
- "No pattern may be used twice in the team": each P1 pattern has exactly one owner and one
  location (see `patterns/README.md`, Phase 4).
