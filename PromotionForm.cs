namespace ChessGui;

class PromotionForm : Form
{
    public PieceType SelectedPieceType { get; private set; } = PieceType.Queen;

    public PromotionForm(
        PieceColor pawnColor,
        Dictionary<string, Image> pieceImages)
    {
        Text = "Choose Promotion";
        Width = 390;
        Height = 150;

        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        CreatePromotionButton(
            PieceType.Queen,
            pawnColor,
            pieceImages,
            20
        );

        CreatePromotionButton(
            PieceType.Rook,
            pawnColor,
            pieceImages,
            110
        );

        CreatePromotionButton(
            PieceType.Bishop,
            pawnColor,
            pieceImages,
            200
        );

        CreatePromotionButton(
            PieceType.Knight,
            pawnColor,
            pieceImages,
            290
        );
    }
     private void CreatePromotionButton(
        PieceType pieceType,
        PieceColor color,
        Dictionary<string, Image> pieceImages,
        int left)
    {
        Button button = new Button();

        button.Width = 70;
        button.Height = 70;
        button.Left = left;
        button.Top = 20;

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = Color.Beige;

        string imageKey = GetImageKey(pieceType, color);

        if (pieceImages.TryGetValue(imageKey, out Image? image))
        {
            button.BackgroundImage = image;
            button.BackgroundImageLayout = ImageLayout.Zoom;
        }
        else
        {
            button.Text = pieceType.ToString(); // רק אם חסרה תמונה
        }

        button.Tag = pieceType;
        button.Click += PromotionButtonClicked;

        Controls.Add(button);
    }
    private void PromotionButtonClicked(object? sender, EventArgs e)
    {
        Button clickedButton = (Button)sender!;

        SelectedPieceType = (PieceType)clickedButton.Tag!;

        DialogResult = DialogResult.OK;
        Close();
    }
    private string GetImageKey(PieceType type, PieceColor color)
    {
        string colorName =
            color == PieceColor.White
                ? "white"
                : "black";

        string typeName = type switch
        {
            PieceType.Queen => "queen",
            PieceType.Rook => "rook",
            PieceType.Bishop => "bishop",
            PieceType.Knight => "knight",
            _
             => ""
        };

        return $"{colorName}{typeName}";
    }

}