using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveValidator : MonoBehaviour
{
    public BoardManager boardManager;

    void Awake()
    {
        boardManager = GetComponent<BoardManager>();
    }

    /// <summary>
    /// Filters all pieces' moves to only include legal moves (doesn't leave king in check)
    /// Call this after all pieces have calculated their candidate moves
    /// </summary>
    public void FilterIllegalMoves(bool forWhite)
    {
        List<BasePiece> pieces = GetAllPiecesForColor(forWhite);

        foreach (BasePiece piece in pieces)
        {
            List<Vector2Int> legalNormalMoves = new List<Vector2Int>();
            List<Vector2Int> legalCaptureMoves = new List<Vector2Int>();

            foreach (Vector2Int move in piece.normalMoves)
            {
                if (IsMoveLegal(piece, move))
                    legalNormalMoves.Add(move);
            }

            foreach (Vector2Int move in piece.captureMoves)
            {
                if (IsMoveLegal(piece, move))
                    legalCaptureMoves.Add(move);
            }

            piece.UpdateLegalMoves(legalNormalMoves, legalCaptureMoves);
        }
    }

    /// <summary>
    /// Checks if a move is legal (doesn't leave own king in check)
    /// </summary>
    private bool IsMoveLegal(BasePiece piece, Vector2Int targetSquare)
    {
        // Create simulated board
        BasePiece[,] simulatedBoard = SimulateMove(piece, targetSquare, out BasePiece capturedPiece, out Vector2Int originalSquare);

        // Find king position on simulated board
        Vector2Int kingPos = FindKingPosition(simulatedBoard, piece.isWhite);

        // Check if king is in check on simulated board
        bool kingInCheck = IsSquareUnderAttack(simulatedBoard, kingPos, !piece.isWhite);

        return !kingInCheck;
    }

    /// <summary>
    /// Simulates a move and returns the resulting board state
    /// </summary>
    private BasePiece[,] SimulateMove(BasePiece piece, Vector2Int targetSquare, out BasePiece capturedPiece, out Vector2Int originalSquare)
    {
        // Copy the board
        BasePiece[,] simBoard = new BasePiece[8, 8];
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                simBoard[x, y] = boardManager.piecesOnBoard[x, y];
            }
        }

        originalSquare = piece.currentSquare;
        capturedPiece = simBoard[targetSquare.x, targetSquare.y];

        // Handle en passant capture in simulation
        if (piece.pieceType == BasePiece.PieceType.Pawn)
        {
            bool isEnPassant = (boardManager.enPassantSquare.x != -1) &&
                               (targetSquare == boardManager.enPassantSquare) &&
                               simBoard[targetSquare.x, targetSquare.y] == null;

            if (isEnPassant)
            {
                int capturedY = piece.isWhite ? targetSquare.y - 1 : targetSquare.y + 1;
                simBoard[targetSquare.x, capturedY] = null;
            }
        }

        // Perform the move on simulated board
        simBoard[originalSquare.x, originalSquare.y] = null;
        simBoard[targetSquare.x, targetSquare.y] = piece;

        // Handle castling in simulation
        if (piece.pieceType == BasePiece.PieceType.King && Mathf.Abs(targetSquare.x - originalSquare.x) == 2)
        {
            int rank = piece.isWhite ? 0 : 7;
            if (targetSquare.x > originalSquare.x) // Kingside
            {
                simBoard[7, rank] = null;
                simBoard[5, rank] = boardManager.piecesOnBoard[7, rank];
            }
            else // Queenside
            {
                simBoard[0, rank] = null;
                simBoard[3, rank] = boardManager.piecesOnBoard[0, rank];
            }
        }

        return simBoard;
    }

    /// <summary>
    /// Finds the king's position on the board
    /// </summary>
    private Vector2Int FindKingPosition(BasePiece[,] board, bool isWhite)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece piece = board[x, y];
                if (piece != null && piece.pieceType == BasePiece.PieceType.King && piece.isWhite == isWhite)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        Debug.LogError($"King not found for color {(isWhite ? "White" : "Black")}!");
        return new Vector2Int(-1, -1);
    }

    /// <summary>
    /// Checks if a square is under attack by the opposing color
    /// </summary>
    private bool IsSquareUnderAttack(BasePiece[,] board, Vector2Int square, bool byWhite)
    {
        // Check all enemy pieces to see if they can attack this square
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece piece = board[x, y];
                if (piece == null || piece.isWhite != byWhite)
                    continue;

                if (CanPieceAttackSquare(board, piece, square))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if a specific piece can attack a square
    /// </summary>
    private bool CanPieceAttackSquare(BasePiece[,] board, BasePiece piece, Vector2Int targetSquare)
    {
        Vector2Int from = piece.currentSquare;

        switch (piece.pieceType)
        {
            case BasePiece.PieceType.Pawn:
                return CanPawnAttack(from, targetSquare, piece.isWhite);

            case BasePiece.PieceType.Knight:
                return CanKnightAttack(from, targetSquare);

            case BasePiece.PieceType.Bishop:
                return CanSlidingPieceAttack(board, from, targetSquare, true, false);

            case BasePiece.PieceType.Rook:
                return CanSlidingPieceAttack(board, from, targetSquare, false, true);

            case BasePiece.PieceType.Queen:
                return CanSlidingPieceAttack(board, from, targetSquare, true, true);

            case BasePiece.PieceType.King:
                return CanKingAttack(from, targetSquare);

            default:
                return false;
        }
    }

    private bool CanPawnAttack(Vector2Int from, Vector2Int to, bool isWhite)
    {
        int direction = isWhite ? 1 : -1;
        int fileDiff = Mathf.Abs(to.x - from.x);
        int rankDiff = to.y - from.y;

        return fileDiff == 1 && rankDiff == direction;
    }

    private bool CanKnightAttack(Vector2Int from, Vector2Int to)
    {
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        return (dx == 2 && dy == 1) || (dx == 1 && dy == 2);
    }

    private bool CanKingAttack(Vector2Int from, Vector2Int to)
    {
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        return dx <= 1 && dy <= 1 && (dx + dy > 0);
    }

    private bool CanSlidingPieceAttack(BasePiece[,] board, Vector2Int from, Vector2Int to, bool diagonal, bool straight)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;

        // Check if movement is in valid direction
        bool isDiagonal = Mathf.Abs(dx) == Mathf.Abs(dy) && dx != 0;
        bool isStraight = (dx == 0 || dy == 0) && (dx != 0 || dy != 0);

        if (diagonal && !isDiagonal && !isStraight) return false;
        if (!diagonal && straight && !isStraight) return false;
        if (diagonal && !straight && !isDiagonal) return false;
        if (!isDiagonal && !isStraight) return false;

        // Get direction
        int stepX = dx == 0 ? 0 : dx / Mathf.Abs(dx);
        int stepY = dy == 0 ? 0 : dy / Mathf.Abs(dy);

        // Check path is clear
        int x = from.x + stepX;
        int y = from.y + stepY;

        while (x != to.x || y != to.y)
        {
            if (board[x, y] != null)
                return false;

            x += stepX;
            y += stepY;
        }

        return true;
    }

    /// <summary>
    /// Checks if the current player is in check
    /// </summary>
    public bool IsKingInCheck(bool isWhite)
    {
        Vector2Int kingPos = FindKingPosition(boardManager.piecesOnBoard, isWhite);
        return IsSquareUnderAttack(boardManager.piecesOnBoard, kingPos, !isWhite);
    }

    /// <summary>
    /// Checks if the current player is in checkmate
    /// </summary>
    public bool IsCheckmate(bool isWhite)
    {
        // Must be in check
        if (!IsKingInCheck(isWhite))
            return false;

        // And have no legal moves
        return !HasAnyLegalMoves(isWhite);
    }

    /// <summary>
    /// Checks if the current player is in stalemate
    /// </summary>
    public bool IsStalemate(bool isWhite)
    {
        // Must NOT be in check
        if (IsKingInCheck(isWhite))
            return false;

        // And have no legal moves
        return !HasAnyLegalMoves(isWhite);
    }

    /// <summary>
    /// Checks if a player has any legal moves available
    /// </summary>
    private bool HasAnyLegalMoves(bool isWhite)
    {
        List<BasePiece> pieces = GetAllPiecesForColor(isWhite);

        foreach (BasePiece piece in pieces)
        {
            if (piece.normalMoves.Count > 0 || piece.captureMoves.Count > 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all pieces of a specific color from the board
    /// </summary>
    private List<BasePiece> GetAllPiecesForColor(bool isWhite)
    {
        List<BasePiece> pieces = new List<BasePiece>();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece piece = boardManager.piecesOnBoard[x, y];
                if (piece != null && piece.isWhite == isWhite)
                {
                    pieces.Add(piece);
                }
            }
        }

        return pieces;
    }

    /// <summary>
    /// Main method to call each turn - calculates and filters moves for the current player
    /// </summary>
    public void ValidateMovesForCurrentPlayer(bool isWhiteTurn)
    {
        // First, calculate all candidate moves for all pieces
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece piece = boardManager.piecesOnBoard[x, y];
                if (piece != null)
                {
                    piece.CalculateValidMoves(boardManager.piecesOnBoard);
                }
            }
        }

        // Then filter out illegal moves for the current player
        FilterIllegalMoves(isWhiteTurn);

        // Check game state
        if (IsCheckmate(isWhiteTurn))
        {
            Debug.Log($"Checkmate! {(isWhiteTurn ? "Black" : "White")} wins!");
            // Handle checkmate (end game, show UI, etc.)
        }
        else if (IsStalemate(isWhiteTurn))
        {
            Debug.Log("Stalemate! Draw!");
            // Handle stalemate (end game, show UI, etc.)
        }
        else if (IsKingInCheck(isWhiteTurn))
        {
            Debug.Log($"{(isWhiteTurn ? "White" : "Black")} is in check!");
        }
    }
}