using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Text;

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
    public TextMeshProUGUI feedbackText;    // 成功 / 錯誤 提示
    public TextMeshProUGUI questionText;      //題數
    public TextMeshProUGUI difficultyText;//難度

    [Header("UI - Hand Cards")]
    public Image[] cardImages;               // 4 張手牌圖片
    public CardDraggable[] cardDraggables;   // 對應的 CardDraggable 元件

    [Header("UI - Slots")]
    public CardSlot[] slots;                 // 4 個 Slot（依序對應 expression 位置）

    [Header("UI - Buttons")]
    public Button resetButton;               // 重置本局
    public Button newPuzzleButton;           // 新題目

    [Header("UI - Panels")]
    public GameObject mainMenuPanel;
    public GameObject gamePanel;
    public GameObject startPanel; 
    public Button startButton;
    public GameObject instructionPanel;
    public Button openInstructionButton;
    public Button closeInstructionButton;

    [Header("UI - End Panel")]
    public GameObject endPanel;
    public TextMeshProUGUI endSummaryText;
    public Button backToMenuButton;
    public Button restartButton; 
    public Button exportExcelButton;

    [Header("Menu Buttons")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;
    public Button expertButton;

    [Header("UI - Back")]
    public Button mainMenuBackButton;
    public Button gameBackButton;
    public Button endBackButton;

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

    //判定限制(讓玩家在判定期間不亂點)
    private bool isCheckingAnswer = false;

    //新增一次出幾題
    private int currentQuestion = 1;
    private int totalQuestions = 5;
    private int correctAnswers = 0;

    //時間與答錯幾題
    private int wrongAnswers = 0;
    private int resetCount = 0; 
    private int newPuzzleCount = 0; 
    private float startTime;
    private bool isGameFinished = false;
    private float lastEscTime = -1f;
    private const float DoubleEscWindow = 0.4f;
    private int _cacheCorrect;
    private int _cacheWrong;
    private float _cacheAccuracy;
    private int _cacheMin;
    private int _cacheSec;
    // ── Patterns ───────────────────────────────────────────
    string[] patterns =
    {
        "a op1 b op2 c op3 d",
        "(a op1 b) op2 c op3 d",
        "a op1 (b op2 c) op3 d",
        "a op1 b op2 (c op3 d)",
        "(a op1 b) op2 (c op3 d)"
        // "((a op1 b) op2 c) op3 d",
        // "(a op1 (b op2 c)) op3 d",
        // "a op1 ((b op2 c) op3 d)",
        // "a op1 (b op2 (c op3 d))"
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
        if (startPanel    != null) startPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gamePanel     != null) gamePanel.SetActive(false);
        if (endPanel      != null) endPanel.SetActive(false);
        if (startButton != null) startButton.onClick.AddListener(ShowDifficultyMenu);
        if (resetButton != null) resetButton.onClick.AddListener(() =>
        {
            resetCount++;
            ResetCurrentPuzzle();
        });
        if (newPuzzleButton != null) newPuzzleButton.onClick.AddListener(() =>
        {
            newPuzzleCount++;
            GeneratePuzzle();
        });
        if (easyButton != null) easyButton.onClick.AddListener(StartEasy);
        if (mediumButton != null) mediumButton.onClick.AddListener(StartMedium);
        if (hardButton != null) hardButton.onClick.AddListener(StartHard);
        if (expertButton != null) expertButton.onClick.AddListener(StartExpert);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(BackToMenu);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (openInstructionButton != null) openInstructionButton.onClick.AddListener(ShowInstruction);
        if (closeInstructionButton != null) closeInstructionButton.onClick.AddListener(HideInstruction);
        WireBack(mainMenuBackButton);
        WireBack(gameBackButton);
        WireBack(endBackButton);

        // 設定 SlotClickReceiver 索引
        for (int i = 0; i < slots.Length; i++)
            slots[i].slotIndex = i;

        startTime = Time.time;
        
        if (startPanel == null && mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            float now = Time.unscaledTime;
            if (lastEscTime >= 0f && now - lastEscTime <= DoubleEscWindow)
            {
                lastEscTime = -1f;
                QuitGame();
            }
            else
            {
                lastEscTime = now;
                GoBack();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
            GeneratePuzzle();

        if (Input.GetKeyDown(KeyCode.R))
        {
            resetCount++;
            ResetCurrentPuzzle();
        }
    }
    // void ShowPanel(GameObject panel, bool show)
    // {
    //     if (panel == null) return;

    //     var cg = panel.GetComponent<CanvasGroup>();

    //     if (cg == null) return;

    //     cg.alpha = show ? 1 : 0;
    //     cg.interactable = show;
    //     cg.blocksRaycasts = show;

    //     if (show)
    //         panel.transform.SetAsLastSibling();
    // }

    // ══════════════════════════════════════════════════════
    //  Puzzle Generation
    // ══════════════════════════════════════════════════════
    void GeneratePuzzle()
    {
        Debug.Log("GeneratePuzzle, difficulty = " + currentDifficulty);
        Debug.Log("GeneratePuzzle Called");
        isGameFinished = false;

        if (resetButton != null) resetButton.interactable = true;
        if (newPuzzleButton != null) newPuzzleButton.interactable = true;
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
            if (HasNegativeIntermediate(expression)) continue;
            if (currentDifficulty == Difficulty.Expert &&
                HasMeaninglessBrackets(expression))
                continue;

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
        UpdateQuestionUI();

        // 🎵 發牌音效
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDealCardSFX();
    }

    // ══════════════════════════════════════════════════════
    //  Interaction — Hand Cards
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 玩家點擊手牌時呼叫（由 CardDraggable 轉發）。
    /// </summary>
    public void OnHandCardClicked(int handIndex)
    {
        if (isCheckingAnswer) return;
        if (isGameFinished) return;
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
        if (isCheckingAnswer) return;
        if (isGameFinished) return;
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
        if (isGameFinished) return;
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

        // 🎵 放牌音效
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPlaceCardSFX();
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
            {
                feedbackText.color = Color.green;
                SetFeedback("✔ Correct!  Target reached!");
                // 🎵 答對音效
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCorrectSFX();
                correctAnswers++;
                if (currentQuestion < totalQuestions)
                {
                    currentQuestion++;
                    StartCoroutine(NextPuzzleRoutine());
                }
                else
                {
                    isGameFinished = true;
                    if (resetButton != null) resetButton.interactable = false;
                    if (newPuzzleButton != null) newPuzzleButton.interactable = false;

                    float totalTime    = Time.time - startTime;
                    int   totalAttempts = correctAnswers + wrongAnswers;
                    float accuracy     = totalAttempts > 0
                        ? (float)correctAnswers / totalAttempts * 100f : 0f;
                    int minutes = Mathf.FloorToInt(totalTime / 60);
                    int seconds = Mathf.FloorToInt(totalTime % 60);

                    // 顯示結束畫面
                    ShowEndPanel(correctAnswers, wrongAnswers, accuracy, minutes, seconds);
                }
            }
            else
            {
                feedbackText.color = Color.red;
                wrongAnswers++;
                SetFeedback($"X Wrong : Result {(result % 1 == 0 ? ((int)result).ToString() : result.ToString("F2"))} ≠ Target {target}");
                // 🎵 答錯音效
                if (AudioManager.Instance != null) AudioManager.Instance.PlayWrongSFX();

                if (!isCheckingAnswer)
                    StartCoroutine(WrongAnswerRoutine());
            }
                
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
        if (difficultyText != null) {
            difficultyText.text = currentDifficulty.ToString();
        }
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
        if (result > 500)    return false;
        return true;
    }

    bool HasNegativeIntermediate(string expr)
    {
        try
        {
            int index = 0;
            ParseExpression(expr.Replace(" ", ""), ref index, out bool hasNegative);
            return hasNegative;
        }
        catch
        {
            return true;
        }
    }

    double ParseExpression(string s, ref int index, out bool hasNegative)
    {
        double value = ParseTerm(s, ref index, out hasNegative);
        while (index < s.Length && (s[index] == '+' || s[index] == '-'))
        {
            char op = s[index++];
            double right = ParseTerm(s, ref index, out bool rightNegative);
            hasNegative |= rightNegative;

            value = op == '+' ? value + right : value - right;

            if (value < 0)
                hasNegative = true;
        }
        return value;
    }

    double ParseTerm(string s, ref int index, out bool hasNegative)
    {
        double value = ParseFactor(s, ref index, out hasNegative);
        while (index < s.Length && (s[index] == '*' || s[index] == '/'))
        {
            char op = s[index++];
            double right = ParseFactor(s, ref index, out bool rightNegative);
            hasNegative |= rightNegative;

            if (op == '/')
            {
                if (right == 0)
                {
                    hasNegative = true;
                    return value;
                }
                value /= right;
            }
            else
            {
                value *= right;
            }

            if (value < 0)
                hasNegative = true;
        }
        return value;
    }

    double ParseFactor(string s, ref int index, out bool hasNegative)
    {
        hasNegative = false;
        if (s[index] == '(')
        {
            index++;
            double value = ParseExpression(s, ref index, out hasNegative);

            if (index < s.Length && s[index] == ')')
                index++;

            if (value < 0)
                hasNegative = true;

            return value;
        }

        int start = index;
        while (index < s.Length && char.IsDigit(s[index]))
            index++;

        return double.Parse(s.Substring(start, index - start));
    }

    bool HasMeaninglessBrackets(string expr)
    {
        if (!expr.Contains("("))
            return false;

        string noBrackets = expr.Replace("(", "").Replace(")", "");

        double original = System.Convert.ToDouble(
            new DataTable().Compute(expr, null));

        double simplified = System.Convert.ToDouble(
            new DataTable().Compute(noBrackets, null));

        return original == simplified;
    }

    const string HiddenSlot = "□";

    string HideNumbers(string expr) =>
        System.Text.RegularExpressions.Regex.Replace(expr, @"\d+", HiddenSlot);

    string GetDisplayExpression(string expr)
    {
        // 使用 ASCII 運算符，避免 TMP 預設字體缺字顯示方塊
        return expr
            .Replace("*", " x ")
            .Replace("/", "  ÷  ")
            .Replace("+", " + ")
            .Replace("-", " - ");
    }
    //答錯機制
    IEnumerator WrongAnswerRoutine()
    {
        isCheckingAnswer = true;

        yield return new WaitForSeconds(1.2f);

        ResetCurrentPuzzle();

        isCheckingAnswer = false;
    }
    //題數更新
    void UpdateQuestionUI()
    {
        if(questionText != null)
        questionText.text = $"{currentQuestion} / {totalQuestions}";
    }

    //答對自動跳題
    IEnumerator NextPuzzleRoutine()
    {
        isCheckingAnswer = true;
        yield return new WaitForSeconds(1.5f);

        GeneratePuzzle();

        isCheckingAnswer = false;
    }
    public void StartEasy()
    {
        Debug.Log("START EASY");
        currentDifficulty = Difficulty.Easy;
        StartGame();
    }

    public void StartMedium()
    {
        Debug.Log("START Medium");
        currentDifficulty = Difficulty.Medium;
        StartGame();
    }

    public void StartHard()
    {
        Debug.Log("START Hard");
        currentDifficulty = Difficulty.Hard;
        StartGame();
    }

    public void StartExpert()
    {
        Debug.Log("START Expert");
        currentDifficulty = Difficulty.Expert;
        StartGame();
    }
    public void ShowDifficultyMenu()
    {
        if (startPanel    != null) startPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void StartGame()
    {
        Debug.Log("StartGame Called");
        currentQuestion = 1;
        correctAnswers = 0;
        wrongAnswers = 0;
        resetCount = 0;
        newPuzzleCount = 0;

        startTime = Time.time;

        mainMenuPanel.SetActive(false);
        gamePanel.SetActive(true);
        if (endPanel != null) endPanel.SetActive(false);

        // 🎵 開始播放背景音樂
        if (AudioManager.Instance != null) AudioManager.Instance.PlayBGM();

        GeneratePuzzle();
    }

    void ShowEndPanel(int correct, int wrong, float accuracy, int min, int sec)
    {
        if (endPanel == null) return;

        endPanel.SetActive(true);

        // 1. 隱藏的按鈕拉出來顯示，並動態綁定點擊事件
        if (exportExcelButton != null)
        {
            exportExcelButton.gameObject.SetActive(true);
            exportExcelButton.onClick.RemoveAllListeners(); // 清除舊的，防止重玩時重複綁定
            exportExcelButton.onClick.AddListener(ExportToExcel); // 綁定下方建立的導出 Function
        }

        // 2. 將傳進來的數據存入類別變數，方便 ExportToExcel 讀取
        _cacheCorrect = correct;
        _cacheWrong = wrong;
        _cacheAccuracy = accuracy;
        _cacheMin = min;
        _cacheSec = sec;

        if (endSummaryText != null)
            endSummaryText.text =
                $"Result\n" +
                $"Difficulty : {difficultyText.text}\n" +
                $"Correct : {correct} / {totalQuestions}\n" +
                $"Wrong : {wrong}\n" +
                $"Reset : {resetCount}\n" +  
                $"Skip : {newPuzzleCount}\n" +
                $"Accuracy : {accuracy:F1}%\n" +
                $"Time : {min:00}:{sec:00}";
    }
    private void ExportToExcel()
    {
        // 1. 準備欄位標頭與真實時間戳
        string header = "No.,Difficulty,Total Questions,Correct,Wrong,Reset,Skip,Accuracy,Time,Timestamp\n";
        string currentTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string timeStr = $"{_cacheMin:00}:{_cacheSec:00}";
        
        int currentNo = 1;
        string dataLine = "";

        // ── 【情況 A：WebGL 網頁版執行邏輯】 ──
#if UNITY_WEBGL && !UNITY_EDITOR
        dataLine = $"{currentNo},{difficultyText.text},{totalQuestions},{_cacheCorrect},{_cacheWrong},{resetCount},{newPuzzleCount},{_cacheAccuracy:F1}%,{timeStr},{currentTimestamp}\n";
        string fileContent = header + dataLine;

        // 核心修正：利用純 C# 將文字轉為 Base64 編碼，並加上防亂碼的 BOM 頭 (\uFEFF)
        // 這樣可以直接調用瀏覽器跳轉下載，100% 不會導致 WebGL 打包失敗
        byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
        byte[] bomBytes = new byte[] { 0xEF, 0xBB, 0xBF };
        byte[] finalBytes = new byte[bomBytes.Length + utf8Bytes.Length];
        System.Buffer.BlockCopy(bomBytes, 0, finalBytes, 0, bomBytes.Length);
        System.Buffer.BlockCopy(utf8Bytes, 0, finalBytes, bomBytes.Length, utf8Bytes.Length);
        
        string base64Data = System.Convert.ToBase64String(finalBytes);
        string dataUrl = "data:text/csv;charset=utf-8;base64," + base64Data;

        // 叫瀏覽器直接開啟這個資料網址，就會立刻觸發 Excel 檔案下載
        Application.OpenURL(dataUrl);
        
        Debug.Log("【網頁輸出】已透過 Data URL 觸發下載");
        if (exportExcelButton != null) exportExcelButton.interactable = false;
        return;
#endif

        // ── 【情況 B：Mac 電腦本機/編輯器執行邏輯】（維持原樣，不影響功能） ──
        string filePath = Path.Combine(Application.dataPath, "../GameResults.csv");
        try
        {
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length > 0) currentNo = lines.Length;
            }

            dataLine = $"{currentNo},{difficultyText.text},{totalQuestions},{_cacheCorrect},{_cacheWrong},{resetCount},{newPuzzleCount},{_cacheAccuracy:F1}%,{timeStr},{currentTimestamp}\n";

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, header + dataLine, new UTF8Encoding(true));
            }
            else
            {
                File.AppendAllText(filePath, dataLine, new UTF8Encoding(true));
            }

            Debug.Log($"【本機輸出成功】成績已儲存至: {filePath}，編號: {currentNo}");
            if (exportExcelButton != null) exportExcelButton.interactable = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"【本機輸出失敗】無法寫入檔案: {e.Message}");
        }
    }

    public void BackToMenu()
    {
        currentQuestion = 1;
        correctAnswers  = 0;
        wrongAnswers    = 0;

        if (endPanel      != null) endPanel.SetActive(false);
        if (gamePanel     != null) gamePanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (startPanel    != null) startPanel.SetActive(true);   // 回開始畫面
    }

    public void RestartGame()
    {
        if (exportExcelButton != null) exportExcelButton.interactable = true; // 👈 新增這行：重置按鈕可點擊狀態
        if (endPanel != null) endPanel.SetActive(false);
        StartGame(); 
    }

    public void ShowInstruction()
    {
        if (instructionPanel != null) instructionPanel.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickSFX();
    }

    public void HideInstruction()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickSFX();
    }

    void WireBack(Button btn)
    {
        if (btn != null) btn.onClick.AddListener(GoBack);
    }

    /// <summary>依目前畫面返回上一層（單次 ESC 或 Back 按鈕）</summary>
    public void GoBack()
    {
        if (instructionPanel != null && instructionPanel.activeSelf)
        {
            HideInstruction();
            return;
        }

        if (endPanel != null && endPanel.activeSelf)
        {
            BackToDifficultyMenu();
            return;
        }

        if (gamePanel != null && gamePanel.activeSelf)
        {
            BackToDifficultyMenu();
            return;
        }

        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
        {
            BackToStartPanel();
        }
    }

    public void BackToStartPanel()
    {
        currentQuestion = 1;
        correctAnswers  = 0;
        wrongAnswers    = 0;

        if (endPanel      != null) endPanel.SetActive(false);
        if (gamePanel     != null) gamePanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (startPanel    != null) startPanel.SetActive(true);
    }

    public void BackToDifficultyMenu()
    {
        currentQuestion = 1;
        correctAnswers  = 0;
        wrongAnswers    = 0;

        if (endPanel      != null) endPanel.SetActive(false);
        if (gamePanel     != null) gamePanel.SetActive(false);
        if (startPanel    != null) startPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    /// <summary>連按兩次 ESC 觸發，直接退出遊戲</summary>
    public void QuitGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopBGM();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
    