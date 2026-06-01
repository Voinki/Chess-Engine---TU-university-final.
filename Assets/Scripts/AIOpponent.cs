using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIOpponent : MonoBehaviour
{
    private BoardManager boardManager;
    private MoveValidator moveValidator;
    private GameManager gameManager;

    private int searchDepth = 4;
    private const int DRAW_CONTEMPT = 15;
    private const int MATE_SCORE = 10000;

    // Debug counters for testing alpha-beta
    private int nodesSearched = 0;
    private int nodesPruned = 0;
    private int ttHits = 0;
    private bool enableAlphaBeta = true; // Toggle for testing

    // Base piece values (in centipawns)
    private static readonly Dictionary<BasePiece.PieceType, int> BASE_PIECE_VALUES = new Dictionary<BasePiece.PieceType, int>
    {
        {BasePiece.PieceType.Pawn, 100},
        {BasePiece.PieceType.Knight, 320},
        {BasePiece.PieceType.Bishop, 330},
        {BasePiece.PieceType.Rook, 500},
        {BasePiece.PieceType.Queen, 900},
        {BasePiece.PieceType.King, 20000},
    };

    // Piece-Square Tables (from white's perspective, will be flipped for black)
    private static readonly int[,] PAWN_TABLE = new int[,]
    {
        {  0,  0,  0,  0,  0,  0,  0,  0 },
        { 50, 50, 50, 50, 50, 50, 50, 50 },
        { 10, 10, 20, 30, 30, 20, 10, 10 },
        {  5,  5, 10, 25, 25, 10,  5,  5 },
        {  0,  0,  0, 20, 20,  0,  0,  0 },
        {  5, -5,-10,  0,  0,-10, -5,  5 },
        {  5, 10, 10,-20,-20, 10, 10,  5 },
        {  0,  0,  0,  0,  0,  0,  0,  0 }
    };

    private static readonly int[,] KNIGHT_TABLE = new int[,]
    {
        { -50,-40,-30,-30,-30,-30,-40,-50 },
        { -40,-20,  0,  0,  0,  0,-20,-40 },
        { -30,  0, 10, 15, 15, 10,  0,-30 },
        { -30,  5, 15, 20, 20, 15,  5,-30 },
        { -30,  0, 15, 20, 20, 15,  0,-30 },
        { -30,  5, 10, 15, 15, 10,  5,-30 },
        { -40,-20,  0,  5,  5,  0,-20,-40 },
        { -50,-40,-30,-30,-30,-30,-40,-50 }
    };

    private static readonly int[,] BISHOP_TABLE = new int[,]
    {
        { -20,-10,-10,-10,-10,-10,-10,-20 },
        { -10,  0,  0,  0,  0,  0,  0,-10 },
        { -10,  0,  5, 10, 10,  5,  0,-10 },
        { -10,  5,  5, 10, 10,  5,  5,-10 },
        { -10,  0, 10, 10, 10, 10,  0,-10 },
        { -10, 10, 10, 10, 10, 10, 10,-10 },
        { -10,  5,  0,  0,  0,  0,  5,-10 },
        { -20,-10,-10,-10,-10,-10,-10,-20 }
    };

    private static readonly int[,] ROOK_TABLE = new int[,]
    {
        {  0,  0,  0,  0,  0,  0,  0,  0 },
        {  5, 10, 10, 10, 10, 10, 10,  5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        {  0,  0,  0,  5,  5,  0,  0,  0 }
    };

    private static readonly int[,] QUEEN_TABLE = new int[,]
    {
        { -20,-10,-10, -5, -5,-10,-10,-20 },
        { -10,  0,  0,  0,  0,  0,  0,-10 },
        { -10,  0,  5,  5,  5,  5,  0,-10 },
        {  -5,  0,  5,  5,  5,  5,  0, -5 },
        {   0,  0,  5,  5,  5,  5,  0, -5 },
        { -10,  5,  5,  5,  5,  5,  0,-10 },
        { -10,  0,  5,  0,  0,  0,  0,-10 },
        { -20,-10,-10, -5, -5,-10,-10,-20 }
    };

    private static readonly int[,] KING_TABLE = new int[,]
    {
        { -30,-40,-40,-50,-50,-40,-40,-30 },
        { -30,-40,-40,-50,-50,-40,-40,-30 },
        { -30,-40,-40,-50,-50,-40,-40,-30 },
        { -30,-40,-40,-50,-50,-40,-40,-30 },
        { -20,-30,-30,-40,-40,-30,-30,-20 },
        { -10,-20,-20,-20,-20,-20,-20,-10 },
        {  20, 20,  0,  0,  0,  0, 20, 20 },
        {  20, 30, 10,  0,  0, 10, 30, 20 }
    };

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
        if (!gameManager.IsWhiteTurn && !gameManager.IsGameOver && !gameManager.isPromotionPending)
            StartCoroutine(MakeBestMove());
    }

    private IEnumerator MakeBestMove()
    {
        yield return new WaitUntil(() => !gameManager.isPromotionPending);

        BasePiece[,] realBoard = boardManager.piecesOnBoard;

        RecalculateAllMoves(realBoard);
        moveValidator.FilterIllegalMoves(false);

        List<BasePiece> blackPieces = GetAllPieces(realBoard, false);

        BasePiece bestPiece = null;
        Vector2Int bestMoveSquare = Vector2Int.zero;
        int bestEval = int.MaxValue;

        var orderedMoves = GetOrderedMoves(realBoard, blackPieces);

        // Reset debug counters
        nodesSearched = 0;
        nodesPruned = 0;
        ttHits = 0;
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        foreach (var (piece, move) in orderedMoves)
        {
            MoveRecord record = ApplyMove(realBoard, piece, move);
            int eval = MiniMax(realBoard, searchDepth - 1, true, int.MinValue, int.MaxValue);

            eval += CheckRepetitionPenalty(realBoard);

            UndoMove(realBoard, record);

            if (eval < bestEval)
            {
                bestEval = eval;
                bestPiece = piece;
                bestMoveSquare = move;
            }
        }

        sw.Stop();
        float prunePercent = nodesSearched > 0 ? (nodesPruned * 100f / nodesSearched) : 0;
        float ttPercent = nodesSearched > 0 ? (ttHits * 100f / nodesSearched) : 0;
        Debug.Log($"Search stats: {nodesSearched} nodes, {nodesPruned} pruned ({prunePercent:F1}%), {ttHits} TT hits ({ttPercent:F1}%), {sw.ElapsedMilliseconds}ms");

        RecalculateAllMoves(realBoard);
        moveValidator.FilterIllegalMoves(false);

        if (bestPiece != null)
        {
            ExecuteBestMove(bestPiece, bestMoveSquare, bestEval);
        }
        else
        {
            Debug.LogWarning("AI found no legal moves");
        }

        yield break;
    }

    private void ExecuteBestMove(BasePiece piece, Vector2Int moveSquare, int eval)
    {
        // Debug.Log($"AI plays {piece.pieceType} from {piece.currentSquare} to {moveSquare} (eval: {eval})");
        // Vector2Int fromSquare = piece.currentSquare;
        // piece.MoveTo(moveSquare);
        // boardManager.UpdateBoardState(piece, moveSquare);

        // boardManager.ClearMoveHighlights();
        // Color orange = new Color(1f, 0.55f, 0f);
        // boardManager.HighlightSquare(fromSquare, orange);
        // boardManager.HighlightSquare(moveSquare, orange);
        // gameManager.SendMessage("SwitchTurns");

        Debug.Log($"AI plays {piece.pieceType} from {piece.currentSquare} to {moveSquare} (eval: {eval})");
        Vector2Int fromSquare = piece.currentSquare;

        // MoveTo handles all board updates including promotion
        piece.MoveTo(moveSquare);

        // DON'T call UpdateBoardState - MoveTo already handled it!
        // If promotion happened, the pawn is destroyed and replaced with a new piece
        // boardManager.UpdateBoardState(piece, moveSquare); // REMOVED

        boardManager.ClearMoveHighlights();
        Color orange = new Color(1f, 0.55f, 0f);
        boardManager.HighlightSquare(fromSquare, orange);
        boardManager.HighlightSquare(moveSquare, orange);
        gameManager.SendMessage("SwitchTurns");
    }

    private int CheckRepetitionPenalty(BasePiece[,] board)
    {
        var history = gameManager.GetPositionHistory();
        string posKey = GetBoardHash(board);

        int startIdx = Mathf.Max(0, history.Count - 8);
        for (int i = history.Count - 1; i >= startIdx; i--)
        {
            if (history[i] == posKey)
                return 20;
        }
        return 0;
    }

    private List<(BasePiece piece, Vector2Int move)> GetOrderedMoves(BasePiece[,] board, List<BasePiece> pieces)
    {
        var moves = new List<(BasePiece, Vector2Int, int)>();

        foreach (BasePiece piece in pieces)
        {
            foreach (Vector2Int move in piece.normalMoves.Concat(piece.captureMoves))
            {
                int score = ScoreMove(board, piece, move);
                moves.Add((piece, move, score));
            }
        }

        return moves.OrderByDescending(m => m.Item3)
                    .Select(m => (m.Item1, m.Item2))
                    .ToList();
    }

    private int ScoreMove(BasePiece[,] board, BasePiece piece, Vector2Int move)
    {
        int score = 0;
        BasePiece target = board[move.x, move.y];

        // 1. Captures (MVV-LVA)
        if (target != null)
        {
            int victim = BASE_PIECE_VALUES[target.pieceType];
            int attacker = BASE_PIECE_VALUES[piece.pieceType];
            score = 10000 + victim - attacker;
        }
        // 2. Piece-square table improvement
        else
        {
            int currentPST = GetPositionalBonus(piece.pieceType, piece.isWhite, piece.currentSquare.x, piece.currentSquare.y);
            int newPST = GetPositionalBonus(piece.pieceType, piece.isWhite, move.x, move.y);
            score = (newPST - currentPST) * 10; // Amplify positional gains
        }

        // 3. Center control bonus
        if (move.x >= 3 && move.x <= 4 && move.y >= 3 && move.y <= 4)
            score += 20;

        // 4. Pawn pushes
        if (piece.pieceType == BasePiece.PieceType.Pawn)
        {
            int pushDist = Mathf.Abs(move.y - piece.currentSquare.y);
            if (pushDist == 2)
                score += 15;
            else if (pushDist == 1)
                score += 5;
        }

        return score;
    }

    private void RecalculateAllMoves(BasePiece[,] board)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                board[x, y]?.CalculateValidMoves(board);
            }
        }
    }

    #region MiniMax

    private int MiniMax(BasePiece[,] board, int depth, bool maximizingPlayer, int alpha, int beta)
    {
        nodesSearched++;

        string boardHash = GetBoardHash(board);
        if (transpositionTable.TryGetValue(boardHash, out var cached) && cached.depth >= depth)
        {
            ttHits++;
            int cachedEval = cached.eval;
            return (cachedEval == 0) ? (maximizingPlayer ? DRAW_CONTEMPT : -DRAW_CONTEMPT) : cachedEval;
        }

        if (depth == 0)
        {
            int eval = EvaluateBoard(board);
            transpositionTable[boardHash] = (eval, depth);
            return eval;
        }

        bool isWhite = maximizingPlayer;

        BasePiece[,] originalBoardRef = boardManager.piecesOnBoard;
        boardManager.piecesOnBoard = board;

        try
        {
            RecalculateAllMoves(board);
            moveValidator.FilterIllegalMoves(isWhite);

            List<BasePiece> pieces = GetAllPieces(board, isWhite);

            if (!HasLegalMoves(pieces))
            {
                bool inCheck = moveValidator.IsKingInCheck(isWhite);
                int result;
                if (inCheck)
                    result = maximizingPlayer ? -MATE_SCORE + depth : MATE_SCORE - depth;
                else
                    result = 0; // Stalemate (will be adjusted below)

                transpositionTable[boardHash] = (result, depth);
                return (result == 0) ? (maximizingPlayer ? DRAW_CONTEMPT : -DRAW_CONTEMPT) : result;
            }

            int bestEval = maximizingPlayer ? int.MinValue : int.MaxValue;
            var orderedMoves = GetOrderedMoves(board, pieces);

            foreach (var (piece, move) in orderedMoves)
            {
                if (board[move.x, move.y]?.pieceType == BasePiece.PieceType.King)
                    continue;

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
                if (enableAlphaBeta && beta <= alpha)
                {
                    nodesPruned++;
                    break;
                }
            }

            // Store raw bestEval in TT (no draw contempt)
            transpositionTable[boardHash] = (bestEval, depth);

            // Apply draw contempt only when returning
            int adjusted = (bestEval == 0) ? (maximizingPlayer ? DRAW_CONTEMPT : -DRAW_CONTEMPT) : bestEval;
            return adjusted;
        }
        finally
        {
            boardManager.piecesOnBoard = originalBoardRef;
        }
    }

    private bool HasLegalMoves(List<BasePiece> pieces)
    {
        foreach (BasePiece piece in pieces)
        {
            if (piece.normalMoves.Count > 0 || piece.captureMoves.Count > 0)
                return true;
        }
        return false;
    }

    private string GetBoardHash(BasePiece[,] board)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece p = board[x, y];
                if (p == null)
                    sb.Append('_');
                else
                    sb.Append($"{(p.isWhite ? 'W' : 'B')}{(int)p.pieceType}");
            }
        }
        return sb.ToString();
    }

    private int EvaluateBoard(BasePiece[,] board)
    {
        int totalScore = 0;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                BasePiece piece = board[x, y];
                if (piece == null)
                    continue;

                int pieceValue = GetPieceValue(piece, x, y);
                totalScore += piece.isWhite ? pieceValue : -pieceValue;
            }
        }

        return totalScore;
    }

    // NEW: Get piece value including positional bonus from piece-square tables
    private int GetPieceValue(BasePiece piece, int x, int y)
    {
        int baseValue = BASE_PIECE_VALUES[piece.pieceType];
        int positionalBonus = GetPositionalBonus(piece.pieceType, piece.isWhite, x, y);

        return baseValue + positionalBonus;
    }

    // NEW: Get positional bonus from piece-square tables
    private int GetPositionalBonus(BasePiece.PieceType pieceType, bool isWhite, int x, int y)
    {
        // For black pieces, flip the y-coordinate to use the same tables
        int tableY = isWhite ? y : (7 - y);

        switch (pieceType)
        {
            case BasePiece.PieceType.Pawn:
                return PAWN_TABLE[tableY, x];

            case BasePiece.PieceType.Knight:
                return KNIGHT_TABLE[tableY, x];

            case BasePiece.PieceType.Bishop:
                return BISHOP_TABLE[tableY, x];

            case BasePiece.PieceType.Rook:
                return ROOK_TABLE[tableY, x];

            case BasePiece.PieceType.Queen:
                return QUEEN_TABLE[tableY, x];

            case BasePiece.PieceType.King:
                return KING_TABLE[tableY, x];

            default:
                return 0;
        }
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

    #region Apply/Undo Moves

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

    public void ClearTranspositionTable()
    {
        transpositionTable.Clear();
    }

    // Testing method - compare alpha-beta vs full minimax
    public void TestAlphaBeta()
    {
        BasePiece[,] board = boardManager.piecesOnBoard;

        // Test with alpha-beta enabled
        enableAlphaBeta = true;
        transpositionTable.Clear();
        nodesSearched = 0;
        nodesPruned = 0;
        ttHits = 0;

        System.Diagnostics.Stopwatch sw1 = System.Diagnostics.Stopwatch.StartNew();
        int evalWithPruning = MiniMax(board, searchDepth, false, int.MinValue, int.MaxValue);
        sw1.Stop();

        int nodesWithPruning = nodesSearched;
        int pruned = nodesPruned;
        int ttWithPruning = ttHits;

        // Test without alpha-beta
        enableAlphaBeta = false;
        transpositionTable.Clear();
        nodesSearched = 0;
        nodesPruned = 0;
        ttHits = 0;

        System.Diagnostics.Stopwatch sw2 = System.Diagnostics.Stopwatch.StartNew();
        int evalWithoutPruning = MiniMax(board, searchDepth, false, int.MinValue, int.MaxValue);
        sw2.Stop();

        int nodesWithoutPruning = nodesSearched;
        int ttWithoutPruning = ttHits;

        // Re-enable alpha-beta
        enableAlphaBeta = true;

        Debug.Log("=== Alpha-Beta Test Results ===");
        Debug.Log($"Eval WITH pruning: {evalWithPruning}");
        Debug.Log($"Eval WITHOUT pruning: {evalWithoutPruning}");
        Debug.Log($"Evaluations match: {evalWithPruning == evalWithoutPruning}");
        Debug.Log($"Nodes with pruning: {nodesWithPruning} ({sw1.ElapsedMilliseconds}ms, {ttWithPruning} TT hits)");
        Debug.Log($"Nodes without pruning: {nodesWithoutPruning} ({sw2.ElapsedMilliseconds}ms, {ttWithoutPruning} TT hits)");
        Debug.Log($"Branches pruned: {pruned}");
        Debug.Log($"Efficiency: {100 - (nodesWithPruning * 100 / nodesWithoutPruning)}% reduction");
    }

    #endregion
}