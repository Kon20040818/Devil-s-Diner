// ============================================================
// BaseSceneAutoBuilder.cs
// 拠点シーン（車の運転席）の自動セットアップエディタ拡張。
// メニュー「DevilsDiner > Auto Setup Base Scene」で一括生成。
// ============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// メニュー <c>DevilsDiner > Auto Setup Base Scene</c> から
/// 拠点シーン（車の運転席）を一括生成する。
/// </summary>
public static class BaseSceneAutoBuilder
{
    private const string MENU_PATH = "DevilsDiner/Auto Setup Base Scene";
    private const string SCENE_PATH = "Assets/Scenes/BaseScene.unity";

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
            EditorUtility.DisplayProgressBar("Base Scene Setup", "シーン作成中…", (float)step / totalSteps);
            var scene = CreateOrOpenScene();

            step++;
            EditorUtility.DisplayProgressBar("Base Scene Setup", "環境構築中…", (float)step / totalSteps);
            BuildEnvironment();

            step++;
            EditorUtility.DisplayProgressBar("Base Scene Setup", "運転席構築中…", (float)step / totalSteps);
            BuildDriverSeat();

            step++;
            EditorUtility.DisplayProgressBar("Base Scene Setup", "UI構築中…", (float)step / totalSteps);
            BuildUI();

            step++;
            EditorUtility.DisplayProgressBar("Base Scene Setup", "保存中…", (float)step / totalSteps);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AddSceneToBuildSettings(SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("[DevilsDiner] Base Scene セットアップ完了！");
        EditorUtility.DisplayDialog(
            "Base Scene Setup",
            "拠点シーンのセットアップが完了しました。\n" +
            "Play ボタンで即テスト可能です。",
            "OK");
    }

    // ================================================================
    // シーン作成
    // ================================================================

    private static Scene CreateOrOpenScene()
    {
        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    // ================================================================
    // 環境構築
    // ================================================================

    private static void BuildEnvironment()
    {
        // ── BaseSystem（Bootstrap）──
        var baseSystemGO = new GameObject("BaseSystem");
        baseSystemGO.AddComponent<BaseSceneBootstrap>();

        // ── Directional Light ──
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.92f, 0.78f);
        light.intensity = 0.8f;
        lightGO.transform.rotation = Quaternion.Euler(30f, 150f, 0f);

        // ── Main Camera（運転席視点：固定） ──
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        var camera = cameraGO.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 60f;
        // 運転席からダッシュボード〜フロントガラスを見る位置
        cameraGO.transform.position = new Vector3(0f, 1.2f, -0.3f);
        cameraGO.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
        cameraGO.AddComponent<AudioListener>();

        Debug.Log("[BaseSceneAutoBuilder] 環境構築完了。");
    }

    // ================================================================
    // 運転席 3D レイアウト
    // ================================================================

    private static void BuildDriverSeat()
    {
        var carParent = new GameObject("--- Car Interior ---");

        // ── ダッシュボード ──
        var dashGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dashGO.name = "Dashboard";
        dashGO.transform.SetParent(carParent.transform);
        dashGO.transform.position = new Vector3(0f, 0.6f, 0.8f);
        dashGO.transform.localScale = new Vector3(2.0f, 0.15f, 0.8f);
        SetMaterial(dashGO, new Color(0.12f, 0.1f, 0.08f));

        // ── ハンドル ──
        var steeringGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        steeringGO.name = "SteeringWheel";
        steeringGO.transform.SetParent(carParent.transform);
        steeringGO.transform.position = new Vector3(0f, 0.85f, 0.6f);
        steeringGO.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);
        steeringGO.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
        SetMaterial(steeringGO, new Color(0.08f, 0.08f, 0.08f));

        // ── ハンドルの軸 ──
        var steeringPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        steeringPole.name = "SteeringPole";
        steeringPole.transform.SetParent(carParent.transform);
        steeringPole.transform.position = new Vector3(0f, 0.65f, 0.65f);
        steeringPole.transform.localScale = new Vector3(0.04f, 0.2f, 0.04f);
        steeringPole.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
        SetMaterial(steeringPole, new Color(0.1f, 0.1f, 0.1f));

        // ── フロントガラス（透明パネル） ──
        var windshieldGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        windshieldGO.name = "Windshield";
        windshieldGO.transform.SetParent(carParent.transform);
        windshieldGO.transform.position = new Vector3(0f, 1.5f, 1.5f);
        windshieldGO.transform.localScale = new Vector3(2.2f, 1.2f, 1f);
        windshieldGO.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        SetMaterial(windshieldGO, new Color(0.6f, 0.7f, 0.8f, 0.15f), transparent: true);

        // ── 座席 ──
        var seatGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seatGO.name = "Seat";
        seatGO.transform.SetParent(carParent.transform);
        seatGO.transform.position = new Vector3(0f, 0.4f, -0.5f);
        seatGO.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
        SetMaterial(seatGO, new Color(0.15f, 0.08f, 0.05f));

        // ── 座席背もたれ ──
        var seatBackGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seatBackGO.name = "SeatBack";
        seatBackGO.transform.SetParent(carParent.transform);
        seatBackGO.transform.position = new Vector3(0f, 0.85f, -0.78f);
        seatBackGO.transform.localScale = new Vector3(0.6f, 0.8f, 0.08f);
        seatBackGO.transform.rotation = Quaternion.Euler(-10f, 0f, 0f);
        SetMaterial(seatBackGO, new Color(0.15f, 0.08f, 0.05f));

        // ── 床 ──
        var floorGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorGO.name = "CarFloor";
        floorGO.transform.SetParent(carParent.transform);
        floorGO.transform.position = new Vector3(0f, 0f, 0f);
        floorGO.transform.localScale = new Vector3(2.0f, 0.05f, 3.0f);
        SetMaterial(floorGO, new Color(0.06f, 0.06f, 0.06f));

        // ── ルーフ ──
        var roofGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofGO.name = "CarRoof";
        roofGO.transform.SetParent(carParent.transform);
        roofGO.transform.position = new Vector3(0f, 2.2f, 0f);
        roofGO.transform.localScale = new Vector3(2.0f, 0.05f, 3.0f);
        SetMaterial(roofGO, new Color(0.1f, 0.1f, 0.1f));

        // ── 左ドア ──
        var leftDoorGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftDoorGO.name = "LeftDoor";
        leftDoorGO.transform.SetParent(carParent.transform);
        leftDoorGO.transform.position = new Vector3(-1f, 1.1f, 0f);
        leftDoorGO.transform.localScale = new Vector3(0.05f, 2.0f, 2.5f);
        SetMaterial(leftDoorGO, new Color(0.12f, 0.12f, 0.12f));

        // ── 右ドア ──
        var rightDoorGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightDoorGO.name = "RightDoor";
        rightDoorGO.transform.SetParent(carParent.transform);
        rightDoorGO.transform.position = new Vector3(1f, 1.1f, 0f);
        rightDoorGO.transform.localScale = new Vector3(0.05f, 2.0f, 2.5f);
        SetMaterial(rightDoorGO, new Color(0.12f, 0.12f, 0.12f));

        Debug.Log("[BaseSceneAutoBuilder] 運転席構築完了。");
    }

    // ================================================================
    // UI 構築
    // ================================================================

    private static void BuildUI()
    {
        var uiGO = new GameObject("BaseSceneUI");

        // UIDocument
        var uiDocument = uiGO.AddComponent<UIDocument>();

        // PanelSettings を探す（既存アセットがあればそれを使う）
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/BattlePanelSettings.asset");
        if (panelSettings != null)
        {
            uiDocument.panelSettings = panelSettings;
        }
        else
        {
            Debug.LogWarning("[BaseSceneAutoBuilder] PanelSettings が見つかりません。BaseSceneUI は実行時に自動設定を試みます。");
        }

        // BaseSceneUI コンポーネント
        var baseUI = uiGO.AddComponent<BaseSceneUI>();

        // SerializedObject で UIDocument 参照を結線
        var so = new SerializedObject(baseUI);
        so.FindProperty("_uiDocument").objectReferenceValue = uiDocument;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[BaseSceneAutoBuilder] UI構築完了。");
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

    private static void SetMaterial(GameObject go, Color color, bool transparent = false)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.color = color;

        if (transparent)
        {
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_AlphaClip", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        renderer.sharedMaterial = mat;
    }
}
#endif
