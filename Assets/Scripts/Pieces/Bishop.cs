using System.Linq;
using UnityEngine;

public class Bishop : BasePiece
{
    void Awake()
    {
        pieceType = PieceType.Bishop;
    }
    public override void MoveTo(Vector2Int boardPosition)
    {
        base.MoveTo(boardPosition);
    }

    public override void CalculateValidMoves(BasePiece[,] board)
    {
        normalMoves.Clear();
        captureMoves.Clear();

        Vector2Int[] directions = GetSlidingMoveDirections();

        foreach (Vector2Int direction in directions)
        {
            for (int step = 1; step < 8; step++)
            {
                int newFile = currentSquare.x + direction.x * step;
                int newRank = currentSquare.y + direction.y * step;

                if (newFile < 0 || newFile >= 8 || newRank < 0 || newRank >= 8)
                    break;

                BasePiece targetPiece = board[newFile, newRank];

                if (targetPiece == null)
                    normalMoves.Add(new Vector2Int(newFile, newRank));
                else
                {
                    if (targetPiece.isWhite != this.isWhite)
                        captureMoves.Add(new Vector2Int(newFile, newRank));
                    break;
                }

            }
        }

        UpdateLegalMoves(normalMoves, captureMoves);
    }
}
