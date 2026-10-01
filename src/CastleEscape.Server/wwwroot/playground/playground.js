// Castle Escape playground: a minimal client that uses the public protocol (REST + SignalR)
// and the /api/dev shortcuts. Open it once per player (two tabs) to play together.
"use strict";

const $ = (id) => document.getElementById(id);
const params = new URLSearchParams(location.search);
const session = { id: params.get("sessionId"), token: params.get("playerToken"), playerId: params.get("playerId") };
const view = { rows: [], width: 0, height: 0, theme: "Dungeon", state: null, layoutLevel: 0 };
const TILE = 32;

// ------------------------------------------------------------------ REST helpers

async function api(method, path, body) {
  const headers = { "Content-Type": "application/json" };
  if (session.token) headers["X-Player-Token"] = session.token;
  const response = await fetch(path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    throw new Error(problem.detail || problem.title || `${response.status} ${response.statusText}`);
  }
  return response.status === 204 || response.status === 202 ? null : response.json();
}

function playerUrl(sessionId, playerId, token) {
  return `${location.pathname}?sessionId=${sessionId}&playerId=${playerId}&playerToken=${encodeURIComponent(token)}`;
}

/** Names come from players: never put them into HTML unescaped. */
function esc(text) {
  return String(text ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
}

function showError(error) {
  addEvent(`⚠ ${error.message}`);
  console.error(error);
}

// ------------------------------------------------------------------ lobby

async function loadCharacters() {
  const characters = await api("GET", "/api/content/characters");
  for (const select of [$("char1"), $("char2"), $("char")]) {
    select.innerHTML = characters.map((c) => `<option value="${esc(c.id)}">${esc(c.name)} (${c.maxHealth} lives)</option>`).join("");
  }
  $("char2").selectedIndex = Math.min(1, characters.length - 1);
}

$("quickstart").onclick = async () => {
  try {
    const seed = $("seed").value ? Number($("seed").value) : null;
    const game = await api("POST", "/api/dev/quickstart", { character1: $("char1").value, character2: $("char2").value, seed });
    $("links").innerHTML = game.players
      .map((p) => `<a href="${esc(playerUrl(game.sessionId, p.playerId, p.playerToken))}" target="_blank">Open as ${esc(p.name)} (${esc(p.characterId)})</a>`)
      .join(" · ") + ` <span class="muted">join code ${esc(game.joinCode)}</span>`;
  } catch (error) { showError(error); }
};

$("create").onclick = async () => {
  try {
    const created = await api("POST", "/api/sessions", { playerName: $("name").value });
    location.href = playerUrl(created.sessionId, created.playerId, created.playerToken);
  } catch (error) { showError(error); }
};

$("join").onclick = async () => {
  try {
    const joined = await api("POST", "/api/sessions/join", { joinCode: $("code").value, playerName: $("name").value });
    location.href = playerUrl(joined.sessionId, joined.playerId, joined.playerToken);
  } catch (error) { showError(error); }
};

$("pick").onclick = async () => {
  try {
    await api("PUT", `/api/sessions/${session.id}/players/me/character`, { characterId: $("char").value });
  } catch (error) { showError(error); }
};

// ------------------------------------------------------------------ hub

async function connect() {
  $("game").classList.remove("hidden");
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`/hubs/game?sessionId=${session.id}&playerToken=${encodeURIComponent(session.token)}`)
    .withAutomaticReconnect()
    .build();

  connection.on("SessionUpdated", (m) => renderLobby(m.session));
  connection.on("LevelStarted", (m) => {
    Object.assign(view, { rows: m.rows, width: m.width, height: m.height, theme: m.theme, state: m.state, layoutLevel: m.levelIndex });
    const canvas = $("board");
    canvas.width = m.width * TILE;
    canvas.height = m.height * TILE;
    addEvent(`Level ${m.levelIndex} (${m.theme}) started`);
    draw();
  });
  connection.on("StateUpdated", (m) => { view.state = m; draw(); renderHud(); });
  connection.on("GameEvent", (m) => {
    if (m.type === "SoundCue") return; // a real client plays a sound here
    addEvent(m.message);
  });
  connection.on("Error", (m) => addEvent(`⚠ ${m.code}: ${m.message}`));

  await connection.start();
  bindKeys(connection);
  bindDevButtons(connection);
}

function renderLobby(s) {
  $("title").textContent = `Level ${s.levelIndex}/${s.maxLevel} · ${s.phase} · join code ${s.joinCode}`;
  if (s.phase === "WaitingForPlayers" || s.phase === "CharacterSelect") {
    $("hud").innerHTML = `<p>Waiting: ${s.players.map((p) => `${esc(p.name)} ${p.characterId ? "✓ " + esc(p.characterId) : "(choosing)"}`).join(", ")}.
      Pick a character under <b>Manual lobby</b> if you haven't.</p>`;
    $("lobby").querySelector("details").open = true;
  }
}

// ------------------------------------------------------------------ input

function bindKeys(connection) {
  const keys = { ArrowUp: "Up", ArrowDown: "Down", ArrowLeft: "Left", ArrowRight: "Right", w: "Up", s: "Down", a: "Left", d: "Right" };
  const held = [];
  const send = () => connection.invoke("SetDirection", held.length ? held[held.length - 1] : "None").catch(showError);

  window.addEventListener("keydown", (e) => {
    const direction = keys[e.key];
    if (!direction || e.target.tagName === "INPUT") return;
    e.preventDefault();
    if (!held.includes(direction)) { held.push(direction); send(); }
  });
  window.addEventListener("keyup", (e) => {
    const direction = keys[e.key];
    if (!direction) return;
    held.splice(held.indexOf(direction), 1);
    send();
  });
  window.addEventListener("blur", () => { held.length = 0; send(); });
}

function bindDevButtons(connection) {
  const dev = (method, path, body) => api(method, `/api/dev${path}`, body).catch(showError);

  $("restart").onclick = () => {
    if (confirm("Restart the level for both players?")) connection.invoke("RequestRestart").catch(showError);
  };
  $("undo").onclick = async () => {
    const result = await dev("POST", `/sessions/${session.id}/undo`);
    if (result) addEvent(result.undone ? `Undid: ${result.undone}` : "Nothing to undo");
  };
  $("skip").onclick = () => dev("POST", `/sessions/${session.id}/skip-level`);
  $("setStrategy").onclick = () => dev("PUT", `/sessions/${session.id}/zombies/strategy`, { strategy: $("strategy").value });
  $("givePower").onclick = () => dev("POST", `/sessions/${session.id}/players/${session.playerId}/powers`, { power: $("power").value });
  $("setClone").onclick = async () => {
    const result = await dev("PUT", "/patterns/prototype-clone-mode", { mode: $("cloneMode").value });
    if (result) addEvent(`Prototype clone mode: ${result.mode} (applies on the next start or restart)`);
  };
  api("GET", "/api/dev/patterns/prototype-clone-mode").then((m) => { $("cloneMode").value = m.mode; }).catch(() => {});
}

// ------------------------------------------------------------------ drawing

const COLORS = {
  Dungeon: { floor: "#cfc6b4", wall: "#5b524a" },
  Crypt: { floor: "#b9bfc9", wall: "#3e4250" },
};

function terrainAt(x, y) {
  const c = view.rows[y]?.[x] ?? "#";
  return "#~ODE".includes(c) ? c : ".";
}

function draw() {
  const canvas = $("board");
  const ctx = canvas.getContext("2d");
  const palette = COLORS[view.theme] ?? COLORS.Dungeon;
  const state = view.state;

  for (let y = 0; y < view.height; y++) {
    for (let x = 0; x < view.width; x++) {
      const t = terrainAt(x, y);
      ctx.fillStyle = { "#": palette.wall, "~": "#3f7fd8", O: "#1b1b1b", E: "#7bd389", D: state?.doorOpen ? "#c9a46a" : "#8a5a2b" }[t] ?? palette.floor;
      ctx.fillRect(x * TILE, y * TILE, TILE, TILE);
      ctx.strokeStyle = "rgba(0,0,0,0.08)";
      ctx.strokeRect(x * TILE, y * TILE, TILE, TILE);
    }
  }
  if (!state) return;

  for (const lever of state.levers) {
    ctx.fillStyle = lever.isActive ? "#f2c94c" : "#7a6a3a";
    ctx.fillRect(lever.x * TILE + 10, lever.y * TILE + 6, 12, 20);
  }
  for (const item of state.items) {
    const color = { Health: "#e5484d", Reward: "#f5d90a", Power: "#9b5de5" }[item.kind];
    circle(ctx, item.x + 0.5, item.y + 0.5, 0.22, color, item.kind === "Power" ? item.power[0] : "");
  }
  for (const zombie of state.zombies) {
    circle(ctx, zombie.x + 0.5, zombie.y + 0.5, 0.38, "#2f9e44", zombie.strategy[0]);
  }
  for (const player of state.players) {
    circle(ctx, player.x + 0.5, player.y + 0.5, 0.4, player.slot === 1 ? "#3b5bdb" : "#e8590c", String(player.slot));
  }
}

function circle(ctx, x, y, radius, color, label) {
  ctx.beginPath();
  ctx.arc(x * TILE, y * TILE, radius * TILE, 0, Math.PI * 2);
  ctx.fillStyle = color;
  ctx.fill();
  if (label) {
    ctx.fillStyle = "#fff";
    ctx.font = "bold 13px system-ui";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(label, x * TILE, y * TILE + 1);
  }
}

function renderHud() {
  const s = view.state;
  if (!s) return;
  $("title").textContent = `Level ${s.levelIndex} · ${s.phase}${s.doorOpen ? " · door open" : ""}`;
  $("hud").innerHTML = `<table><tr><th></th><th>Lives</th><th>Score</th><th>Powers</th></tr>${s.players.map((p) => `
    <tr><td>${p.playerId === session.playerId ? "▶ " : ""}${esc(p.name)}</td><td>${"♥".repeat(p.lives)}<span class="muted">${"♥".repeat(Math.max(0, p.maxLives - p.lives))}</span></td>
    <td>${p.score}</td><td>${[...p.powers.map((w) => `${w.power}${w.level > 1 ? "×" + w.level : ""} ${w.remainingSeconds.toFixed(0)}s`), ...p.combos].join(", ")}</td></tr>`).join("")}</table>`;
}

function addEvent(text) {
  const li = document.createElement("li");
  li.textContent = text;
  $("events").prepend(li);
  while ($("events").children.length > 60) $("events").lastChild.remove();
}

// ------------------------------------------------------------------ start

(async () => {
  await loadCharacters().catch(showError);
  if (session.id && session.token) {
    $("lobby").querySelector("details").open = false;
    await connect().catch(showError);
  }
})();
