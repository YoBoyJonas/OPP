using CastleEscape.Contracts;

namespace CastleEscape.Game.World;

/// <summary>
/// The ASCII map legend shared by preset files and the static layout sent to clients
/// (see <c>content/presets/README.md</c>).
/// </summary>
public static class MapLegend
{
    public const char Wall = '#';
    public const char Floor = '.';
    public const char Water = '~';
    public const char Pit = 'O';
    public const char Door = 'D';
    public const char Exit = 'E';
    public const char Lever = 'L';
    public const char Player1Start = '1';
    public const char Player2Start = '2';
    public const char ZombieSpawn = 'Z';
    public const char HealthItem = 'h';
    public const char RewardItem = 'r';
    public const char JumpItem = 'j';
    public const char SprintItem = 's';
    public const char SwimItem = 'w';

    /// <summary>Every legend character with its meaning, for the protocol description endpoint.</summary>
    public static readonly IReadOnlyDictionary<char, string> Descriptions = new Dictionary<char, string>
    {
        [Wall] = "wall",
        [Floor] = "floor",
        [Water] = "water (needs Swim)",
        [Pit] = "pit (needs Jump)",
        [Door] = "exit door (closed until both levers are active)",
        [Exit] = "exit tile",
        [Lever] = "lever",
        [Player1Start] = "player 1 start",
        [Player2Start] = "player 2 start",
        [ZombieSpawn] = "zombie spawn",
        [HealthItem] = "health item",
        [RewardItem] = "reward item",
        [JumpItem] = "Jump power item",
        [SprintItem] = "Sprint power item",
        [SwimItem] = "Swim power item",
    };

    public static char ForTerrain(TerrainKind terrain) => terrain switch
    {
        TerrainKind.Floor => Floor,
        TerrainKind.Wall => Wall,
        TerrainKind.Water => Water,
        TerrainKind.Pit => Pit,
        TerrainKind.Door => Door,
        TerrainKind.Exit => Exit,
        _ => throw new ArgumentOutOfRangeException(nameof(terrain), terrain, null),
    };

    /// <summary>The terrain under a legend character. Entity characters stand on floor.</summary>
    public static TerrainKind TerrainOf(char c) => c switch
    {
        Wall => TerrainKind.Wall,
        Water => TerrainKind.Water,
        Pit => TerrainKind.Pit,
        Door => TerrainKind.Door,
        Exit => TerrainKind.Exit,
        Floor or Lever or Player1Start or Player2Start or ZombieSpawn
            or HealthItem or RewardItem or JumpItem or SprintItem or SwimItem => TerrainKind.Floor,
        _ => throw new FormatException($"'{c}' is not in the map legend."),
    };

    /// <summary>The power a power-item character grants, or null for other characters.</summary>
    public static PowerType? PowerOf(char c) => c switch
    {
        JumpItem => PowerType.Jump,
        SprintItem => PowerType.Sprint,
        SwimItem => PowerType.Swim,
        _ => null,
    };
}
