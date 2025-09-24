using UnityEngine;

public class Pawn : BasePiece
{
    public override void MoveTo(Vector2Int boardPosition)
    {

    }
    
    public override bool IsMoveLegal(Vector2Int targetPosition)
    {
        return false;
    }
}
