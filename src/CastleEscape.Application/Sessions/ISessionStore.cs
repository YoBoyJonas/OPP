namespace CastleEscape.Application.Sessions;

public interface ISessionStore
{
    IReadOnlyCollection<GameSession> All { get; }
    void Add(GameSession session);
    GameSession? Find(Guid id);
}
