using UnityEngine;

public class BasePiece : MonoBehaviour
{
    virtual public void MoveTo(Vector2Int boardPosition)
    {
        // Implement movement logic here
    }

    virtual public bool IsMoveLegal(Vector2Int targetPosition)
    {
        return false;
    }
}
