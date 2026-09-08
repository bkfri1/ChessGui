using System.Reflection;


namespace ChessGui;

public partial class MainForm : Form
{
    private NetworkManager networkManager = new NetworkManager();
    private System.Windows.Forms.Timer WhiteGameTimer = new System.Windows.Forms.Timer();
    private System.Windows.Forms.Timer BlackGameTimer = new System.Windows.Forms.Timer();
    private int WhiteelapsedTime = 600;
    private int BlackelapsedTime = 600;
    private Board board = new Board();
    private Button[,] buttons = new Button[8, 8];
    private Dictionary<string, Image> pieceImages = new Dictionary<string, Image>();
    private Button hostButton = new Button();
    private Button connectButton = new Button();
    private Button SendTestMessageButton = new Button();
    private int selectedRow = -1;
    private int selectedCol = -1;
    private Button undoButton = new Button();
    private Button resetButton = new Button();
    private Button drawButton = new Button();
    private Button forfeitButton = new Button();
    private Label WhiteTimerLabel = new Label();
    private Label BlackTimerLabel = new Label();
    private Label statusLabel = new Label();
    private PieceColor? playerColor;
    private bool isRemoteGame;
    private bool isHost;
    private bool boardFlipped;
    public MainForm()
    {
        networkManager.Connected += NetworkConnected;
        networkManager.MessageReceived += NetworkMessageReceived;
        networkManager.Disconnected += NetworkDisconnected;
        Text = "Chess Game";
        Width = 500;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        LoadPieceImages();
        CreateBoardButtons();
        // CreateSendTestMessageButton();
        CreateStatusLabel();
        CreateWhiteTimerLabel();
        CreateBlackTimerLabel();
        SetupWhiteTimer();
        SetupBlackTimer();
        RefreshBoard();
        WhiteTimerLabel.Text = "White Time Left: 10:00";
        BlackTimerLabel.Text = "Black Time Left: 10:00";
        LocalRemote();
    }
    private void LoadPieceImages()
    {
        string[] colors = { "white", "black" };
        string[] pieces =
        {
            "pawn",
            "rook",
            "knight",
            "bishop",
            "queen",
            "king"
        };

        foreach (string color in colors)
        {
            foreach (string piece in pieces)
            {
                // המפתח שבו נשתמש בקוד
                string key = $"{color}_{piece}";

                // שם הקובץ האמיתי
                string fileName = $"{color}-{piece}.png";

                string path = Path.Combine("Images", fileName);

                pieceImages[key] = Image.FromFile(path);
            }
        }
    }

    private void CreateHostButton()
    {
        hostButton.Text = "Host Game";
        hostButton.Width = 120;
        hostButton.Height = 40;
        hostButton.Top = 510;
        hostButton.Left = 170;
        hostButton.Font = new Font(hostButton.Font.FontFamily, 10, FontStyle.Bold);
        hostButton.FlatStyle = FlatStyle.Flat;
        hostButton.Click += HostButtonClicked;
        Controls.Add(hostButton);
    }

    private void CreateConnectButton()
    {
        connectButton.Text = "Connect";
        connectButton.Width = 120;
        connectButton.Height = 40;
        connectButton.Top = 510;
        connectButton.Left = 300;
        connectButton.Font = new Font(connectButton.Font.FontFamily, 10, FontStyle.Bold);
        connectButton.FlatStyle = FlatStyle.Flat;
        connectButton.Click += ConnectButtonClicked;
        Controls.Add(connectButton);
    }
    private void CreateSendTestMessageButton()
    {
        SendTestMessageButton.Text = "Send Test Message";
        SendTestMessageButton.Width = 150;
        SendTestMessageButton.Height = 40;
        SendTestMessageButton.Top = 510;
        SendTestMessageButton.Left = 30;
        SendTestMessageButton.Font = new Font(SendTestMessageButton.Font.FontFamily, 10, FontStyle.Bold);
        SendTestMessageButton.FlatStyle = FlatStyle.Flat;
        SendTestMessageButton.Click += SendTestButtonClicked;
        Controls.Add(SendTestMessageButton);
    }
    
    private async void SendTestButtonClicked(object? sender, EventArgs e)
    {
        await networkManager.SendMessageAsync("Hello from chess!");
    }
    
    private async void ConnectButtonClicked(object? sender, EventArgs e)
    {
        statusLabel.Text = "Connecting...";
        await networkManager.ConnectAsync(
            "127.0.0.1",
            5000
        );
    }
    private void NetworkConnected()
    {
        if (InvokeRequired)
        {
            Invoke(NetworkConnected);
            return;
        }
        statusLabel.Text = "Connected!";
    }

    private PieceColor ChooseSide()
    {
        using Form dialog = new Form
        {
            Text = "Choose Side",
            Width = 320,
            Height = 160,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false
        };

        Label label = new Label
        {
            Text = "Choose the side for this game:",
            AutoSize = true,
            Left = 75,
            Top = 20
        };

        Button whiteButton = new Button
        {
            Text = "White",
            Width = 90,
            Left = 45,
            Top = 60,
            DialogResult = DialogResult.Yes
        };

        Button blackButton = new Button
        {
            Text = "Black",
            Width = 90,
            Left = 175,
            Top = 60,
            DialogResult = DialogResult.No
        };

        dialog.Controls.Add(label);
        dialog.Controls.Add(whiteButton);
        dialog.Controls.Add(blackButton);

        return dialog.ShowDialog(this) == DialogResult.Yes
            ? PieceColor.White
            : PieceColor.Black;
    }

    private DialogResult LocalRemote()
    {
        using Form dialog = new Form
        {
            Text = "Chess Game",
            Width = 320,
            Height = 160,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false
        };

        Label label = new Label
        {
            Text = "Choose a game mode:",
            AutoSize = true,
            Left = 95,
            Top = 20
        };

        Button localButton = new Button
        {
            Text = "Local",
            Width = 90,
            Left = 45,
            Top = 60,
            DialogResult = DialogResult.Yes
        };

        Button remoteButton = new Button
        {
            Text = "Remote",
            Width = 90,
            Left = 175,
            Top = 60,
            DialogResult = DialogResult.No
        };

        dialog.Controls.Add(label);
        dialog.Controls.Add(localButton);
        dialog.Controls.Add(remoteButton);

        DialogResult gameMode = dialog.ShowDialog(this);

        if (gameMode == DialogResult.Yes)
        {
            CreateUndoButton();
            CreateResetButton();
            UpdateTimersForTurn();
            EnableBoard();
        }
        else if (gameMode == DialogResult.No)
        {
            isRemoteGame = true;
            CreateDrawButton();
            CreateForfeitButton();
            DisableBoard();
            ShowConnectionDialog();
        }
        else
        {
            Close();
        }

        return gameMode;
    }

    private void ShowConnectionDialog()
    {
        using Form dialog = new Form
        {
            Text = "Remote Game",
            Width = 320,
            Height = 160,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false
        };

        Label label = new Label
        {
            Text = "Choose how to connect:",
            AutoSize = true,
            Left = 85,
            Top = 20
        };

        Button hostDialogButton = new Button
        {
            Text = "Host",
            Width = 90,
            Left = 45,
            Top = 60,
            DialogResult = DialogResult.Yes
        };

        Button connectDialogButton = new Button
        {
            Text = "Connect",
            Width = 90,
            Left = 175,
            Top = 60,
            DialogResult = DialogResult.No
        };

        dialog.Controls.Add(label);
        dialog.Controls.Add(hostDialogButton);
        dialog.Controls.Add(connectDialogButton);

        DialogResult connectionMode = dialog.ShowDialog(this);

        if (connectionMode == DialogResult.Yes)
        {
            isHost = true;
            SetPlayerColor(ChooseSide());
            HostButtonClicked(this, EventArgs.Empty);
        }
        else if (connectionMode == DialogResult.No)
        {
            isHost = false;
            ConnectButtonClicked(this, EventArgs.Empty);
        }
    }
    private async void HostButtonClicked(object? sender, EventArgs e)
    {
        statusLabel.Text = "waiting for connection...";
        await networkManager.StartServerAsync(5000);

        if (playerColor.HasValue)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(async () => await FinishHostConnectionAsync()));
                return;
            }

            await FinishHostConnectionAsync();
        }
    }

    private async Task FinishHostConnectionAsync()
    {
        await networkManager.SendMessageAsync($"SIDE:{playerColor!.Value}");
        EnableBoard();
        UpdateTimersForTurn();
        statusLabel.Text = $"Connected! You are {playerColor.Value}.";
    }

    private void NetworkMessageReceived(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => NetworkMessageReceived(message));
            return;
        }

        if (message.StartsWith("SIDE:", StringComparison.OrdinalIgnoreCase))
        {
            string hostSide = message[5..];
            SetPlayerColor(hostSide.Equals(nameof(PieceColor.White), StringComparison.OrdinalIgnoreCase)
                ? PieceColor.Black
                : PieceColor.White);

            if (board.MoveHistory.Count > 0)
            {
                ResetGame();
            }
            else
            {
                EnableBoard();
                UpdateTimersForTurn();
            }

            statusLabel.Text = $"Connected! You are {playerColor!.Value}.";
            return;
        }

        if (message.Equals("DRAW", StringComparison.OrdinalIgnoreCase))
        {
            EndGame("Draw agreed!");
            return;
        }

        if (message.Equals("FORFEIT", StringComparison.OrdinalIgnoreCase))
        {
            string winner = playerColor == PieceColor.White ? "White" : "Black";
            EndGame($"{winner} wins by forfeit!");
            return;
        }

        ChessGui.Move? move = ChessGui.Move.Parse(message);

        if(move == null)
        {
            statusLabel.Text = "Received invalid move.";
            return;
        }

        Piece movedPiece = board.Squares[move.FromRow, move.FromCol];
        bool success = board.MovePiece(move);

        if(success)
        {
            RefreshBoard();
            UpdateTimersForTurn();
            statusLabel.Text =
                $"Opponent moved: {movedPiece.Color} {movedPiece.Type} " +
                $"{GetSquareName(move.FromRow, move.FromCol)} " +
                $"{GetSquareName(move.ToRow, move.ToCol)}";
            HandleGameStateAfterMove();
        }

    }
    private void NetworkDisconnected()
    {
        if (InvokeRequired)
        {
            Invoke(NetworkDisconnected);
            return;
        }
        statusLabel.Text = "Disconnected.";
    }
    private void CreateBoardButtons()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Button button = new Button();

                button.Width = 60;
                button.Height = 60;
                button.Location = new Point(col * 60, row * 60);
                button.Click += BoardButton_Click;
                buttons[row, col] = button;
                Controls.Add(button);
            }
        }
    }
    private void CreateUndoButton()
    {
        undoButton.Width = 100;
        undoButton.Height = 30;
        undoButton.Top = 490;
        undoButton.Left = 35;
        undoButton.Text = "Undo";
        undoButton.Click += UndoButton_Click;
        Controls.Add(undoButton);
    }
    private void UndoButton_Click(object? sender, EventArgs e)
    {

        if (board.MoveHistory.Count == 0)
        {
            statusLabel.Text = "No moves to undo.";
            return;
        }
        board.UndoMove();
        selectedRow = -1;
        selectedCol = -1;
        RefreshBoard();
        UpdateTimersForTurn();
        statusLabel.Text = $"Current turn: {board.CurrentTurn}";
        EnableBoard();
    }
    private void CreateResetButton()
    {
        resetButton.Width = 100;
        resetButton.Height = 30;
        resetButton.Top = 525;
        resetButton.Left = 35;
        resetButton.Text = "Reset";
        resetButton.Click += ResetButton_Click;
        Controls.Add(resetButton);
    }

    private void CreateDrawButton()
    {
        drawButton.Width = 100;
        drawButton.Height = 30;
        drawButton.Top = 490;
        drawButton.Left = 35;
        drawButton.Text = "Draw";
        drawButton.Click += DrawButton_Click;
        Controls.Add(drawButton);
    }

    private void CreateForfeitButton()
    {
        forfeitButton.Width = 100;
        forfeitButton.Height = 30;
        forfeitButton.Top = 525;
        forfeitButton.Left = 35;
        forfeitButton.Text = "Forfeit";
        forfeitButton.Click += ForfeitButton_Click;
        Controls.Add(forfeitButton);
    }

    private async void DrawButton_Click(object? sender, EventArgs e)
    {
        DialogResult result = MessageBox.Show(
            "Offer a draw to your opponent?",
            "Draw",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        await networkManager.SendMessageAsync("DRAW");
        EndGame("Draw agreed!");
    }

    private async void ForfeitButton_Click(object? sender, EventArgs e)
    {
        DialogResult result = MessageBox.Show(
            "Are you sure you want to forfeit?",
            "Forfeit",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        await networkManager.SendMessageAsync("FORFEIT");
        string opponent = playerColor == PieceColor.White ? "Black" : "White";
        EndGame($"{opponent} wins by forfeit!");
    }
    private void ResetButton_Click(object? sender, EventArgs e)
    {
        DialogResult result = MessageBox.Show("Are you sure you want to reset the game?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            ResetGame();
        }
    }

    private void ResetGame()
    {
        board = new Board();
        selectedRow = -1;
        selectedCol = -1;

        WhiteelapsedTime = 600;
        BlackelapsedTime = 600;
        WhiteTimerLabel.Text = "White Time Left: 10:00";
        BlackTimerLabel.Text = "Black Time Left: 10:00";
        RefreshBoard();
        statusLabel.Text = $"Current turn: {board.CurrentTurn}";
        UpdateTimersForTurn();
        EnableBoard();
    }
    private void CreateWhiteTimerLabel()
    {
        WhiteTimerLabel.Width = 150;
        WhiteTimerLabel.Height = 34;
        WhiteTimerLabel.Top = 485;
        WhiteTimerLabel.Left = 170;
        WhiteTimerLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        WhiteTimerLabel.BackColor = Color.PaleGreen;
        WhiteTimerLabel.ForeColor = Color.DarkSlateGray;
        WhiteTimerLabel.BorderStyle = BorderStyle.Fixed3D;
        WhiteTimerLabel.Padding = new Padding(4, 2, 4, 2);
        WhiteTimerLabel.AutoSize = false;
        WhiteTimerLabel.TextAlign = ContentAlignment.MiddleCenter;
        Controls.Add(WhiteTimerLabel);
        WhiteTimerLabel.BringToFront();
    }
    private void CreateBlackTimerLabel()
    {
        BlackTimerLabel.Width = 150;
        BlackTimerLabel.Height = 34;
        BlackTimerLabel.Top = 525;
        BlackTimerLabel.Left = 170;
        BlackTimerLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        BlackTimerLabel.BackColor = Color.FromArgb(45, 45, 50);
        BlackTimerLabel.ForeColor = Color.DarkGray;
        BlackTimerLabel.BorderStyle = BorderStyle.Fixed3D;
        BlackTimerLabel.Padding = new Padding(4, 2, 4, 2);
        BlackTimerLabel.AutoSize = false;
        BlackTimerLabel.TextAlign = ContentAlignment.MiddleCenter;
        Controls.Add(BlackTimerLabel);
        BlackTimerLabel.BringToFront();
    }
    private void SetupWhiteTimer()
    {
        WhiteGameTimer.Interval = 1000; // 1 second
        WhiteGameTimer.Tick += WhiteGameTimer_Tick;
    }
    private void SetupBlackTimer()
    {
        BlackGameTimer.Interval = 1000; // 1 second
        BlackGameTimer.Tick += BlackGameTimer_Tick;
    }

    private void UpdateTimersForTurn()
    {
        if (board.CurrentTurn == PieceColor.White)
        {
            WhiteGameTimer.Start();
            BlackGameTimer.Stop();
        }
        else
        {
            WhiteGameTimer.Stop();
            BlackGameTimer.Start();
        }

        UpdateTimerVisuals();
    }

    private void UpdateTimerVisuals()
    {
        if (BlackGameTimer.Enabled)
        {
            BlackTimerLabel.ForeColor = Color.LightGray;
            WhiteTimerLabel.ForeColor = Color.LimeGreen;
        }
        else
        {
            BlackTimerLabel.ForeColor = Color.DarkSlateGray;
            WhiteTimerLabel.ForeColor = Color.DarkSlateGray;
            
        }
        WhiteTimerLabel.BackColor = Color.PaleGreen;
        BlackTimerLabel.BackColor = Color.FromArgb(45, 45, 50);
    }

    private void WhiteGameTimer_Tick(object? sender, EventArgs e)
    {
        WhiteelapsedTime--;
        TimeSpan timeSpan = TimeSpan.FromSeconds(WhiteelapsedTime);
        TimeSpan.FromSeconds(WhiteelapsedTime);
        WhiteTimerLabel.Text = $"White Time Left: {timeSpan:mm\\:ss}";
        UpdateTimerVisuals();
        if (WhiteelapsedTime <= 0)
        {
            EndGame("Black wins on time!");
        }
    }
    private void BlackGameTimer_Tick(object? sender, EventArgs e)
    {
        BlackelapsedTime--;
        TimeSpan timeSpan = TimeSpan.FromSeconds(BlackelapsedTime);
        BlackTimerLabel.Text = $"Black Time Left: {timeSpan:mm\\:ss}";
        UpdateTimerVisuals();
        if (BlackelapsedTime <= 0)
        {
            EndGame("White wins on time!");
        }
    }

    private async void BoardButton_Click(object? sender, EventArgs e)
    {
        Button clickedButton = (Button)sender!;
        Tuple<int, int> position = (Tuple<int, int>)clickedButton.Tag!;

        int row = position.Item1;
        int col = position.Item2;

        if (isRemoteGame && !playerColor.HasValue)
        {
            statusLabel.Text = "Waiting for the host to assign your side.";
            return;
        }

        // First click = choose piece
        if (selectedRow == -1)
        {
            Piece piece = board.Squares[row, col];

            if (piece.Type == PieceType.Empty)
            {
                statusLabel.Text = "Choose a piece first.";
                return;
            }

            if (piece.Color != board.CurrentTurn ||
                (isRemoteGame && piece.Color != playerColor))
            {
                statusLabel.Text = isRemoteGame && piece.Color != playerColor
                    ? $"You are playing {playerColor}."
                    : "Not your turn.";
                return;
                
            }

            selectedRow = row;
            selectedCol = col;

            statusLabel.Text = $"Selected {piece.Color} {piece.Type}";
            RefreshBoard();
            return;
        }

        if (selectedRow == row && selectedCol == col)
        {
            selectedRow = -1;
            selectedCol = -1;
            statusLabel.Text = $"Current turn: {board.CurrentTurn}";
            RefreshBoard();
            return;
        }

        // Second click = destination
        Move move = new Move(selectedRow, selectedCol, row, col);

        bool success = board.MovePiece(move);

        if (success)
        {
            // Send the move over the network
            await networkManager.SendMessageAsync(move.ToString());

            selectedRow = -1;
            selectedCol = -1;

            RefreshBoard();
        }
        
        if (!success)
        {
            statusLabel.Text = "Illegal move.";
            return;
        }

        if (board.PawnPromotion(move))
        {
            MoveRecord lastMove = board.MoveHistory[^1];

            Piece promotedPawn = board.Squares[lastMove.Move.ToRow, lastMove.Move.ToCol];
            
            Dictionary<string, Image> promotionImages = new Dictionary<string, Image>();
            string colorName = promotedPawn.Color == PieceColor.White ? "white" : "black";
            foreach (PieceType pieceType in new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight })
            {
                string type = pieceType switch
                {
                    PieceType.Rook => "rook",
                    PieceType.Knight => "knight",
                    PieceType.Bishop => "bishop",
                    PieceType.Queen => "queen",
                    _ => ""
                };
                string path = Path.Combine("Images", $"{colorName}-{type}.png");
                if (File.Exists(path))
                {
                    // Match the key format used by PromotionForm's GetImageKey method: "whitequeen", "blackrook", etc.
                    string imageKey = $"{colorName}{type}";
                    promotionImages[imageKey] = Image.FromFile(path);
                }
            }

            using PromotionForm promotionForm = 
                new PromotionForm(promotedPawn.Color, promotionImages);

            DialogResult result = promotionForm.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                PieceType selectedPieceType = promotionForm.SelectedPieceType;
                board.Squares[lastMove.Move.ToRow, lastMove.Move.ToCol] = new Piece(selectedPieceType, promotedPawn.Color);
                statusLabel.Text = $"Pawn promoted to {selectedPieceType}.";
                RefreshBoard();
            }
        }

        if (HandleGameStateAfterMove())
            return;
    }

    private bool HandleGameStateAfterMove()
    {
        if (board.IsCheckmate(PieceColor.White))
        {
            EndGame("Checkmate! Black wins!");
            return true;
        }

        if (board.IsCheckmate(PieceColor.Black))
        {
            EndGame("Checkmate! White wins!");
            return true;
        }

        if (board.IsStalemate(PieceColor.White) || board.IsStalemate(PieceColor.Black))
        {
            EndGame("Stalemate! It's a draw!");
            return true;
        }

        if (board.IsKingInCheck(PieceColor.White))
        {
            statusLabel.Text = "White king is in check!";
        }
        else if (board.IsKingInCheck(PieceColor.Black))
        {
            statusLabel.Text = "Black king is in check!";
        }
        else
        {
            statusLabel.Text = $"Current turn: {board.CurrentTurn}";
        }

        UpdateTimersForTurn();
        return false;
    }

    private void EndGame(string result)
    {
        WhiteGameTimer.Stop();
        BlackGameTimer.Stop();
        DisableBoard();
        statusLabel.Text = result;

        if (isRemoteGame && !isHost)
        {
            MessageBox.Show(result, "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult playAgain = MessageBox.Show(
            $"{result}\n\nDo you want to play again?",
            "Game Over",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (playAgain == DialogResult.Yes)
        {
            if (isRemoteGame && isHost)
            {
                SetPlayerColor(ChooseSide());
                ResetGame();
                _ = networkManager.SendMessageAsync($"SIDE:{playerColor!.Value}");
            }
            else
            {
                ResetGame();
            }
        }
        else
        {
            Close();
        }
    }

    private void CreateStatusLabel()
    {
        statusLabel.Width = 100;
        statusLabel.Height = 50;
        statusLabel.Top = 495;
        statusLabel.Left = 350;
        statusLabel.TextAlign = ContentAlignment.MiddleCenter;
        statusLabel.BringToFront();
        Controls.Add(statusLabel);
    }

    private void SetPlayerColor(PieceColor color)
    {
        playerColor = color;
        boardFlipped = color == PieceColor.Black;
        RefreshBoard();
    }

    private void RefreshBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Button button = buttons[row, col];
                int boardRow = boardFlipped ? 7 - row : row;
                int boardCol = boardFlipped ? 7 - col : col;
                Piece piece = board.Squares[boardRow, boardCol];

                button.Tag = new Tuple<int, int>(boardRow, boardCol);

                if (piece.Type == PieceType.Empty)
                {
                    button.Image = null;
                }
                else
                {
                    Image? pieceImage = GetPieceImage(piece);
                    if (pieceImage != null)
                    {
                        button.Image = new Bitmap(pieceImage, new Size(50, 50));
                    }
                }
                Color baseColor = (row + col) % 2 == 0 ? Color.White : Color.Gray;

                if (selectedRow != -1)
                {
                    if (boardRow == selectedRow && boardCol == selectedCol)
                    {
                        baseColor = Color.Yellow;
                    }
                    else if (board.CanMoveTo(selectedRow, selectedCol, boardRow, boardCol))
                    {
                        if (piece.Type == PieceType.Empty)
                        {
                            baseColor = Color.Gold;
                        }
                        else if (piece.Color != board.CurrentTurn)
                        {
                            baseColor = Color.Red;
                        }
                    }
                }

                // If this square contains a king that's currently in check, override color
                if (piece.Type == PieceType.King && board.IsKingInCheck(piece.Color))
                {
                    baseColor = Color.Red;
                }

                button.BackColor = baseColor;
            }
        }
    }
    private void DisableBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                buttons[row, col].Enabled = false;
            }
        }
    }
    private void EnableBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                buttons[row, col].Enabled = true;
            }
        }
    }

    private static string GetSquareName(int row, int col)
    {
        return $"{(char)('a' + col)}{8 - row}";
    }

    // private string GetPieceSymbol(Piece piece)
    // {
    //     return piece.Color switch
    //     {
    //         PieceColor.White => piece.Type switch
    //         {
    //             PieceType.Pawn => "♙",
    //             PieceType.Rook => "♖",
    //             PieceType.Knight => "♘",
    //             PieceType.Bishop => "♗",
    //             PieceType.Queen => "♕",
    //             PieceType.King => "♔",
    //             _ => ""
    //         },
    //         PieceColor.Black => piece.Type switch
    //         {
    //             PieceType.Pawn => "♟",
    //             PieceType.Rook => "♜",
    //             PieceType.Knight => "♞",
    //             PieceType.Bishop => "♝",
    //             PieceType.Queen => "♛",
    //             PieceType.King => "♚",
    //             _ => ""
    //         },
    //         _ => ""
    //     };
    // }
    private Image? GetPieceImage(Piece piece)
    {
        if (piece.Type == PieceType.Empty)
            return null;
        
        string color = piece.Color == PieceColor.White ? "white" : "black";
        string type = piece.Type switch
        {
            PieceType.Pawn => "pawn",
            PieceType.Rook => "rook",
            PieceType.Knight => "knight",
            PieceType.Bishop => "bishop",
            PieceType.Queen => "queen",
            PieceType.King => "king",
            _ => ""
        };
        string path = Path.Combine("Images", $"{color}-{type}.png");
        if (!File.Exists(path))
        {
            return null;
        }
        return Image.FromFile(path);
    }    
}
