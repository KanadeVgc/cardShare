using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 代表玩家放牌區域中的一個 Slot（共 4 個）。
/// 負責持有一張 CardData、更新顯示圖片，以及回報自身狀態。
/// </summary>
public class CardSlot : MonoBehaviour
{
    [Header("UI References")]
    public Image cardImage;          // 顯示放入卡牌的圖片
    public GameObject emptyOverlay;  // 「空 Slot」提示 UI（可選）

    [Header("State")]
    public int slotIndex;            // 在 4 個 Slot 中的索引（0–3）

    // 目前放在這個 Slot 的牌（null = 空）
    private CardData _placedCard = null;
    public CardData PlacedCard => _placedCard;

    public bool IsEmpty => _placedCard == null;

    // ──────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────

    /// <summary>
    /// 將一張牌放入此 Slot。若已有牌，先清空再放。
    /// </summary>
    public void PlaceCard(CardData card)
    {
        _placedCard = card;
        RefreshVisual();
    }

    /// <summary>
    /// 清空此 Slot，讓卡牌回手牌。
    /// </summary>
    public void ClearSlot()
    {
        _placedCard = null;
        RefreshVisual();
    }

    // ──────────────────────────────────────────
    //  Visual
    // ──────────────────────────────────────────

    private void RefreshVisual()
    {
        bool occupied = !IsEmpty;

        if (cardImage != null)
        {
            cardImage.enabled = occupied;
            if (occupied)
                cardImage.sprite = _placedCard.sprite;
        }

        if (emptyOverlay != null)
            emptyOverlay.SetActive(!occupied);
    }

    private void Awake()
    {
        // 初始狀態：空 Slot
        RefreshVisual();
    }
}
