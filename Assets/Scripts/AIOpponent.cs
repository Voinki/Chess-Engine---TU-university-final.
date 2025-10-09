using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
/// <summary>
/// /////////////////////////////////////////////// before making the optimization changes
/// </summary>
public class AIOpponent : MonoBehaviour
{
    private BoardManager boardManager;
    private MoveValidator moveValidator;
    private GameManager gameManager;

    private int searchDepth = 3;

    // Transposition table to cache board evaluations
    private Dictionary<string, (int eval, int depth)> transpositionTable = new Dictionary<string, (int, int)>();

    void Start()
    {
        boardManager = FindFirstObjectByType<BoardManager>();
        moveValidator = FindFirstObjectByType<MoveValidator>();
        moveValidator.boardManager = boardManager;
        gameManager = FindFirstObjectByType<GameManager>();
    }

    public void MakeMoveIfItsAITurn()
    {
        if (!gameManager.IsWhiteTurn && !gameManager.IsGameOver)
            StartCoroutine(MakeBestMove());
    }

    private IEnumerator MakeBestMove()
    {
        yield return new WaitUntil(() => !gameManager.isPromotionPending);

        Debug.Log("========== AI TURN START ==========");
        Debug.Log("Checking all black pieces on board:");
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece p = boardManager.piecesOnBoard[x, y];
                if (p != null && !p.isWhite)
                {
                    Debug.Log($"  [{x},{y}] = {p.pieceType} (name: {p.name})");
                }
            }
        }
        Debug.Log("AI thinking...");
        yield return new WaitForSeconds(1);

        // Store the ORIGINAL board reference
        BasePiece[,] realBoard = boardManager.piecesOnBoard;

        // Calculate moves for all pieces on the REAL board
        RecalculateAllMoves(realBoard);
        moveValidator.FilterIllegalMoves(false);

        List<BasePiece> blackPieces = GetAllPieces(realBoard, false);

        BasePiece bestPiece = null;
        Vector2Int bestMoveSquare = Vector2Int.zero;
        int bestEval = int.MaxValue;

        // Store all pieces' current squares before simulation
        Dictionary<BasePiece, Vector2Int> originalSquares = new Dictionary<BasePiece, Vector2Int>();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (realBoard[x, y] != null)
                    originalSquares[realBoard[x, y]] = realBoard[x, y].currentSquare;
            }
        }

        foreach (BasePiece piece in blackPieces)
        {
            List<Vector2Int> allMoves = piece.normalMoves.Concat(piece.captureMoves).ToList();

            // MOVE ORDERING: Evaluate captures first, they're more likely to be good
            allMoves = allMoves.OrderByDescending(move =>
            {
                BasePiece target = realBoard[move.x, move.y];
                if (target != null) return 100; // Captures first
                if (piece.pieceType == BasePiece.PieceType.Pawn && Mathf.Abs(move.y - piece.currentSquare.y) == 2) return 50; // Two-square pawn moves
                return 0; // Normal moves last
            }).ToList();

            if (allMoves.Count > 0)
                Debug.Log($"Evaluating {piece.pieceType} at {piece.currentSquare} with {allMoves.Count} moves: {string.Join(", ", allMoves)}");

            foreach (Vector2Int move in allMoves)
            {
                // Store ALL pieces' move lists before MiniMax
                Dictionary<BasePiece, (List<Vector2Int> normal, List<Vector2Int> capture)> savedMoves =
                    new Dictionary<BasePiece, (List<Vector2Int>, List<Vector2Int>)>();

                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        BasePiece p = realBoard[x, y];
                        if (p != null)
                        {
                            savedMoves[p] = (
                                new List<Vector2Int>(p.normalMoves),
                                new List<Vector2Int>(p.captureMoves)
                            );
                        }
                    }
                }

                // Apply move on the REAL board temporarily
                MoveRecord rec = ApplyMove(realBoard, piece, move);

                int eval = MiniMax(realBoard, searchDepth - 1, true, int.MinValue, int.MaxValue);

                // Undo the move
                UndoMove(realBoard, rec);

                // Restore ALL pieces' move lists
                foreach (var kvp in savedMoves)
                {
                    kvp.Key.UpdateLegalMoves(kvp.Value.normal, kvp.Value.capture);
                }

                if (eval < bestEval)
                {
                    bestEval = eval;
                    bestPiece = piece;
                    bestMoveSquare = move;
                }
            }
        }

        // Restore all pieces to their original squares (in case something went wrong)
        foreach (var kvp in originalSquares)
        {
            kvp.Key.currentSquare = kvp.Value;
        }

        // Recalculate moves on the real board after all simulations
        RecalculateAllMoves(realBoard);
        moveValidator.FilterIllegalMoves(false);

        if (bestPiece != null)
        {
            Debug.Log($"=== FINAL DECISION ===");
            Debug.Log($"Best piece is {bestPiece.pieceType} at {bestPiece.currentSquare}");
            Debug.Log($"It currently has {bestPiece.normalMoves.Count} normal moves: {string.Join(", ", bestPiece.normalMoves)}");
            Debug.Log($"It currently has {bestPiece.captureMoves.Count} capture moves: {string.Join(", ", bestPiece.captureMoves)}");
            Debug.Log($"But we're trying to move it to {bestMoveSquare}");
            Debug.Log($"AI plays {bestPiece.pieceType} from {bestPiece.currentSquare} to {bestMoveSquare} (eval: {bestEval})");

            // stupid promotion
            Vector2Int fromSquare = bestPiece.currentSquare;
            bestPiece.MoveTo(bestMoveSquare);

            // After MoveTo, if it was a pawn promotion, bestPiece is destroyed
            // We need to get the NEW piece at the target square
            BasePiece actualPiece = boardManager.piecesOnBoard[bestMoveSquare.x, bestMoveSquare.y];

            if (actualPiece != null && actualPiece != bestPiece)
            {
                Debug.Log($"Promotion, new piece is {actualPiece.pieceType}");
            }
            else
                boardManager.UpdateBoardState(bestPiece, bestMoveSquare);
            

            gameManager.SendMessage("SwitchTurns");

            // bestPiece.MoveTo(bestMoveSquare);
            // boardManager.UpdateBoardState(bestPiece, bestMoveSquare);
            // gameManager.SendMessage("SwitchTurns");
        }
        else
        {
            Debug.LogWarning("AI found no legal moves");
        }

        yield break;
    }

    private void RecalculateAllMoves(BasePiece[,] board)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece piece = board[x, y];
                if (piece != null)
                {
                    piece.CalculateValidMoves(board);
                }
            }
        }
    }

    #region MiniMax

    private int MiniMax(BasePiece[,] board, int depth, bool maximizingPlayer, int alpha, int beta)
    {
        // Check transposition table
        string boardHash = GetBoardHash(board);
        if (transpositionTable.ContainsKey(boardHash))
        {
            var cached = transpositionTable[boardHash];
            if (cached.depth >= depth)
                return cached.eval;
        }

        if (depth == 0)
        {
            int eval = EvaluateBoard(board);
            transpositionTable[boardHash] = (eval, depth);
            return eval;
        }

        bool isWhite = maximizingPlayer;

        // CRITICAL: Temporarily swap the board so MoveValidator uses the simulation
        BasePiece[,] originalBoardRef = boardManager.piecesOnBoard;
        boardManager.piecesOnBoard = board;

        // Now recalculate and filter moves - MoveValidator will use the correct board
        RecalculateAllMoves(board);
        moveValidator.FilterIllegalMoves(isWhite);

        List<BasePiece> pieces = GetAllPieces(board, isWhite);

        // Restore the original board reference immediately after filtering
        boardManager.piecesOnBoard = originalBoardRef;

        // Check if there are any legal moves
        bool hasLegalMoves = false;
        foreach (BasePiece p in pieces)
        {
            if (p.normalMoves.Count > 0 || p.captureMoves.Count > 0)
            {
                hasLegalMoves = true;
                break;
            }
        }

        if (!hasLegalMoves)
        {
            // Need to check king status with the simulated board
            boardManager.piecesOnBoard = board;
            bool inCheck = moveValidator.IsKingInCheck(isWhite);
            boardManager.piecesOnBoard = originalBoardRef;

            if (inCheck)
                return maximizingPlayer ? -10000 + depth : 10000 - depth;
            else
                return 0; // Stalemate
        }

        int bestEval = maximizingPlayer ? int.MinValue : int.MaxValue;

        foreach (BasePiece piece in pieces)
        {
            List<Vector2Int> moves = piece.normalMoves.Concat(piece.captureMoves).ToList();

            // MOVE ORDERING in search too
            moves = moves.OrderByDescending(move =>
            {
                BasePiece target = board[move.x, move.y];
                return target != null ? 100 : 0;
            }).ToList();

            foreach (Vector2Int move in moves)
            {
                MoveRecord rec = ApplyMove(board, piece, move);
                int eval = MiniMax(board, depth - 1, !maximizingPlayer, alpha, beta);
                UndoMove(board, rec);

                if (maximizingPlayer)
                {
                    bestEval = Mathf.Max(bestEval, eval);
                    alpha = Mathf.Max(alpha, eval);
                }
                else
                {
                    bestEval = Mathf.Min(bestEval, eval);
                    beta = Mathf.Min(beta, eval);
                }

                // Alpha-beta pruning
                if (beta <= alpha)
                    break;
            }

            if (beta <= alpha)
                break;
        }

        // Store in transposition table
        transpositionTable[boardHash] = (bestEval, depth);

        return bestEval;
    }

    private string GetBoardHash(BasePiece[,] board)
    {
        // Simple hash of board state
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece p = board[x, y];
                if (p == null)
                    sb.Append("_");
                else
                    sb.Append($"{(p.isWhite ? "W" : "B")}{(int)p.pieceType}");
            }
        }
        return sb.ToString();
    }

    private int EvaluateBoard(BasePiece[,] board)
    {
        Dictionary<BasePiece.PieceType, int> pieceValues = new Dictionary<BasePiece.PieceType, int>
        {
            {BasePiece.PieceType.Pawn, 100},
            {BasePiece.PieceType.Knight, 320},
            {BasePiece.PieceType.Bishop, 330},
            {BasePiece.PieceType.Rook, 500},
            {BasePiece.PieceType.Queen, 900},
            {BasePiece.PieceType.King, 20000},
        };

        int totalScore = 0;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece piece = board[x, y];
                if (piece == null)
                    continue;

                int value = pieceValues[piece.pieceType];

                // Add positional bonuses
                int positionalBonus = 0;

                // Encourage pawns to advance (especially center pawns)
                if (piece.pieceType == BasePiece.PieceType.Pawn)
                {
                    int advancement = piece.isWhite ? y : (7 - y);
                    positionalBonus += advancement * 5;

                    // Center pawns are worth more
                    if (x >= 2 && x <= 5)
                        positionalBonus += 10;
                }

                // Knights and bishops better in center
                if (piece.pieceType == BasePiece.PieceType.Knight || piece.pieceType == BasePiece.PieceType.Bishop)
                {
                    if (x >= 2 && x <= 5 && y >= 2 && y <= 5)
                        positionalBonus += 15;
                }

                // Slight bonus for development (getting pieces off back rank)
                if ((piece.isWhite && y > 0) || (!piece.isWhite && y < 7))
                {
                    if (piece.pieceType == BasePiece.PieceType.Knight ||
                        piece.pieceType == BasePiece.PieceType.Bishop)
                        positionalBonus += 10;
                }

                totalScore += piece.isWhite ? (value + positionalBonus) : -(value + positionalBonus);
            }
        }

        return totalScore;
    }

    private List<BasePiece> GetAllPieces(BasePiece[,] board, bool isWhite)
    {
        List<BasePiece> pieces = new List<BasePiece>();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece piece = board[x, y];
                if (piece != null && piece.isWhite == isWhite)
                    pieces.Add(piece);
            }
        }

        return pieces;
    }

    #endregion

    #region Apply/Undo moves

    private struct MoveRecord
    {
        public BasePiece movedPiece;
        public Vector2Int from;
        public Vector2Int to;
        public BasePiece capturedPiece;
        public bool movedPieceHasMoved;
    }

    private MoveRecord ApplyMove(BasePiece[,] board, BasePiece piece, Vector2Int targetSquare)
    {
        MoveRecord record = new MoveRecord
        {
            movedPiece = piece,
            from = piece.currentSquare,
            to = targetSquare,
            capturedPiece = board[targetSquare.x, targetSquare.y],
            movedPieceHasMoved = piece.hasMoved
        };

        // Verify the piece is actually where it thinks it is
        if (board[record.from.x, record.from.y] != piece)
        {
            Debug.LogError($"MISMATCH! {piece.pieceType} thinks it's at {record.from} but board has {(board[record.from.x, record.from.y]?.pieceType.ToString() ?? "null")}");
        }

        board[record.from.x, record.from.y] = null;
        board[record.to.x, record.to.y] = piece;
        piece.currentSquare = record.to;
        piece.hasMoved = true;

        return record;
    }

    private void UndoMove(BasePiece[,] board, MoveRecord record)
    {
        board[record.from.x, record.from.y] = record.movedPiece;
        board[record.to.x, record.to.y] = record.capturedPiece;
        record.movedPiece.currentSquare = record.from;
        record.movedPiece.hasMoved = record.movedPieceHasMoved;
    }

    #endregion
}