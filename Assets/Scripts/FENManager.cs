using UnityEngine;
public class FENManager : MonoBehaviour
{
    [SerializeField] private PieceManager pieceManager;

    public void SetupPositionFromFEN(string fen)
    {
        // starting FEN -> rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
        string[] parts = fen.Split(' ');
        string[] ranks = parts[0].Split('/');

        for (int rank = 0; rank < 8; rank++)
        {
            int file = 0;

            foreach (char c in ranks[rank])
            {
                if (char.IsDigit(c))
                    file += (int)char.GetNumericValue(c); // empty squares
                else
                {
                    string color = char.IsUpper(c) ? "white" : "black";

                    string type = char.ToUpper(c).ToString();

                    char fileChar = (char)('a' + file);
                    int boardRank = 8 - rank; 
                    string squareName = $"{fileChar}{boardRank}";

                    pieceManager.PlacePiece(type, color, squareName);

                    file++;
                }
            }
        }
    }

    public void UpdateFen()
    {
        // TODO: implement FEN update logic after every move
    }
}
