using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class StartMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public Button puzzleButton;
    public GameObject fenInputPanel;     
    public TMP_InputField fenInputField;
    public TMP_Text fenPlaceholderText;  
    public Button submitButton;         
    public Button backButton;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";

    private void Start()
    {
        fenInputPanel.SetActive(false);

        startButton.onClick.AddListener(OnStartClicked);
        puzzleButton.onClick.AddListener(OnPuzzleClicked);
        submitButton.onClick.AddListener(OnFENSubmit);
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnStartClicked()
    {
        GameSettings.FEN = "";          
        GameSettings.IsPuzzle = false;

        SceneManager.LoadScene(gameSceneName);
    }

    private void OnPuzzleClicked()
    {
        startButton.gameObject.SetActive(false);
        puzzleButton.gameObject.SetActive(false);

        fenInputPanel.SetActive(true);

        fenPlaceholderText.text = "Enter FEN string...";
        fenInputField.text = "";
    }

    private void OnFENSubmit()
    {
        string fen = fenInputField.text.Trim();

        if (string.IsNullOrEmpty(fen) || !IsValidFEN(fen))
        {
            fenInputField.text = "";
            fenPlaceholderText.text = "Incorrect format";
            return;
        }

        GameSettings.FEN = fen;
        GameSettings.IsPuzzle = true;

        SceneManager.LoadScene(gameSceneName);
    }

    private void OnBackClicked()
    {
        fenInputPanel.SetActive(false);

        startButton.gameObject.SetActive(true);
        puzzleButton.gameObject.SetActive(true);
    }

    private bool IsValidFEN(string fen)
    {
        string[] parts = fen.Split(' ');
        if (parts.Length != 6) return false; 

        string[] rows = parts[0].Split('/');
        if (rows.Length != 8) return false; 

        foreach (string row in rows)
        {
            int fileCount = 0;
            foreach (char c in row)
            {
                if (char.IsDigit(c))
                    fileCount += (int)char.GetNumericValue(c);
                else if ("rnbqkpRNBQKP".IndexOf(c) == -1)
                    return false;
                else
                    fileCount++;
            }
            
            if (fileCount != 8) return false; 
        }

        return true;
    }
}
