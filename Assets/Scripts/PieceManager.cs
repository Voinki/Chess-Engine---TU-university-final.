using System.Numerics;
using UnityEngine;

public class PieceManager : MonoBehaviour
{
    [Header("Prefabs and Materials")]
    [SerializeField] private Material whitePieceMaterial;
    [SerializeField] private Material blackPieceMaterial;
    [SerializeField] private GameObject kingPrefab;
    [SerializeField] private GameObject queenPrefab;
    [SerializeField] private GameObject rookPrefab;
    [SerializeField] private GameObject bishopPrefab;
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject pawnPrefab;

    [Header("Board/Piece reference")]
    [SerializeField] private Transform boardParent; // reference to the board. using transform instead of GameObject for easier access to child squares
    [SerializeField] private Transform piecesParent; 

    public void PlacePiece(string type, string color, string squareName)
    {
        GameObject prefab = type switch
        {
            "K" => kingPrefab,
            "Q" => queenPrefab,
            "R" => rookPrefab,
            "B" => bishopPrefab,
            "N" => knightPrefab,
            "P" => pawnPrefab,
            _ => null
        };

        if (prefab == null)
        {
            Debug.LogError($"Invalid piece type: {type}");
            return;
        }

        Transform square = boardParent.Find(squareName);
        if (square == null) return;

        GameObject pieceGO = Instantiate(prefab, square.position + UnityEngine.Vector3.up * 0.05f, UnityEngine.Quaternion.identity, piecesParent);
        if (color == "black")
            pieceGO.transform.Rotate(0, 180, 0);

        foreach (var comp in pieceGO.GetComponents<BasePiece>())
            Destroy(comp);

        BasePiece pieceScript = type switch
        {
            "K" => pieceGO.AddComponent<King>(),
            "Q" => pieceGO.AddComponent<Queen>(),
            "R" => pieceGO.AddComponent<Rook>(),
            "B" => pieceGO.AddComponent<Bishop>(),
            "N" => pieceGO.AddComponent<Knight>(),
            "P" => pieceGO.AddComponent<Pawn>(),
            _ => null
        };

        if (pieceScript == null)
        {
            Debug.LogError("Failed to add BasePiece component");
            Destroy(pieceGO);
            return;
        }

        pieceGO.name = $"{color}_{type}_{squareName}";
        pieceScript.isWhite = color == "white";

        int file = squareName[0] - 'a';
        int rank = int.Parse(squareName[1].ToString()) - 1;
        pieceScript.currentSquare = new Vector2Int(file, rank);

        BoardManager boardManager = FindFirstObjectByType<BoardManager>();
        pieceScript.boardManager = boardManager;
        boardManager.piecesOnBoard[file, rank] = pieceScript;

        // Material
        Renderer renderer = pieceGO.GetComponent<Renderer>();
        renderer.material = color == "white" ? whitePieceMaterial : blackPieceMaterial;

        // Pawn hasMoved for fen setup
        if (pieceScript is Pawn pawn)
        {
            if (pawn.isWhite)
                pawn.hasMoved = pawn.currentSquare.y != 1; // white 
            else
                pawn.hasMoved = pawn.currentSquare.y != 6; // black 
        }

        Debug.Log($"Placed {pieceGO.name} at {squareName}, type={type}, component={pieceScript.GetType().Name}");
    }


public BasePiece PromotePieceAt(Vector2Int position, bool isWhite, BasePiece.PieceType newType)
{
    GameObject prefab;

    if (!isWhite)
    {
        newType = BasePiece.PieceType.Queen;
        prefab = queenPrefab;
    }
    else
    {
        switch (newType)
        {
            case BasePiece.PieceType.Queen: prefab = queenPrefab; break;
            case BasePiece.PieceType.Rook: prefab = rookPrefab; break;
            case BasePiece.PieceType.Bishop: prefab = bishopPrefab; break;
            case BasePiece.PieceType.Knight: prefab = knightPrefab; break;
            default:
                Debug.LogError($"Invalid promotion piece: {newType}");
                return null;
        }
    }

    if (prefab == null)
    {
        Debug.LogError("Prefab for promoted piece is missing!");
        return null;
    }

    BoardManager boardManager = FindFirstObjectByType<BoardManager>();
    UnityEngine.Vector3 squarePos = boardManager.GetSquareWorldPosition(position);
    GameObject pieceGO = Instantiate(prefab, squarePos + UnityEngine.Vector3.up * 0.05f, UnityEngine.Quaternion.identity, piecesParent);

    // Remove any existing BasePiece components from prefab
    BasePiece[] existingComponents = pieceGO.GetComponents<BasePiece>();
    foreach (var comp in existingComponents)
    {
        DestroyImmediate(comp); // Use DestroyImmediate to remove components immediately
    }

    // Add the correct component
    BasePiece pieceScript;
    switch (newType)
    {
        case BasePiece.PieceType.Queen:
            pieceScript = pieceGO.AddComponent<Queen>();
            break;
        case BasePiece.PieceType.Rook:
            pieceScript = pieceGO.AddComponent<Rook>();
            break;
        case BasePiece.PieceType.Bishop:
            pieceScript = pieceGO.AddComponent<Bishop>();
            break;
        case BasePiece.PieceType.Knight:
            pieceScript = pieceGO.AddComponent<Knight>();
            break;
        default:
            Debug.LogError("Invalid promotion type, cannot add script.");
            Destroy(pieceGO);
            return null;
    }

    // Rotate if black
    if (!isWhite)
        pieceGO.transform.Rotate(0, 180, 0);

    // Set material
    Renderer renderer = pieceGO.GetComponent<Renderer>();
    if (renderer != null)
        renderer.material = isWhite ? whitePieceMaterial : blackPieceMaterial;

    // Set piece properties
    pieceScript.isWhite = isWhite;
    pieceScript.currentSquare = position;
    pieceScript.boardManager = boardManager;
    pieceScript.hasMoved = true;

    // Set the name
    char file = (char)('a' + position.x);
    int rank = position.y + 1;
    pieceGO.name = $"{(isWhite ? "white" : "black")}_{newType}_{file}{rank}";

    Debug.Log($"Created promoted piece: {pieceGO.name} at {position}");

    return pieceScript;
}

}
