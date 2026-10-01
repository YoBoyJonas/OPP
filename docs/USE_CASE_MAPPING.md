# Use cases and requirements → code

Where each use case (the team's activity diagrams in [diagrams/](diagrams/)) and each requirement
([requirements/reikalavimai_zaidimui.txt](requirements/reikalavimai_zaidimui.txt)) is implemented,
how a client triggers it, and which test proves it. Where the diagrams and the requirements
disagree, [DECISIONS.md](DECISIONS.md) says what the server does (Dn).

## Use cases

| Use case (diagram) | Client does | Server code | Tests |
|---|---|---|---|
| Join game session (`join_game_session.png`) | `POST /api/sessions`, `POST /api/sessions/join`, `PUT …/players/me/character`, hub connect | `GameFacade` → `SessionRegistry.Create/Join/Authenticate`, `GameSession.AddPlayer/SelectCharacter` | `CreateJoinSelect_StartsLevelOne`, `Join_WrongCode_Is404WithCode`, `Join_ThirdPlayer_Is409`, `SelectCharacter_WithoutToken_Is401`, `Hub_WrongToken_SendsErrorAndCloses` |
| Start level (`start_level.png`) | nothing: automatic when both have characters, and after each level (D11) | `GameSession.LoadLevel` → `ILevelProvider` → `LevelDirector` (Builder, Abstract Factory, Factory Method) → `GameWorld.BeginLevel` (Prototype); `LevelStarted` message | `CreateJoinSelect_StartsLevelOne`, `EveryLevel_ManySeeds_PassesValidation`, `BuilderTests` |
| Move character (`move_character.png`) | hub `SetDirection` (key down / `None` on key up) | `SetDirectionCommand` → `PlayerMovement` (`StartStepCommand`) → `MovementRules.CanPlayerEnter` → `IAbilities.SpeedOn` | `Movement_*` tests, `RestInput_MovesThePlayer`, `CommandTests` |
| Cross obstacle (`cross_obstacle.png`) | move into water/pit while the power is active | `IAbilities.CanEnter` (Jump/Swim decorators), themed obstacle speed | `Movement_SwimCrossesWater_Mov2`, `Movement_JumpCrossesPit_Mov2`, `…BlocksWithout…`, `DecoratorTests` |
| Collect item (`collect_item.png`), Collect health item (`collect_health_item.png`) | walk onto the item | `InteractionResolver.CollectItems` → `item.Apply` (`HealthItem`, `RewardItem`, `PowerItem`); `ItemCollected` event | `Items_HealthIsCappedAndRewardScores_Itm2`, `FactoryMethodTests` |
| Use active power (`use_active_power.png`) | nothing extra: active from pickup until it expires (D3) | `PowerManager` (timers, stacking, combos) rebuilding the decorator chain | `Powers_*` tests, `DecoratorTests` |
| Pull lever (`pull_lever.png`) | stand on the lever (D2) | `ExitMechanism.UpdateLevers`; `LeverChanged` event | `Door_OpensWhenBothLeversActive_AndLatches_D1` |
| Open exit door (`open_exit_door.png`) | both players on levers in the same tick | `ExitMechanism.Update` (Latch or HoldWhileActive, D1); `DoorOpened` | `Door_OpensWhenBothLeversActive_AndLatches_D1`, `Door_HoldWhileActive_ClosesWhenALeverIsReleased` |
| Lose life (`lose_life.png`) | — (a zombie touches the player) | `InteractionResolver.ResolveZombieContacts`: −1 life, zombie back to spawn (D6); `LifeLost` + `SoundCue` | `Zombie_ContactCostsALifeAndResetsZombie_Zmb2_Zmb3` |
| Lose game (`lose_game.png`) | — | `GameSession.TickPlaying`: any player at 0 lives → `GameLost`, phase `Defeat` (D10) | `Defeat_WhenAnyPlayerHasNoLives_Plr3` |
| Restart level (`restart_level.png`) | hub `RequestRestart` (confirm in the client) | `RestartLevelCommand`: fresh prototype clone, checkpoint stats (D8); undoable | `Restart_RestoresItemsAndScore_D8`, `PrototypeTests`, `CommandTests` |
| View game state (`view_game_state.png`) | `GET /api/sessions/{id}/hud`, or draw from `StateUpdated` | `SnapshotMapper.ToHud`, statistics from `SessionStatisticsObserver` | `GameRulesTests` (HUD stats), `ObserverTests` |
| Complete level / win (requirements) | both players stand on exit tiles | `ExitMechanism.UpdateExit` → `LevelComplete` → next level, or `Victory` after 10 | `Exit_NeedsBothPlayers_Door2`, `Victory_AfterTheLastLevel_Win1`, `TwoSignalRClients_PullLevers_ReachExit_NextLevelStarts` |

## Requirements

IDs are the ones used in code comments and test names.

| ID | Requirement (short) | Where | Proof |
|---|---|---|---|
| LVL-1 | Fixed sequence of 10 levels; next loads automatically; victory after the 10th | `content/levels.json`, `GameSession.AdvanceLevel` | `Victory_AfterTheLastLevel_Win1` |
| LVL-2 | 2D top-down grid ≥ 20×15 | `Game:GridWidth/Height` (24×16), `ContentCatalog.Validate` | `Validate_RoomSmallerThan20x15_ReportsLvl2`, `EveryLevel_ManySeeds…` |
| LVL-3 | Two distinct, unblocked start tiles | `ProceduralLevelBuilder.PlaceStartTiles`, `LevelState.AddStartTile`, `LevelValidator` | `LevelState_StartTilesMustBeDistinct_Lvl3`, validator |
| DOOR-1 | Door opens when both levers are active (D1: latches) | `ExitMechanism` | `Door_*` tests |
| DOOR-2 | Level completes only with both players on exit tiles | `ExitMechanism.UpdateExit` | `Exit_NeedsBothPlayers_Door2` |
| WIN-1 | Victory after level 10 | `GameSession.AdvanceLevel` | `Victory_AfterTheLastLevel_Win1` |
| PLR-1 | ≥3 characters differing in lives, speed and jump | `content/characters.json` (C6) | `Characters_DifferInLivesSpeedAndJump_Plr1`, `Validate_FewerThanThreeCharacters_ReportsPlr1` |
| PLR-2 | Lives counter, −1 on zombie contact, shown in the HUD | `PlayerEntity.LoseLives`, HUD | `Zombie_ContactCostsALife…` |
| PLR-3 | Any player at 0 lives → defeat | `GameSession.TickPlaying` | `Defeat_WhenAnyPlayerHasNoLives_Plr3` |
| MOV-1 | Real-time, grid-aligned, constant speed; walls block | `MovableEntity`, `PlayerMovement`, `MovementRules` | `Movement_ConstantSpeedGridSteps_Mov1`, `Movement_WallBlocks_Col1` |
| MOV-2 | Water needs Swim, pits need Jump | `IAbilities` decorators | `Movement_*Mov2`, `Obstacles_NeedTheMatchingPower_Mov2` |
| MOV-3 | Players move independently and simultaneously | per-player held direction, `PlayerMovement` | `Movement_PlayersMoveIndependently_Mov3` |
| ITM-1 | Three item kinds: Power, Reward, Health | `Consumable` subclasses, item products | `Consumables_CoverAllThreeKinds_Itm1`, `FactoryMethodTests` |
| ITM-2 | Pickup updates the counter and removes the item | `InteractionResolver.CollectItems` | `Items_HealthIsCappedAndRewardScores_Itm2` |
| PWR-1 | Jump, Sprint, Swim last a fixed time | `PowerManager`, `Game:PowerDurationSeconds` | `Powers_ExpireAfterDuration_Pwr1` |
| PWR-2 | Two different powers → JumpDash / FastSwim | `PowerManager.RefreshCombos`, combo decorators | `Powers_TwoDifferentPowersMakeACombo_Pwr2` |
| PWR-3 | The same power stacks (time and effect) | `ActivePower.Stack`, one decorator per level | `Powers_SamePowerStacks_Pwr3`, `StackedPower_IsOneDecoratorPerLevel_Pwr3` |
| ZMB-1 | Zombies chase the nearest player, re-deciding on each tile (D16) | `IZombieMovementStrategy`, `WorldView.NearestPlayer` | `Zombie_BfsChasesAroundWalls_Zmb1`, `StrategyTests` |
| ZMB-2 | Contact damage: −1 life | `InteractionResolver.ResolveZombieContacts` | `Zombie_ContactCostsALife…` |
| ZMB-3 | After contact the zombie returns to its spawn | `ZombieEntity.ResetToSpawn` | same |
| ZMB-4 | Zombie count never decreases with level | `content/levels.json`, catalog validation | `Levels_ZombieCountNeverDecreases_Zmb4`, `Validate_ZombieCountDecreases_ReportsZmb4` |
| GEN-1 | Generated layout always has a path start → exit | `ProceduralLevelBuilder` + `LevelValidator` (D5: without powers) | `EveryLevel_ManySeeds_PassesValidation` (250 levels) |
| GEN-2 | No item, zombie or lever on walls or start tiles | builders, `ItemSpawner.Spawn`, `LevelValidator` | same, `FactoryMethodTests` |
| GEN-3 | ≥2 levers, 1 exit, ≥1 obstacle needing a power | builders, `LevelValidator`, catalog | same, `Validate_LevelWithoutPowerObstacle_ReportsGen3` |
| COL-1 | Moves into impassable tiles are rejected | `MovementRules.CanPlayerEnter` | `Movement_WallBlocks_Col1` |
| COL-2 | Stepping onto an item applies it and removes it | `InteractionResolver.CollectItems` | `Items_…` |
| COL-3 | Player–zombie contact: ZMB-2 + ZMB-3 | `InteractionResolver` | `Zombie_…` |
| COL-4 | Lever active while stood on; open door is passable | `ExitMechanism`, `MovementRules` | `Door_…` |
| NET-1 | Server simulates; clients only send input | `GameSession` tick, hub accepts only input | `TwoSignalRClients_…` |
| NET-2 | Both clients show the same state; JSON/XML | one state per tick to the session group; XML via the Adapter | `State_AsXml_ViaQueryAndAcceptHeader`, `AdapterTests` |
| NET-3 | Two players join one session; it starts with two | `SessionRegistry`, `GameSession.AddPlayer` | `Join_ThirdPlayer_Is409`, `CreateJoinSelect_StartsLevelOne` |
