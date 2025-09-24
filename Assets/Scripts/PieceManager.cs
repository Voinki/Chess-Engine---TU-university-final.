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
    [SerializeField] private Transform piecesParent; // reference to the parent object for pieces
    void Start()
    {
        // starting FEN position
        // FindFirstObjectByType<FENManager>().SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
    }

    public void PlacePiece(string type, string color, string squareName)
    {
        GameObject prefab = null;
        switch (type)
        {
            case "K": prefab = kingPrefab; break;
            case "Q": prefab = queenPrefab; break;
            case "R": prefab = rookPrefab; break;
            case "B": prefab = bishopPrefab; break;
            case "N": prefab = knightPrefab; break;
            case "P": prefab = pawnPrefab; break;
        }

        if (prefab == null)
        {
            Debug.LogError($"Invalid piece type: {type}");
            return;
        }

        Transform square = boardParent.Find(squareName);
        if (square == null) return;

        Debug.Log($"Placing {color} {type} on {squareName} at {square.position}");

        GameObject piece = Instantiate(prefab, square.position + Vector3.up * 0.05f, Quaternion.identity, piecesParent); // Quaternion.identity = no rotation, perfectly aligned

        switch (type)
        {
            case "K": piece.AddComponent<King>(); break;
            case "Q": piece.AddComponent<Queen>(); break;
            case "R": piece.AddComponent<Rook>(); break;
            case "B": piece.AddComponent<Bishop>(); break;
            case "N": piece.AddComponent<Knight>(); break;
            case "P": piece.AddComponent<Pawn>(); break;
        }

        if (color == "black")
            piece.transform.Rotate(0, 180, 0); // rotate black pieces to face white side

        Renderer renderer = piece.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = (color == "white") ? whitePieceMaterial : blackPieceMaterial;
            piece.name = (color == "white") ? $"{color}_{type}_{squareName}" : $"{color}_{type.ToLower()}_{squareName}";
        }
            
        
    }

    // private void PlaceStartingPieces()
    // {
    //     string[] backRank = { "R", "N", "B", "Q", "K", "B", "N", "R" };

    //     // white pieces
    //     for (int i = 0; i < 8; i++)
    //     {
    //         char file = (char)('a' + i);
    //         PlacePiece(backRank[i], "white", $"{file}1");
    //         PlacePiece("P", "white", $"{file}2");
    //     }

    //     //black pieces

    //     for (int i = 0; i < 8; i++)
    //     {
    //         char file = (char)('a' + i);
    //         PlacePiece(backRank[i], "black", $"{file}8");
    //         PlacePiece("P", "black", $"{file}7");
    //     }
    // }
}
