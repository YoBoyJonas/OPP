using CasteEscapeServer.Models;

namespace CasteEscapeServer.Game;

public static class Characters
{
    public const string WarriorId = "warrior";
    public const string ScoutId = "scout";
    public const string SwimmerId = "swimmer";

    public static PlayerState Create(string characterId, string connectionId, string name)
    {
        var (maxHealth, speed) = characterId switch
        {
            ScoutId => (3, 6.0),
            SwimmerId => (4, 4.5),
            _ => (5, 3.5) // WarriorId and unknown ids fall back to Warrior
        };

        return new PlayerState
        {
            ConnectionId = connectionId,
            Name = name,
            CharacterId = characterId == ScoutId || characterId == SwimmerId ? characterId : WarriorId,
            Health = maxHealth,
            MaxHealth = maxHealth,
            MoveSpeed = speed
        };
    }
}
