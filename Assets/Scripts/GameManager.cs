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
    void Start()
    {
        // boardManager = FindFirstObjectByType<BoardManager>();
        // if (moveValidator == null)
        //     moveValidator = boardManager.gameObject.AddComponent<MoveValidator>();

        // // fenManager.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        // fenManager.SetupPositionFromFEN("4k3/pppppppp/8/8/8/8/PPPPPPPP/4K3 w - - 0 1");
        // moveValidator.ValidateMovesForCurrentPlayer(isWhiteTurn);

        boardManager = FindFirstObjectByType<BoardManager>();
        if (moveValidator == null)
            moveValidator = boardManager.gameObject.AddComponent<MoveValidator>();

        // Use GameSettings to setup board
        if (GameSettings.IsPuzzle && !string.IsNullOrEmpty(GameSettings.FEN))
        {
            fenManager.SetupPositionFromFEN(GameSettings.FEN);
        }
        else
        {
            fenManager.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        }

        moveValidator.ValidateMovesForCurrentPlayer(isWhiteTurn);
    }
    void Update()
    {
        if (PauseMenu.isPaused || gameOver)
            return;
        // if (gameOver) return;


        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2Int clickedSquare = boardManager.GetSquareUnderMouse();
            if (clickedSquare.x < 0 || clickedSquare.y < 0) return; // invalid clicks

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
                    selectedPiece.MoveTo(clickedSquare);
                    boardManager.UpdateBoardState(selectedPiece, clickedSquare);
                    Debug.Log($"{selectedPiece.pieceType} moved to {clickedSquare}");
                    boardManager.ClearMoveHighlights();
                    selectedPiece = null;

                    SwitchTurns();
                }
                else if (piece != null && piece.isWhite == selectedPiece.isWhite)
                {
                    boardManager.ClearMoveHighlights();
                    SelectPiece(piece);
                    Debug.Log($"REelected {selectedPiece.pieceType} at {selectedPiece.currentSquare}");
                }
                else
                {
                    boardManager.ClearMoveHighlights();
                    selectedPiece = null;
                }
            }
        }

        if (boardManager == null)
        {
            Debug.LogError("BoardManager is not assigned in GameManager!");
            return;
        }

        if (Camera.main == null) // remove later
        {
            Debug.LogError("No MainCamera");
            return;
        }
    }

    private void SelectPiece(BasePiece piece)
    {
        selectedPiece = piece;
        boardManager.ClearMoveHighlights(); // here
        boardManager.HighightMoveSquares(selectedPiece);
    }

    public void SwitchTurns()
    {
        isWhiteTurn = !isWhiteTurn;

        Debug.Log($"Turn switched to {(isWhiteTurn ? "White" : "Black")}");

        moveValidator.ValidateMovesForCurrentPlayer(isWhiteTurn);

        if (moveValidator.IsCheckmate(isWhiteTurn))
        {
            gameOver = true;
            Debug.Log($"CHECKMATE! {(isWhiteTurn ? "Black" : "White")} wins!");
            OnGameOver($"{(isWhiteTurn ? "Black" : "White")} wins by checkmate!");
        }
        else if (moveValidator.IsStalemate(isWhiteTurn))
        {
            gameOver = true;
            Debug.Log("STALEMATE! Game is a draw!");
            OnGameOver("Draw by stalemate!");
        }
        else if (moveValidator.IsKingInCheck(isWhiteTurn))
            Debug.Log($"{(isWhiteTurn ? "White" : "Black")} is in CHECK!");

        if (!isWhiteTurn)
            FindFirstObjectByType<AIOpponent>().MakeMoveIfItsAITurn();
    }

    private void OnGameOver(string result)
    {
        Debug.Log($"Game Over: {result}"); // add screen later
    }

    public void ResetGame()
    {
        gameOver = false;
        isWhiteTurn = true;
        selectedPiece = null;
        boardManager.ClearMoveHighlights();

        // Reset board to starting position
        fenManager.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        // Validate moves for white
        moveValidator.ValidateMovesForCurrentPlayer(isWhiteTurn);
    }
    public bool IsWhiteTurn => isWhiteTurn;
    public bool IsGameOver => gameOver;
}
