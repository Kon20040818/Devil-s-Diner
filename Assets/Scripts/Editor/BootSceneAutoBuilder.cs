// ============================================================
// BootSceneAutoBuilder.cs
// BootScene（タイトル画面）を全自動構築するエディタ拡張。
// メニュー「DevilsDiner > Build Test Boot Scene」で実行する。
// ============================================================
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// BootScene をワンクリックで全自動構築するエディタ拡張。
/// GameManager（コアシステム群）と TitleMenuUI（Canvas）を生成・結線する。
/// </summary>
public static class BootSceneAutoBuilder
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const string MENU_PATH = "DevilsDiner/Build Test Boot Scene";

    // ──────────────────────────────────────────────
    // メニュー実行
    // ──────────────────────────────────────────────

    [MenuItem(MENU_PATH)]
    private static void Execute()
    {
        int totalSteps = 4;
        int currentStep = 0;

        try
        {
            // ── Step 1: 新規シーン作成 ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Boot Scene",
                "Creating new scene...", (float)currentStep / totalSteps);
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ── Step 2: GameManager（コアシステム群） ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Boot Scene",
                "Creating GameManager with core systems...", (float)currentStep / totalSteps);
            GameObject gmObj = EnsureGameManager();

            // ── Step 3: Title Canvas + TitleMenuUI ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Boot Scene",
                "Creating Title Canvas...", (float)currentStep / totalSteps);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            CreateTitleCanvas(font);

            // ── Step 4: カメラ背景色設定 ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Boot Scene",
                "Setting up camera...", (float)currentStep / totalSteps);
            SetupCamera();

            // ── 完了 ──
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[BootSceneAutoBuilder] Test Boot Scene を構築しました。\n" +
                "  - GameManager (singleton + InventoryManager + DebugController + SaveDataManager + AudioManager)\n" +
                "  - Title Canvas (TitleMenuUI)\n" +
                "    - Title Label: \"Devil's Diner\"\n" +
                "    - Subtitle Label: \"～魔界の荒野とガンブレード～\"\n" +
                "    - New Game Button\n" +
                "    - Continue Button (セーブデータ無しでグレーアウト)\n" +
                "    - Options Button (モック)\n" +
                "    - Options Panel (モック、初期非表示)\n\n" +
                "========== リリース確認テスト手順 ==========\n" +
                "1. BootScene を Play モードで再生する\n" +
                "2. タイトル画面が表示されることを確認する\n" +
                "   - Continue ボタンがグレーアウト（セーブデータ無し）であること\n" +
                "3. [New Game] をクリックする\n" +
                "   → ManagementScene に遷移し Morning フェーズが開始されること\n" +
                "4. [F2] を数回押してゴールドを追加する（5000G 程度）\n" +
                "5. スキルツリーでスキルを解放し、店舗拡張でレベルアップする\n" +
                "6. [F4] で手動セーブする → コンソールに「セーブ完了」\n" +
                "7. Play モードを停止する\n" +
                "8. 再度 BootScene を Play モードで再生する\n" +
                "9. Continue ボタンが活性化していることを確認する\n" +
                "10. [Continue] をクリックする\n" +
                "    → ManagementScene に遷移し、セーブ時の状態が復元されること\n" +
                "11. 以下を検証する:\n" +
                "    - 所持金がセーブ時の値に復元されている\n" +
                "    - 解放済みスキルが復元されている\n" +
                "    - 店舗レベルがセーブ時の値に復元されている\n" +
                "==========================================");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ══════════════════════════════════════════════
    // GameManager
    // ══════════════════════════════════════════════

    private static GameObject EnsureGameManager()
    {
        // 既存の GameManager があればスキップ
        GameManager existingGM = Object.FindFirstObjectByType<GameManager>();
        if (existingGM != null)
        {
            Debug.Log("[BootSceneAutoBuilder] GameManager は既に存在します。スキップ。");
            return existingGM.gameObject;
        }

        GameObject gmObj = new GameObject("GameManager");
        Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
        gmObj.AddComponent<GameManager>();

        // GameManager.Awake で以下が自動追加される:
        // - InventoryManager
        // - DebugController
        // - SaveDataManager
        // - AudioManager

        return gmObj;
    }

    // ══════════════════════════════════════════════
    // Title Canvas
    // ══════════════════════════════════════════════

    private static void CreateTitleCanvas(Font font)
    {
        // ── Canvas ──
        GameObject canvasObj = new GameObject("Title Canvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Title Canvas");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObj.AddComponent<GraphicRaycaster>();

        // ── 背景パネル ──
        GameObject bgPanel = new GameObject("BackgroundPanel");
        Undo.RegisterCreatedObjectUndo(bgPanel, "Create BG Panel");
        bgPanel.transform.SetParent(canvasObj.transform, false);

        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.02f, 0.1f, 1f);  // 深紫の魔界色

        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // ── TitleMenuUI ルート ──
        GameObject titleUIObj = new GameObject("TitleMenuUI");
        Undo.RegisterCreatedObjectUndo(titleUIObj, "Create TitleMenuUI");
        titleUIObj.transform.SetParent(canvasObj.transform, false);

        RectTransform titleUIRect = titleUIObj.AddComponent<RectTransform>();
        titleUIRect.anchorMin = Vector2.zero;
        titleUIRect.anchorMax = Vector2.one;
        titleUIRect.sizeDelta = Vector2.zero;

        TitleMenuUI titleMenuUI = titleUIObj.AddComponent<TitleMenuUI>();

        // ── タイトルラベル ──
        GameObject titleObj = new GameObject("TitleLabel");
        Undo.RegisterCreatedObjectUndo(titleObj, "Create TitleLabel");
        titleObj.transform.SetParent(titleUIObj.transform, false);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "Devil's Diner";
        titleText.fontSize = 72;
        titleText.color = new Color(1f, 0.3f, 0.2f, 1f);  // 炎色
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = font;
        titleText.fontStyle = FontStyle.Bold;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(800f, 100f);
        titleRect.anchoredPosition = new Vector2(0f, -120f);

        // ── サブタイトルラベル ──
        GameObject subtitleObj = new GameObject("SubtitleLabel");
        Undo.RegisterCreatedObjectUndo(subtitleObj, "Create SubtitleLabel");
        subtitleObj.transform.SetParent(titleUIObj.transform, false);

        Text subtitleText = subtitleObj.AddComponent<Text>();
        subtitleText.text = "～魔界の荒野とガンブレード～";
        subtitleText.fontSize = 28;
        subtitleText.color = new Color(0.8f, 0.6f, 0.3f, 1f);  // 暖色
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.font = font;

        RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.sizeDelta = new Vector2(600f, 50f);
        subtitleRect.anchoredPosition = new Vector2(0f, -230f);

        // ── New Game ボタン ──
        GameObject newGameBtn = CreateMenuButton("NewGameButton", "New Game", font,
            titleUIObj.transform, new Vector2(0f, 0f));

        // ── Continue ボタン ──
        GameObject continueBtn = CreateMenuButton("ContinueButton", "Continue", font,
            titleUIObj.transform, new Vector2(0f, -80f));

        // ── Options ボタン ──
        GameObject optionsBtn = CreateMenuButton("OptionsButton", "Options", font,
            titleUIObj.transform, new Vector2(0f, -160f));

        // ── Options パネル（モック） ──
        GameObject optionsPanel = new GameObject("OptionsPanel");
        Undo.RegisterCreatedObjectUndo(optionsPanel, "Create OptionsPanel");
        optionsPanel.transform.SetParent(titleUIObj.transform, false);

        Image optionsPanelImage = optionsPanel.AddComponent<Image>();
        optionsPanelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        RectTransform optionsPanelRect = optionsPanel.GetComponent<RectTransform>();
        optionsPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        optionsPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        optionsPanelRect.sizeDelta = new Vector2(500f, 300f);
        optionsPanelRect.anchoredPosition = new Vector2(0f, -50f);

        // Options パネル内テキスト
        GameObject optionsTextObj = new GameObject("OptionsText");
        Undo.RegisterCreatedObjectUndo(optionsTextObj, "Create OptionsText");
        optionsTextObj.transform.SetParent(optionsPanel.transform, false);

        Text optionsText = optionsTextObj.AddComponent<Text>();
        optionsText.text = "Options\n\n(Coming Soon)";
        optionsText.fontSize = 28;
        optionsText.color = Color.white;
        optionsText.alignment = TextAnchor.MiddleCenter;
        optionsText.font = font;

        RectTransform optionsTextRect = optionsTextObj.GetComponent<RectTransform>();
        optionsTextRect.anchorMin = Vector2.zero;
        optionsTextRect.anchorMax = Vector2.one;
        optionsTextRect.sizeDelta = Vector2.zero;

        optionsPanel.SetActive(false);

        // ── フィールド結線 ──
        SetPrivateFieldViaReflection(titleMenuUI, "_titleLabel", titleText);
        SetPrivateFieldViaReflection(titleMenuUI, "_subtitleLabel", subtitleText);
        SetPrivateFieldViaReflection(titleMenuUI, "_newGameButton", newGameBtn.GetComponent<Button>());
        SetPrivateFieldViaReflection(titleMenuUI, "_continueButton", continueBtn.GetComponent<Button>());
        SetPrivateFieldViaReflection(titleMenuUI, "_optionsButton", optionsBtn.GetComponent<Button>());
        SetPrivateFieldViaReflection(titleMenuUI, "_optionsPanel", optionsPanel);
    }

    // ══════════════════════════════════════════════
    // カメラ設定
    // ══════════════════════════════════════════════

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.03f, 0.01f, 0.07f, 1f);
        }
    }

    // ══════════════════════════════════════════════
    // ヘルパー — メニューボタン生成
    // ══════════════════════════════════════════════

    private static GameObject CreateMenuButton(
        string name, string label, Font font,
        Transform parent, Vector2 offset)
    {
        GameObject btnObj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(btnObj, $"Create {name}");
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.15f, 0.08f, 0.25f, 0.9f);  // 魔界パープル

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;

        // ハイライトカラー
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.4f, 0.15f, 0.5f, 1f);
        colors.pressedColor = new Color(0.6f, 0.2f, 0.7f, 1f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = colors;

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(320f, 60f);
        btnRect.anchoredPosition = offset;

        // ── ボタンテキスト ──
        GameObject textObj = new GameObject("Text");
        Undo.RegisterCreatedObjectUndo(textObj, $"Create {name}/Text");
        textObj.transform.SetParent(btnObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = font;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return btnObj;
    }

    // ══════════════════════════════════════════════
    // ヘルパー — リフレクション
    // ══════════════════════════════════════════════

    private static void SetPrivateFieldViaReflection(object target, string fieldName, object value)
    {
        if (target == null || value == null) return;

        System.Type type = target.GetType();
        FieldInfo field = null;

        while (type != null && field == null)
        {
            field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            type = type.BaseType;
        }

        if (field == null)
        {
            Debug.LogWarning(
                $"[BootSceneAutoBuilder] {target.GetType().Name} にフィールド '{fieldName}' が見つかりません。");
            return;
        }

        field.SetValue(target, value);

        if (target is Object unityObj)
        {
            EditorUtility.SetDirty(unityObj);
        }
    }
}
