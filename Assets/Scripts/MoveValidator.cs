using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveValidator : MonoBehaviour
{
    public BoardManager boardManager;

    void Awake()
    {
            Debug.Log($"MoveValidator Awake on {gameObject.name} (InstanceID {GetInstanceID()})");

        boardManager = GetComponent<BoardManager>();
    }
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

    private bool IsMoveLegal(BasePiece piece, Vector2Int targetSquare)
    {
        BasePiece[,] simulatedBoard = SimulateMove(piece, targetSquare, out BasePiece capturedPiece, out Vector2Int originalSquare);

        Vector2Int kingPos = FindKingPosition(simulatedBoard, piece.isWhite);

        bool kingInCheck = IsSquareUnderAttack(simulatedBoard, kingPos, !piece.isWhite);

        return !kingInCheck;
    }
    private BasePiece[,] SimulateMove(BasePiece piece, Vector2Int targetSquare, out BasePiece capturedPiece, out Vector2Int originalSquare)
    {
        // cop the board
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

        simBoard[originalSquare.x, originalSquare.y] = null;
        simBoard[targetSquare.x, targetSquare.y] = piece;

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

    private bool IsSquareUnderAttack(BasePiece[,] board, Vector2Int square, bool byWhite)
    {
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
        int diagonalX = to.x - from.x;
        int diagonalY = to.y - from.y;

        bool isDiagonal = Mathf.Abs(diagonalX) == Mathf.Abs(diagonalY) && diagonalX != 0;
        bool isStraight = (diagonalX == 0 || diagonalY == 0) && (diagonalX != 0 || diagonalY != 0);

        if (diagonal && !isDiagonal && !isStraight) return false;
        if (!diagonal && straight && !isStraight) return false;
        if (diagonal && !straight && !isDiagonal) return false;
        if (!isDiagonal && !isStraight) return false;

        // direction
        int stepX = diagonalX == 0 ? 0 : diagonalX / Mathf.Abs(diagonalX);
        int stepY = diagonalY == 0 ? 0 : diagonalY / Mathf.Abs(diagonalY);

        // chekc if path clear
        int pathX = from.x + stepX;
        int pathY = from.y + stepY;

        while (pathX != to.x || pathY != to.y)
        {
            if (board[pathX, pathY] != null)
                return false;

            pathX += stepX;
            pathY += stepY;
        }

        return true;
    }

    public bool IsKingInCheck(bool isWhite)
    {
        Vector2Int kingPos = FindKingPosition(boardManager.piecesOnBoard, isWhite);
        return IsSquareUnderAttack(boardManager.piecesOnBoard, kingPos, !isWhite);
    }

    public bool IsCheckmate(bool isWhite)
    {
        if (!IsKingInCheck(isWhite))
            return false;

        return !HasAnyLegalMoves(isWhite);
    }

    public bool IsStalemate(bool isWhite)
    {
        if (IsKingInCheck(isWhite))
            return false;

        return !HasAnyLegalMoves(isWhite);
    }

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

    public void ValidateMovesForCurrentPlayer(bool isWhiteTurn)
    {
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

        FilterIllegalMoves(isWhiteTurn);
    }

public bool IsInsufficientMaterial()
{
    List<BasePiece> whitePieces = GetAllPiecesForColor(true);
    List<BasePiece> blackPieces = GetAllPiecesForColor(false);
    
    if (whitePieces.Count == 1 && blackPieces.Count == 1)
        return true;
    
    if ((whitePieces.Count == 1 && blackPieces.Count == 2) ||
        (whitePieces.Count == 2 && blackPieces.Count == 1))
    {
        List<BasePiece> twoPieces = whitePieces.Count == 2 ? whitePieces : blackPieces;
        
        foreach (BasePiece piece in twoPieces)
        {
            if (piece.pieceType == BasePiece.PieceType.Bishop || 
                piece.pieceType == BasePiece.PieceType.Knight)
                return true;
        }
    }
    
    if (whitePieces.Count == 2 && blackPieces.Count == 2)
    {
        BasePiece whiteBishop = whitePieces.Find(p => p.pieceType == BasePiece.PieceType.Bishop);
        BasePiece blackBishop = blackPieces.Find(p => p.pieceType == BasePiece.PieceType.Bishop);
        
        if (whiteBishop != null && blackBishop != null)
        {
            bool whiteOnLightSquare = (whiteBishop.currentSquare.x + whiteBishop.currentSquare.y) % 2 == 1;
            bool blackOnLightSquare = (blackBishop.currentSquare.x + blackBishop.currentSquare.y) % 2 == 1;
            
            if (whiteOnLightSquare == blackOnLightSquare)
                return true;
        }
    }
    
    return false;
}
}