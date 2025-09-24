using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private FENManager fenManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fenManager.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
