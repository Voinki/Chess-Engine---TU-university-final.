using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private FENManager fenManager;
    private BoardManager boardManager;
    private BasePiece selectedPiece;
    private MoveValidator moveValidator;
    public bool isPromotionPending = false;
    private bool isWhiteTurn = true;
    private bool gameOver = false;

    private List<string> positionHistory = new List<string>();
    private int halfMoveClock = 0;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMPro.TextMeshProUGUI gameOverText;
    void Start()
    {
        PauseMenu.isPaused = false;
        Time.timeScale = 1f;

        boardManager = FindFirstObjectByType<BoardManager>();
        if (moveValidator == null)
            moveValidator = boardManager.gameObject.AddComponent<MoveValidator>();

        if (GameSettings.IsPuzzle && !string.IsNullOrEmpty(GameSettings.FEN))
            fenManager.SetupPositionFromFEN(GameSettings.FEN);
        else
            fenManager.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        moveValidator.ValidateMovesForCurrentPlayer(isWhiteTurn);
    }
   void Update()
{
    if (PauseMenu.isPaused || gameOver)
        return;

    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
    {
        Vector2Int clickedSquare = boardManager.GetSquareUnderMouse();
        if (clickedSquare.x < 0 || clickedSquare.y < 0) return; 

        BasePiece piece = boardManager.GetPieceAtSquare(clickedSquare);

        if (selectedPiece == null)
        {
            if (piece != null && piece.isWhite == isWhiteTurn) // first time piece selection
                SelectPiece(piece);
        }
        else
        {
            if (selectedPiece.GetLegalMoves.Contains(clickedSquare))
            {
                BasePiece capturedPiece = boardManager.GetPieceAtSquare(clickedSquare);
                bool isPawnMove = selectedPiece.pieceType == BasePiece.PieceType.Pawn;
                bool isCapture = capturedPiece != null;

                selectedPiece.MoveTo(clickedSquare);
                // REMOVED: boardManager.UpdateBoardState(selectedPiece, clickedSquare);
                // MoveTo() already handles all board updates!
                
                Debug.Log($"{selectedPiece.pieceType} moved to {clickedSquare}");
                boardManager.ClearMoveHighlights();
                selectedPiece = null;

                if (isPawnMove || isCapture)
                    halfMoveClock = 0;
                else
                    halfMoveClock++;

                SwitchTurns();
            }
            else if (piece != null && piece.isWhite == selectedPiece.isWhite)
            {
                boardManager.ClearMoveHighlights();
                SelectPiece(piece);
            }
            else
            {
                boardManager.ClearMoveHighlights();
                selectedPiece = null;
            }
        }
    }
}

    private void SelectPiece(BasePiece piece)
    {
        selectedPiece = piece;
        boardManager.ClearMoveHighlights();
        boardManager.HighightMoveSquares(selectedPiece);
    }

   public void SwitchTurns()
{
    // IMPORTANT: Validate moves for the player who JUST moved before switching
    // This ensures promoted pieces have their legal moves calculated
    moveValidator.ValidateMovesForCurrentPlayer(isWhiteTurn);
    
    // Now switch turns
    isWhiteTurn = !isWhiteTurn;

    // Validate moves for the NEW current player
    moveValidator.ValidateMovesForCurrentPlayer(isWhiteTurn);

    string currentPosition = GetPositionKey();
    positionHistory.Add(currentPosition);

    if (moveValidator.IsCheckmate(isWhiteTurn))
    {
        gameOver = true;
        OnGameOver($"{(isWhiteTurn ? "Black" : "White")} wins by checkmate!");
    }
    else if (moveValidator.IsStalemate(isWhiteTurn))
    {
        gameOver = true;
        OnGameOver("Draw by stalemate!");
    }
    else if (moveValidator.IsInsufficientMaterial())
    {
        gameOver = true;
        OnGameOver("Draw by insufficient material!");
    }
    else if (IsThreefoldRepetition(currentPosition))
    {
        gameOver = true;
        OnGameOver("Draw by threefold repetition!");
    }
    else if (halfMoveClock >= 100) // 50 full moves = 100 half moves
    {
        gameOver = true;
        OnGameOver("Draw by fifty-move rule!");
    }
    else if (moveValidator.IsKingInCheck(isWhiteTurn))
        Debug.Log($"{(isWhiteTurn ? "White" : "Black")} is in CHECK!");

    if (!isWhiteTurn)
        FindFirstObjectByType<AIOpponent>().MakeMoveIfItsAITurn();
}

    public IReadOnlyList<string> GetPositionHistory()
    {
        return positionHistory;
    }

    private void OnGameOver(string result)
    {
        gameOver = true;
        Time.timeScale = 0f;

        if (gameOverPanel != null && gameOverText != null)
        {
            gameOverPanel.SetActive(true);
            gameOverText.text = result;
        }
    }

    public void ResetGame()
    {
        gameOver = false;
        isWhiteTurn = true;
        selectedPiece = null;
        boardManager.ClearMoveHighlights();

        positionHistory.Clear();
        halfMoveClock = 0;

        fenManager.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        moveValidator.ValidateMovesForCurrentPlayer(isWhiteTurn);
    }
    private string GetPositionKey()
    {
        string key = "";

        for (int y = 7; y >= 0; y--)
        {
            for (int x = 0; x < 8; x++)
            {
                BasePiece piece = boardManager.piecesOnBoard[x, y];
                if (piece == null)
                    key += ".";
                else
                {
                    char pieceChar = GetPieceChar(piece);
                    key += pieceChar;
                }
            }
            key += "/";
        }

        key += isWhiteTurn ? "w" : "b";
        key += GetCastlingRights();
        key += GetEnPassantSquare();

        return key;   // piece positions + turn + castling rights + en passant
    }

    private char GetPieceChar(BasePiece piece)
    {
        char c = piece.pieceType switch
        {
            BasePiece.PieceType.Pawn => 'p',
            BasePiece.PieceType.Knight => 'n',
            BasePiece.PieceType.Bishop => 'b',
            BasePiece.PieceType.Rook => 'r',
            BasePiece.PieceType.Queen => 'q',
            BasePiece.PieceType.King => 'k',
            _ => '?'
        };

        return piece.isWhite ? char.ToUpper(c) : c;
    }

    private string GetCastlingRights()
    {
        string castlingAvailability = "";

        // white kingside
        BasePiece whiteKing = boardManager.piecesOnBoard[4, 0];
        if (whiteKing != null && !whiteKing.hasMoved)
        {
            BasePiece whiteKingsideRook = boardManager.piecesOnBoard[7, 0];
            if (whiteKingsideRook != null && !whiteKingsideRook.hasMoved)
                castlingAvailability += "K";

            BasePiece whiteQueensideRook = boardManager.piecesOnBoard[0, 0];
            if (whiteQueensideRook != null && !whiteQueensideRook.hasMoved)
                castlingAvailability += "Q";
        }

        // black kingside
        BasePiece blackKing = boardManager.piecesOnBoard[4, 7];
        if (blackKing != null && !blackKing.hasMoved)
        {
            BasePiece blackKingsideRook = boardManager.piecesOnBoard[7, 7];
            if (blackKingsideRook != null && !blackKingsideRook.hasMoved)
                castlingAvailability += "k";

            BasePiece blackQueensideRook = boardManager.piecesOnBoard[0, 7];
            if (blackQueensideRook != null && !blackQueensideRook.hasMoved)
                castlingAvailability += "q";
        }

        if (castlingAvailability.Length > 0)
            return castlingAvailability;
        else
            return "-";
    }

    private string GetEnPassantSquare()
    {
        if (boardManager.enPassantSquare.x == -1)
            return "-";

        char file = (char)('a' + boardManager.enPassantSquare.x);
        int rank = boardManager.enPassantSquare.y + 1;
        return $"{file}{rank}";
    }

    private bool IsThreefoldRepetition(string currentPosition)
    {
        int count = 0;
        foreach (string position in positionHistory)
        {
            if (position == currentPosition)
            {
                count++;
                if (count >= 3)
                    return true;
            }
        }
        return false;
    }
    public bool IsWhiteTurn => isWhiteTurn;
    public bool IsGameOver => gameOver;
}
