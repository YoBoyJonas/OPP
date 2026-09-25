namespace CasteEscapeServer.Models;

public class JoinResult
{
    public string SessionId { get; set; } = string.Empty;
    public int PlayerIndex { get; set; }
    public bool Started { get; set; }
}
