namespace CastleEscape.Contracts.Realtime;

/// <summary>Methods the server calls on clients (<c>connection.on(...)</c>).</summary>
public static class ClientMethods
{
    public const string SessionUpdated = nameof(SessionUpdated);
    public const string LevelStarted = nameof(LevelStarted);
    public const string StateUpdated = nameof(StateUpdated);
    public const string GameEvent = nameof(GameEvent);
    public const string Error = nameof(Error);
    public const string Pong = nameof(Pong);
}

/// <summary>Methods clients call on the hub (<c>connection.invoke(...)</c>).</summary>
public static class HubMethods
{
    public const string SetDirection = nameof(SetDirection);
    public const string RequestRestart = nameof(RequestRestart);
    public const string Ping = nameof(Ping);
}
