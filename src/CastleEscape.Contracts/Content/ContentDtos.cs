namespace CastleEscape.Contracts.Content;

public sealed record StatModifiersDto(double MoveSpeedMultiplier, double JumpForceMultiplier, double SwimSpeedMultiplier);
public sealed record CharacterDto(string Id, string Name, int MaxHealth, double BaseMoveSpeed, double BaseJumpForce, StatModifiersDto BaseModifiers);
public sealed record PowerGrantDto(string Type, PowerType Power, double? DurationSeconds, StatModifiersDto Modifiers);
public sealed record ConsumableDto(string Type, string Id, string Name, string Kind, PowerGrantDto? Grant, int HealthValue, int ScoreValue, double SpawnWeight, string IconPath);
public sealed record PowerComboDto(string Id, string Name, PowerType[] RequiredPowers, PowerType Granted, StatModifiersDto Modifiers);
public sealed record ObstacleDto(string Type, string Id, string Name, string Kind, PowerType TraversalRequirements, bool BlocksZombies, int MinSize, int MaxSize, double MoveSpeedMultiplier);
public sealed record ZombieDefinitionDto(string Id, string Name, double Speed, int ContactDamage, double AttackCooldownSeconds, int Size, MovementStrategyKind MovementStrategy);
public sealed record GridSizeDto(int Width, int Height);
public sealed record SpawnTableEntryDto(string Id, double Weight);
public sealed record LevelDefinitionDto(string Id, int Index, LevelTheme Theme, GridSizeDto? RoomSize, int ObstacleCount, SpawnTableEntryDto[] ObstacleTable, int ZombieCount, SpawnTableEntryDto[] ZombieTable, int PickupCount, SpawnTableEntryDto[] ConsumableTable, double TimeLimitSeconds);
