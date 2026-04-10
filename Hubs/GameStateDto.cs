namespace ProteusChess.Hubs;

using ProteusChess.Models;

public record PieceDto(PlayerColor Owner, PieceType Type, int PointValue, string ImagePath, bool CanRotateUp, bool CanRotateDown);

public class GameStateDto
{
    public PieceDto?[][] Board { get; set; } = [];
    public PlayerColor CurrentPlayer { get; set; }
    public TurnPhase Phase { get; set; }
    public bool IsGameOver { get; set; }
    public GameEndReason EndReason { get; set; }
    public PlayerColor? Winner { get; set; }
    public string StatusMessage { get; set; } = "";
    public int WhiteScore { get; set; }
    public int BlackScore { get; set; }
    public List<PieceDto> WhiteCaptured { get; set; } = [];
    public List<PieceDto> BlackCaptured { get; set; } = [];
    public int[]? LastMoveFrom { get; set; }
    public int[]? LastMoveTo { get; set; }
    public int[]? MovedPieceLocation { get; set; }
    public string RoomCode { get; set; } = "";
    public bool IsFull { get; set; }

    public static GameStateDto FromRoom(GameRoom room)
    {
        var board = new PieceDto?[8][];
        for (int r = 0; r < 8; r++)
        {
            board[r] = new PieceDto?[8];
            for (int c = 0; c < 8; c++)
            {
                var p = room.Game.Board[r, c];
                if (p != null)
                    board[r][c] = new PieceDto(p.Owner, p.Type, p.PointValue, p.ImagePath, p.CanRotateUp, p.CanRotateDown);
            }
        }

        return new GameStateDto
        {
            Board = board,
            CurrentPlayer = room.Game.CurrentPlayer,
            Phase = room.Game.Phase,
            IsGameOver = room.Game.IsGameOver,
            EndReason = room.Game.EndReason,
            Winner = room.Game.Winner,
            StatusMessage = room.Game.StatusMessage,
            WhiteScore = room.Game.WhiteScore,
            BlackScore = room.Game.BlackScore,
            WhiteCaptured = room.Game.WhiteCaptured.Select(p => new PieceDto(p.Owner, p.Type, p.PointValue, p.ImagePath, p.CanRotateUp, p.CanRotateDown)).ToList(),
            BlackCaptured = room.Game.BlackCaptured.Select(p => new PieceDto(p.Owner, p.Type, p.PointValue, p.ImagePath, p.CanRotateUp, p.CanRotateDown)).ToList(),
            LastMoveFrom = room.Game.LastMoveFrom.HasValue ? [room.Game.LastMoveFrom.Value.Row, room.Game.LastMoveFrom.Value.Col] : null,
            LastMoveTo = room.Game.LastMoveTo.HasValue ? [room.Game.LastMoveTo.Value.Row, room.Game.LastMoveTo.Value.Col] : null,
            MovedPieceLocation = room.Game.LastMoveTo.HasValue ? [room.Game.LastMoveTo.Value.Row, room.Game.LastMoveTo.Value.Col] : null,
            RoomCode = room.RoomCode,
            IsFull = room.IsFull
        };
    }
}
