# P1 design patterns

Patterns live with the feature they implement in Domain, Application, Infrastructure or Server. Course demos live separately under `tools/CastleEscape.PatternDemos`. It has:
- its participants tagged `[DesignPattern("<Pattern>", "<Role>")]`;
- a demo shared by the console app and the API;
- a unit test proving the course requirement;
- one document here: the problem, why the pattern fits, a before/after Mermaid class diagram,
  the key code, how the requirement is met, and likely live-change requests at the defence.

The "before" diagrams show the naive prototype at git tag `p1-prototype-before-patterns`.

| Owner | Patterns | Theme |
|---|---|---|
| Student A | [Abstract Factory](AbstractFactory.md), [Builder](Builder.md), [Prototype](Prototype.md) | World creation: themes, level construction, restart copies |
| Student B | [Factory Method](FactoryMethod.md), [Strategy](Strategy.md), [Decorator](Decorator.md) | Gameplay rules: items, zombie AI, powers |
| Student C | [Singleton](Singleton.md), [Adapter](Adapter.md), [Bridge](Bridge.md) | Infrastructure: content, serialization, delivery channels |
| Student D | [Observer](Observer.md), [Command](Command.md), [Facade](Facade.md) | Session orchestration: events, inputs and undo, the API surface |

No pattern is used twice, as the course requires.

## Running the demos

```bash
dotnet run --project tools/CastleEscape.PatternDemos                      # all 12
dotnet run --project tools/CastleEscape.PatternDemos -- prototype --mode Shallow
```

The normal server exposes gameplay operations only. Run demonstrations in the console; the tooling catalog discovers participants across the runtime assemblies.

## Defence switches

| Switch | Where |
|---|---|
| Prototype deep ↔ shallow | `--mode Deep` or `--mode Shallow` on the console demo; live sessions always use Deep |
| Zombie strategy | `movementStrategy` in `content/zombies.json`; live: `PUT /api/dev/sessions/{id}/zombies/strategy` |
| Door mode | `Game:DoorMode = Latch \| HoldWhileActive` |
| Delivery channels | `Realtime:EnabledChannels` |
