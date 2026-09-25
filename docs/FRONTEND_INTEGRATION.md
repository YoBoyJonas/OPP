# Frontend integration

> The full step-by-step guide (lobby REST calls, hub connection with `@microsoft/signalr`
> and the C# client, message order, input handling, interpolation) is written in Phase 6,
> once the protocol is final. Until then, this page tracks protocol changes.

## Current status

| What | Where |
|---|---|
| API reference (Scalar) | `http://localhost:5035/scalar` |
| OpenAPI document | `http://localhost:5035/openapi/v1.json` |
| Health | `GET /health` |
| SignalR hub | `/hubs/game` |

JSON conventions (REST and SignalR): camelCase property names, enums as strings
(`"Up"`, not `1`).

CORS: allowed browser origins come from `Cors:AllowedOrigins` in `appsettings.json`
(defaults: `http://localhost:5173`, `http://localhost:3000`, `http://localhost:5035`).
Add your dev server's origin there. Credentials are allowed, as SignalR needs.

## Migration from the first prototype protocol

The first prototype (commit `6e1eed7`) used a different hub path and different method names.
Anything written against it must switch:

| Prototype (`6e1eed7`) | Now / planned | Status |
|---|---|---|
| Hub `/gamehub` | Hub `/hubs/game` | ✅ switched (Phase 1) |
| Enums as numbers (`"direction": 1`) | Enums as strings (`"direction": "Up"`) | ✅ switched (Phase 1) |
| `JoinSession(playerName, characterId)` hub method with auto-matchmaking | `POST /api/sessions`, `POST /api/sessions/join` (join code), `PUT …/players/me/character`; the hub connects with `?sessionId=…&playerToken=…` | Phase 3 |
| `SendInput({ direction })` | `SetDirection(direction)`: send on key down, and `None` on key up | Phase 3 |
| `GameStateUpdate(GameStateSnapshot)` every tick | `LevelStarted` (static layout) once per level, then `StateUpdated` (dynamic state) every tick; `GameEvent`, `SessionUpdated`, `Error` | Phase 3 |
| `OpponentLeft` | `SessionUpdated` with phase `Aborted`, plus a `GameEvent` (`PlayerLeft`) | Phase 3 |
| Continuous `x`/`y` doubles | Tile position plus step progress; a render position is still sent as doubles for interpolation | Phase 3 |

Until Phase 3 lands, the old hub methods still exist at the new path.
