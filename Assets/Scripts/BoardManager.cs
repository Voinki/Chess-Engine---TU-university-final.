using UnityEngine;
using UnityEngine.InputSystem;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private Transform boardParent;
    private GameObject lastHighlightedSquare;
    private Color originalColor;
    public Material transparentMaterial;

    void Awake() // awake is called before start, which means the board will be generated before trying to place pieces.
    {
        GenerateBoard();
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HighLightSquare();
    }

    private void GenerateBoard()
    {
        float squareSize = 0.1f;
        float offset = (8 * squareSize) / 2f;

        Color lightColor = Color.white;
        Color darkColor = new Color(0.55f, 0.27f, 0.07f); // wood-ish brown

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
                if ((files + ranks) % 2 == 0)
                    renderer.material.color = darkColor;
                else
                    renderer.material.color = lightColor;

                char file = (char)('a' + files);
                int rank = ranks + 1;
                square.name = $"{file}{rank}";
            }
        }
    }

    private void HighLightSquare()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject square = hit.collider.gameObject;
            Renderer renderer = square.GetComponent<Renderer>();

            if (lastHighlightedSquare != null && lastHighlightedSquare != square)
                lastHighlightedSquare.GetComponent<Renderer>().material.color = originalColor;

            originalColor = renderer.material.color;

            Material tempTransparentMaterial = new Material(transparentMaterial);
            Color highlightColor = originalColor;
            highlightColor.a = 0.5f; // transparency level
            tempTransparentMaterial.color = highlightColor;
            renderer.material = tempTransparentMaterial;

            lastHighlightedSquare = square;

            Debug.Log($"Square {square.name} highlighted.");
        }
    }

   
}
