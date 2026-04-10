namespace ProteusChess.Hubs;

using ProteusChess.Models;

public class GameRoom
{
    public string RoomCode { get; } = GenerateCode();
    public ProteusChessGame Game { get; } = new();
    public string? WhiteConnectionId { get; set; }
    public string? BlackConnectionId { get; set; }

    public PlayerColor? GetPlayerColor(string connectionId)
    {
        if (connectionId == WhiteConnectionId) return PlayerColor.White;
        if (connectionId == BlackConnectionId) return PlayerColor.Black;
        return null;
    }

    public bool IsFull => WhiteConnectionId != null && BlackConnectionId != null;

    private static string GenerateCode()
    {
        return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }
}
