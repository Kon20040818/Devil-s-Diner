// ============================================================
// ManagementSceneAutoBuilder.cs
// 経営シーンの自動セットアップエディタ拡張。
// メニュー「DevilsDiner > Auto Setup Management Scene」で一括生成。
// ============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// メニュー <c>DevilsDiner > Auto Setup Management Scene</c> から
/// 経営シーンを一括生成する。
/// </summary>
public static class ManagementSceneAutoBuilder
{
    private const string MENU_PATH = "DevilsDiner/Auto Setup Management Scene";
    private const string SCENE_PATH = "Assets/Scenes/ManagementScene.unity";

    // ──────────────────────────────────────────────
    // メニューエントリ
    // ──────────────────────────────────────────────

    [MenuItem(MENU_PATH)]
    private static void Execute()
    {
        int totalSteps = 4;
        int step = 0;

        try
        {
            step++;
            EditorUtility.DisplayProgressBar("Management Scene Setup", "シーン作成中…", (float)step / totalSteps);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            step++;
            EditorUtility.DisplayProgressBar("Management Scene Setup", "環境構築中…", (float)step / totalSteps);
            BuildEnvironment();

            step++;
            EditorUtility.DisplayProgressBar("Management Scene Setup", "UI構築中…", (float)step / totalSteps);
            BuildUI();

            step++;
            EditorUtility.DisplayProgressBar("Management Scene Setup", "保存中…", (float)step / totalSteps);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AddSceneToBuildSettings(SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("[DevilsDiner] Management Scene セットアップ完了！");
        EditorUtility.DisplayDialog(
            "Management Scene Setup",
            "経営シーンのセットアップが完了しました。\n" +
            "Play ボタンで即テスト可能です。",
            "OK");
    }

    // ================================================================
    // 環境構築
    // ================================================================

    private static void BuildEnvironment()
    {
        // ── ManagementSystem（Bootstrap）──
        var systemGO = new GameObject("ManagementSystem");
        systemGO.AddComponent<ManagementSceneBootstrap>();

        // ── Main Camera ──
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        var camera = cameraGO.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 60f;
        cameraGO.transform.position = new Vector3(0f, 3f, -6f);
        cameraGO.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        cameraGO.AddComponent<AudioListener>();

        // ── Directional Light（暖色・夕方っぽい雰囲気） ──
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.8f, 0.55f);
        light.intensity = 1.0f;
        lightGO.transform.rotation = Quaternion.Euler(25f, 120f, 0f);

        // ── 店舗フロア（プレースホルダー） ──
        var floorGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floorGO.name = "DinerFloor";
        floorGO.transform.position = Vector3.zero;
        floorGO.transform.localScale = new Vector3(3f, 1f, 3f);
        SetMaterial(floorGO, new Color(0.4f, 0.25f, 0.15f));

        // ── カウンター（プレースホルダー） ──
        var counterGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        counterGO.name = "Counter";
        counterGO.transform.position = new Vector3(0f, 0.5f, 2f);
        counterGO.transform.localScale = new Vector3(4f, 1f, 0.6f);
        SetMaterial(counterGO, new Color(0.3f, 0.18f, 0.1f));

        // ── テーブル x2（プレースホルダー） ──
        for (int i = 0; i < 2; i++)
        {
            var tableGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableGO.name = $"Table_{i}";
            float x = (i == 0) ? -2f : 2f;
            tableGO.transform.position = new Vector3(x, 0.4f, -1f);
            tableGO.transform.localScale = new Vector3(1.2f, 0.8f, 0.8f);
            SetMaterial(tableGO, new Color(0.35f, 0.2f, 0.12f));
        }

        Debug.Log("[ManagementSceneAutoBuilder] 環境構築完了。");
    }

    // ================================================================
    // UI 構築
    // ================================================================

    private static void BuildUI()
    {
        var uiGO = new GameObject("ManagementSceneUI");

        var uiDocument = uiGO.AddComponent<UIDocument>();

        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/BattlePanelSettings.asset");
        if (panelSettings != null)
        {
            uiDocument.panelSettings = panelSettings;
        }
        else
        {
            Debug.LogWarning("[ManagementSceneAutoBuilder] PanelSettings が見つかりません。");
        }

        var managementUI = uiGO.AddComponent<ManagementSceneUI>();

        var so = new SerializedObject(managementUI);
        so.FindProperty("_uiDocument").objectReferenceValue = uiDocument;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[ManagementSceneAutoBuilder] UI構築完了。");
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

    private static void SetMaterial(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.color = color;
        renderer.sharedMaterial = mat;
    }
}
#endif
