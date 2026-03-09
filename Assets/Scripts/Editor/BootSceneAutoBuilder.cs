// ============================================================
// BootSceneAutoBuilder.cs
// BootScene（起動シーン）の自動セットアップエディタ拡張。
// GameManager のみを配置し、BaseScene へ即ロードする。
// メニュー「DevilsDiner > Auto Setup Boot Scene」で一括生成。
// ============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// メニュー <c>DevilsDiner > Auto Setup Boot Scene</c> から
/// BootScene を一括生成する。
/// BootScene はビルド設定のインデックス 0 に登録され、
/// GameManager を DontDestroyOnLoad で永続化したあと BaseScene へ即遷移する。
/// </summary>
public static class BootSceneAutoBuilder
{
    private const string MENU_PATH  = "DevilsDiner/Auto Setup Boot Scene";
    private const string SCENE_PATH = "Assets/Scenes/BootScene.unity";

    // ──────────────────────────────────────────────
    // メニューエントリ
    // ──────────────────────────────────────────────

    [MenuItem(MENU_PATH)]
    private static void Execute()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Boot Scene Setup", "シーン作成中…", 0.2f);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EditorUtility.DisplayProgressBar("Boot Scene Setup", "GameManager 配置中…", 0.5f);
            BuildSceneHierarchy();

            EditorUtility.DisplayProgressBar("Boot Scene Setup", "保存中…", 0.8f);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            InsertSceneAtIndexZero(SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("[DevilsDiner] Boot Scene セットアップ完了！");
        EditorUtility.DisplayDialog(
            "Boot Scene Setup",
            "BootScene のセットアップが完了しました。\n" +
            "Build Settings でインデックス 0 に登録されています。\n" +
            "Play すると GameManager 生成後に BaseScene へ自動遷移します。",
            "OK");
    }

    // ================================================================
    // シーン階層構築
    // ================================================================

    private static void BuildSceneHierarchy()
    {
        // ── GameManager + BootLoader ──
        // GameManager.Awake() で全サブマネージャーを自動追加 + DontDestroyOnLoad
        // BootLoader.Start() で BaseScene へ遷移
        var gameManagerGO = new GameObject("GameManager");
        gameManagerGO.AddComponent<GameManager>();
        gameManagerGO.AddComponent<BootLoader>();

        // ── Main Camera（最小構成） ──
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        cameraGO.AddComponent<Camera>();
        cameraGO.AddComponent<AudioListener>();

        // ── Directional Light ──
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;

        Debug.Log("[BootSceneAutoBuilder] GameManager + BootLoader 配置完了。");
    }

    // ================================================================
    // Build Settings — インデックス 0 に挿入
    // ================================================================

    private static void InsertSceneAtIndexZero(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // 既に登録済みなら一度削除して先頭に再挿入
        scenes.RemoveAll(s => s.path == scenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[BootSceneAutoBuilder] BootScene をビルド設定のインデックス 0 に登録しました。");
    }
}
#endif
