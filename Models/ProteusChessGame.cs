namespace ProteusChess.Models;

public enum PieceType
{
    Pyramid = 0, // Cannot move or be captured; worth 0 points
    Pawn = 1,    // Worth 2 points
    Bishop = 2,  // Worth 3 points
    Knight = 3,  // Worth 4 points
    Rook = 4,    // Worth 5 points
    Queen = 5    // Worth 6 points
}

public enum PlayerColor
{
    White,
    Black
}

public class Die
{
    public PlayerColor Owner { get; }
    public PieceType Type { get; set; }

    public Die(PlayerColor owner, PieceType type = PieceType.Pawn)
    {
        Owner = owner;
        Type = type;
    }

    public int PointValue => Type switch
    {
        PieceType.Pyramid => 0,
        PieceType.Pawn => 2,
        PieceType.Bishop => 3,
        PieceType.Knight => 4,
        PieceType.Rook => 5,
        PieceType.Queen => 6,
        _ => 0
    };

    public string Symbol => Type switch
    {
        PieceType.Pyramid => "?",
        PieceType.Pawn => Owner == PlayerColor.White ? "?" : "?",
        PieceType.Bishop => Owner == PlayerColor.White ? "?" : "?",
        PieceType.Knight => Owner == PlayerColor.White ? "?" : "?",
        PieceType.Rook => Owner == PlayerColor.White ? "?" : "?",
        PieceType.Queen => Owner == PlayerColor.White ? "?" : "?",
        _ => "?"
    };

    public string ImagePath => $"/images/pieces/{GetImageFileName()}";

    private string GetImageFileName()
    {
        char color = Owner == PlayerColor.White ? 'w' : 'b';
        char piece = Type switch
        {
            PieceType.Pyramid => 'C',
            PieceType.Pawn => 'P',
            PieceType.Bishop => 'B',
            PieceType.Knight => 'N',
            PieceType.Rook => 'R',
            PieceType.Queen => 'Q',
            _ => 'P'
        };
        return $"{color}{piece}.svg";
    }

    public bool CanRotateUp => Type != PieceType.Queen;
    public bool CanRotateDown => Type != PieceType.Pyramid;

    public void RotateUp()
    {
        if (CanRotateUp) Type = (PieceType)((int)Type + 1);
    }

    public void RotateDown()
    {
        if (CanRotateDown) Type = (PieceType)((int)Type - 1);
    }
}

public enum TurnPhase
{
    Move,    // Player must move a die
    Rotate   // Player must rotate a different die
}

public enum GameEndReason
{
    None,
    CannotMove,    // Current player cannot move — opponent wins
    OnePlayerDown  // A player has only one die left — game ends, highest score wins
}

public class ProteusChessGame
{
    public Die?[,] Board { get; } = new Die?[8, 8];
    public PlayerColor CurrentPlayer { get; private set; } = PlayerColor.White;
    public TurnPhase Phase { get; private set; } = TurnPhase.Move;
    public List<Die> WhiteCaptured { get; } = []; // Pieces white captured (from black)
    public List<Die> BlackCaptured { get; } = []; // Pieces black captured (from white)
    public int WhiteScore => WhiteCaptured.Sum(d => d.PointValue);
    public int BlackScore => BlackCaptured.Sum(d => d.PointValue);
    public bool IsGameOver { get; private set; }
    public GameEndReason EndReason { get; private set; } = GameEndReason.None;
    public PlayerColor? Winner { get; private set; }

    // Selection state
    public (int Row, int Col)? SelectedSquare { get; set; }
    public Die? MovedPiece { get; private set; } // The piece that was moved this turn (cannot be rotated)

    // Track the last move for highlighting
    public (int Row, int Col)? LastMoveFrom { get; private set; }
    public (int Row, int Col)? LastMoveTo { get; private set; }

    public string StatusMessage { get; private set; } = "White's turn: Move a piece";

    public ProteusChessGame()
    {
        SetupBoard();
        CheckForNoMoves();
    }

    private void SetupBoard()
    {
        // White pieces on the 8 dark squares in rows 0 and 1 (bottom two rows for white)
        // Black pieces on the 8 dark squares in rows 6 and 7 (top two rows for black)
        // Dark squares: (row + col) % 2 == 1

        PlaceStartingPieces(PlayerColor.White, 0, 1);
        PlaceStartingPieces(PlayerColor.Black, 6, 7);
    }

    private void PlaceStartingPieces(PlayerColor color, int row1, int row2)
    {
        foreach (int row in new[] { row1, row2 })
        {
            for (int col = 0; col < 8; col++)
            {
                if ((row + col) % 2 == 1)
                {
                    Board[row, col] = new Die(color);
                }
            }
        }
    }

    public void OnSquareClick(int row, int col)
    {
        if (IsGameOver) return;

        if (Phase == TurnPhase.Move)
            HandleMovePhase(row, col);
        else
            HandleRotatePhase(row, col);
    }

    private void HandleMovePhase(int row, int col)
    {
        var die = Board[row, col];

        if (SelectedSquare == null)
        {
            // Select a piece to move
            if (die != null && die.Owner == CurrentPlayer && die.Type != PieceType.Pyramid)
            {
                SelectedSquare = (row, col);
            }
            return;
        }

        var (sr, sc) = SelectedSquare.Value;

        // Clicking the same square deselects
        if (sr == row && sc == col)
        {
            SelectedSquare = null;
            return;
        }

        // Clicking another friendly piece switches selection
        if (die != null && die.Owner == CurrentPlayer && die.Type != PieceType.Pyramid)
        {
            SelectedSquare = (row, col);
            return;
        }

        // Try to move
        if (IsValidMove(sr, sc, row, col))
        {
            ExecuteMove(sr, sc, row, col);
        }
        else
        {
            // Invalid move — deselect
            SelectedSquare = null;
        }
    }

    private void HandleRotatePhase(int row, int col)
    {
        var die = Board[row, col];
        if (die == null || die.Owner != CurrentPlayer) return;
        if (die == MovedPiece) return; // Cannot rotate the piece that just moved

        if (SelectedSquare == null)
        {
            // Select a piece to rotate
            if (die.CanRotateUp || die.CanRotateDown)
            {
                SelectedSquare = (row, col);
            }
            return;
        }

        var (sr, sc) = SelectedSquare.Value;
        if (sr == row && sc == col)
        {
            SelectedSquare = null;
            return;
        }

        // Select different piece
        if (die.CanRotateUp || die.CanRotateDown)
        {
            SelectedSquare = (row, col);
        }
    }

    public void RotateSelectedUp()
    {
        if (Phase != TurnPhase.Rotate || SelectedSquare == null) return;
        var (r, c) = SelectedSquare.Value;
        var die = Board[r, c];
        if (die == null || die.Owner != CurrentPlayer || die == MovedPiece) return;
        if (!die.CanRotateUp) return;

        die.RotateUp();
        FinishTurn();
    }

    public void RotateSelectedDown()
    {
        if (Phase != TurnPhase.Rotate || SelectedSquare == null) return;
        var (r, c) = SelectedSquare.Value;
        var die = Board[r, c];
        if (die == null || die.Owner != CurrentPlayer || die == MovedPiece) return;
        if (!die.CanRotateDown) return;

        die.RotateDown();
        FinishTurn();
    }

    private void ExecuteMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        var movingDie = Board[fromRow, fromCol]!;
        var targetDie = Board[toRow, toCol];

        // Handle capture
        if (targetDie != null && targetDie.Owner != movingDie.Owner)
        {
            Capture(movingDie.Owner, targetDie);
        }

        Board[toRow, toCol] = movingDie;
        Board[fromRow, fromCol] = null;

        // Check for Queen backstab: moving to the square directly behind an opponent's Queen
        CheckQueenBackstab(movingDie, toRow, toCol);

        LastMoveFrom = (fromRow, fromCol);
        LastMoveTo = (toRow, toCol);
        MovedPiece = movingDie;
        SelectedSquare = null;

        // Check if opponent has only 1 piece left
        if (CountPieces(Opponent(CurrentPlayer)) <= 1)
        {
            EndGame(GameEndReason.OnePlayerDown);
            return;
        }

        // Move to rotation phase
        Phase = TurnPhase.Rotate;
        StatusMessage = $"{CurrentPlayer}'s turn: Rotate a different piece (up or down)";

        // Check if there are any pieces to rotate (other than the moved one)
        if (!HasRotatablePiece())
        {
            // Skip rotation — end turn
            FinishTurn();
        }
    }

    private void CheckQueenBackstab(Die movingDie, int toRow, int toCol)
    {
        // After moving, check all 8 neighbors and diagonals for opponent Queens
        // whose "back square" is (toRow, toCol).
        // A Queen's "back square" is the square between the Queen and her first rank.
        // White's first rank = row 0, so back is row - 1
        // Black's first rank = row 7, so back is row + 1

        // Check: is there an opponent Queen where toRow,toCol is directly behind her?
        // For a white Queen at (qr, qc): back square is (qr - 1, qc) — if qr > 0
        // For a black Queen at (qr, qc): back square is (qr + 1, qc) — if qr < 7

        // White Queen's back is at (qr-1, qc). If movingDie moved to (qr-1, qc), check (qr-1+1, qc) = (qr, qc) for white queen.
        // So if we moved to (toRow, toCol), check if there's a White Queen at (toRow+1, toCol) whose back is (toRow, toCol).
        // And check if there's a Black Queen at (toRow-1, toCol) whose back is (toRow, toCol).

        // Check for opponent White Queen at (toRow+1, toCol)
        if (movingDie.Owner == PlayerColor.Black && toRow + 1 < 8)
        {
            var candidate = Board[toRow + 1, toCol];
            if (candidate != null && candidate.Owner == PlayerColor.White && candidate.Type == PieceType.Queen
                && toRow + 1 > 0) // Queen not on her first rank (row 0)
            {
                // Backstab! Capture the Queen
                Board[toRow + 1, toCol] = null;
                Capture(PlayerColor.Black, candidate);
            }
        }

        // Check for opponent Black Queen at (toRow-1, toCol)
        if (movingDie.Owner == PlayerColor.White && toRow - 1 >= 0)
        {
            var candidate = Board[toRow - 1, toCol];
            if (candidate != null && candidate.Owner == PlayerColor.Black && candidate.Type == PieceType.Queen
                && toRow - 1 < 7) // Queen not on her first rank (row 7)
            {
                // Backstab! Capture the Queen
                Board[toRow - 1, toCol] = null;
                Capture(PlayerColor.White, candidate);
            }
        }
    }

    private void Capture(PlayerColor capturer, Die captured)
    {
        if (capturer == PlayerColor.White)
            WhiteCaptured.Add(captured);
        else
            BlackCaptured.Add(captured);
    }

    private void FinishTurn()
    {
        SelectedSquare = null;
        MovedPiece = null;
        CurrentPlayer = Opponent(CurrentPlayer);
        Phase = TurnPhase.Move;

        // Check if the new current player can move
        if (!CheckForNoMoves())
        {
            StatusMessage = $"{CurrentPlayer}'s turn: Move a piece";
        }
    }

    /// <summary>
    /// Returns true if the game ended due to no moves.
    /// </summary>
    private bool CheckForNoMoves()
    {
        if (!HasAnyValidMove(CurrentPlayer))
        {
            // Current player cannot move — opponent wins
            Winner = Opponent(CurrentPlayer);
            EndGame(GameEndReason.CannotMove);
            return true;
        }
        return false;
    }

    private void EndGame(GameEndReason reason)
    {
        IsGameOver = true;
        EndReason = reason;

        if (reason == GameEndReason.CannotMove)
        {
            Winner = Opponent(CurrentPlayer);
            StatusMessage = $"Game Over! {Winner} wins — {CurrentPlayer} cannot move!";
        }
        else if (reason == GameEndReason.OnePlayerDown)
        {
            if (WhiteScore > BlackScore)
                Winner = PlayerColor.White;
            else if (BlackScore > WhiteScore)
                Winner = PlayerColor.Black;
            else
                Winner = null; // tie

            StatusMessage = Winner != null
                ? $"Game Over! {Winner} wins {WhiteScore}–{BlackScore}!"
                : $"Game Over! It's a tie {WhiteScore}–{BlackScore}!";
        }
    }

    private static PlayerColor Opponent(PlayerColor color) =>
        color == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;

    private int CountPieces(PlayerColor color)
    {
        int count = 0;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (Board[r, c]?.Owner == color) count++;
        return count;
    }

    private bool HasRotatablePiece()
    {
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var d = Board[r, c];
                if (d != null && d.Owner == CurrentPlayer && d != MovedPiece
                    && (d.CanRotateUp || d.CanRotateDown))
                    return true;
            }
        return false;
    }

    // ---------- Move validation ----------

    public bool HasAnyValidMove(PlayerColor color)
    {
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var d = Board[r, c];
                if (d != null && d.Owner == color && d.Type != PieceType.Pyramid)
                {
                    if (GetValidMoves(r, c).Count > 0) return true;
                }
            }
        return false;
    }

    public List<(int Row, int Col)> GetValidMoves(int fromRow, int fromCol)
    {
        var die = Board[fromRow, fromCol];
        if (die == null) return [];

        return die.Type switch
        {
            PieceType.Pyramid => [],
            PieceType.Pawn => GetPawnMoves(fromRow, fromCol, die),
            PieceType.Bishop => GetBishopMoves(fromRow, fromCol, die),
            PieceType.Knight => GetKnightMoves(fromRow, fromCol, die),
            PieceType.Rook => GetRookMoves(fromRow, fromCol, die),
            PieceType.Queen => GetQueenMoves(fromRow, fromCol, die),
            _ => []
        };
    }

    public bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        return GetValidMoves(fromRow, fromCol).Contains((toRow, toCol));
    }

    private List<(int Row, int Col)> GetPawnMoves(int row, int col, Die die)
    {
        var moves = new List<(int, int)>();
        int direction = die.Owner == PlayerColor.White ? 1 : -1;

        // Forward one
        int newRow = row + direction;
        if (InBounds(newRow, col) && Board[newRow, col] == null)
        {
            moves.Add((newRow, col));

            // Forward two from starting squares
            if (IsOnStartingSquares(row, die.Owner))
            {
                int newRow2 = row + 2 * direction;
                if (InBounds(newRow2, col) && Board[newRow2, col] == null)
                {
                    moves.Add((newRow2, col));
                }
            }
        }

        // Diagonal captures
        foreach (int dc in new[] { -1, 1 })
        {
            int nc = col + dc;
            if (InBounds(newRow, nc))
            {
                var target = Board[newRow, nc];
                if (target != null && target.Owner != die.Owner && target.Type != PieceType.Pyramid)
                {
                    moves.Add((newRow, nc));
                }
            }
        }

        return moves;
    }

    private bool IsOnStartingSquares(int row, PlayerColor color)
    {
        // White starting squares: rows 0 and 1
        // Black starting squares: rows 6 and 7
        return color == PlayerColor.White ? row is 0 or 1 : row is 6 or 7;
    }

    private List<(int Row, int Col)> GetBishopMoves(int row, int col, Die die)
    {
        var moves = new List<(int, int)>();
        int[][] directions = [[1, 1], [1, -1], [-1, 1], [-1, -1]];
        foreach (var dir in directions)
            AddSlidingMoves(moves, row, col, dir[0], dir[1], die);
        return moves;
    }

    private List<(int Row, int Col)> GetKnightMoves(int row, int col, Die die)
    {
        var moves = new List<(int, int)>();
        int[][] offsets = [[2, 1], [2, -1], [-2, 1], [-2, -1], [1, 2], [1, -2], [-1, 2], [-1, -2]];
        foreach (var off in offsets)
        {
            int nr = row + off[0], nc = col + off[1];
            if (!InBounds(nr, nc)) continue;
            var target = Board[nr, nc];
            if (target == null)
                moves.Add((nr, nc));
            else if (target.Owner != die.Owner && target.Type != PieceType.Pyramid)
                moves.Add((nr, nc));
        }
        return moves;
    }

    private List<(int Row, int Col)> GetRookMoves(int row, int col, Die die)
    {
        var moves = new List<(int, int)>();
        int[][] directions = [[1, 0], [-1, 0], [0, 1], [0, -1]];
        foreach (var dir in directions)
            AddSlidingMoves(moves, row, col, dir[0], dir[1], die);
        return moves;
    }

    private List<(int Row, int Col)> GetQueenMoves(int row, int col, Die die)
    {
        var moves = new List<(int, int)>();
        int[][] directions = [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, 1], [-1, -1]];
        foreach (var dir in directions)
            AddSlidingMoves(moves, row, col, dir[0], dir[1], die);
        return moves;
    }

    private void AddSlidingMoves(List<(int, int)> moves, int row, int col, int dr, int dc, Die die)
    {
        int r = row + dr, c = col + dc;
        while (InBounds(r, c))
        {
            var target = Board[r, c];
            if (target == null)
            {
                moves.Add((r, c));
            }
            else if (target.Owner != die.Owner && target.Type != PieceType.Pyramid)
            {
                moves.Add((r, c));
                break; // Can capture but can't go further
            }
            else
            {
                break; // Blocked by friendly piece or Pyramid
            }
            r += dr;
            c += dc;
        }
    }

    private static bool InBounds(int row, int col) =>
        row >= 0 && row < 8 && col >= 0 && col < 8;

    // ---------- Public helpers for UI ----------

    public bool IsValidMoveTarget(int row, int col)
    {
        if (Phase != TurnPhase.Move || SelectedSquare == null) return false;
        var (sr, sc) = SelectedSquare.Value;
        return IsValidMove(sr, sc, row, col);
    }

    public bool IsRotateCandidate(int row, int col)
    {
        if (Phase != TurnPhase.Rotate) return false;
        var d = Board[row, col];
        return d != null && d.Owner == CurrentPlayer && d != MovedPiece
               && (d.CanRotateUp || d.CanRotateDown);
    }

    public void NewGame()
    {
        // Clear board
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                Board[r, c] = null;

        WhiteCaptured.Clear();
        BlackCaptured.Clear();
        CurrentPlayer = PlayerColor.White;
        Phase = TurnPhase.Move;
        IsGameOver = false;
        EndReason = GameEndReason.None;
        Winner = null;
        SelectedSquare = null;
        MovedPiece = null;
        LastMoveFrom = null;
        LastMoveTo = null;
        StatusMessage = "White's turn: Move a piece";

        SetupBoard();
        CheckForNoMoves();
    }
}
