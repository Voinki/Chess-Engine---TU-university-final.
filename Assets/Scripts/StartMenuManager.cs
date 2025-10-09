using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class StartMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public Button puzzleButton;
    public GameObject fenInputPanel;       // Panel containing TMP InputField and Submit button
    public TMP_InputField fenInputField;
    public TMP_Text fenPlaceholderText;    // Placeholder text inside the TMP Input Field
    public Button submitButton;            // Submit button for FEN input
    public Button backButton;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene"; // Name of the scene that has GameManager & FENManager

    private void Start()
    {
        // Hide FEN input at start
        fenInputPanel.SetActive(false);

        // Add button listeners
        startButton.onClick.AddListener(OnStartClicked);
        puzzleButton.onClick.AddListener(OnPuzzleClicked);
        submitButton.onClick.AddListener(OnFENSubmit);
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnStartClicked()
    {
        // Store normal game mode
        GameSettings.FEN = "";          // Empty = normal start
        GameSettings.IsPuzzle = false;

        // Load GameScene
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnPuzzleClicked()
    {
        // Hide main menu buttons
        startButton.gameObject.SetActive(false);
        puzzleButton.gameObject.SetActive(false);

        // Show FEN input panel
        fenInputPanel.SetActive(true);

        // Reset placeholder
        fenPlaceholderText.text = "Enter FEN string...";
        fenInputField.text = "";
    }

    private void OnFENSubmit()
    {
        string fen = fenInputField.text.Trim();

        if (string.IsNullOrEmpty(fen) || !IsValidFEN(fen))
        {
            // Invalid FEN: clear input and show placeholder
            fenInputField.text = "";
            fenPlaceholderText.text = "Incorrect format";
            return;
        }

        // Valid FEN: store and start puzzle
        GameSettings.FEN = fen;
        GameSettings.IsPuzzle = true;

        // Load GameScene
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnBackClicked()
    {
        // Hide FEN panel
        fenInputPanel.SetActive(false);

        // Show main menu buttons again
        startButton.gameObject.SetActive(true);
        puzzleButton.gameObject.SetActive(true);
    }

    // Basic FEN validation
    private bool IsValidFEN(string fen)
    {
        string[] parts = fen.Split(' ');
        if (parts.Length != 6) return false; // FEN must have 6 space-separated parts

        string[] rows = parts[0].Split('/');
        if (rows.Length != 8) return false; // Piece placement must have 8 ranks

        foreach (string row in rows)
        {
            int fileCount = 0;
            foreach (char c in row)
            {
                if (char.IsDigit(c))
                    fileCount += (int)char.GetNumericValue(c);
                else if ("rnbqkpRNBQKP".IndexOf(c) == -1)
                    return false; // invalid piece character
                else
                    fileCount++;
            }
            if (fileCount != 8) return false; // Each rank must have exactly 8 files
        }

        // Optional: further checks for side to move, castling rights, en passant, etc.
        return true;
    }
}
