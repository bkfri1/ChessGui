namespace ChessGui;
class Move
{
    public int FromRow { get; set; }
    public int FromCol { get; set; }
    public int ToRow { get; set; }
    public int ToCol { get; set; }

    public Move(int fromRow, int fromCol, int toRow, int toCol)
    {
        FromRow = fromRow;
        FromCol = fromCol;
        ToRow = toRow;
        ToCol = toCol;
    }
    public override string ToString()
    {
        return $"{FromRow},{FromCol},{ToRow},{ToCol}";
    }
    public static Move Parse(string moveString)//gives e2 e4 as input and returns a Move object with the corresponding from and to coordinates
    {
        string[] parts = moveString.Split(',');

        int fromRow = int.Parse(parts[0]);
        int fromCol = int.Parse(parts[1]);
        int toRow = int.Parse(parts[2]);
        int toCol = int.Parse(parts[3]);

        return new Move(fromRow, fromCol, toRow, toCol);
    }

}


class MoveRecord
{
    public Move Move { get; set; }
    public Piece MovedPiece { get; set; }
    public Piece CapturedPiece { get; set; }
    public PieceColor PreviousTurn { get; set; }
    public bool MovedPieceHasMoved { get; set; }
    public bool CapturedPieceHasMoved { get; set; }
    public bool WasCastle;
    public Move? RookMove;
    public Piece? RookPiece;
    public Piece? RookCapturedPiece;
    public bool WasEnPassant;
    public Move? EnPassantCapturedPawnMove;
    public Piece? EnPassantCapturedPawn;

    public MoveRecord(Move move, Piece movedPiece, Piece capturedPiece, PieceColor previousTurn)
    {
        Move = move;
        MovedPiece = movedPiece;
        CapturedPiece = capturedPiece;
        PreviousTurn = previousTurn;
        MovedPieceHasMoved = movedPiece.HasMoved;
        CapturedPieceHasMoved = capturedPiece.HasMoved;

        WasCastle = false;
        RookMove = null;
        RookPiece = null;
        RookCapturedPiece = null;
        WasEnPassant = false;
        EnPassantCapturedPawnMove = null;
        EnPassantCapturedPawn = null;
    }
}