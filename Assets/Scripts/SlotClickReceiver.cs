using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 掛在每個 Slot GameObject 上。
/// 玩家點擊 Slot 時通知 GameManager。
/// </summary>
public class SlotClickReceiver : MonoBehaviour, IPointerClickHandler
{
    public int slotIndex; // 由 GameManager 或 Inspector 設定

    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager.Instance.OnSlotClicked(slotIndex);
    }
}
