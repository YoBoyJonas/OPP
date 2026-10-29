# Understanding map generation: Abstract Factory and Builder

This guide follows one request through Castle Escape: asking the backend to generate a preview of
level 6. It shows where the theme factory is chosen, how the builder uses it, and how to watch it run
in Visual Studio.

The main idea: **the builder decides where map objects go; the theme factory decides which kind of
object it is.** Level 6 uses the Crypt theme, so its factory supplies bone walls, poison water,
abyss pits and Crypt zombies.

Both patterns are plain C# classes in `CastleEscape.Game`. They don't use ASP.NET, dependency
injection, attributes or reflection. The server only calls them.

## 1 The pieces

| Piece | File | Job |
|---|---|---|
| `LevelProvider` | `src/CastleEscape.Game/Generation/LevelProvider.cs` | The client: picks the factory and the builder, asks the director to build, validates, retries. |
| `ThemeFactories.For` | `src/CastleEscape.Game/Generation/Themes/ThemeFactories.cs` | Turns a theme (Dungeon/Crypt) into a factory object. |
| `IThemeFactory` | `src/CastleEscape.Game/Generation/Themes/IThemeFactory.cs` | Abstract factory: `CreateWall`, `CreateWater`, `CreatePit`, `CreateZombie`. |
| `DungeonThemeFactory`, `CryptThemeFactory` | `Themes/DungeonTheme.cs`, `Themes/CryptTheme.cs` | Concrete factories, each file also holds its four products. |
| `ILevelBuilder` | `src/CastleEscape.Game/Generation/ILevelBuilder.cs` | Builder: the seven steps and `GetResult`. |
| `ProceduralLevelBuilder`, `PresetLevelBuilder` | `Generation/` | Concrete builders: random map, or a hand-made map. |
| `LevelDirector` | `src/CastleEscape.Game/Generation/LevelDirector.cs` | Director: calls the seven steps in order. |
| `LevelValidator` | `src/CastleEscape.Game/Generation/LevelValidator.cs` | Checks a finished level against the generation rules. |
| `content/levels.json` | | Each level's theme: levels 1–5 Dungeon, 6–10 Crypt. |

## 2 The request we will follow

```http
GET http://localhost:5035/api/levels/6/preview?seed=42
```

"Build level 6 from random seed 42 and show it to me." No `preset` is given, so the map is
generated. The seed decides the layout; the theme comes from `levels.json`, not from the seed.

```text
LevelEndpoints.Preview                      (server, HTTP)
  └─ LevelPreviewer.Preview                 (finds level 6, converts the result to JSON data)
       └─ LevelProvider.Generate            (client of both patterns)
            ├─ ThemeFactories.For(Crypt)    → new CryptThemeFactory(...)        Abstract Factory
            ├─ new ProceduralLevelBuilder(..., theme, seed, ...)                Builder
            ├─ LevelDirector.Construct(builder)
            │    └─ BuildTerrain, PlaceExitAndDoor, ... PlaceZombies, GetResult
            │         └─ theme.CreateWall(), theme.CreateWater(), theme.CreateZombie(...)
            └─ LevelValidator.Validate      (not valid? new builder with the next seed)
```

## 3 Step by step

### Step 1 The endpoint

[LevelEndpoints.cs](../src/CastleEscape.Server/Endpoints/LevelEndpoints.cs), method `Preview`, only
forwards the numbers: `levels.Preview(index, seed ?? 1, preset)`.

### Step 2 The previewer

[LevelPreviewer.cs](../src/CastleEscape.Game/Generation/LevelPreviewer.cs), method `Preview`, finds the
level definition with `Index == 6` (its `Theme` is `Crypt`) and, because there is no preset, calls
`LevelProvider.Generate(definition, seed)`.

### Step 3 The client picks the factory and the builder

[LevelProvider.cs](../src/CastleEscape.Game/Generation/LevelProvider.cs), method `Generate`:

```csharp
IThemeFactory theme = ThemeFactories.For(definition.Theme, catalog);
...
var builder = new ProceduralLevelBuilder(catalog, definition, theme, attemptSeed, game, generation);
var level = _director.Construct(builder);
errors = LevelValidator.Validate(level, generation.MinZombieDistanceFromStart);
```

This is the one place where both patterns meet. The factory is picked once, before the loop, so
every attempt (and every part of the level) uses the same family.

### Step 4 The concrete factory is created

[ThemeFactories.cs](../src/CastleEscape.Game/Generation/Themes/ThemeFactories.cs):

```csharp
return theme switch
{
    LevelTheme.Dungeon => new DungeonThemeFactory(wall, water, pit),
    LevelTheme.Crypt => new CryptThemeFactory(wall, water, pit),
    ...
};
```

For level 6 this runs `new CryptThemeFactory(...)`. `wall`, `water` and `pit` are the generic
obstacles from `content/obstacles.json`; the themed products copy their rules (e.g. "water needs
Swim") and change what makes the theme different. The method returns the interface type
`IThemeFactory`, so the code that uses it never names `CryptThemeFactory`.

### Step 5 The builder gets the factory in its constructor

[ProceduralLevelBuilder.cs](../src/CastleEscape.Game/Generation/ProceduralLevelBuilder.cs), constructor:

```csharp
_theme = theme;
_rng = new Random(seed);
_grid = new Grid(...);
_level = new LevelState(definition, seed, _grid);
_wall = theme.CreateWall();
```

A builder is made for one level only, so everything it needs comes in through the constructor.

### Step 6 The director runs the steps

[LevelDirector.cs](../src/CastleEscape.Game/Generation/LevelDirector.cs):

```text
BuildTerrain → PlaceExitAndDoor → PlaceStartTiles → PlaceLevers
             → PlacePowerObstacles → PlaceItems → PlaceZombies → GetResult
```

The director only knows the order. Inside the steps the builder asks the factory for parts:

- `BuildTerrain` places the wall it made in the constructor (`BoneWall` for Crypt);
- `PlacePowerObstacles` calls `_theme.CreateWater()` or `_theme.CreatePit()` (`PoisonWater`, `AbyssPit`);
- `PlaceZombies` calls `_theme.CreateZombie(...)` (`CryptZombie`).

The builder decides *where*; the factory decides *which class*. Items are made by the item
spawners, a separate pattern (Factory Method).

### Step 7 Validate, retry, respond

Back in `LevelProvider.Generate`, the level is checked by `LevelValidator`. If a step found no room
(`LevelBuildException`) or the level breaks a rule, a new builder is made with the next seed
(`seed + attempt * 7919`). Then `LevelPreviewer` turns the level into a `LevelPreviewResponse`
(rows, obstacles, zombies, validation result) and the endpoint returns it as JSON. The `seed` in the
response is the attempt that worked, so it can differ from the one you asked for.

With `?preset=tutorial` the previewer calls `LevelProvider.BuildPreset` instead: the same factory,
the same director, but a `PresetLevelBuilder` that reads the map from `content/presets/tutorial.txt`.

## 4 Roles

| Pattern role | Code |
|---|---|
| Abstract factory | `IThemeFactory` |
| Concrete factories | `DungeonThemeFactory`, `CryptThemeFactory` |
| Abstract products | `Wall`, `Water`, `Pit`, `ZombieEntity` |
| Concrete products | `StoneWall`, `MurkyWater`, `SpikePit`, `DungeonZombie` / `BoneWall`, `PoisonWater`, `AbyssPit`, `CryptZombie` |
| Builder | `ILevelBuilder` |
| Concrete builders | `ProceduralLevelBuilder`, `PresetLevelBuilder` |
| Director | `LevelDirector` |
| Product (of the builder) | `LevelState` |
| Client | `LevelProvider` (picks the factory and the builder) |

## 5 How a real game reaches the same code

| Order | Code | What happens |
|---|---|---|
| 1 | [SessionEndpoints](../src/CastleEscape.Server/Endpoints/SessionEndpoints.cs), `PUT /api/sessions/{sessionId}/players/me/character` | A player picks a character. |
| 2 | [GameFacade.SelectCharacter](../src/CastleEscape.Game/Sessions/GameFacade.cs) | Forwards to the session. |
| 3 | [GameSession.SelectCharacter](../src/CastleEscape.Game/Sessions/GameSession.cs) | When both players are ready, the phase becomes `LoadingLevel`. |
| 4 | `GameSession.LoadLevel` (next tick) | Calls `_levels.CreateLevel(index, seed)`. |
| 5 | `LevelProvider.CreateLevel` | Calls `Generate` (or builds the `Generation:PresetLevel` map), as in steps 3–7 above. |

## 6 Watch it in Visual Studio

1. Set **CastleEscape.Server** as the startup project, pick the **http** profile, press **F5**.
2. Set breakpoints (**F9**) on the lines below.
3. Open `http://localhost:5035/api/levels/6/preview?seed=42` in a browser.

| Breakpoint | What to look at |
|---|---|
| `LevelProvider.Generate`, the `ThemeFactories.For` line | `definition.Theme == Crypt`; step in (**F11**) to see `new CryptThemeFactory` |
| `LevelProvider.Generate`, the `_director.Construct(builder)` line | `theme` is a `CryptThemeFactory`; `attemptSeed` |
| `LevelDirector.Construct`, first line | step through the seven calls |
| `CryptThemeFactory.CreateZombie` | a `CryptZombie` is created |
| `LevelProvider.Generate`, the `if (errors.Count == 0)` line | the validation result of this attempt |

Then try `.../api/levels/1/preview?seed=42`: the factory becomes `DungeonThemeFactory`, the rest of
the flow is the same. Without the server, run `dotnet run --project src/CastleEscape.PatternDemos -- abstract-factory`
(or `-- builder`) and put the breakpoints in the demo.

## 7 A short explanation for the defence

> Each level has a theme, Dungeon or Crypt. `LevelProvider` picks the theme's factory once with
> `ThemeFactories.For` and gives it to the level builder as an `IThemeFactory`. The builder calls
> `CreateWall`, `CreateWater`, `CreatePit` and `CreateZombie` without knowing the concrete classes,
> so a Crypt level gets `BoneWall`, `PoisonWater`, `AbyssPit` and `CryptZombie`, and can never get a
> Dungeon part. The Builder pattern decides how the level is assembled: `LevelDirector` runs the same
> seven steps on either the random `ProceduralLevelBuilder` or the hand-made `PresetLevelBuilder`.
