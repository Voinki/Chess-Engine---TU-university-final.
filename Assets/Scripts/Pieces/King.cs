using System;
using System.Linq;
using UnityEngine;

public class King : BasePiece
{
  void Awake()
  {
    pieceType = PieceType.King;
  }
  public override void MoveTo(Vector2Int boardPosition)
  {
    if (pieceType == PieceType.King && Mathf.Abs(boardPosition.x - currentSquare.x) == 2)
    {
      int rank = isWhite ? 0 : 7;

      if (boardPosition.x > currentSquare.x) // king side castle, else is queen side
      {

        BasePiece rook = boardManager.piecesOnBoard[7, rank];
        if (rook != null && rook.pieceType == PieceType.Rook)     
          rook.MoveTo(new Vector2Int(5, rank));
      }
      else
      {
        BasePiece rook = boardManager.piecesOnBoard[0, rank];
        if (rook != null && rook.pieceType == PieceType.Rook)   
          rook.MoveTo(new Vector2Int(3, rank));
      }
    }

    base.MoveTo(boardPosition);
    boardManager.piecesOnBoard[boardPosition.x, boardPosition.y] = this;
  }

  public override void CalculateValidMoves(BasePiece[,] board)
  {
    normalMoves.Clear();
    captureMoves.Clear();

    Vector2Int[] directions = GetSlidingMoveDirections();

    foreach (Vector2Int direction in directions)
    {
      for (int step = 1; step < 2; step++)
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

    BoardManager boardManager = FindFirstObjectByType<BoardManager>();
    var allMoves = normalMoves.Concat(captureMoves).ToList();
    normalMoves = normalMoves
            .Where(move => !boardManager.IsSquareUnderAttack(move, !isWhite))
            .ToList();

    captureMoves = captureMoves
            .Where(move => !boardManager.IsSquareUnderAttack(move, !isWhite))
            .ToList();
    AddCastlingMoves(board);

    UpdateLegalMoves(normalMoves, captureMoves);
  }

  private void AddCastlingMoves(BasePiece[,] board)
  {
    BoardManager boardManager = FindFirstObjectByType<BoardManager>();

    if (hasMoved) return;

    int rank = isWhite ? 0 : 7; // 00 if white/ 7 if black

    BasePiece rookKingSide = board[7, rank];
    if (rookKingSide != null && rookKingSide.pieceType == PieceType.Rook && !rookKingSide.hasMoved)
    {
      if (board[5, rank] == null && board[6, rank] == null)
      {
        if (!boardManager.IsSquareUnderAttack(new Vector2Int(4, rank), !isWhite) &&
            !boardManager.IsSquareUnderAttack(new Vector2Int(5, rank), !isWhite) &&
            !boardManager.IsSquareUnderAttack(new Vector2Int(6, rank), !isWhite))
          normalMoves.Add(new Vector2Int(6, rank)); // king moves 2 squares to the right
      }
    }

    BasePiece rookQueenSide = board[0, rank];

    if (rookQueenSide != null && rookQueenSide.pieceType == PieceType.Rook && !rookQueenSide.hasMoved)
    {
      if (board[1, rank] == null && board[2, rank] == null && board[3, rank] == null)
      {
        if (!boardManager.IsSquareUnderAttack(new Vector2Int(4, rank), !isWhite) &&
            !boardManager.IsSquareUnderAttack(new Vector2Int(3, rank), !isWhite) &&
            !boardManager.IsSquareUnderAttack(new Vector2Int(2, rank), !isWhite))
        {
          normalMoves.Add(new Vector2Int(2, rank)); // king moves two squaresto the left
        }
      }
    }

  }
}
