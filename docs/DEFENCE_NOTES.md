# Defence notes

One page to run the P1 defence from: how to show the 12 patterns, which switches exist,
and where the "explain the difference" answers live.

Each pattern's own document ends with a **Likely live-change requests** section — that is
where the detail is. This page is the index and the running order.

## Before you start

```bash
dotnet run --project src/CastleEscape.Server          # http://localhost:5035
```

| Open | What it gives you |
|---|---|
| http://localhost:5035/scalar | every endpoint, grouped; the pattern demos are under Course → Patterns |
| http://localhost:5035/playground | **Quickstart** gives two links; open each in its own tab and play with arrows/WASD |
| `GET /api/patterns` | all 12 patterns with participants **read from the code** by the `[DesignPattern]` attribute |

`/playground` and `/api/dev/*` need `DevTools:Enabled`, which is `true` in Development only.

## Showing a pattern

Three ways, strongest first:

1. **In the running game** — play on `/playground` and point at the behaviour (powers stacking,
   zombies changing chase, restart restoring items).
2. **The demo** — `POST /api/patterns/{key}/demo` in Scalar, or all 12 in the console:
   ```bash
   dotnet run --project src/CastleEscape.PatternDemos                      # all 12
   dotnet run --project src/CastleEscape.PatternDemos -- prototype --mode Shallow
   ```
   Each demo returns a step-by-step trace plus the **evidence** values that prove the requirement
   (counts, identities, addresses).
3. **The test** — one test per pattern in `tests/CastleEscape.Game.Tests/Patterns/`, named after
   the requirement it proves.

## Switches for "change it now"

| Switch | Where | Shows |
|---|---|---|
| `Patterns:PrototypeCloneMode` = `Deep` \| `Shallow` | appsettings, `?mode=` on the demo, or `PUT /api/dev/patterns/prototype-clone-mode` at runtime | Prototype: with `Shallow`, collected items stay gone after a restart — the bug deep copies prevent |
| Zombie chase strategy | `movementStrategy` in `content/zombies.json`; live: `PUT /api/dev/sessions/{sessionId}/zombies/strategy` (`Greedy`, `Bfs`, `AStar`, `Predictive`) | Strategy: swapped at runtime; it is a command, so `/undo` switches back |
| `Game:DoorMode` = `Latch` \| `HoldWhileActive` | appsettings | the D1 rule interpretation |
| `Realtime:EnabledChannels` = `SignalR`, `Polling` | appsettings | Bridge: the same notifiers over either channel |
| Serialization format | `Accept: application/xml` on a polled read | Adapter: the same message as JSON or XML |

Other dev actions: `POST /api/dev/quickstart` (two players, characters picked, level 1 starting),
`POST /api/dev/sessions/{sessionId}/undo` (Command), `POST .../players/{playerId}/powers`
(Decorator), `POST .../skip-level`.

## "Explain the difference" answers

| Question | Answer |
|---|---|
| Bridge vs Adapter | Adapter makes an *existing* incompatible API fit the interface we need, after the fact. Bridge is designed up front so two hierarchies grow independently. The polling path uses both — see [Bridge.md](patterns/Bridge.md). |
| Bridge vs Strategy | A strategy is one swappable algorithm inside one object. In Bridge *both* sides are hierarchies: refined notifiers × channels. |

Composite vs Decorator, State vs Strategy and Mediator vs Observer are P2 questions — see
[P2_ROADMAP.md](P2_ROADMAP.md).

## Facts worth having ready

- **12 patterns, none used twice**, three per student — the owner table is in [patterns/README.md](patterns/README.md).
- Participants in `GET /api/patterns` come from attributes by reflection, so the documentation
  cannot drift from the code.
- The "before" class diagrams are the naive prototype at tag `p1-prototype-before-patterns`.
- Rule interpretations where the requirements were ambiguous: [DECISIONS.md](DECISIONS.md).
- Requirement → code → endpoint → test mapping: [USE_CASE_MAPPING.md](USE_CASE_MAPPING.md).
