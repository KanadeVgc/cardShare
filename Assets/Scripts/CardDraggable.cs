using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 掛在每張手牌 Image 上。
/// 支援：
///   • 拖曳放牌 (IBeginDragHandler / IDragHandler / IEndDragHandler)
///   • 點擊選取後再點 Slot 放牌 (IPointerClickHandler，作為拖曳的備用操作)
/// </summary>
[RequireComponent(typeof(Image))]
public class CardDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerClickHandler
{
    // ── Inspector ────────────────────────────────────────
    [Header("Setup")]
    public int handIndex;

    [Header("Visual")]
    public GameObject selectedHighlight;

    // ── Private ──────────────────────────────────────────
    Image         _image;
    RectTransform _rectTransform;
    Canvas        _rootCanvas;

    // 拖曳時的浮動替身
    GameObject    _proxy;
    RectTransform _proxyRect;

    bool _isDragging  = false;
    bool _isSelected  = false;

    // ════════════════════════════════════════════════════
    //  Unity Awake
    // ════════════════════════════════════════════════════
    void Awake()
    {
        _image        = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();

        // 找最頂層 Canvas（供 Proxy 用）
        var all = GetComponentsInParent<Canvas>(true);
        foreach (var c in all)
            if (c.isRootCanvas) { _rootCanvas = c; break; }
    }

    // ════════════════════════════════════════════════════
    //  Drag
    // ════════════════════════════════════════════════════

    public void OnBeginDrag(PointerEventData e)
    {
        if (!gameObject.activeSelf) return;

        _isDragging = true;
        DeselectSelf();

        // ── 建立浮動替身 ──
        _proxy = new GameObject("CardProxy");
        _proxy.transform.SetParent(_rootCanvas.transform, false);
        _proxy.transform.SetAsLastSibling(); // 永遠在最上層

        _proxyRect = _proxy.AddComponent<RectTransform>();
        // 和原牌一樣大
        _proxyRect.sizeDelta = _rectTransform.rect.size;

        var proxyImg            = _proxy.AddComponent<Image>();
        proxyImg.sprite         = _image.sprite;
        proxyImg.preserveAspect = true;
        proxyImg.raycastTarget  = false; // 不阻擋滑鼠 Raycast

        // 原牌半透明
        _image.color = new Color(1f, 1f, 1f, 0.35f);

        MoveProxyToPointer(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_proxy == null) return;
        MoveProxyToPointer(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        _isDragging  = false;
        _image.color = Color.white;

        // 移除替身
        if (_proxy != null) { Destroy(_proxy); _proxy = null; }

        // 偵測落點是否在某個 Slot 上
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(e, results);

        foreach (var hit in results)
        {
            // 從 hit 物件或其父物件找 CardSlot
            CardSlot slot = hit.gameObject.GetComponent<CardSlot>()
                         ?? hit.gameObject.GetComponentInParent<CardSlot>();

            if (slot != null && slot.IsEmpty)
            {
                GameManager.Instance.OnHandCardDroppedOnSlot(handIndex, slot.slotIndex);
                return;
            }
        }
        // 沒有放到合法 Slot → 留在手牌（不做任何事）
    }

    // ════════════════════════════════════════════════════
    //  Click（備用操作：點選 → 再點 Slot）
    // ════════════════════════════════════════════════════

    public void OnPointerClick(PointerEventData e)
    {
        // 拖曳結束時 OnPointerClick 會被誤觸發，忽略
        if (_isDragging) return;
        if (e.dragging)  return;

        GameManager.Instance.OnHandCardClicked(handIndex);
    }

    // ════════════════════════════════════════════════════
    //  Public API（供 GameManager 呼叫）
    // ════════════════════════════════════════════════════

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (selectedHighlight != null)
            selectedHighlight.SetActive(selected);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible) _image.color = Color.white;
    }

    public bool IsSelected => _isSelected;

    // ════════════════════════════════════════════════════
    //  Private Helpers
    // ════════════════════════════════════════════════════

    void MoveProxyToPointer(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            e.position,
            e.pressEventCamera,
            out Vector2 local);

        _proxyRect.localPosition = local;
    }

    void DeselectSelf()
    {
        _isSelected = false;
        if (selectedHighlight != null) selectedHighlight.SetActive(false);
    }
}
