# P2 roadmap — where the 11 remaining patterns plug in

P1 (due 2026-11-10) applies the **12 patterns** listed in [patterns/README.md](patterns/README.md).
P2 (due 2026-12-16) adds **11 more**, for all 23 GoF patterns across the team.

This document is a placement plan, not an implementation. Nothing here is built yet. Its
purpose is to show that the P1 code already leaves one obvious place for each P2 pattern,
so P2 is an insertion rather than a rewrite.

P2 grading also requires the game to **actually work in multiplayer mode**.

## Placement

| Pattern | Course requirement | Seam in the P1 code |
|---|---|---|
| Template Method | ≥2 concrete classes, marked `sealed` | [`ILevelBuilder`](../src/CastleEscape.Game/Generation/ILevelBuilder.cs): [`LevelDirector`](../src/CastleEscape.Game/Generation/LevelDirector.cs) already runs the same ordered steps on [`ProceduralLevelBuilder`](../src/CastleEscape.Game/Generation/ProceduralLevelBuilder.cs) and [`PresetLevelBuilder`](../src/CastleEscape.Game/Generation/PresetLevelBuilder.cs). Lift the fixed order into a base class; the two builders become the sealed concretes. |
| Iterator | iterate ≥3 data structures through one interface | [`Grid`](../src/CastleEscape.Game/World/Grid.cs) (2D tile array), the item/zombie/lever collections on [`LevelState`](../src/CastleEscape.Game/World/LevelState.cs), and the session outbox — three different structures, one walk interface. |
| Composite | switch transparency ↔ safety at the defence; vs Decorator | Group tiles/entities into composite map regions (rooms, corridors) over `Grid`/`Tile`, so a region and a single tile answer the same interface. Contrast with the live Decorator on powers. |
| Flyweight | measure performance and memory | [`Tile`](../src/CastleEscape.Game/World/Tile.cs): terrain and obstacle data repeat across thousands of tiles. Share immutable tile descriptors from [`ContentCatalog`](../src/CastleEscape.Game/Content/ContentCatalog.cs) and measure before/after. |
| State | state diagram ≥4 states; vs Strategy | The phase `switch` in [`GameSession.Tick`](../src/CastleEscape.Game/Sessions/GameSession.cs) (`WaitingForPlayers`, `CharacterSelect`, `LoadingLevel`, `Playing`, …) — already marked in the code as the P2 State seam. |
| Proxy | switch protection / added-functionality / lazy-creation; report performance and memory | [`ILevelProvider`](../src/CastleEscape.Game/Generation/ILevelProvider.cs): one interface in front of level creation, so a caching (lazy), logging (added-functionality) or token-checking (protection) proxy drops in. |
| Chain of Responsibility | chain of ≥4 handlers | [`MovementRules`](../src/CastleEscape.Game/World/MovementRules.cs) and [`InteractionResolver`](../src/CastleEscape.Game/World/InteractionResolver.cs): the per-tick checks (bounds → terrain → obstacle → ability → entity contact) become ordered handlers. |
| Interpreter | used for commands typed in a console | The dev console: parse a typed line (`move right 2`, `give sprint`) into the existing [`IGameCommand`](../src/CastleEscape.Game/Commands/IGameCommand.cs) objects the P1 Command pattern already executes and undoes. |
| Mediator | mediates ≥3 classes; vs Observer | [`ExitMechanism`](../src/CastleEscape.Game/World/ExitMechanism.cs) already coordinates levers, door and exit. Promote it to a mediator between levers, door, players and the exit, and contrast with the broadcast Observer. |
| Memento | other classes cannot access the saved state | [`GameWorld`](../src/CastleEscape.Game/Sessions/GameWorld.cs) keeps `Pristine` and a `LevelCheckpoint`. Make the checkpoint an opaque memento that only `GameWorld` can read back. |
| Visitor | ≥3 visitor classes | [`SnapshotMapper`](../src/CastleEscape.Game/Sessions/SnapshotMapper.cs) already walks the whole world to build DTOs. Turn the walk into visitors: snapshot, preview, and a statistics/scoring pass. |

## Comparison answers the defence will ask for

Bridge vs Strategy vs Adapter is answered in [Bridge.md](patterns/Bridge.md); the P2 ones
(Composite vs Decorator, State vs Strategy, Mediator vs Observer) belong with those patterns
when they are implemented.

## Design already done so P2 is not blocked

- A fixed-step tick with a single phase switch — State and Memento hook into it.
- Commands as objects with `Undo` — Interpreter produces them, Memento complements them.
- A message protocol and one serializer interface — Interpreter and Chain can process it.
- One grid/tile model and one mapper over it — Flyweight, Iterator, Composite and Visitor walk it.
