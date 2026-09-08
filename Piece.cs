enum PieceType
{   
    Empty,
    Pawn,
    Rook,
    Knight,
    Bishop,
    Queen,
    King
} 

enum PieceColor
{
    None,
    White,
    Black
}

class Piece
{
    public PieceType Type;
    public PieceColor Color;
    public bool HasMoved;

    public Piece(PieceType type, PieceColor color)
    {
        Type = type;
        Color = color;
        HasMoved = false;
    }
}