using System.Runtime.CompilerServices;
namespace ChessGui;
class Board
{
    public Piece[,] Squares;
    public PieceColor CurrentTurn;
    public List<MoveRecord> MoveHistory = new List<MoveRecord>();

    public Board()
    {
        Squares = new Piece[8, 8];
        CurrentTurn = PieceColor.White;
        InitBoard();
    }

    void InitBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Squares[row, col] = new Piece(PieceType.Empty, PieceColor.None);
            }
        }

        for (int col = 0; col < 8; col++)
        {
            Squares[1, col] = new Piece(PieceType.Pawn, PieceColor.Black);
            Squares[6, col] = new Piece(PieceType.Pawn, PieceColor.White);
        }

        Squares[0, 0] = new Piece(PieceType.Rook, PieceColor.Black);
        Squares[0, 1] = new Piece(PieceType.Knight, PieceColor.Black);
        Squares[0, 2] = new Piece(PieceType.Bishop, PieceColor.Black);
        Squares[0, 3] = new Piece(PieceType.Queen, PieceColor.Black);
        Squares[0, 4] = new Piece(PieceType.King, PieceColor.Black);
        Squares[0, 5] = new Piece(PieceType.Bishop, PieceColor.Black);
        Squares[0, 6] = new Piece(PieceType.Knight, PieceColor.Black);
        Squares[0, 7] = new Piece(PieceType.Rook, PieceColor.Black);

        Squares[7, 0] = new Piece(PieceType.Rook, PieceColor.White);
        Squares[7, 1] = new Piece(PieceType.Knight, PieceColor.White);
        Squares[7, 2] = new Piece(PieceType.Bishop, PieceColor.White);
        Squares[7, 3] = new Piece(PieceType.Queen, PieceColor.White);
        Squares[7, 4] = new Piece(PieceType.King, PieceColor.White);
        Squares[7, 5] = new Piece(PieceType.Bishop, PieceColor.White);
        Squares[7, 6] = new Piece(PieceType.Knight, PieceColor.White);
        Squares[7, 7] = new Piece(PieceType.Rook, PieceColor.White);
    }

    public void Print()
{
    Console.WriteLine();
    Console.WriteLine("    a  b  c  d  e  f  g  h");
    Console.WriteLine("   +------------------------+");

    var originalBg = Console.BackgroundColor;
    var originalFg = Console.ForegroundColor;

    for (int row = 0; row < 8; row++)
    {
        Console.Write((8 - row) + " | ");

        for (int col = 0; col < 8; col++)
        {
            bool lightSquare = ((row + col) % 2 == 0);
                Console.BackgroundColor = lightSquare ? ConsoleColor.White : ConsoleColor.DarkGreen;

            Piece p = Squares[row, col];

            // Determine foreground so white pieces appear lighter than black pieces
            ConsoleColor fore;
            if (p.Type == PieceType.Empty)
            {
                fore = lightSquare ? ConsoleColor.Black : ConsoleColor.White;
            }
            else if (p.Color == PieceColor.White)
            {
                // White pieces: dark on white squares, white on green squares
                fore = lightSquare ? ConsoleColor.DarkYellow : ConsoleColor.White;
            }
            else // black piece
            {
                // Black pieces: use black so they appear dark on both square colors
                fore = ConsoleColor.Black;
            }

            Console.ForegroundColor = fore;

            if (p.Type == PieceType.Empty)
            {
                Console.Write("   ");
            }
            else
            {
                string sym = GetSymbol(p);
                Console.Write(" " + sym + " ");
            }

            // Reset colors for next output
            Console.BackgroundColor = originalBg;
            Console.ForegroundColor = originalFg;
        }

        Console.WriteLine(" | " + (8 - row));
    }

    Console.WriteLine("   +------------------------+");
    Console.WriteLine("    a  b  c  d  e  f  g  h");
    Console.WriteLine();
}

    string GetSymbol(Piece piece)
    {
        if (piece.Type == PieceType.Empty)
            return ".";

        // Use correct Unicode glyphs for white vs black pieces
        if (piece.Color == PieceColor.White)
        {
            return piece.Type switch
            {
                PieceType.Pawn => "♙",
                PieceType.Rook => "♖",
                PieceType.Knight => "♘",
                PieceType.Bishop => "♗",
                PieceType.Queen => "♕",
                PieceType.King => "♔",
                _=> "?"
            };
        }

        return piece.Type switch
        {
            PieceType.Pawn => "♟",
            PieceType.Rook => "♜",
            PieceType.Knight => "♞",
            PieceType.Bishop => "♝",
            PieceType.Queen => "♛",
            PieceType.King => "♚",
            _=> "?"
        };
    }

    public bool MovePiece(Move move)
    {
        if (!IsInsideBoard(move.FromRow, move.FromCol) || !IsInsideBoard(move.ToRow, move.ToCol))
            return false;

        Piece piece = Squares[move.FromRow, move.FromCol];
        Piece target = Squares[move.ToRow, move.ToCol];

        if (piece.Type == PieceType.Empty)
            return false;

        if (piece.Color != CurrentTurn)
            return false;

        if (target.Color == piece.Color && target.Color != PieceColor.None)
            return false;

        bool valid = false;
        if(piece.Type == PieceType.King&&Math.Abs(move.ToCol - move.FromCol) == 2)
        {
            return TryCastle(move);
        }
        if (piece.Type == PieceType.Pawn)
            valid = IsValidPawnMove(move, piece);
        else if (piece.Type == PieceType.Rook)
            valid = IsValidRookMove(move);
        else if (piece.Type == PieceType.Bishop)
            valid = IsValidBishopMove(move);
        else if (piece.Type == PieceType.Queen)
            valid = IsValidQueenMove(move);
        else if (piece.Type == PieceType.Knight)
            valid = IsValidKnightMove(move);
        else if (piece.Type == PieceType.King)
            valid = IsValidKingMove(move);

        if (!valid)
            return false;

        if (WouldLeaveKingInCheck(move))
            return false;

        if (!DoesMoveSolvePinned(move))
            return false;

        // Detect en passant capture and prepare correct captured piece record
        MoveRecord record;
        bool isEnPassantMove = false;
        Move? enPassantCapturedPawnMove = null;
        Piece? enPassantCapturedPawn = null;
        if (piece.Type == PieceType.Pawn)
        {
            int rowDiff = move.ToRow - move.FromRow;
            int colDiff = move.ToCol - move.FromCol;
            if (isEnPassant(move, piece, rowDiff, colDiff))
            {
                isEnPassantMove = true;
                Piece capturedPawnOnBoard = Squares[move.FromRow, move.ToCol];
                enPassantCapturedPawn = new Piece(capturedPawnOnBoard.Type, capturedPawnOnBoard.Color);
                enPassantCapturedPawn.HasMoved = capturedPawnOnBoard.HasMoved;
                enPassantCapturedPawnMove = new Move(move.FromRow, move.ToCol, move.ToRow, move.ToCol);
            }
        }
        record = new MoveRecord(
            move,
            new Piece(piece.Type, piece.Color),
            new Piece(target.Type, target.Color),
            CurrentTurn
        );
        if (isEnPassantMove)
        {
            record.WasEnPassant = true;
            record.EnPassantCapturedPawnMove = enPassantCapturedPawnMove;
            record.EnPassantCapturedPawn = enPassantCapturedPawn;
        }

        ApplyMove(move);
        MoveHistory.Add(record);

        if (CurrentTurn == PieceColor.White)
            CurrentTurn = PieceColor.Black;
        else
            CurrentTurn = PieceColor.White;

        return true;
    }
    private void ApplyMove(Move move)
    {
        Piece piece = Squares[move.FromRow, move.FromCol];

        // Handle en passant capture: moving pawn diagonally to an empty square
        if (piece.Type == PieceType.Pawn)
        {
            int rowDiff = move.ToRow - move.FromRow;
            int colDiff = move.ToCol - move.FromCol;
            if (Math.Abs(colDiff) == 1 && rowDiff != 0 && Squares[move.ToRow, move.ToCol].Type == PieceType.Empty && MoveHistory.Count > 0)
            {
                MoveRecord last = MoveHistory[MoveHistory.Count - 1];
                if (last.MovedPiece.Type == PieceType.Pawn && Math.Abs(last.Move.ToRow - last.Move.FromRow) == 2 && last.Move.ToRow == move.FromRow && last.Move.ToCol == move.ToCol)
                {
                    // Remove the captured pawn that sits next to the from-square
                    Squares[move.FromRow, move.ToCol] = new Piece(PieceType.Empty, PieceColor.None);
                }
            }
        }

        Squares[move.ToRow, move.ToCol] = piece;
        Squares[move.FromRow, move.FromCol] = new Piece(PieceType.Empty, PieceColor.None);
        piece.HasMoved = true;
    }
    private bool TryCastle(Move move)
    {
        Piece king = Squares[move.FromRow, move.FromCol];
        if (king.HasMoved)
            return false;
        int row = move.FromRow;
        bool isKingside = move.ToCol > move.FromCol;
        int rookCol = isKingside ? 7 : 0;
        int rookTargetCol = isKingside ? 5 : 3;
        int kingTargetCol = isKingside ? 6 : 2;

        Piece rook = Squares[row, rookCol];
        if(rook.Type != PieceType.Rook || rook.HasMoved)
            return false;
        int startCol = Math.Min(move.FromCol, rookCol) + 1;
        int endCol = Math.Max(move.FromCol, rookCol) - 1;
        for(int col = startCol; col <= endCol; col++)
        {
            if(Squares[row, col].Type != PieceType.Empty)
                return false;
        }
        if(IsKingInCheck(CurrentTurn))
            return false;
        PieceColor enemyColor = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        int step = isKingside ? 1 : -1;
        for(int col = move.FromCol + step; col != kingTargetCol + step; col += step)
        {
            if(IsSquareAttacked(row, col, enemyColor))
                return false;
        }
        Move rookMove = new Move(row, rookCol, row, rookTargetCol);
        Move kingMove = new Move(row, move.FromCol, row, kingTargetCol);
        MoveRecord kingRecord = new MoveRecord(
            kingMove,
            new Piece(king.Type, king.Color),
            new Piece(Squares[kingMove.ToRow, kingMove.ToCol].Type, Squares[kingMove.ToRow, kingMove.ToCol].Color),
            CurrentTurn
        );
        kingRecord.WasCastle = true;
        kingRecord.RookMove = rookMove;
        kingRecord.RookPiece = new Piece(rook.Type, rook.Color);
        kingRecord.RookCapturedPiece = new Piece(Squares[rookMove.ToRow, rookMove.ToCol].Type, Squares[rookMove.ToRow, rookMove.ToCol].Color);
        MoveHistory.Add(kingRecord);
        ApplyMove(kingMove);
        ApplyMove(rookMove);
        CurrentTurn = OpponentColor(CurrentTurn);
        return true;

    }
    public void UndoMove()
    {
        if (MoveHistory.Count == 0)
            return;

        MoveRecord last = MoveHistory[MoveHistory.Count - 1];
        MoveHistory.RemoveAt(MoveHistory.Count - 1);

        if (last.WasEnPassant && last.EnPassantCapturedPawnMove != null && last.EnPassantCapturedPawn != null)
        {
            // Restore the moved pawn to its original square
            Squares[last.Move.FromRow, last.Move.FromCol] = last.MovedPiece;
            Squares[last.Move.FromRow, last.Move.FromCol].HasMoved = last.MovedPieceHasMoved;

            // The destination square was empty before the move
            Squares[last.Move.ToRow, last.Move.ToCol] = new Piece(PieceType.Empty, PieceColor.None);

            // Restore the captured pawn to its original square
            var capMove = last.EnPassantCapturedPawnMove;
            Squares[capMove.FromRow, capMove.FromCol] = last.EnPassantCapturedPawn;
            Squares[capMove.FromRow, capMove.FromCol].HasMoved = last.EnPassantCapturedPawn.HasMoved;
        }
        else
        {
            Squares[last.Move.FromRow, last.Move.FromCol] = last.MovedPiece;
            Squares[last.Move.FromRow, last.Move.FromCol].HasMoved = last.MovedPieceHasMoved;
            Squares[last.Move.ToRow, last.Move.ToCol] = last.CapturedPiece;
            Squares[last.Move.ToRow, last.Move.ToCol].HasMoved = last.CapturedPieceHasMoved;
        }

        if (last.WasCastle && last.RookMove != null && last.RookPiece != null && last.RookCapturedPiece != null)
        {
            Move rookMove = last.RookMove;

            Squares[rookMove.FromRow, rookMove.FromCol] = last.RookPiece;
            Squares[rookMove.ToRow, rookMove.ToCol] = last.RookCapturedPiece;
        }
        

        CurrentTurn = last.PreviousTurn;
    }
    public bool WouldLeaveKingInCheck(Move move)
    {
        Piece movedPiece = Squares[move.FromRow, move.FromCol];
        Piece capturedPiece = Squares[move.ToRow, move.ToCol];
        bool originalHasMoved = movedPiece.HasMoved;

        Piece? enPassantCapturedPawn = null;
        int enPassantRow = -1;
        int enPassantCol = -1;
        if (movedPiece.Type == PieceType.Pawn)
        {
            int rowDiff = move.ToRow - move.FromRow;
            int colDiff = move.ToCol - move.FromCol;
            if (isEnPassant(move, movedPiece, rowDiff, colDiff))
            {
                enPassantCapturedPawn = Squares[move.FromRow, move.ToCol];
                enPassantRow = move.FromRow;
                enPassantCol = move.ToCol;
            }
        }

        ApplyMove(move);

        bool inCheck = IsKingInCheck(movedPiece.Color);

        Squares[move.FromRow, move.FromCol] = movedPiece;
        movedPiece.HasMoved = originalHasMoved;

        if (enPassantCapturedPawn != null)
        {
            Squares[move.ToRow, move.ToCol] = new Piece(PieceType.Empty, PieceColor.None);
            Squares[enPassantRow, enPassantCol] = enPassantCapturedPawn;
        }
        else
        {
            Squares[move.ToRow, move.ToCol] = capturedPiece;
        }

        return inCheck;
    }

    public void PrintMoveHistory()
    {
        Console.WriteLine("Move History:");

        if (MoveHistory.Count == 0)
        {
            Console.WriteLine("No moves yet.");
            return;
        }

        foreach (var record in MoveHistory)
        {
            string text =
                $"{record.MovedPiece.Color} {record.MovedPiece.Type} " +
                $"from {(char)('a' + record.Move.FromCol)}{8 - record.Move.FromRow} " +
                $"to {(char)('a' + record.Move.ToCol)}{8 - record.Move.ToRow}";

            if (record.CapturedPiece.Type != PieceType.Empty)
            {
                text += $" capturing {record.CapturedPiece.Color} {record.CapturedPiece.Type}";
            }

            Console.WriteLine(text);
        }
    }

    bool IsValidPawnMove(Move move, Piece pawn)
    {
        int direction = pawn.Color == PieceColor.White ? -1 : 1;

        int rowDiff = move.ToRow - move.FromRow;
        int colDiff = move.ToCol - move.FromCol;

        Piece target = Squares[move.ToRow, move.ToCol];

        if (colDiff == 0 && rowDiff == direction)
        {
            return target.Type == PieceType.Empty;
        }

        int startRow = pawn.Color == PieceColor.White ? 6 : 1;

        if (colDiff == 0 && rowDiff == 2 * direction && move.FromRow == startRow)
        {
            int middleRow = move.FromRow + direction;

            return Squares[middleRow, move.FromCol].Type == PieceType.Empty &&
                   target.Type == PieceType.Empty;
        }

        if (Math.Abs(colDiff) == 1 && rowDiff == direction)
        {
            if (target.Type != PieceType.Empty && target.Color != pawn.Color)
                return true;
            if (isEnPassant(move, pawn, rowDiff, colDiff))
                return true;
        }
        return false;
        
    }
    bool isEnPassant(Move move, Piece pawn, int rowDiff, int colDiff)
    {
        if(MoveHistory.Count == 0)
            return false;
       int direction = pawn.Color == PieceColor.White ? -1 : 1;
       if(Math.Abs(colDiff) != 1 || rowDiff != direction)
        {
            return false;
        }
        if(Squares[move.ToRow, move.ToCol].Type != PieceType.Empty)
        {
            return false;
        }
        MoveRecord lastMove = MoveHistory[MoveHistory.Count - 1];
        if(lastMove.MovedPiece.Type != PieceType.Pawn)
        {
            return false;
        }
        if(Math.Abs(lastMove.Move.ToRow - lastMove.Move.FromRow) != 2)
        {
            return false;
        }
        if(lastMove.Move.ToRow != move.FromRow)
        {
            return false;
        }
        if(lastMove.Move.ToCol != move.ToCol)
        {
            return false;
        }
        return true;
    }

    bool IsValidRookMove(Move move)
    {
        bool sameRow = move.FromRow == move.ToRow;
        bool sameCol = move.FromCol == move.ToCol;

        if (!sameRow && !sameCol)
            return false;

        if (move.FromRow == move.ToRow && move.FromCol == move.ToCol)
            return false;

        return IsPathClear(move);
    }

    bool IsValidBishopMove(Move move)
    {
        int rowDiff = Math.Abs(move.ToRow - move.FromRow);
        int colDiff = Math.Abs(move.ToCol - move.FromCol);

        if (rowDiff != colDiff)
            return false;

        if (rowDiff == 0)
            return false;

        return IsPathClear(move);
    }

    bool IsValidQueenMove(Move move)
    {
        return IsValidRookMove(move) || IsValidBishopMove(move);
    }

    bool IsValidKnightMove(Move move)
    {
        int rowDiff = Math.Abs(move.ToRow - move.FromRow);
        int colDiff = Math.Abs(move.ToCol - move.FromCol);

        return (rowDiff == 2 && colDiff == 1) ||
               (rowDiff == 1 && colDiff == 2);
    }

    bool IsValidKingMove(Move move)
    {
        int rowDiff = Math.Abs(move.ToRow - move.FromRow);
        int colDiff = Math.Abs(move.ToCol - move.FromCol);

        if (rowDiff == 0 && colDiff == 0)
            return false;

        return rowDiff <= 1 && colDiff <= 1;
    }

    public (int row, int col) FindKing(PieceColor color)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Piece piece = Squares[row, col];

                if (piece.Type == PieceType.King && piece.Color == color)
                    return (row, col);
            }
        }

        return (-1, -1);
    }

    public bool IsKingInCheck(PieceColor kingColor)
    {
        var kingPos = FindKing(kingColor);

        if (kingPos.row == -1)
            return false;

        PieceColor enemyColor = kingColor == PieceColor.White
            ? PieceColor.Black
            : PieceColor.White;

        return IsSquareAttacked(kingPos.row, kingPos.col, enemyColor);
    }

    public bool IsSquareAttacked(int targetRow, int targetCol, PieceColor attackerColor)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Piece piece = Squares[row, col];

                if (piece.Color == attackerColor)
                {
                    if (IsSquareAttackedByPiece(targetRow, targetCol, row, col))
                        return true;
                }
            }
        }

        return false;
    }

    public bool IsSquareAttackedByPiece(int targetRow, int targetCol, int attackerRow, int attackerCol)
    {
        Piece attacker = Squares[attackerRow, attackerCol];

        if (attacker.Type == PieceType.Empty)
            return false;

        Move move = new Move(attackerRow, attackerCol, targetRow, targetCol);

        if (attacker.Type == PieceType.Pawn)
            return IsPawnAttacking(move, attacker);
        else if (attacker.Type == PieceType.Rook)
            return IsValidRookMove(move);
        else if (attacker.Type == PieceType.Bishop)
            return IsValidBishopMove(move);
        else if (attacker.Type == PieceType.Queen)
            return IsValidQueenMove(move);
        else if (attacker.Type == PieceType.Knight)
            return IsValidKnightMove(move);
        else if (attacker.Type == PieceType.King)
            return IsValidKingAttack(move);

        return false;
    }

    bool IsPawnAttacking(Move move, Piece pawn)
    {
        int direction = pawn.Color == PieceColor.White ? -1 : 1;

        int rowDiff = move.ToRow - move.FromRow;
        int colDiff = move.ToCol - move.FromCol;

        return rowDiff == direction && Math.Abs(colDiff) == 1;
    }

    bool IsValidKingAttack(Move move)
    {
        int rowDiff = Math.Abs(move.ToRow - move.FromRow);
        int colDiff = Math.Abs(move.ToCol - move.FromCol);

        if (rowDiff == 0 && colDiff == 0)
            return false;

        return rowDiff <= 1 && colDiff <= 1;
    }

    bool IsPathClear(Move move)
    {
        int rowStep = Math.Sign(move.ToRow - move.FromRow);
        int colStep = Math.Sign(move.ToCol - move.FromCol);

        int currentRow = move.FromRow + rowStep;
        int currentCol = move.FromCol + colStep;

        while (currentRow != move.ToRow || currentCol != move.ToCol)
        {
            if (Squares[currentRow, currentCol].Type != PieceType.Empty)
                return false;

            currentRow += rowStep;
            currentCol += colStep;
        }

        return true;
    }

    bool IsInsideBoard(int row, int col)
    {
        return row >= 0 && row < 8 && col >= 0 && col < 8;
    }

    public List<(int row, int col)> GetPinningAttackers(int pieceRow, int pieceCol)
    {
        var result = new List<(int row, int col)>();
        var kingPos = FindKing(CurrentTurn);
        
        if (kingPos.row == -1)
            return result;

        Piece piece = Squares[pieceRow, pieceCol];
        
        if (piece.Type == PieceType.Empty || piece.Color == PieceColor.None)
            return result;

        PieceColor enemyColor = CurrentTurn == PieceColor.White
            ? PieceColor.Black
            : PieceColor.White;

        // Check all 8 directions from king
        int[][] directions = new int[][]
        {
            new int[] {-1, -1}, new int[] {-1, 0}, new int[] {-1, 1},
            new int[] {0, -1},                      new int[] {0, 1},
            new int[] {1, -1},  new int[] {1, 0},  new int[] {1, 1}
        };

        foreach (var dir in directions)
        {
            int currentRow = kingPos.row + dir[0];
            int currentCol = kingPos.col + dir[1];
            bool foundPiece = false;

            while (IsInsideBoard(currentRow, currentCol))
            {
                Piece current = Squares[currentRow, currentCol];

                if (current.Type != PieceType.Empty)
                {
                    if (!foundPiece)
                    {
                        if (currentRow == pieceRow && currentCol == pieceCol)
                        {
                            foundPiece = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (current.Color == enemyColor)
                    {
                        // Temporarily remove the pinned piece to check if attacker can reach king
                        Piece tempPiece = Squares[pieceRow, pieceCol];
                        Squares[pieceRow, pieceCol] = new Piece(PieceType.Empty, PieceColor.None);
                        
                        Move checkMove = new Move(currentRow, currentCol, kingPos.row, kingPos.col);
                        bool isPinner = false;
                        
                        if (current.Type == PieceType.Rook && IsValidRookMove(checkMove))
                            isPinner = true;
                        else if (current.Type == PieceType.Bishop && IsValidBishopMove(checkMove))
                            isPinner = true;
                        else if (current.Type == PieceType.Queen && IsValidQueenMove(checkMove))
                            isPinner = true;
                        
                        Squares[pieceRow, pieceCol] = tempPiece;
                        
                        if (isPinner)
                            result.Add((currentRow, currentCol));
                        
                        break;
                    }
                    else
                    {
                        break;
                    }
                }

                currentRow += dir[0];
                currentCol += dir[1];
            }
        }

        return result;
    }

    public bool DoesMoveSolvePinned(Move move)
    {
        var pinners = GetPinningAttackers(move.FromRow, move.FromCol);
        
        if (pinners.Count == 0)
            return true; // Not pinned

        // Check if move captures a pinner
        foreach (var pinner in pinners)
        {
            if (move.ToRow == pinner.row && move.ToCol == pinner.col)
                return true; // Captures the attacking piece
        }

        // Check if move blocks all attack paths
        foreach (var pinner in pinners)
        {
            var kingPos = FindKing(CurrentTurn);
            
                // Temporarily apply move
            Piece movedPiece = Squares[move.FromRow, move.FromCol];
            Piece targetPiece = Squares[move.ToRow, move.ToCol];
            bool originalHasMoved = movedPiece.HasMoved;

            Piece? enPassantCapturedPawn = null;
            int enPassantRow = -1;
            int enPassantCol = -1;
            if (movedPiece.Type == PieceType.Pawn)
            {
                int rowDiff = move.ToRow - move.FromRow;
                int colDiff = move.ToCol - move.FromCol;
                if (isEnPassant(move, movedPiece, rowDiff, colDiff))
                {
                    enPassantCapturedPawn = Squares[move.FromRow, move.ToCol];
                    enPassantRow = move.FromRow;
                    enPassantCol = move.ToCol;
                }
            }

            Squares[move.ToRow, move.ToCol] = movedPiece;
            Squares[move.FromRow, move.FromCol] = new Piece(PieceType.Empty, PieceColor.None);
            if (enPassantCapturedPawn != null)
            {
                Squares[enPassantRow, enPassantCol] = new Piece(PieceType.Empty, PieceColor.None);
            }

            // Check if king still attacked by this pinner
            bool stillAttacks = IsSquareAttackedByPiece(kingPos.row, kingPos.col, pinner.row, pinner.col);

            // Undo move
            Squares[move.FromRow, move.FromCol] = movedPiece;
            movedPiece.HasMoved = originalHasMoved;
            Squares[move.ToRow, move.ToCol] = targetPiece;
            if (enPassantCapturedPawn != null)
            {
                Squares[enPassantRow, enPassantCol] = enPassantCapturedPawn;
            }

            if (stillAttacks)
                return false; // Move doesn't block this attack
        }

        return true; // Move blocks all attacks
    }
    private bool IsLegalSim(Move move, PieceColor color)
    {
        if(!IsInsideBoard(move.FromRow, move.FromCol) || !IsInsideBoard(move.ToRow, move.ToCol))
            return false;
        Piece piece = Squares[move.FromRow, move.FromCol];
        Piece target = Squares[move.ToRow, move.ToCol];

        if(piece.Color != color)
            return false;
        
        if(target.Color == color)
            return false;

        bool valid = false;

        if(piece.Type== PieceType.Pawn)
            valid = IsValidPawnMove(move, piece);
        else if(piece.Type== PieceType.Rook)
            valid = IsValidRookMove(move);
        else if(piece.Type== PieceType.Bishop)
            valid = IsValidBishopMove(move);
        else if(piece.Type== PieceType.Knight)
            valid = IsValidKnightMove(move);
        else if(piece.Type== PieceType.Queen)
            valid = IsValidQueenMove(move);
        else if(piece.Type== PieceType.King)
            valid = IsValidKingMove(move);
        
        if(!valid)
            return false;
        
        return!WouldLeaveKingInCheck(move);

    }
    public bool HasAnyLegalMove(PieceColor color)
    {
        for(int row=0; row<8; row++)
        {
            for(int col=0; col<8; col++)
            {
                Piece piece = Squares[row, col];
                if(piece.Color != color)               
                    continue;
                for(int toRow=0; toRow<8; toRow++)
                {
                    for(int toCol=0; toCol<8; toCol++)
                    {
                            Move move = new Move(row, col, toRow, toCol);
                            if(IsLegalSim(move, color))
                                return true;
                    }
                }
                
            }
        }
        return false;
    }
    public bool IsCheckmate(PieceColor color)
    {
        return IsKingInCheck(color) && !HasAnyLegalMove(color);
    }
    public bool IsStalemate(PieceColor color)
    {
        return !IsKingInCheck(color) && !HasAnyLegalMove(color);
    }
    public bool PawnPromotion(Move move)
    {
        if (Squares[move.ToRow, move.ToCol].Type == PieceType.Pawn && (move.ToRow == 0 || move.ToRow == 7))
        {
            Console.Write("Promote to (Q/R/B/N): ");
            string? choice = Console.ReadLine()?.Trim().ToUpper();

            PieceType promotionType = choice switch
            {
                "Q" => PieceType.Queen,
                "R" => PieceType.Rook,
                "B" => PieceType.Bishop,
                "N" => PieceType.Knight,
                _ => PieceType.Queen // Default to Queen if invalid input
            };

            PieceColor promotionColor = Squares[move.ToRow, move.ToCol].Color;
            Squares[move.ToRow, move.ToCol] = new Piece(promotionType, promotionColor);
            return true;
        }

        return false;
    }
        private PieceColor OpponentColor(PieceColor color)
    {
        return color == PieceColor.White 
        ? PieceColor.Black 
        : PieceColor.White;
    }
    public void SetupStalemateTest()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Squares[row, col] = new Piece(PieceType.Empty, PieceColor.None);
            }
        }

        // Black king on a8
        Squares[0, 0] = new Piece(PieceType.King, PieceColor.Black);

        // White king on c6
        Squares[2, 2] = new Piece(PieceType.King, PieceColor.White);

        // White queen on d6
        Squares[2, 3] = new Piece(PieceType.Queen, PieceColor.White);

        CurrentTurn = PieceColor.White;
        MoveHistory.Clear();
    }
    public bool CanMoveTo(int fromRow, int fromCol, int toRow, int toCol)
    {
        Move move = new Move(fromRow, fromCol, toRow, toCol);

        if (!IsInsideBoard(fromRow, fromCol) || !IsInsideBoard(toRow, toCol))
            return false;

        if (fromRow == toRow && fromCol == toCol)
            return false;

        Piece piece = Squares[fromRow, fromCol];
        Piece target = Squares[toRow, toCol];

        if (piece.Type == PieceType.Empty)
            return false;

        if (piece.Color != CurrentTurn)
            return false;

        if (target.Color == piece.Color)
            return false;

        bool valid = IsLegalSim(move, piece.Color);

        if (!valid)
            return false;

        return !WouldLeaveKingInCheck(move);
    }
}