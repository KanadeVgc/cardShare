using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor 工具腳本：自動在場景中建立遊戲說明面板（InstructionPanel），
/// 並綁定到 GameManager 的對應欄位。
/// 
/// 使用方式：Unity 上方選單 → CardShare → Setup Instruction Panel
/// </summary>
public class SetupInstructionPanel : Editor
{
    [MenuItem("CardShare/Setup Instruction Panel")]
    static void Setup()
    {
        // ── 1. 找到場景中的 GameManager ──
        GameManager gm = Object.FindObjectOfType<GameManager>();
        if (gm == null)
        {
            EditorUtility.DisplayDialog("Error", "找不到場景中的 GameManager！", "OK");
            return;
        }

        // ── 2. 找到 Canvas ──
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "找不到場景中的 Canvas！", "OK");
            return;
        }

        // ── 3. 嘗試載入說明圖片 Sprite ──
        Sprite instructionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Sprites/game_instruction.png");

        if (instructionSprite == null)
        {
            Debug.LogWarning("找不到 Assets/Sprites/game_instruction.png，面板將使用白色背景。" +
                             "請確認圖片的 Texture Type 設為 Sprite (2D and UI)。");
        }

        // ── 4. 建立 InstructionPanel ──
        GameObject panelObj = new GameObject("InstructionPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        // 全螢幕覆蓋
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // 加上底色（半透明黑色遮罩）
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.7f);
        panelBg.raycastTarget = true;

        // ── 5. 建立說明圖片 ──
        GameObject imgObj = new GameObject("InstructionImage");
        imgObj.transform.SetParent(panelObj.transform, false);

        RectTransform imgRect = imgObj.AddComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.5f, 0.5f);
        imgRect.anchorMax = new Vector2(0.5f, 0.5f);
        imgRect.sizeDelta = new Vector2(700, 900);

        Image img = imgObj.AddComponent<Image>();
        if (instructionSprite != null)
        {
            img.sprite = instructionSprite;
            img.preserveAspect = true;
        }
        else
        {
            img.color = new Color(0.95f, 0.98f, 0.95f, 1f); // 淺綠色底
        }

        // ── 6. 建立關閉按鈕（右上角 X） ──
        GameObject closeBtnObj = new GameObject("CloseInstructionButton");
        closeBtnObj.transform.SetParent(panelObj.transform, false);

        RectTransform closeBtnRect = closeBtnObj.AddComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1f, 1f);
        closeBtnRect.anchorMax = new Vector2(1f, 1f);
        closeBtnRect.pivot = new Vector2(1f, 1f);
        closeBtnRect.anchoredPosition = new Vector2(-30, -30);
        closeBtnRect.sizeDelta = new Vector2(80, 80);

        Image closeBtnImg = closeBtnObj.AddComponent<Image>();
        closeBtnImg.color = new Color(0.85f, 0.25f, 0.25f, 1f); // 紅色背景

        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;

        // 按鈕上的 X 文字
        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);

        RectTransform closeTextRect = closeTextObj.AddComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeTxt = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeTxt.text = "✕";
        closeTxt.fontSize = 42;
        closeTxt.color = Color.white;
        closeTxt.alignment = TextAlignmentOptions.Center;

        // ── 7. 在 StartPanel 裡找到適當位置建立「遊戲說明」按鈕 ──
        Button openBtn = null;
        if (gm.startPanel != null)
        {
            GameObject openBtnObj = new GameObject("OpenInstructionButton");
            openBtnObj.transform.SetParent(gm.startPanel.transform, false);

            RectTransform openBtnRect = openBtnObj.AddComponent<RectTransform>();
            openBtnRect.anchorMin = new Vector2(0.5f, 0f);
            openBtnRect.anchorMax = new Vector2(0.5f, 0f);
            openBtnRect.pivot = new Vector2(0.5f, 0f);
            openBtnRect.anchoredPosition = new Vector2(0, 50);
            openBtnRect.sizeDelta = new Vector2(300, 80);

            Image openBtnImg = openBtnObj.AddComponent<Image>();
            openBtnImg.color = new Color(0.45f, 0.82f, 0.72f, 1f); // 馬卡龍綠

            openBtn = openBtnObj.AddComponent<Button>();
            openBtn.targetGraphic = openBtnImg;

            // 按鈕文字
            GameObject openTextObj = new GameObject("Text");
            openTextObj.transform.SetParent(openBtnObj.transform, false);

            RectTransform openTextRect = openTextObj.AddComponent<RectTransform>();
            openTextRect.anchorMin = Vector2.zero;
            openTextRect.anchorMax = Vector2.one;
            openTextRect.offsetMin = Vector2.zero;
            openTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI openTxt = openTextObj.AddComponent<TextMeshProUGUI>();
            openTxt.text = "遊戲說明";
            openTxt.fontSize = 36;
            openTxt.color = Color.white;
            openTxt.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            Debug.LogWarning("GameManager 的 startPanel 為 null，無法在首頁建立「遊戲說明」按鈕。" +
                             "請手動建立按鈕並拖入 GameManager 的 Open Instruction Button 欄位。");
        }

        // ── 8. 綁定到 GameManager 的欄位 ──
        SerializedObject so = new SerializedObject(gm);

        SerializedProperty propPanel = so.FindProperty("instructionPanel");
        if (propPanel != null) propPanel.objectReferenceValue = panelObj;

        SerializedProperty propOpenBtn = so.FindProperty("openInstructionButton");
        if (propOpenBtn != null && openBtn != null) propOpenBtn.objectReferenceValue = openBtn;

        SerializedProperty propCloseBtn = so.FindProperty("closeInstructionButton");
        if (propCloseBtn != null) propCloseBtn.objectReferenceValue = closeBtn;

        so.ApplyModifiedProperties();

        // ── 9. 預設隱藏面板 ──
        panelObj.SetActive(false);

        // ── 10. 標記場景已修改（提醒存檔） ──
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("完成！",
            "已自動建立：\n" +
            "• InstructionPanel（說明面板 + 說明圖片）\n" +
            "• CloseInstructionButton（關閉按鈕 ✕）\n" +
            "• OpenInstructionButton（首頁「遊戲說明」按鈕）\n\n" +
            "所有欄位已自動綁定到 GameManager。\n" +
            "請記得按 Ctrl+S 存檔場景！", "OK");

        // 選中面板方便調整
        Selection.activeGameObject = panelObj;
    }
}
