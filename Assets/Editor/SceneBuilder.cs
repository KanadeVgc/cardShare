#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 選單：CardShare → Build Scene
/// 一鍵自動建立完整遊戲 UI（直式版面）並完成所有 Inspector 接線。
/// </summary>
public static class SceneBuilder
{
    // ── 牌值表 ──────────────────────────────────────────────
    static readonly (string prefix, int value)[] RANKS =
    {
        ("A",1),("2",2),("3",3),("4",4),("5",5),("6",6),("7",7),
        ("8",8),("9",9),("10",10),("J",11),("Q",12),("K",13)
    };
    static readonly string[] SUIT_SUFFIXES =
        { "C_clubs", "D_diamonds", "H_hearts", "S_spades" };

    // ── 顏色 ────────────────────────────────────────────────
    static readonly Color C_BG        = Hex("#1A2035");
    static readonly Color C_PANEL     = Hex("#1E2D45CC");  // 半透明深藍
    static readonly Color C_SLOT_FILL = Hex("#243352CC");
    static readonly Color C_SLOT_BORD = Hex("#4B7FCC");
    static readonly Color C_BTN_RESET = Hex("#2A6EC2");
    static readonly Color C_BTN_NEW   = Hex("#1DAF75");
    static readonly Color C_GOLD      = Hex("#FFD966");
    static readonly Color C_WHITE     = Color.white;
    static readonly Color C_FAINT     = Hex("#8BAFD0");
    static readonly Color C_GREEN     = Hex("#5EE89A");
    static readonly Color C_BTN_BACK  = Hex("#1D3D95");

    const float MENU_BTN_W = 0.3f;
    const float MENU_BTN_H = 0.08f;
    static float CenteredMenuX() => (1f - MENU_BTN_W) * 0.5f;

    // ════════════════════════════════════════════════════════
    [MenuItem("CardShare/Build Scene")]
    static void BuildScene()
    {
        // 清除舊物件（Find 只會回傳第一個同名物件，需迴圈刪除全部）
        foreach (var name in new[]{"GameManager","Canvas","EventSystem", "AudioManager"})
        {
            GameObject old;
            while ((old = GameObject.Find(name)) != null)
                Object.DestroyImmediate(old);
        }

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // GameManager
        var gmGO = new GameObject("GameManager");
        var gm   = gmGO.AddComponent<GameManager>();

        // Camera
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = C_BG;
            Camera.main.clearFlags      = CameraClearFlags.SolidColor;
        }

        // AudioManager
        var amGO = new GameObject("AudioManager");
        var am   = amGO.AddComponent<AudioManager>();

        // ── 自動載入音效 ──────────────────────────────────────
        am.bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/背景音樂.mp3");
        am.correctClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/答對音效.mp3");
        am.dealCardClips = new AudioClip[]
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/發牌音效 1.mp3"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/發牌音效 2.mp3"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/發牌音效 3.mp3"),
        };
        if (am.bgmClip == null)
            Debug.LogWarning("⚠ 找不到 Assets/Audio/背景音樂.mp3，請確認音檔存在");

        // Canvas（1080×1920 直式）
        var canvasGO = new GameObject("Canvas", typeof(RectTransform));
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.transform;
        
        // ── 全畫面背景 ──
        MakeFullBG(root);

        var gamePanel = CreateUIObject("GamePanel",root);
        StretchFull(gamePanel);

        var mainMenuPanel = CreateUIObject("MainMenuPanel",root);
        StretchFull(mainMenuPanel);

        var endPanel = CreateUIObject("EndPenal",root);
        StretchFull(endPanel);

        var startPanel = CreateUIObject("StartPanel",root);
        StretchFull(startPanel);

        var instructionPanel = CreateUIObject("InstructionPanel",root);
        StretchFull(instructionPanel);
        


        // ── 建各區塊 ──
        var infoPanel   = BuildInfoPanel(gamePanel);
        var handPanel   = BuildHandPanel(gamePanel);
        var slotPanel   = BuildSlotPanel(gamePanel);
        var btnPanel    = BuildButtonPanel(gamePanel, gm);

        // ── 取得元件 ──
        var targetTMP  = infoPanel.Find("TargetText")       .GetComponent<TextMeshProUGUI>();
        var exprTMP    = infoPanel.Find("ExpressionText")   .GetComponent<TextMeshProUGUI>();
        var curTMP     = infoPanel.Find("CurrentResultText").GetComponent<TextMeshProUGUI>();
        var fbTMP      = infoPanel.Find("FeedbackText")     .GetComponent<TextMeshProUGUI>();
        var questionTMP = infoPanel.Find("QuestionText")    .GetComponent<TextMeshProUGUI>();    

        var cardImages     = new Image[4];
        var cardDraggables = new CardDraggable[4];
        for (int i = 0; i < 4; i++)
        {
            var t          = handPanel.Find($"Card{i}");
            cardImages[i]     = t.GetComponent<Image>();
            cardDraggables[i] = t.GetComponent<CardDraggable>();
            cardDraggables[i].handIndex = i;

            var hl = t.Find("SelectHighlight");
            if (hl) cardDraggables[i].selectedHighlight = hl.gameObject;
        }

        var cardSlots = new CardSlot[4];
        for (int i = 0; i < 4; i++)
            cardSlots[i] = slotPanel.Find($"Slot{i}").GetComponent<CardSlot>();

        var resetBtn  = btnPanel.Find("ResetButton") .GetComponent<Button>();
        var newBtn    = btnPanel.Find("NewPuzzleButton").GetComponent<Button>();

        // ── 載入牌 ──
        var allCards = LoadAllCards();

        // ── 接線 ──
        gm.targetText        = targetTMP;
        gm.expressionText    = exprTMP;
        gm.currentResultText = curTMP;
        gm.feedbackText      = fbTMP;
        gm.mainMenuPanel = mainMenuPanel.gameObject;
        gm.gamePanel = gamePanel.gameObject;   
        gm.endPanel = endPanel.gameObject;
        gm.startPanel = startPanel.gameObject;
        gm.instructionPanel = instructionPanel.gameObject;
        gm.questionText      = questionTMP;
        gm.cardImages        = cardImages;
        gm.cardDraggables    = cardDraggables;
        gm.slots             = cardSlots;
        gm.resetButton       = resetBtn;
        gm.newPuzzleButton   = newBtn;
        gm.allCards          = allCards;
        gm.currentDifficulty = GameManager.Difficulty.Easy;


        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"✅ 場景建置完成！載入 {allCards.Length} 張牌。");
        EditorUtility.DisplayDialog("CardShare 場景建置完成",
            $"✅ 建置成功！載入 {allCards.Length} 張牌。\n\n" +
            "按 ▶ Play 開始遊戲。\n\n快捷鍵：\n  Space = 新題目\n  R = 重置", "OK");
        BuildMainMenu(mainMenuPanel, gm);
        BuildEndPanel(endPanel, gm);
        BuildStartPanel(startPanel, gm);
        BuildInstructionPanel(instructionPanel, gm);

        startPanel.gameObject.SetActive(true);
        mainMenuPanel.gameObject.SetActive(false);
        gamePanel.gameObject.SetActive(false);
        endPanel.gameObject.SetActive(false);
        instructionPanel.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════
    //  全畫面背景
    // ════════════════════════════════════════════════════════
    static void MakeFullBG(Transform root)
    {
        var go   = new GameObject("Background");
        go.transform.SetParent(root, false);
        var r    = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var img  = go.AddComponent<Image>();
        img.color = C_BG;
        img.raycastTarget = false;
    }

    // ════════════════════════════════════════════════════════
    //  Info Panel  (上方 10%–38%)
    // ════════════════════════════════════════════════════════
    static Transform BuildInfoPanel(Transform root)
    {
        // anchorMin=(0.04, 0.62)  anchorMax=(0.96, 0.94)
        var panel = MakePanel(root, "InfoPanel",
            0.04f, 0.62f, 0.96f, 0.94f, C_PANEL, 24f);

        // TargetText
        var tgt = MakeTMP(panel, "TargetText", "Target : —",
            0f, 0.70f, 1f, 1.0f, 64, C_GOLD, FontStyles.Bold, TextAlignmentOptions.Center);

        // ExpressionText
        MakeTMP(panel, "ExpressionText", "? + ? - ? x ?",
            0f, 0.38f, 1f, 0.72f, 42, C_FAINT, FontStyles.Normal, TextAlignmentOptions.Center);

        // CurrentResultText
        MakeTMP(panel, "CurrentResultText", "Current : —",
            0f, 0.12f, 1f, 0.42f, 48, C_GOLD, FontStyles.Bold, TextAlignmentOptions.Center);

        // FeedbackText
        MakeTMP(panel, "FeedbackText", "",
            0f, 0f, 1f, 0.16f, 36, C_GREEN, FontStyles.Bold, TextAlignmentOptions.Center);
        
        //位置
        MakeTMP(panel, "QuestionText", "1/10",
            0.72f, 0.82f, 0.96f, 0.98f,
            28, C_WHITE, FontStyles.Bold,
            TextAlignmentOptions.TopRight);

        return panel;
    }

    // ════════════════════════════════════════════════════════
    //  Hand Cards Panel  (中段 36%–60%)
    // ════════════════════════════════════════════════════════
    static Transform BuildHandPanel(Transform root)
    {
        var panel = MakePanel(root, "HandPanel",
            0.04f, 0.35f, 0.96f, 0.60f, C_PANEL, 20f);

        // 標題
        MakeTMP(panel, "HandLabel", "-- HAND CARDS --",
            0f, 0.78f, 1f, 1.0f, 34, C_FAINT, FontStyles.Normal, TextAlignmentOptions.Center);

        // 4 張牌（水平排列）
        float[] xs = { 0.06f, 0.30f, 0.54f, 0.78f }; // 每張左錨
        float   cardW = 0.21f, cardH = 0.72f;
        float   cardYMin = 0.04f, cardYMax = cardYMin + cardH;

        for (int i = 0; i < 4; i++)
        {
            float xMin = xs[i], xMax = xMin + cardW;

            var cardGO = new GameObject($"Card{i}");
            cardGO.transform.SetParent(panel, false);
            var r = cardGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, cardYMin);
            r.anchorMax = new Vector2(xMax, cardYMax);
            r.offsetMin = r.offsetMax = Vector2.zero;

            var img            = cardGO.AddComponent<Image>();
            img.color          = C_WHITE;
            img.preserveAspect = true;
            img.raycastTarget  = true;

            // CardDraggable
            var cd = cardGO.AddComponent<CardDraggable>();
            cd.handIndex = i;

            // 選取高亮框
            var hlGO = new GameObject("SelectHighlight");
            hlGO.transform.SetParent(cardGO.transform, false);
            var hlR = hlGO.AddComponent<RectTransform>();
            hlR.anchorMin = Vector2.zero;
            hlR.anchorMax = Vector2.one;
            hlR.offsetMin = new Vector2(-8, -8);
            hlR.offsetMax = new Vector2(8, 8);
            var hlImg  = hlGO.AddComponent<Image>();
            hlImg.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            hlImg.raycastTarget = false;
            // 讓高亮框變成空心（只顯示邊框）- 放在卡牌之後
            hlGO.transform.SetAsFirstSibling();
            hlGO.SetActive(false);
            cd.selectedHighlight = hlGO;
        }

        return panel;
    }

    // ════════════════════════════════════════════════════════
    //  Slot Panel  (下段 10%–34%)
    // ════════════════════════════════════════════════════════
    static Transform BuildSlotPanel(Transform root)
    {
        var panel = MakePanel(root, "SlotPanel",
            0.04f, 0.08f, 0.96f, 0.34f, C_PANEL, 20f);

        MakeTMP(panel, "SlotLabel", "-- PLACE CARDS (in order) --",
            0f, 0.78f, 1f, 1.0f, 34, new Color(0.6f, 0.85f, 1f), FontStyles.Normal,
            TextAlignmentOptions.Center);

        float[] xs = { 0.06f, 0.30f, 0.54f, 0.78f };
        float   cardW = 0.21f, cardH = 0.72f;
        float   cardYMin = 0.04f, cardYMax = cardYMin + cardH;

        for (int i = 0; i < 4; i++)
        {
            float xMin = xs[i], xMax = xMin + cardW;

            // Slot 根物件
            var slotGO = new GameObject($"Slot{i}");
            slotGO.transform.SetParent(panel, false);
            var r = slotGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, cardYMin);
            r.anchorMax = new Vector2(xMax, cardYMax);
            r.offsetMin = r.offsetMax = Vector2.zero;

            // 空 Slot 背景
            var bgImg           = slotGO.AddComponent<Image>();
            bgImg.color         = C_SLOT_FILL;
            bgImg.raycastTarget = true;

            // 放入牌的 Image（初始關閉）
            var ciGO = new GameObject("CardImage");
            ciGO.transform.SetParent(slotGO.transform, false);
            var ciR = ciGO.AddComponent<RectTransform>();
            ciR.anchorMin = Vector2.zero;
            ciR.anchorMax = Vector2.one;
            ciR.offsetMin = ciR.offsetMax = Vector2.zero;
            var ci = ciGO.AddComponent<Image>();
            ci.preserveAspect  = true;
            ci.raycastTarget   = false;
            ci.enabled         = false;

            // 空 Slot 編號提示
            var numGO = MakeTMP(slotGO.transform, $"SlotNum{i}", (i + 1).ToString(),
                0f, 0.2f, 1f, 0.8f, 60, new Color(0.4f, 0.65f, 1f, 0.4f),
                FontStyles.Bold, TextAlignmentOptions.Center);

            // CardSlot
            var cs          = slotGO.AddComponent<CardSlot>();
            cs.slotIndex    = i;
            cs.cardImage    = ci;
            cs.emptyOverlay = numGO;

            // SlotClickReceiver
            var scr        = slotGO.AddComponent<SlotClickReceiver>();
            scr.slotIndex  = i;
        }

        return panel;
    }

    // ════════════════════════════════════════════════════════
    //  Button Panel  (最底 1%–7%)
    // ════════════════════════════════════════════════════════
    static Transform BuildButtonPanel(Transform root, GameManager gm)
    {
        var panel = MakePanel(root, "ButtonPanel",
            0.04f, 0.01f, 0.96f, 0.07f, new Color(0, 0, 0, 0), 0f);

        MakeButton(panel, "ResetButton",    "Reset",
            0.02f, 0f, 0.32f, 1f, C_BTN_RESET);
        MakeButton(panel, "NewPuzzleButton", "New Puzzle",
            0.35f, 0f, 0.65f, 1f, C_BTN_NEW);
        gm.gameBackButton = MakeBackButton(panel, "BackButton", "Back",
            0.68f, 0f, 0.98f, 1f);

        return panel;
    }

    // ════════════════════════════════════════════════════════
    //  載入 52 張 CardData（並修正 Sprite 匯入設定）
    // ════════════════════════════════════════════════════════
    static CardData[] LoadAllCards()
    {
        var list = new List<CardData>();

        foreach (var suit in SUIT_SUFFIXES)
        {
            foreach (var (prefix, value) in RANKS)
            {
                string path   = $"Assets/Sprites/{prefix}_{suit}.png";
                var    importer = AssetImporter.GetAtPath(path) as TextureImporter;

                // 確保 Sprite 匯入設定正確
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType      = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    AssetDatabase.ImportAsset(path,
                        ImportAssetOptions.ForceUpdate |
                        ImportAssetOptions.ForceSynchronousImport);
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogWarning($"⚠ 找不到 Sprite：{path}");
                    continue;
                }

                list.Add(new CardData { sprite = sprite, value = value });
            }
        }

        return list.ToArray();
    }

    // ════════════════════════════════════════════════════════
    //  UI 工具
    // ════════════════════════════════════════════════════════

    /// <summary>建立一個有背景 Image 的 Panel，錨點用絕對 0-1 值。</summary>
    static Transform MakePanel(Transform parent, string name,
        float xMin, float yMin, float xMax, float yMax,
        Color color, float cornerRadius = 0f)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r   = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(xMin, yMin);
        r.anchorMax = new Vector2(xMax, yMax);
        r.offsetMin = r.offsetMax = Vector2.zero;

        var img  = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go.transform;
    }

    /// <summary>建立 TMP 文字，錨點用父 Panel 的相對 0-1。</summary>
    static GameObject MakeTMP(Transform parent, string name, string text,
        float xMin, float yMin, float xMax, float yMax,
        float fontSize, Color color,
        FontStyles style = FontStyles.Normal,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r   = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(xMin, yMin);
        r.anchorMax = new Vector2(xMax, yMax);
        r.offsetMin = r.offsetMax = Vector2.zero;

        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return go;
    }

    /// <summary>建立一個 Button，錨點用父 Panel 的相對 0-1。</summary>
    static void MakeButton(Transform parent, string name, string label,
        float xMin, float yMin, float xMax, float yMax, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r   = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(xMin, yMin);
        r.anchorMax = new Vector2(xMax, yMax);
        r.offsetMin = r.offsetMax = Vector2.zero;

        var img  = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb  = ColorBlock.defaultColorBlock;
        cb.normalColor      = color;
        cb.highlightedColor = color * 1.25f;
        cb.pressedColor     = color * 0.65f;
        btn.colors          = cb;

        // 文字
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var tr = txtGO.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        var tmp       = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 36;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    static Button MakeBackButton(Transform parent, string name, string label,
        float xMin, float yMin, float xMax, float yMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r   = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(xMin, yMin);
        r.anchorMax = new Vector2(xMax, yMax);
        r.offsetMin = r.offsetMax = Vector2.zero;

        var img  = go.AddComponent<Image>();
        img.color = C_BTN_BACK;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb  = ColorBlock.defaultColorBlock;
        cb.normalColor      = C_BTN_BACK;
        cb.highlightedColor = C_BTN_BACK * 1.25f;
        cb.pressedColor     = C_BTN_BACK * 0.65f;
        btn.colors          = cb;

        MakeTMP(go.transform, "Text", label,
            0f, 0f, 1f, 1f, 36, C_WHITE, FontStyles.Bold, TextAlignmentOptions.Center);

        return btn;
    }

    // ════════════════════════════════════════════════════════
    //  顏色工具
    // ════════════════════════════════════════════════════════
    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
    static void BuildStartPanel(Transform panel, GameManager gm)
    {
        // ── 背景圖 ──
        var bgSprite = EnsureSprite("Assets/Sprites/game_start_background.png");
        if (bgSprite != null)
        {
            var bgGO = CreateUIObject("StartBackground", panel);
            SetAnchor(bgGO, 0f, 0f, 1f, 1f);
            var bgImg = bgGO.gameObject.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
            bgGO.SetAsFirstSibling();
        }

        // ── 標題 ──
        MakeTMP(panel, "Title", "Puzzle Mystery",
            0.05f, 0.62f, 0.95f, 0.88f,
            96, C_GOLD, FontStyles.Bold, TextAlignmentOptions.Center);

        // ── Start 按鈕 ──
        var startBtn = CreateMenuButton(panel, gm, "StartButton", "Start Game", CenteredMenuX(), 0.38f, GameManager.Difficulty.Easy);
        gm.startButton = startBtn;

        // ── Instructions 按鈕 ──
        var btn2 = CreateMenuButton(panel, gm, "OpenInstructionButton", "Instructions", CenteredMenuX(), 0.24f, GameManager.Difficulty.Easy);
        gm.openInstructionButton = btn2;

    }

    static void BuildEndPanel(Transform panel, GameManager gm)
    {
        var summaryGO = MakeTMP(panel, "EndSummaryText", "Result", 0.1f, 0.3f, 0.9f, 0.8f, 48, C_WHITE, FontStyles.Bold, TextAlignmentOptions.Center);
        gm.endSummaryText = summaryGO.GetComponent<TextMeshProUGUI>();

        var backBtn = CreateMenuButton(panel, gm, "BackToMenuButton", "Main Menu", 0.05f, 0.1f, GameManager.Difficulty.Easy);
        gm.backToMenuButton = backBtn;

        var restartBtn = CreateMenuButton(panel, gm, "RestartButton", "Restart", 0.37f, 0.1f, GameManager.Difficulty.Easy);
        gm.restartButton = restartBtn;

        gm.endBackButton = MakeBackButton(panel, "EndBackButton", "Back", 0.69f, 0.1f, 0.99f, 0.18f);
    }

    static void BuildInstructionPanel(Transform panel, GameManager gm)
    {
        // 半透明遮罩
        var overlay = CreateUIObject("Overlay", panel);
        SetAnchor(overlay, 0f, 0f, 1f, 1f);
        overlay.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);

        // 說明圖片
        var sprite = EnsureSprite("Assets/Sprites/game_instruction.png");
        var img = CreateUIObject("InstructionImage", panel);
        SetAnchor(img, 0.1f, 0.2f, 0.9f, 0.9f);
        var imgComp = img.gameObject.AddComponent<Image>();
        if (sprite != null)
        {
            imgComp.sprite = sprite;
            imgComp.type = Image.Type.Simple;
            imgComp.preserveAspect = true;
            imgComp.color = Color.white;
        }
        else
        {
            Debug.LogWarning("⚠ 找不到 Assets/Sprites/game_instruction.png");
            imgComp.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        }
        imgComp.raycastTarget = false;

        var closeBtn = CreateUIObject("CloseInstructionButton", panel);
        SetAnchor(closeBtn, 0.4f, 0.05f, 0.6f, 0.15f);
        var btnImg = closeBtn.gameObject.AddComponent<Image>();
        btnImg.color = Color.red;
        var btn = closeBtn.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        
        MakeTMP(closeBtn, "Text", "Close", 0f, 0f, 1f, 1f, 36, C_WHITE, FontStyles.Bold, TextAlignmentOptions.Center);
        gm.closeInstructionButton = btn;
    }

    static void BuildMainMenu(Transform root, GameManager gm)
    {
        // ── 背景圖（確保 Sprite 匯入設定正確）──
        var bgSprite = EnsureSprite("Assets/Sprites/game_start_background.png");
        if (bgSprite != null)
        {
            var bgGO = CreateUIObject("MenuBackground", root);
            SetAnchor(bgGO, 0f, 0f, 1f, 1f);
            var bgImg = bgGO.gameObject.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
            bgGO.SetAsFirstSibling();
        }
        else Debug.LogWarning("⚠ 找不到 Assets/Sprites/game_start_background.png");

        // ── 標題 ──
        MakeTMP(root, "Title", "Puzzle Mystery",
            0.1f, 0.78f, 0.9f, 0.95f,
            80, C_GOLD, FontStyles.Bold, TextAlignmentOptions.Center);

        // ── 難度卡片路徑 ──
        var cards = new[]
        {
            ("EasyButton",   "Assets/Sprites/four_level_cards/easy_簡單.png"),
            ("MediumButton", "Assets/Sprites/four_level_cards/normal_普通.png"),
            ("HardButton",   "Assets/Sprites/four_level_cards/hard_困難.png"),
            ("ExpertButton", "Assets/Sprites/four_level_cards/expert_專家.png"),
        };

        // 2×2 格局錨點
        float[,] anchors = {
            { 0.04f, 0.42f, 0.49f, 0.76f },   // 左上 Easy
            { 0.51f, 0.42f, 0.96f, 0.76f },   // 右上 Medium
            { 0.04f, 0.06f, 0.49f, 0.40f },   // 左下 Hard
            { 0.51f, 0.06f, 0.96f, 0.40f },   // 右下 Expert
        };

        for (int i = 0; i < cards.Length; i++)
        {
            var (name, spritePath) = cards[i];

            // 強制設定 TextureType = Sprite
            var sprite = EnsureSprite(spritePath);
            if (sprite == null)
                Debug.LogWarning($"⚠ 找不到難度圖片：{spritePath}");

            var cardGO = CreateUIObject(name, root);
            SetAnchor(cardGO, anchors[i,0], anchors[i,1], anchors[i,2], anchors[i,3]);

            var cardImg = cardGO.gameObject.AddComponent<Image>();
            if (sprite != null)
            {
                cardImg.sprite = sprite;
                cardImg.type = Image.Type.Simple;
                cardImg.preserveAspect = true;
                cardImg.color = Color.white;
            }
            else
            {
                cardImg.color = new Color(0.1f, 0.25f, 0.6f);
            }

            var btn = cardGO.gameObject.AddComponent<Button>();
            btn.targetGraphic = cardImg;
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.colors = cb;

            switch (i)
            {
                case 0: gm.easyButton   = btn; break;
                case 1: gm.mediumButton = btn; break;
                case 2: gm.hardButton   = btn; break;
                case 3: gm.expertButton = btn; break;
            }
            Debug.Log($"🃏 {name}：{(sprite != null ? "✅" : "❌ 無圖片")}");
        }

        gm.mainMenuBackButton = MakeBackButton(root, "MainMenuBackButton", "Back",
            0.82f, 0.94f, 0.98f, 0.99f);
    }

    /// <summary>
    /// 確保圖片被匯入為 Sprite 類型，並回傳 Sprite 參考。
    /// </summary>
    static Sprite EnsureSprite(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            AssetDatabase.ImportAsset(assetPath,
                ImportAssetOptions.ForceUpdate |
                ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"🔄 已重新匯入為 Sprite：{assetPath}");
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }


   static Button CreateMenuButton(
        Transform root,
        GameManager gm,
        string name,
        string text,
        float x,
        float y,
        GameManager.Difficulty difficulty)
    {
        var btnGO = CreateUIObject(name, root);

        SetAnchor(btnGO, x, y, x + MENU_BTN_W, y + MENU_BTN_H);

        var img = btnGO.gameObject.AddComponent<Image>();
        img.color = new Color(0.1f, 0.25f, 0.6f);

        var btn = btnGO.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        MakeTMP(btnGO,
            "Text",
            text,
            0f, 0f, 1f, 1f,
            36,
            C_WHITE,
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        return btn;

        // btn.onClick.AddListener(() =>
        // {
        //     Debug.Log("Easy Clicked");
        //     switch (difficulty)
        //     {
        //         case GameManager.Difficulty.Easy:
        //             GameManager.Instance.StartEasy();
        //             break;

        //         case GameManager.Difficulty.Medium:
        //             GameManager.Instance.StartMedium();
        //             break;

        //         case GameManager.Difficulty.Hard:
        //             GameManager.Instance.StartHard();
        //             break;

        //         case GameManager.Difficulty.Expert:
        //             GameManager.Instance.StartExpert();
        //             break;
        //     }
        // });
    }
    static Transform CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;

        return go.transform;
    }

    static void StretchFull(Transform t)
    {
        var rt = t.GetComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;

        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetAnchor(
        Transform t,
        float xmin,
        float ymin,
        float xmax,
        float ymax)
    {
        var rt = t.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(xmin, ymin);
        rt.anchorMax = new Vector2(xmax, ymax);

        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
