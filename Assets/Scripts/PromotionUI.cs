using System;
using UnityEngine;
using UnityEngine.UI;

public class PromotionUI : MonoBehaviour
{
    public GameObject promotionPanel;
    public Button queenButton;
    public Button rookButton;
    public Button bishopButton;
    public Button knightButton;

    private Action<BasePiece.PieceType> onPieceSelected;

    void Start()
    {
        // Hide the panel initially
        promotionPanel.SetActive(false);
    }

    public void Show(Action<BasePiece.PieceType> callback)
    {
        onPieceSelected = callback;
        promotionPanel.SetActive(true);
    }

    public void Hide()
    {
        promotionPanel.SetActive(false);
    }

    public void SelectQueen() => SelectPiece(BasePiece.PieceType.Queen);
    public void SelectRook() => SelectPiece(BasePiece.PieceType.Rook);
    public void SelectBishop() => SelectPiece(BasePiece.PieceType.Bishop);
    public void SelectKnight() => SelectPiece(BasePiece.PieceType.Knight);

    private void SelectPiece(BasePiece.PieceType type)
    {
        onPieceSelected?.Invoke(type);
        Hide();
    }
}
