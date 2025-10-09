using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
public class Pawn : BasePiece
{
    void Awake()
    {
        pieceType = PieceType.Pawn;
    }
    public override void MoveTo(Vector2Int boardPosition)
    {
        int oldRank = currentSquare.y;

        bool isEnPassant = (boardManager.enPassantSquare.x != -1) &&
                           (boardPosition == boardManager.enPassantSquare) &&
                           boardManager.piecesOnBoard[boardPosition.x, boardPosition.y] == null;

        if (isEnPassant)
        {
            int capturedY = isWhite ? boardPosition.y - 1 : boardPosition.y + 1;
            Vector2Int capturedPawnSquare = new Vector2Int(boardPosition.x, capturedY);
            BasePiece capturedPawn = boardManager.piecesOnBoard[capturedPawnSquare.x, capturedPawnSquare.y];

            if (capturedPawn != null)
            {
                Destroy(capturedPawn.gameObject);
                boardManager.piecesOnBoard[capturedPawnSquare.x, capturedPawnSquare.y] = null;
                Debug.Log($"En passant captured pawn at {capturedPawnSquare}");
            }
        }

        base.MoveTo(boardPosition);

        if (Mathf.Abs(boardPosition.y - oldRank) == 2)
        {
            int middleRank = (oldRank + boardPosition.y) / 2;
            boardManager.enPassantSquare = new Vector2Int(boardPosition.x, middleRank);
            Debug.Log($"En passant square set to {boardManager.enPassantSquare}");
        }

        if ((isWhite && currentSquare.y == 7) || (!isWhite && currentSquare.y == 0))
        {
            if (!isWhite)
            {
                GameManager gameManager = FindFirstObjectByType<GameManager>();
                gameManager.isPromotionPending = true;
                Promote(BasePiece.PieceType.Queen);
                gameManager.isPromotionPending = false;
            }
            else
                StartCoroutine(HandlePromotion()); // user -> coroutine for UI

        }

    }

    public override void CalculateValidMoves(BasePiece[,] board)
    {
        normalMoves.Clear();
        captureMoves.Clear();

        int direction = isWhite ? 1 : -1;
        int startRow = isWhite ? 1 : 6;
        int x = currentSquare.x;
        int y = currentSquare.y;

        Vector2Int[] diagonals = { new Vector2Int(-1, direction), new Vector2Int(1, direction) };

        foreach (var diagonal in diagonals)
        {
            Vector2Int target = currentSquare + diagonal;

            if (!IsInsideBoard(target)) continue;

            BasePiece targetPiece = board[target.x, target.y];

            if (targetPiece != null && targetPiece.isWhite != isWhite)
                captureMoves.Add(target); // normal
            else if (target == boardManager.enPassantSquare)
            {
                if ((isWhite && y == 4) || (!isWhite && y == 3))
                {
                    int enemyY = isWhite ? y : y;
                    BasePiece sidePawn = board[target.x, enemyY];
                    if (sidePawn != null && sidePawn.pieceType == PieceType.Pawn && sidePawn.isWhite != isWhite)
                        captureMoves.Add(target); // en passant
                }
            }
        }

        // Forward move
        int oneStepY = y + direction;
        int twoStepY = y + 2 * direction;

        if (oneStepY >= 0 && oneStepY < 8 && board[x, oneStepY] == null)
        {
            normalMoves.Add(new Vector2Int(x, oneStepY));

            // Only allow 2-step if still inside board
            if (!hasMoved && twoStepY >= 0 && twoStepY < 8 && board[x, twoStepY] == null)
                normalMoves.Add(new Vector2Int(x, twoStepY));
        }


        UpdateLegalMoves(normalMoves, captureMoves);
    }


    private System.Collections.IEnumerator HandlePromotion()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        gameManager.isPromotionPending = true;

        PromotionUI promotionUI = FindFirstObjectByType<PromotionUI>();
        PieceType chosenType = PieceType.Queen; // default
        bool selectionDone = false;

        promotionUI.Show((selectedPiece) =>
        {
            chosenType = selectedPiece;
            selectionDone = true;
        });

        yield return new WaitUntil(() => selectionDone);

        Promote(chosenType);
        gameManager.isPromotionPending = false;
    }
    private void Promote(PieceType newType)
    {
        Vector2Int pos = currentSquare;

        Destroy(gameObject);

        PieceManager pieceManager = FindFirstObjectByType<PieceManager>();
        BasePiece promotedPiece = pieceManager.PromotePieceAt(pos, isWhite, newType);

        if (promotedPiece == null)
            return;

        boardManager.piecesOnBoard[pos.x, pos.y] = promotedPiece;
        promotedPiece.CalculateValidMoves(boardManager.piecesOnBoard);
    }

    private bool IsInsideBoard(Vector2Int square)
    {
        return square.x >= 0 && square.x < 8 && square.y >= 0 && square.y < 8;
    }

}
