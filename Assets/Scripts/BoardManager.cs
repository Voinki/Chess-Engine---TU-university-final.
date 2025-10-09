using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardManager : MonoBehaviour
{
    [SerializeField] public Transform boardParent;
    public BasePiece[,] piecesOnBoard = new BasePiece[8, 8];
    private Dictionary<Transform, Color> squareOriginalColors = new Dictionary<Transform, Color>();
    public Material transparentMaterial;
    private GameObject lastHighlightedSquare;

    // public Vector2Int? lastPawnToDoubleMoveSquare = null;
    public Vector2Int enPassantSquare = new Vector2Int(-1, -1);

    void Awake() // awake is called before start, which means the board will be generated before trying to place pieces.
    {
        GenerateBoard();
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HighlightEmptySquare();
    }

    private void GenerateBoard()
    {
        float squareSize = 0.1f;
        float offset = (8 * squareSize) / 2f;

        Color lightColor = Color.white;
        // Color darkColor = new Color(0.55f, 0.27f, 0.07f); // wood-ish brown
         Color darkColor = new Color(0.0f, 0.0f, 0.0f);


        for (int files = 0; files < 8; files++)
        {
            for (int ranks = 0; ranks < 8; ranks++)
            {
                GameObject square = GameObject.CreatePrimitive(PrimitiveType.Quad);
                square.transform.rotation = Quaternion.Euler(90, 0, 0);

                float positionX = (files * squareSize) - offset + squareSize / 2f;
                float positionZ = (ranks * squareSize) - offset + squareSize / 2f;

                square.transform.position = new Vector3(positionX, 0.01f, positionZ);
                square.transform.localScale = new Vector3(squareSize, squareSize, 1);
                square.transform.parent = boardParent;

                Renderer renderer = square.GetComponent<Renderer>();
                Color baseColor = ((files + ranks) % 2 == 0) ? darkColor : lightColor;

                squareOriginalColors[square.transform] = baseColor;
                renderer.material.color = baseColor;

                char file = (char)('a' + files);
                int rank = ranks + 1;
                square.name = $"{file}{rank}";

            }
        }
    }

    public void HighlightEmptySquare()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            Transform squareTransform = null;

            // Hit a square
            if (hitObject.transform.parent == boardParent)
                squareTransform = hitObject.transform;
            // Hit a piece
            else if (hitObject.TryGetComponent<BasePiece>(out BasePiece piece))
            {
                char file = (char)('a' + piece.currentSquare.x);
                int rank = piece.currentSquare.y + 1;
                string squareName = $"{file}{rank}";
                squareTransform = boardParent.Find(squareName);
            }

            if (squareTransform == null) return;

            if (lastHighlightedSquare != null && lastHighlightedSquare != squareTransform.gameObject)
            {
                if (squareOriginalColors.TryGetValue(lastHighlightedSquare.transform, out Color originalColor))
                    lastHighlightedSquare.GetComponent<Renderer>().material.color = originalColor;
            }

            if (squareOriginalColors.TryGetValue(squareTransform, out Color baseColor))
            {
                Material tempTransparentMaterial = new Material(transparentMaterial);
                Color hoverCover = baseColor;
                hoverCover.a = 0.5f;
                // tempTransparentMaterial.color = hoverCover;
                tempTransparentMaterial.color = new Color32(0xDA, 0xA5, 0x20, 255);

                Renderer renderer = squareTransform.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material = tempTransparentMaterial;

                lastHighlightedSquare = squareTransform.gameObject;
                Debug.Log($"Square {squareTransform.name} highlighted.");
            }
        }
    }


    public Vector2Int GetSquareUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject clicked = hit.collider.gameObject;

            // Only accept squares (not pieces)
            if (clicked.transform.parent == boardParent)
            {
                string squareName = clicked.name;

                if (squareName.Length >= 2 && char.IsLetter(squareName[0]) && char.IsDigit(squareName[1]))
                {
                    int file = squareName[0] - 'a';
                    string rankString = squareName.Substring(1);

                    if (int.TryParse(rankString, out int rank))
                    {
                        // Convert from chess rank (1–8) to array index (0–7)
                        return new Vector2Int(file, rank - 1);
                    }
                }
            }
            else if (clicked.TryGetComponent<BasePiece>(out BasePiece piece))
            {
                return piece.currentSquare;
            }
        }

        return new Vector2Int(-1, -1); // invalid
    }

    public BasePiece GetPieceAtSquare(Vector2Int square)
    {
        if (square.x < 0 || square.x >= 8 || square.y < 0 || square.y >= 8)
        {
            Debug.LogError($"Invalid square {square} requested!");
            return null;
        }

        return piecesOnBoard[square.x, square.y];
    }

    public void UpdateBoardState(BasePiece piece, Vector2Int newSquare)
    {
        piecesOnBoard[piece.currentSquare.x, piece.currentSquare.y] = null;
        piecesOnBoard[newSquare.x, newSquare.y] = piece;

        piece.currentSquare = newSquare;
    }

    public void HighightMoveSquares(BasePiece piece)
    {
        ClearMoveHighlights();

        foreach (var move in piece.GetLegalMoves)
            HighlightSquare(move, Color.green);

        foreach (var capture in piece.GetLegalCaptures)
            HighlightSquare(capture, Color.red);

        HighlightSquare(piece.currentSquare, Color.yellow);
    }

    public void HighlightSquare(Vector2Int square, Color color)
    {
        char file = (char)('a' + square.x);
        int rank = square.y + 1;
        string SquareName = $"{file}{rank}";

        Transform squareTransform = boardParent.Find(SquareName);
        if (squareTransform != null)
        {
            Renderer rend = squareTransform.GetComponent<Renderer>();
            if (rend != null)
            {

                Material tempMat = new Material(transparentMaterial);
                Color col = color;
                col.a = 0.5f; // transparency
                tempMat.color = col;
                rend.material = tempMat;
            }
        }
    }

    public void ClearMoveHighlights()
    {
        foreach (Transform square in boardParent)
        {
            Renderer rend = square.GetComponent<Renderer>();
            if (rend != null && squareOriginalColors.ContainsKey(square))
            {
                rend.material.color = squareOriginalColors[square];
            }
        }
    }

    public bool IsSquareUnderAttack(Vector2Int square, bool byWhite)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int y = 0; y < 8; y++)
            {
                var piece = piecesOnBoard[i, y];
                if (piece != null && piece.isWhite == byWhite)
                {
                    // pawn
                    if (piece.pieceType == BasePiece.PieceType.Pawn)
                    {
                        int direction = piece.isWhite ? 1 : -1;

                        Vector2Int leftDiagonal = new Vector2Int(piece.currentSquare.x - 1, piece.currentSquare.y + direction);
                        Vector2Int rightDiagonal = new Vector2Int(piece.currentSquare.x + 1, piece.currentSquare.y + direction);

                        if (leftDiagonal == square || rightDiagonal == square) return true;

                        continue;
                    }

                    if (piece.pieceType == BasePiece.PieceType.King)
                    {
                        int dx = Mathf.Abs(piece.currentSquare.x - square.x);
                        int dy = Mathf.Abs(piece.currentSquare.y - square.y);

                        if ((dx <= 1 && dy <= 1) && (dx + dy > 0)) return true;
                        continue;
                    }

                    piece.CalculateValidMoves(piecesOnBoard);
                    if (piece.captureMoves.Contains(square) || piece.normalMoves.Contains(square))
                        return true;
                }
            }
        }

        return false;
    }

    public Vector3 GetSquareWorldPosition(Vector2Int square)
    {
        string squareName = $"{(char)('a' + square.x)}{square.y + 1}";
        Transform squareTransform = boardParent.Find(squareName);
        if (squareTransform != null)
            return squareTransform.position;
        return Vector3.zero;
    }
}
