// ============================================================
// DevilsDinerSetupTool.cs
// バトルシーンの自動セットアップエディタ拡張。
// ScriptableObject の生成、シーン内の全 GameObject 配置、
// コンポーネントのアタッチ、参照の結線を一括で行う。
// 崩壊スターレイル風UI対応版。
// ============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

/// <summary>
/// メニュー <c>DevilsDiner > Auto Setup Battle Scene</c> から
/// バトルシーンを一括生成する。既存アセットはスキップ（冪等）。
/// </summary>
public static class DevilsDinerSetupTool
{
    private const string MENU_PATH = "DevilsDiner/Auto Setup Battle Scene";
    private const string SCENE_PATH = "Assets/Scenes/BattleScene.unity";

    // ──────────────────────────────────────────────
    // フォルダ定義
    // ──────────────────────────────────────────────

    private static readonly string[] _folders = new string[]
    {
        "Assets/Data/Materials",
        "Assets/Data/Weapons",
        "Assets/Data/Enemies",
        "Assets/Data/Characters",
        "Assets/Data/Config",
        "Assets/UI",
    };

    // ──────────────────────────────────────────────
    // カラー定数 — スターレイル風トグルボタン
    // ──────────────────────────────────────────────

    private static readonly Color TOGGLE_BG_COLOR       = new Color(0.04f, 0.04f, 0.12f, 0.8f);
    private static readonly Color TOGGLE_TEXT_COLOR      = Color.white;

    // ──────────────────────────────────────────────
    // メニューエントリ
    // ──────────────────────────────────────────────

    [MenuItem(MENU_PATH)]
    private static void Execute()
    {
        int totalSteps = 5;
        int step = 0;

        try
        {
            step++;
            EditorUtility.DisplayProgressBar("Battle Scene Setup", "フォルダ作成中…", (float)step / totalSteps);
            CreateFolders();

            step++;
            EditorUtility.DisplayProgressBar("Battle Scene Setup", "ScriptableObject 生成中…", (float)step / totalSteps);
            CreateScriptableObjects();

            step++;
            EditorUtility.DisplayProgressBar("Battle Scene Setup", "バトルシーン作成中…", (float)step / totalSteps);
            var scene = CreateOrOpenScene();

            step++;
            EditorUtility.DisplayProgressBar("Battle Scene Setup", "GameObjects 配置中…", (float)step / totalSteps);
            BuildSceneHierarchy();

            step++;
            EditorUtility.DisplayProgressBar("Battle Scene Setup", "シーン保存中…", (float)step / totalSteps);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AddSceneToBuildSettings(SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("[DevilsDiner] Battle Scene セットアップ完了！");
        EditorUtility.DisplayDialog(
            "Battle Scene Setup",
            "バトルシーンのセットアップが完了しました。\n" +
            "Play ボタンで即テスト可能です。",
            "OK");
    }

    // ================================================================
    // フォルダ作成
    // ================================================================

    private static void CreateFolders()
    {
        foreach (string folder in _folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                CreateFolderRecursive(folder);
        }
    }

    // ================================================================
    // ScriptableObject 生成
    // ================================================================

    private static void CreateScriptableObjects()
    {
        CreateSOIfNotExists<MaterialData>("Assets/Data/Materials", "MAT_Dummy");
        CreateSOIfNotExists<WeaponData>("Assets/Data/Weapons", "WPN_Dummy");
        CreateSOIfNotExists<EnemyData>("Assets/Data/Enemies", "ENM_Dummy");

        var heroStats = CreateSOIfNotExists<CharacterStats>("Assets/Data/Characters", "STAT_Hero");
        if (heroStats != null)
        {
            SetCharacterStats(heroStats, "hero_01", "ヒーロー",
                maxHP: 500, attack: 45, defense: 20, speed: 110,
                element: CharacterStats.ElementType.Fire);
        }

        var slimeStats = CreateSOIfNotExists<CharacterStats>("Assets/Data/Characters", "STAT_Slime");
        if (slimeStats != null)
        {
            SetCharacterStats(slimeStats, "enemy_slime", "スライム",
                maxHP: 200, attack: 25, defense: 8, speed: 80,
                element: CharacterStats.ElementType.Ice,
                maxToughness: 90,
                weakElements: new[] {
                    CharacterStats.ElementType.Fire,
                    CharacterStats.ElementType.Lightning
                });
        }
    }

    private static void SetCharacterStats(CharacterStats stats, string id, string displayName,
        int maxHP, int attack, int defense, int speed, CharacterStats.ElementType element,
        int maxToughness = 0, CharacterStats.ElementType[] weakElements = null)
    {
        var so = new SerializedObject(stats);
        so.FindProperty("_id").stringValue = id;
        so.FindProperty("_displayName").stringValue = displayName;
        so.FindProperty("_maxHP").intValue = maxHP;
        so.FindProperty("_attack").intValue = attack;
        so.FindProperty("_defense").intValue = defense;
        so.FindProperty("_speed").intValue = speed;
        so.FindProperty("_element").enumValueIndex = (int)element;
        so.FindProperty("_maxToughness").intValue = maxToughness;

        // 弱点属性
        var weakProp = so.FindProperty("_weakElements");
        if (weakElements != null)
        {
            weakProp.arraySize = weakElements.Length;
            for (int i = 0; i < weakElements.Length; i++)
            {
                weakProp.GetArrayElementAtIndex(i).enumValueIndex = (int)weakElements[i];
            }
        }
        else
        {
            weakProp.arraySize = 0;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(stats);
    }

    // ================================================================
    // シーン作成
    // ================================================================

    private static Scene CreateOrOpenScene()
    {
        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    // ================================================================
    // シーン階層構築
    // ================================================================

    private static void BuildSceneHierarchy()
    {
        var heroStats = AssetDatabase.LoadAssetAtPath<CharacterStats>("Assets/Data/Characters/STAT_Hero.asset");
        var slimeStats = AssetDatabase.LoadAssetAtPath<CharacterStats>("Assets/Data/Characters/STAT_Slime.asset");

        // ════════════════════════════════════════════════
        // 1. BattleSystem
        // ════════════════════════════════════════════════
        var battleSystemGO = new GameObject("BattleSystem");
        battleSystemGO.AddComponent<BattleManager>();
        battleSystemGO.AddComponent<BattleSceneBootstrap>();

        var camMgr = battleSystemGO.AddComponent<BattleCameraManager>();
        var camMgrSO = new SerializedObject(camMgr);
        camMgrSO.FindProperty("_overviewOffset").vector3Value = new Vector3(2.0f, 5.0f, -8.0f);
        camMgrSO.FindProperty("_overviewLookOffset").vector3Value = new Vector3(0f, 1.0f, 2.0f);
        camMgrSO.FindProperty("_followSpeed").floatValue = 5f;
        camMgrSO.ApplyModifiedPropertiesWithoutUndo();

        // ════════════════════════════════════════════════
        // 2. Overview Position Marker
        // ════════════════════════════════════════════════
        var overviewMarker = new GameObject("OverviewPosition");
        overviewMarker.transform.position = new Vector3(0f, 0f, 0f);
        camMgrSO = new SerializedObject(camMgr);
        camMgrSO.FindProperty("_overviewPosition").objectReferenceValue = overviewMarker.transform;
        camMgrSO.ApplyModifiedPropertiesWithoutUndo();

        // ════════════════════════════════════════════════
        // 3. Main Camera
        // ════════════════════════════════════════════════
        var cameraGO = new GameObject("Main Camera");
        var camera = cameraGO.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 42f;
        cameraGO.transform.position = new Vector3(2.0f, 5.0f, -8.0f);
        cameraGO.transform.LookAt(new Vector3(0f, 2.0f, 2.0f));
        cameraGO.AddComponent<AudioListener>();

        // ════════════════════════════════════════════════
        // 4. Directional Light
        // ════════════════════════════════════════════════
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.88f);
        light.intensity = 1.2f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ════════════════════════════════════════════════
        // 5. Ground Plane
        // ════════════════════════════════════════════════
        var groundGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundGO.name = "BattleField_Ground";
        groundGO.transform.position = Vector3.zero;
        groundGO.transform.localScale = new Vector3(3f, 1f, 3f);

        // ════════════════════════════════════════════════
        // 6. Player Characters
        // ════════════════════════════════════════════════
        var playerParent = new GameObject("--- Players ---");

        var heroGO = CreateBattleCharacter(
            "Player_Hero", heroStats,
            CharacterBattleController.Faction.Player,
            new Vector3(-4f, 1f, 0f),
            Quaternion.LookRotation(Vector3.right));
        heroGO.transform.SetParent(playerParent.transform);

        // ════════════════════════════════════════════════
        // 7. Enemy Characters
        // ════════════════════════════════════════════════
        var enemyParent = new GameObject("--- Enemies ---");

        var slimeGO = CreateBattleCharacter(
            "Enemy_Slime", slimeStats,
            CharacterBattleController.Faction.Enemy,
            new Vector3(4f, 1f, 0f),
            Quaternion.LookRotation(Vector3.left));
        slimeGO.transform.SetParent(enemyParent.transform);

        // ════════════════════════════════════════════════
        // 8. BattleCanvas (uGUI) — スターレイル風レイアウト
        // ════════════════════════════════════════════════
        CreateBattleCanvas();

        // ════════════════════════════════════════════════
        // 9. MetaphorBattleUI (UI Toolkit) — メタファー風UI
        // ════════════════════════════════════════════════
        CreateMetaphorBattleUI();

        Debug.Log("[DevilsDiner] シーン階層構築完了。");
    }

    // ──────────────────────────────────────────────
    // バトルキャラクター生成
    // ──────────────────────────────────────────────

    private static GameObject CreateBattleCharacter(
        string name, CharacterStats stats,
        CharacterBattleController.Faction faction,
        Vector3 position, Quaternion rotation)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.position = position;
        go.transform.rotation = rotation;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = faction == CharacterBattleController.Faction.Player
                ? new Color(0.2f, 0.5f, 1f)
                : new Color(1f, 0.3f, 0.3f);
            renderer.sharedMaterial = mat;
        }

        var controller = go.AddComponent<CharacterBattleController>();
        var so = new SerializedObject(controller);
        if (stats != null)
            so.FindProperty("_stats").objectReferenceValue = stats;
        so.FindProperty("_faction").enumValueIndex = (int)faction;
        so.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    // ──────────────────────────────────────────────
    // BattleCanvas (uGUI) — スターレイル風全面改修
    // ──────────────────────────────────────────────

    private static void CreateBattleCanvas()
    {
        // ── Canvas ──
        var canvasGO = new GameObject("BattleCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── EventSystem ──
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ── BattleUIManager ──
        var uiManager = canvasGO.AddComponent<BattleUIManager>();

        // ════════════════════════════════════════════════
        // A. ActionTimelineUI (画面上部・横型バー — Star Rail風)
        //    自前で Canvas を生成するため、
        //    ここでは空の GameObject にコンポーネントを付けるだけ。
        // ════════════════════════════════════════════════
        var timelineGO = new GameObject("ActionTimeline");
        timelineGO.transform.SetParent(canvasGO.transform, false);

        // RectTransform はフル展開（ActionTimelineUI が内部でルートパネルを構築する）
        var timelineRT = timelineGO.AddComponent<RectTransform>();
        StretchFull(timelineRT);

        var timelineUI = timelineGO.AddComponent<ActionTimelineUI>();

        // ════════════════════════════════════════════════
        // B. SkillCommandUI (画面下部中央 — 通常攻撃/スキルボタン + SP)
        //    自前で内部UIを構築するため、ストレッチ展開のみ。
        // ════════════════════════════════════════════════
        var skillCmdGO = new GameObject("SkillCommand");
        skillCmdGO.transform.SetParent(canvasGO.transform, false);

        var skillCmdRT = skillCmdGO.AddComponent<RectTransform>();
        StretchFull(skillCmdRT);

        skillCmdGO.AddComponent<SkillCommandUI>();

        // ════════════════════════════════════════════════
        // C. UltimatePortraitUI (画面左下 — EPリングポートレート)
        //    自前で内部UIを構築するため、ストレッチ展開のみ。
        // ════════════════════════════════════════════════
        var ultimateGO = new GameObject("UltimatePortraits");
        ultimateGO.transform.SetParent(canvasGO.transform, false);

        var ultimateRT = ultimateGO.AddComponent<RectTransform>();
        StretchFull(ultimateRT);

        ultimateGO.AddComponent<UltimatePortraitUI>();

        // ════════════════════════════════════════════════
        // D. CharacterStatusUI (パーティHPカード)
        //    Star Rail準拠: 画面左下に横一列で配置。
        //    スキルボタンは右下、HPカードは左下。
        // ════════════════════════════════════════════════
        var statusGO = new GameObject("CharacterStatus");
        statusGO.transform.SetParent(canvasGO.transform, false);

        var statusRT = statusGO.AddComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0f, 0f);
        statusRT.anchorMax = new Vector2(0f, 0f);
        statusRT.pivot = new Vector2(0f, 0f);
        statusRT.anchoredPosition = new Vector2(36f, 80f);
        statusRT.sizeDelta = new Vector2(780f, 90f);

        var statusHLG = statusGO.AddComponent<HorizontalLayoutGroup>();
        statusHLG.spacing = 12f;
        statusHLG.childAlignment = TextAnchor.MiddleLeft;
        statusHLG.childForceExpandWidth = false;
        statusHLG.childForceExpandHeight = false;
        statusHLG.childControlWidth = false;
        statusHLG.childControlHeight = false;
        statusHLG.padding = new RectOffset(4, 4, 2, 2);

        var characterStatusUI = statusGO.AddComponent<CharacterStatusUI>();
        var statusSO = new SerializedObject(characterStatusUI);
        statusSO.FindProperty("_statusContainer").objectReferenceValue = statusGO.transform;
        statusSO.ApplyModifiedPropertiesWithoutUndo();

        // ════════════════════════════════════════════════
        // E. DamageNumberUI (ダメージ数字表示 — フルスクリーン、独自Canvas)
        // ════════════════════════════════════════════════
        var damageNumGO = new GameObject("DamageNumbers");
        damageNumGO.transform.SetParent(canvasGO.transform, false);

        var damageNumRT = damageNumGO.AddComponent<RectTransform>();
        StretchFull(damageNumRT);

        var damageNumUI = damageNumGO.AddComponent<DamageNumberUI>();

        // ════════════════════════════════════════════════
        // F. BattleEffectsUI (演出エフェクト — フルスクリーン、独自Canvas)
        // ════════════════════════════════════════════════
        var effectsGO = new GameObject("BattleEffects");
        effectsGO.transform.SetParent(canvasGO.transform, false);

        var effectsRT = effectsGO.AddComponent<RectTransform>();
        StretchFull(effectsRT);

        effectsGO.AddComponent<BattleEffectsUI>();

        // ════════════════════════════════════════════════
        // G. Auto/Speed トグルボタン (画面右上)
        //    Anchors: (0.82, 0.92) to (0.98, 0.98)
        // ════════════════════════════════════════════════
        var toggleContainer = new GameObject("ToggleButtons");
        toggleContainer.transform.SetParent(canvasGO.transform, false);

        var toggleRT = toggleContainer.AddComponent<RectTransform>();
        SetAnchors(toggleRT, new Vector2(0.82f, 0.92f), new Vector2(0.98f, 0.98f));
        toggleRT.offsetMin = new Vector2(0, 0);
        toggleRT.offsetMax = new Vector2(-8, -4);

        var toggleHLG = toggleContainer.AddComponent<HorizontalLayoutGroup>();
        toggleHLG.spacing = 8f;
        toggleHLG.childAlignment = TextAnchor.MiddleRight;
        toggleHLG.childForceExpandWidth = true;
        toggleHLG.childForceExpandHeight = true;

        var autoBtn = CreateToggleButton(toggleContainer.transform, "AutoButton", "Auto");
        var speedBtn = CreateToggleButton(toggleContainer.transform, "SpeedButton", "1x");

        // ── BattleUIManager 参照結線 ──
        var uiManagerSO = new SerializedObject(uiManager);
        uiManagerSO.FindProperty("_timelineUI").objectReferenceValue = timelineUI;
        uiManagerSO.FindProperty("_characterStatusUI").objectReferenceValue = characterStatusUI;
        uiManagerSO.FindProperty("_skillCommandUI").objectReferenceValue = skillCmdGO.GetComponent<SkillCommandUI>();
        uiManagerSO.FindProperty("_ultimatePortraitUI").objectReferenceValue = ultimateGO.GetComponent<UltimatePortraitUI>();
        uiManagerSO.FindProperty("_damageNumberUI").objectReferenceValue = damageNumUI;
        uiManagerSO.FindProperty("_battleEffectsUI").objectReferenceValue = effectsGO.GetComponent<BattleEffectsUI>();
        uiManagerSO.FindProperty("_autoToggleButton").objectReferenceValue = autoBtn.GetComponent<UnityEngine.UI.Button>();
        uiManagerSO.FindProperty("_speedToggleButton").objectReferenceValue = speedBtn.GetComponent<UnityEngine.UI.Button>();
        uiManagerSO.FindProperty("_autoToggleText").objectReferenceValue = autoBtn.GetComponentInChildren<Text>();
        uiManagerSO.FindProperty("_speedToggleText").objectReferenceValue = speedBtn.GetComponentInChildren<Text>();
        uiManagerSO.ApplyModifiedPropertiesWithoutUndo();
    }

    // ──────────────────────────────────────────────
    // トグルボタン生成 — スターレイル風スタイリング
    // ──────────────────────────────────────────────

    private static GameObject CreateToggleButton(Transform parent, string name, string label)
    {
        var btnGO = new GameObject(name, typeof(RectTransform));
        btnGO.transform.SetParent(parent, false);

        // ダーク丸角風背景
        var btnImage = btnGO.AddComponent<UnityEngine.UI.Image>();
        btnImage.color = TOGGLE_BG_COLOR;

        var btn = btnGO.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = btnImage;

        // ボタンの色遷移設定
        var colors = btn.colors;
        colors.normalColor = TOGGLE_BG_COLOR;
        colors.highlightedColor = new Color(0.08f, 0.08f, 0.20f, 0.9f);
        colors.pressedColor = new Color(0.12f, 0.12f, 0.25f, 0.95f);
        colors.selectedColor = TOGGLE_BG_COLOR;
        btn.colors = colors;

        // ボーダー風 Outline
        var btnOutline = btnGO.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.12f, 0.18f, 0.30f, 0.5f);
        btnOutline.effectDistance = new Vector2(1f, -1f);

        var btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.preferredWidth = 80;
        btnLE.preferredHeight = 36;

        // テキスト
        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(btnGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.color = TOGGLE_TEXT_COLOR;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;

        // テキストシャドウ
        var textOutline = textGO.AddComponent<Outline>();
        textOutline.effectColor = new Color(0f, 0f, 0f, 0.5f);
        textOutline.effectDistance = new Vector2(1f, -1f);

        return btnGO;
    }

    // ──────────────────────────────────────────────
    // MetaphorBattleUI (UI Toolkit) 生成
    // ──────────────────────────────────────────────

    private static void CreateMetaphorBattleUI()
    {
        var go = new GameObject("MetaphorBattleUI");

        var uiDocument = go.AddComponent<UIDocument>();

        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BattleUI.uxml");
        if (uxml != null)
            uiDocument.visualTreeAsset = uxml;
        else
            Debug.LogWarning("[DevilsDiner] Assets/UI/BattleUI.uxml が見つかりません。");

        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/BattlePanelSettings.asset");
        if (panelSettings != null)
            uiDocument.panelSettings = panelSettings;
        else
            Debug.LogWarning("[DevilsDiner] Assets/UI/BattlePanelSettings.asset が見つかりません。");

        go.AddComponent<DynamicBattleUIController>();

        // 旧uGUI UIを非アクティブ化
        foreach (var legacy in Object.FindObjectsByType<SkillCommandUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            legacy.gameObject.SetActive(false);
        foreach (var legacy in Object.FindObjectsByType<ActionTimelineUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            legacy.gameObject.SetActive(false);
        foreach (var legacy in Object.FindObjectsByType<CharacterStatusUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            legacy.gameObject.SetActive(false);
        foreach (var legacy in Object.FindObjectsByType<UltimatePortraitUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            legacy.gameObject.SetActive(false);
        var toggleButtons = GameObject.Find("ToggleButtons");
        if (toggleButtons != null) toggleButtons.SetActive(false);

        Debug.Log("[DevilsDiner] MetaphorBattleUI (UI Toolkit) を構築しました。");
    }

    // ================================================================
    // Build Settings 登録
    // ================================================================

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ================================================================
    // ヘルパー
    // ================================================================

    private static T CreateSOIfNotExists<T>(string folder, string fileName) where T : ScriptableObject
    {
        string path = $"{folder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return null;

        if (!AssetDatabase.IsValidFolder(folder))
            CreateFolderRecursive(folder);

        T instance = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(instance, path);
        return instance;
    }

    private static void CreateFolderRecursive(string folderPath)
    {
        int lastSlash = folderPath.LastIndexOf('/');
        if (lastSlash < 0) return;

        string parent = folderPath.Substring(0, lastSlash);
        string leaf = folderPath.Substring(lastSlash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
            CreateFolderRecursive(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
