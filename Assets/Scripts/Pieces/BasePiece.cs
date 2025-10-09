using System.Collections.Generic;
using UnityEngine;

public abstract class BasePiece : MonoBehaviour
{
    public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King };
    public bool isWhite; // True for white , false for black 
    public bool hasMoved = false; // for pawns and castling
    public Vector2Int currentSquare;
    private List<Vector2Int> legalMoves = new List<Vector2Int>();
    private List<Vector2Int> legalCaptures = new List<Vector2Int>();
    public List<Vector2Int> GetLegalMoves => legalMoves;

    public List<Vector2Int> GetLegalCaptures => legalCaptures;
    public PieceType pieceType;
    public BoardManager boardManager;
    public List<Vector2Int> captureMoves = new List<Vector2Int>();
    public List<Vector2Int> normalMoves = new List<Vector2Int>();
    virtual public void MoveTo(Vector2Int targetSquare)
    {
        BasePiece targetPiece = boardManager.piecesOnBoard[targetSquare.x, targetSquare.y];
        if (targetPiece != null && targetPiece != this)
        {
            // Capture it
            Destroy(targetPiece.gameObject);
            boardManager.piecesOnBoard[targetSquare.x, targetSquare.y] = null;
        }

         boardManager.piecesOnBoard[currentSquare.x, currentSquare.y] = null;

        currentSquare = targetSquare;
        hasMoved = true;
        boardManager.piecesOnBoard[targetSquare.x, targetSquare.y] = this;
       
        Transform squareTransform = boardManager.boardParent.Find($"{(char)('a' + targetSquare.x)}{targetSquare.y + 1}");
        if (squareTransform != null)
            transform.position = squareTransform.position + Vector3.up * 0.05f;
        else
            Debug.LogError($"MoveTo cannot find a valid square");
    }

    public abstract void CalculateValidMoves(BasePiece[,] board);
    public void UpdateLegalMoves(List<Vector2Int> normal, List<Vector2Int> captures)
    {
        normalMoves = new List<Vector2Int>(normal);
        captureMoves = new List<Vector2Int>(captures);

        // For validation purposes, legalMoves contains all
        legalMoves = new List<Vector2Int>();
        legalMoves.AddRange(normalMoves);
        legalMoves.AddRange(captureMoves);

        // Keep legalCaptures separate for highlighting red
        legalCaptures = new List<Vector2Int>(captureMoves);
    }

    protected Vector2Int[] GetSlidingMoveDirections()
    {
        // rook/queen/bishop/king
        Vector2Int[] directions = new Vector2Int[] { };
        if (pieceType == PieceType.Queen || pieceType == PieceType.King)
        {
            directions = new Vector2Int[]{
            new Vector2Int(0,1), // up
            new Vector2Int(0,-1), //down
            new Vector2Int(1,0), //right
            new Vector2Int(-1,0), //left
            new Vector2Int(1, 1), // up-right
            new Vector2Int(-1, -1), //down-left
            new Vector2Int(1, -1), //right-down
            new Vector2Int(-1, 1) //left-up     
        };
        }
        else if (pieceType == PieceType.Rook)
        {
            directions = new Vector2Int[]{
            new Vector2Int(0,1), // up
            new Vector2Int(0,-1), //down
            new Vector2Int(1,0), //right
            new Vector2Int(-1,0) //left
        };
        }
        else
        {
            directions = new Vector2Int[] {
                new Vector2Int(1, 1), // up-right
                new Vector2Int(-1, -1), //down-left
                new Vector2Int(1, -1), //right-down
                new Vector2Int(-1, 1) //left-up
            };
        }

        return directions;
    }
}

