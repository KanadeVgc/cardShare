using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ══════════════════════════════════════════════════════════
//  CardData
// ══════════════════════════════════════════════════════════
[System.Serializable]
public class CardData
{
    public Sprite sprite;
    public int value;

    public override string ToString() => value.ToString();
}

// ══════════════════════════════════════════════════════════
//  GameManager
// ══════════════════════════════════════════════════════════
public class GameManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Difficulty ─────────────────────────────────────────
    public enum Difficulty { Easy, Medium, Hard, Expert }
    public Difficulty currentDifficulty;

    // ── Inspector References ───────────────────────────────
    [Header("UI - Info")]
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI expressionText;   // 隱藏數字的題目
    public TextMeshProUGUI currentResultText; // 即時顯示 Current Result
    public TextMeshProUGUI feedbackText;      // 成功 / 錯誤 提示

    [Header("UI - Hand Cards")]
    public Image[] cardImages;               // 4 張手牌圖片
    public CardDraggable[] cardDraggables;   // 對應的 CardDraggable 元件

    [Header("UI - Slots")]
    public CardSlot[] slots;                 // 4 個 Slot（依序對應 expression 位置）

    [Header("UI - Buttons")]
    public Button resetButton;               // 重置本局
    public Button newPuzzleButton;           // 新題目

    [Header("Card Data")]
    public CardData[] allCards;              // 全部 52 張牌

    // ── Runtime State ──────────────────────────────────────
    private List<CardData> solutionCards = new List<CardData>(); // expression 裡 a,b,c,d 對應的牌
    private List<CardData> displayCards  = new List<CardData>(); // 玩家看到的手牌（打亂順序）

    private int    target;
    private string expression;       // 完整 expression（含數字）
    private string hiddenExpression; // 數字挖空版（給玩家看）

    // 記錄手牌哪張已被放入 Slot（-1 = 未放）
    private int[] handCardSlotMap; // handCardSlotMap[handIndex] = slotIndex or -1

    // 目前選中的手牌（-1 = 無）
    private int selectedHandIndex = -1;

    // ── Patterns ───────────────────────────────────────────
    string[] patterns =
    {
        "a op1 b op2 c op3 d",
        "(a op1 b) op2 c op3 d",
        "a op1 (b op2 c) op3 d",
        "a op1 b op2 (c op3 d)",
        "(a op1 b) op2 (c op3 d)",
        "((a op1 b) op2 c) op3 d",
        "(a op1 (b op2 c)) op3 d",
        "a op1 ((b op2 c) op3 d)",
        "a op1 (b op2 (c op3 d))"
    };

    // ══════════════════════════════════════════════════════
    //  Unity Lifecycle
    // ══════════════════════════════════════════════════════
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // 設定按鈕
        if (resetButton    != null) resetButton.onClick.AddListener(ResetCurrentPuzzle);
        if (newPuzzleButton != null) newPuzzleButton.onClick.AddListener(GeneratePuzzle);

        // 設定 SlotClickReceiver 索引
        for (int i = 0; i < slots.Length; i++)
            slots[i].slotIndex = i;

        GeneratePuzzle();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GeneratePuzzle();

        if (Input.GetKeyDown(KeyCode.R))
            ResetCurrentPuzzle();
    }

    // ══════════════════════════════════════════════════════
    //  Puzzle Generation
    // ══════════════════════════════════════════════════════
    void GeneratePuzzle()
    {
        bool valid = false;

        while (!valid)
        {
            solutionCards = GenerateCards();
            Shuffle(solutionCards);

            string pattern = GetRandomPattern();
            char op1 = RandomOp();
            char op2 = RandomOp();
            char op3 = RandomOp();

            expression = pattern
                .Replace("a",   solutionCards[0].value.ToString())
                .Replace("b",   solutionCards[1].value.ToString())
                .Replace("c",   solutionCards[2].value.ToString())
                .Replace("d",   solutionCards[3].value.ToString())
                .Replace("op1", op1.ToString())
                .Replace("op2", op2.ToString())
                .Replace("op3", op3.ToString());

            if (HasInvalidDivision(expression)) continue;

            try
            {
                double result = System.Convert.ToDouble(
                    new DataTable().Compute(expression, null));

                if (IsValidResult(result))
                {
                    target = (int)result;
                    valid  = true;

                    displayCards = new List<CardData>(solutionCards);
                    Shuffle(displayCards);
                }
            }
            catch { continue; }
        }

        hiddenExpression = HideNumbers(expression);

        Debug.Log("────────────────────────────────");
        Debug.Log("Difficulty : " + currentDifficulty);
        Debug.Log("Solution   : " + string.Join(", ", solutionCards));
        Debug.Log("Display    : " + string.Join(", ", displayCards));
        Debug.Log("Expression : " + GetDisplayExpression(expression));
        Debug.Log("Hidden     : " + GetDisplayExpression(hiddenExpression));
        Debug.Log("Target     : " + target);

        // 初始化互動狀態
        handCardSlotMap = new int[4];
        for (int i = 0; i < 4; i++) handCardSlotMap[i] = -1;
        selectedHandIndex = -1;

        // 清空所有 Slot
        foreach (var slot in slots) slot.ClearSlot();

        UpdateUI();
        UpdateCardsUI();
        UpdateCurrentResult();
        SetFeedback("");
    }

    // ══════════════════════════════════════════════════════
    //  Interaction — Hand Cards
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 玩家點擊手牌時呼叫（由 CardDraggable 轉發）。
    /// </summary>
    public void OnHandCardClicked(int handIndex)
    {
        // 若此牌已放入 Slot，點擊就退回手牌
        if (handCardSlotMap[handIndex] != -1)
        {
            ReturnCardFromSlot(handIndex);
            return;
        }

        // 切換選取狀態
        if (selectedHandIndex == handIndex)
        {
            DeselectAll();
        }
        else
        {
            DeselectAll();
            selectedHandIndex = handIndex;
            if (cardDraggables != null && cardDraggables.Length > handIndex)
                cardDraggables[handIndex].SetSelected(true);
        }
    }

    // ══════════════════════════════════════════════════════
    //  Interaction — Slots
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 玩家點擊 Slot 時呼叫（由 SlotClickReceiver 轉發）。
    /// </summary>
    public void OnSlotClicked(int slotIndex)
    {
        // 若 Slot 已有牌 → 退回手牌
        if (!slots[slotIndex].IsEmpty)
        {
            ReturnCardToHandFromSlot(slotIndex);
            UpdateCurrentResult();
            return;
        }

        // 若有選取的手牌 → 放入 Slot
        if (selectedHandIndex != -1)
        {
            PlaceCardIntoSlot(selectedHandIndex, slotIndex);
            DeselectAll();
            UpdateCurrentResult();
        }
    }

    /// <summary>
    /// 拖曳放牌：CardDraggable.OnEndDrag 偵測到 Slot 後呼叫。
    /// </summary>
    public void OnHandCardDroppedOnSlot(int handIndex, int slotIndex)
    {
        // 已放入其他 Slot → 先退回
        if (handCardSlotMap[handIndex] != -1)
            ReturnCardFromSlot(handIndex);

        if (slots[slotIndex].IsEmpty)
        {
            PlaceCardIntoSlot(handIndex, slotIndex);
            DeselectAll();
            UpdateCurrentResult();
        }
    }

    // ══════════════════════════════════════════════════════
    //  Placement & Return
    // ══════════════════════════════════════════════════════

    void PlaceCardIntoSlot(int handIndex, int slotIndex)
    {
        CardData card = displayCards[handIndex];

        slots[slotIndex].PlaceCard(card);
        handCardSlotMap[handIndex] = slotIndex;

        // 隱藏手牌圖片
        if (cardDraggables != null && cardDraggables.Length > handIndex)
            cardDraggables[handIndex].SetVisible(false);
        else if (cardImages != null && cardImages.Length > handIndex)
            cardImages[handIndex].gameObject.SetActive(false);
    }

    /// <summary>
    /// 從手牌 index 找到對應的 Slot，並退回手牌。
    /// </summary>
    void ReturnCardFromSlot(int handIndex)
    {
        int si = handCardSlotMap[handIndex];
        if (si == -1) return;

        slots[si].ClearSlot();
        handCardSlotMap[handIndex] = -1;

        // 恢復手牌顯示
        if (cardDraggables != null && cardDraggables.Length > handIndex)
            cardDraggables[handIndex].SetVisible(true);
        else if (cardImages != null && cardImages.Length > handIndex)
            cardImages[handIndex].gameObject.SetActive(true);

        UpdateCurrentResult();
    }

    /// <summary>
    /// 從 Slot index 找到對應的手牌，並退回手牌。
    /// </summary>
    void ReturnCardToHandFromSlot(int slotIndex)
    {
        for (int i = 0; i < 4; i++)
        {
            if (handCardSlotMap[i] == slotIndex)
            {
                slots[slotIndex].ClearSlot();
                handCardSlotMap[i] = -1;

                if (cardDraggables != null && cardDraggables.Length > i)
                    cardDraggables[i].SetVisible(true);
                else if (cardImages != null && cardImages.Length > i)
                    cardImages[i].gameObject.SetActive(true);

                return;
            }
        }
    }

    void DeselectAll()
    {
        selectedHandIndex = -1;
        if (cardDraggables == null) return;
        foreach (var cd in cardDraggables)
            if (cd != null) cd.SetSelected(false);
    }

    // ══════════════════════════════════════════════════════
    //  Reset Current Puzzle
    // ══════════════════════════════════════════════════════

    public void ResetCurrentPuzzle()
    {
        // 清空所有 Slot、恢復手牌
        for (int i = 0; i < 4; i++)
        {
            if (handCardSlotMap[i] != -1)
            {
                slots[handCardSlotMap[i]].ClearSlot();
                handCardSlotMap[i] = -1;
            }

            if (cardDraggables != null && cardDraggables.Length > i)
                cardDraggables[i].SetVisible(true);
            else if (cardImages != null && cardImages.Length > i)
                cardImages[i].gameObject.SetActive(true);
        }

        DeselectAll();
        UpdateCurrentResult();
        SetFeedback("");
    }

    // ══════════════════════════════════════════════════════
    //  Current Result Calculation & Target Check
    // ══════════════════════════════════════════════════════

    void UpdateCurrentResult()
    {
        // 計算目前放了幾張
        int filledCount = 0;
        foreach (var slot in slots)
            if (!slot.IsEmpty) filledCount++;

        if (filledCount == 0)
        {
            SetCurrentResultText("—");
            SetFeedback("");
            return;
        }

        if (filledCount < 4)
        {
            // 部分放入：只顯示已填的值（不計算式子）
            SetCurrentResultText($"{filledCount} / 4 placed");
            SetFeedback("");
            return;
        }

        // ── 全部 4 張放入 → 依 slot 順序重建 expression 計算 ──
        // slots[0..3] 對應 expression 中的 a, b, c, d
        string evalExpr = expression;

        // 用 slot 中的牌值重建 expression
        // 因為 expression 是用 solutionCards 的值建出來的，
        // 玩家放的牌（displayCards）可能順序不同，
        // 所以我們把 expression 中的數字依序換成 slots[0..3] 的值。
        evalExpr = RebuildExpressionFromSlots();

        if (evalExpr == null)
        {
            SetCurrentResultText("?");
            return;
        }

        if (HasInvalidDivision(evalExpr))
        {
            SetCurrentResultText("Divide by zero!");
            SetFeedback("X  Divide by zero is illegal");
            return;
        }

        try
        {
            double result = System.Convert.ToDouble(
                new DataTable().Compute(evalExpr, null));

            SetCurrentResultText(result % 1 == 0
                ? ((int)result).ToString()
                : result.ToString("F2"));

            if (result % 1 == 0 && (int)result == target)
                SetFeedback("Correct!  Target reached!");
            else
                SetFeedback($"X  Result {(result % 1 == 0 ? ((int)result).ToString() : result.ToString("F2"))} != Target {target}");
        }
        catch
        {
            SetCurrentResultText("Error");
            SetFeedback("Calculation error");
        }
    }

    /// <summary>
    /// 用 slots[0..3] 中的牌值，按照原始 expression 的結構重建算式。
    /// expression 的結構（括號、運算子）不變，只把 a/b/c/d 換成 slots 的值。
    /// </summary>
    string RebuildExpressionFromSlots()
    {
        // 確保 4 個 Slot 都有牌
        for (int i = 0; i < 4; i++)
            if (slots[i].IsEmpty) return null;

        // 取得原始 expression 的「結構」（帶 op，帶括號，數字替換回佔位符）
        // 做法：把 expression 中的數字依序用 slots[i].PlacedCard.value 取代
        // 因為 expression 裡已經是具體數字，我們用 Regex 依序替換
        int replaceIndex = 0;
        string rebuilt = System.Text.RegularExpressions.Regex.Replace(
            expression,
            @"\d+",
            match =>
            {
                int val = slots[replaceIndex].PlacedCard.value;
                replaceIndex++;
                return val.ToString();
            });

        return rebuilt;
    }

    // ══════════════════════════════════════════════════════
    //  UI Helpers
    // ══════════════════════════════════════════════════════

    void UpdateUI()
    {
        if (targetText     != null) targetText.text     = "Target : " + target;
        if (expressionText != null) expressionText.text = GetDisplayExpression(hiddenExpression);
    }

    void UpdateCardsUI()
    {
        for (int i = 0; i < 4; i++)
        {
            if (cardImages != null && cardImages.Length > i && cardImages[i] != null)
            {
                cardImages[i].sprite = displayCards[i].sprite;
                cardImages[i].gameObject.SetActive(true);
            }

            if (cardDraggables != null && cardDraggables.Length > i && cardDraggables[i] != null)
            {
                cardDraggables[i].handIndex = i;
                cardDraggables[i].SetVisible(true);
                cardDraggables[i].SetSelected(false);
            }
        }
    }

    void SetCurrentResultText(string text)
    {
        if (currentResultText != null)
            currentResultText.text = "Current : " + text;
    }

    void SetFeedback(string text)
    {
        if (feedbackText != null)
            feedbackText.text = text;
    }

    // ══════════════════════════════════════════════════════
    //  Card / Puzzle Helpers
    // ══════════════════════════════════════════════════════

    List<CardData> GenerateCards()
    {
        var result      = new List<CardData>();
        var usedIndexes = new List<int>();

        while (result.Count < 4)
        {
            int rand = Random.Range(0, allCards.Length);
            if (!usedIndexes.Contains(rand))
            {
                usedIndexes.Add(rand);
                result.Add(allCards[rand]);
            }
        }
        return result;
    }

    void Shuffle(List<CardData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand      = Random.Range(i, list.Count);
            CardData temp = list[i];
            list[i]       = list[rand];
            list[rand]    = temp;
        }
    }

    string GetRandomPattern()
    {
        if (currentDifficulty != Difficulty.Expert)
            return "a op1 b op2 c op3 d";

        return patterns[Random.Range(0, patterns.Length)];
    }

    char RandomOp()
    {
        char[] ops;
        switch (currentDifficulty)
        {
            case Difficulty.Easy:   ops = new[] { '+', '-' };             break;
            case Difficulty.Medium: ops = new[] { '+', '-', '*' };        break;
            case Difficulty.Hard:
            case Difficulty.Expert: ops = new[] { '+', '-', '*', '/' };   break;
            default:                ops = new[] { '+', '-' };             break;
        }
        return ops[Random.Range(0, ops.Length)];
    }

    bool HasInvalidDivision(string expr) => expr.Contains("/0");

    bool IsValidResult(double result)
    {
        if (result <= 0)     return false;
        if (result % 1 != 0) return false;
        return true;
    }

    string HideNumbers(string expr) =>
        System.Text.RegularExpressions.Regex.Replace(expr, @"\d+", "□");

    string GetDisplayExpression(string expr)
    {
        return expr
            .Replace("*", " × ")
            .Replace("/", " ÷ ")
            .Replace("+", " + ")
            .Replace("-", " − ");
    }
}
