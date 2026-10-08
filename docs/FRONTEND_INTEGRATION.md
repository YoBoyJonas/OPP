# Frontend integration

How to build a Castle Escape client. The server decides everything; a client only (1) sets up a
session over REST, (2) connects to the SignalR hub, (3) sends key presses, and (4) draws what it receives.

**Live references while you work** (run `dotnet run --project src/CastleEscape.Server`):
- http://localhost:5035/scalar is the REST reference, where you can try every call.
- `GET /api/realtime/protocol` describes the hub: methods, payloads, event types, error codes.
- `GET /api/realtime/examples` gives a real example of every hub message, as JSON.
- http://localhost:5035/playground is a working reference client (`src/CastleEscape.Server/wwwroot/playground/playground.js`, ~250 lines).
- [docs/openapi.json](openapi.json) is the OpenAPI document, for code generators.

## Conventions

- JSON everywhere, **camelCase** property names, **enums as strings** (`"Up"`, `"Playing"`).
- Errors are RFC 7807 problem details with an extra `code` (a `GameErrorCode`, e.g. `"InvalidJoinCode"`).
  Status codes: 400 bad input, 401 wrong/missing token, 404 unknown session or code, 409 wrong
  phase (e.g. the session already started), 422 level generation failed.
- Calls that act as a player need the header **`X-Player-Token: <playerToken>`** from create/join.
- CORS: add your dev server's origin to `Cors:AllowedOrigins` in `appsettings.json` (defaults allow
  `http://localhost:5173`, `:3000`, `:5035`). Credentials are allowed, as SignalR needs.

## 1. Lobby (REST)

```http
POST /api/sessions                     { "playerName": "Ana" }
→ 201 { "sessionId": "…", "joinCode": "K7Q2MX", "playerId": "…", "playerToken": "…" }

POST /api/sessions/join                { "joinCode": "K7Q2MX", "playerName": "Ben" }
→ 200 { "sessionId": "…", "playerId": "…", "playerToken": "…" }

GET  /api/content/characters           → [{ "id": "warrior", "name": "Warrior", "maxHealth": 5, "baseMoveSpeed": 3.5, … }, …]

PUT  /api/sessions/{sessionId}/players/me/character      X-Player-Token: …
     { "characterId": "scout" }
→ 200 SessionDto

DELETE /api/sessions/{sessionId}/players/me              X-Player-Token: …   (leave: ends the session for both)
```

The first player shows the join code to the second. When both have picked a character, the
first level loads automatically; there is no "start" call (D11). For development, skip all of this
with `POST /api/dev/quickstart` (Development only), which returns two ready players with their tokens.

## 2. Connect to the hub

URL: `/hubs/game?sessionId={sessionId}&playerToken={playerToken}`. Connect right after create/join,
because the hub also delivers lobby updates.

**JavaScript/TypeScript** (`npm i @microsoft/signalr`):

```js
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${baseUrl}/hubs/game?sessionId=${sessionId}&playerToken=${encodeURIComponent(token)}`)
  .withAutomaticReconnect()
  .build();

connection.on("SessionUpdated", (m) => showLobby(m.session));     // players, characters, phase
connection.on("LevelStarted",   (m) => loadLevel(m));              // static layout + first state
connection.on("StateUpdated",   (m) => setState(m));               // every tick while playing
connection.on("GameEvent",      (m) => onEvent(m));                // things that happened
connection.on("Error",          (m) => showError(m.code, m.message));

await connection.start();
```

**C# client** (`Microsoft.AspNetCore.SignalR.Client`); the same JSON options as the server
(camelCase is the default; add string enums):

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl($"{baseUrl}/hubs/game?sessionId={sessionId}&playerToken={token}")
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .WithAutomaticReconnect()
    .Build();
connection.On<TickStateMessage>("StateUpdated", state => …);   // types from CastleEscape.Contracts
await connection.StartAsync();
```

On (re)connect the server immediately sends `SessionUpdated`, and while a level runs also
`LevelStarted` and the latest `StateUpdated`, so a client can join or reconnect at any moment.

## 3. Messages you receive

Every message has `sessionId`, `seq` (increases by one per message within a session; use it to
order and drop duplicates) and `tick`.

| Method | When | Use it to |
|---|---|---|
| `SessionUpdated` | lobby changes, phase changes | lobby screen; react to `session.phase` |
| `LevelStarted` | each level start and restart | draw the static map from `rows` (legend below); `theme` picks the tile set; `state` is the first state |
| `StateUpdated` | 20× per second while playing | draw players, zombies, items, levers, door; HUD |
| `GameEvent` | something happened | toasts, sounds, effects (`type`, `message`, `data`) |
| `Error` | rejected connection, or the session was stopped | show and go back to the menu |
| `Pong` | after you call `Ping` | latency |

**Phases** (`session.phase`): `WaitingForPlayers` → `CharacterSelect` → `LoadingLevel` →
`Playing` ⇄ `LevelComplete` → … → `Victory` | `Defeat` | `Aborted`. `Defeat`, `Victory` and
`Aborted` are final: show the result and return to the menu (D10).

**Map legend** (`rows`, one string per row): `#` wall, `.` floor, `~` water (needs Swim), `O` pit
(needs Jump), `D` door, `E` exit tile, and at their starting positions `L` lever, `1`/`2` start
tiles, `Z` zombie spawn, `h` health, `r` reward, `j`/`s`/`w` Jump/Sprint/Swim items. Draw only the
terrain from `rows`; take levers, items and zombies from the state, because they change.
`GET /api/sessions/{id}/level` also returns the legend and the themed obstacle variants.

**State** (`StateUpdated`): `players[]` with `tileX/tileY` (the tile they occupy), `x/y`
(interpolated render position, doubles), `facing`, `isMoving`, `lives`, `maxLives`, `score`,
`powers[]` (`power`, `remainingSeconds`, `level`) and `combos[]`; `zombies[]` with `x/y` and their
current `strategy`; `items[]`; `levers[]` (`isActive`); `doorOpen`; `phase`; `levelIndex`.

**Events** (`GameEvent.type`): `ItemCollected`, `PowerGained`, `PowerExpired`, `ComboActivated`,
`LifeLost`, `LeverChanged`, `DoorOpened`, `DoorClosed`, `PlayerReachedExit`, `LevelStarted`,
`LevelRestarted`, `LevelCompleted`, `GameWon`, `GameLost`, `PlayerJoined`, `PlayerLeft`,
`PhaseChanged`, `CommandUndone`, and `SoundCue`. `data` holds the event's fields as strings, e.g.
`LifeLost` → `{ "zombieId": "zombie-1", "lives": "2" }`. **`SoundCue`** events name a sound in
`data.cue` (`pickup`, `power_up`, `combo`, `life_lost`, `door_open`, `level_complete`, `victory`,
`game_over`); map these names to your sound files.

## 4. Sending input

| Hub method | Arguments | Notes |
|---|---|---|
| `SetDirection` | `"Up"`, `"Down"`, `"Left"`, `"Right"` or `"None"` | Send the direction on key **down** and `"None"` on key **up**. Holding keeps walking tile by tile. A quick tap still moves one tile. |
| `RequestRestart` | — | Restarts the level for both players (D8). Ask "are you sure?" first. |
| `Ping` | — | Answered with `Pong`. |

```js
const keys = { ArrowUp: "Up", ArrowDown: "Down", ArrowLeft: "Left", ArrowRight: "Right" };
const held = [];
const send = () => connection.invoke("SetDirection", held.at(-1) ?? "None");
addEventListener("keydown", (e) => { const d = keys[e.key]; if (d && !held.includes(d)) { held.push(d); send(); } });
addEventListener("keyup",   (e) => { const d = keys[e.key]; if (d && held.includes(d)) { held.splice(held.indexOf(d), 1); send(); } });
addEventListener("blur",    () => { if (held.length) { held.length = 0; send(); } });
```

Don't move anything locally: the server may reject a step (wall, water without Swim, the other
player in the way). Just draw the latest `x/y`. For smoother motion at 60 fps, interpolate between
the last two `StateUpdated` messages.

## 5. HUD and other reads (REST)

`GET /api/sessions/{id}/hud` is the "view game state" table: per player lives, score, powers with
time left, combos and statistics, plus level, levers and door. `GET /api/sessions/{id}/state`
returns the latest state (`?format=xml` or `Accept: application/xml` for XML, NET-2).

## Clients without SignalR: polling

`GET /api/sessions/{id}/messages?afterSeq={last seq you saw}&format=json|xml` returns the same
messages from a per-session buffer (the last 200). `StateUpdated` is kept every 5th tick, and
events are never dropped. Each `body` is the serialized message; `latestSeq` is your next `afterSeq`.

## Developer shortcuts (Development only)

`/api/dev/*` (see Scalar → Ops → Dev): quickstart, undo the last command, switch every zombie's
strategy, give a power, skip the level, flip the Prototype clone mode. `/api/diagnostics/sessions/{id}/commands`
and `/events` show what the server did.

## Migration from the first prototype protocol

The first prototype (commit `6e1eed7`) used a different hub path and method names:

| Prototype (`6e1eed7`) | Now |
|---|---|
| Hub `/gamehub` | Hub `/hubs/game?sessionId=…&playerToken=…` |
| Enums as numbers (`"direction": 1`) | Enums as strings (`"direction": "Up"`) |
| `JoinSession(playerName, characterId)` hub method with auto-matchmaking | `POST /api/sessions`, `POST /api/sessions/join` (join code), `PUT …/players/me/character` |
| `SendInput({ direction })` | `SetDirection(direction)`: on key down, and `None` on key up |
| `GameStateUpdate(GameStateSnapshot)` every tick | `LevelStarted` once per level, then `StateUpdated` every tick; plus `GameEvent`, `SessionUpdated`, `Error` |
| `OpponentLeft` | `SessionUpdated` with phase `Aborted`, plus a `PlayerLeft` `GameEvent` |
| Continuous `x`/`y` only | `tileX/tileY` plus interpolated `x/y` |
