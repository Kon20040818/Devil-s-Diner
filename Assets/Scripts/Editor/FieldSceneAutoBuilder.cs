// ============================================================
// FieldSceneAutoBuilder.cs
// フィールドシーンの自動セットアップエディタ拡張。
// メニュー「DevilsDiner > Auto Setup Field Scene」で一括生成。
// ============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using UnityEngine.AI;

/// <summary>
/// メニュー <c>DevilsDiner > Auto Setup Field Scene</c> から
/// フィールドシーンを一括生成する。
/// </summary>
public static class FieldSceneAutoBuilder
{
    private const string MENU_PATH = "DevilsDiner/Auto Setup Field Scene";
    private const string SCENE_PATH = "Assets/Scenes/FieldScene.unity";

    // ──────────────────────────────────────────────
    // 敵シンボル配置
    // ──────────────────────────────────────────────

    private static readonly Vector3[] ENEMY_POSITIONS = new Vector3[]
    {
        new Vector3(10f, 0f, 10f),
        new Vector3(-8f, 0f, 15f),
        new Vector3(5f, 0f, -10f),
    };

    // ──────────────────────────────────────────────
    // メニューエントリ
    // ──────────────────────────────────────────────

    [MenuItem(MENU_PATH)]
    private static void Execute()
    {
        int totalSteps = 6;
        int step = 0;

        try
        {
            step++;
            EditorUtility.DisplayProgressBar("Field Scene Setup", "シーン作成中…", (float)step / totalSteps);
            var scene = CreateOrOpenScene();

            step++;
            EditorUtility.DisplayProgressBar("Field Scene Setup", "環境構築中…", (float)step / totalSteps);
            BuildEnvironment();

            step++;
            EditorUtility.DisplayProgressBar("Field Scene Setup", "プレイヤー配置中…", (float)step / totalSteps);
            var playerGO = BuildPlayer();

            step++;
            EditorUtility.DisplayProgressBar("Field Scene Setup", "カメラ構築中…", (float)step / totalSteps);
            BuildCamera(playerGO.transform);

            step++;
            EditorUtility.DisplayProgressBar("Field Scene Setup", "敵シンボル配置中…", (float)step / totalSteps);
            BuildEnemySymbols();

            step++;
            EditorUtility.DisplayProgressBar("Field Scene Setup", "保存中…", (float)step / totalSteps);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AddSceneToBuildSettings(SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("[DevilsDiner] Field Scene セットアップ完了！");
        EditorUtility.DisplayDialog(
            "Field Scene Setup",
            "フィールドシーンのセットアップが完了しました。\n" +
            "NavMesh をベイクしてから Play してください。\n" +
            "(Window > AI > Navigation で Surface を選択し Bake)",
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
        // ── FieldSystem（Bootstrap + EncounterHandler）──
        var fieldSystemGO = new GameObject("FieldSystem");
        fieldSystemGO.AddComponent<FieldSceneBootstrap>();
        fieldSystemGO.AddComponent<FieldEncounterHandler>();

        // ── Directional Light ──
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.88f);
        light.intensity = 1.2f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ── Ground Plane（大きめ、NavMeshSurface 付き）──
        var groundGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundGO.name = "FieldGround";
        groundGO.transform.position = Vector3.zero;
        groundGO.transform.localScale = new Vector3(10f, 1f, 10f);

        // マテリアル設定
        var groundRenderer = groundGO.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = new Color(0.35f, 0.55f, 0.25f);
            groundRenderer.sharedMaterial = mat;
        }

        // NavMeshSurface を追加
        groundGO.AddComponent<NavMeshSurface>();

        Debug.Log("[FieldSceneAutoBuilder] 環境構築完了。");
    }

    // ================================================================
    // プレイヤー構築
    // ================================================================

    private static GameObject BuildPlayer()
    {
        var playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerGO.name = "Player";
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(0f, 1f, 0f);

        // マテリアル
        var renderer = playerGO.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = new Color(0.2f, 0.5f, 1f);
            renderer.sharedMaterial = mat;
        }

        // CharacterController
        var cc = playerGO.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0f, 0f);
        cc.height = 2f;
        cc.radius = 0.5f;

        // Collider 重複を回避（CreatePrimitive が CapsuleCollider を付けるため）
        var existingCollider = playerGO.GetComponent<CapsuleCollider>();
        if (existingCollider != null)
        {
            Object.DestroyImmediate(existingCollider);
        }

        // FieldPlayerController
        playerGO.AddComponent<FieldPlayerController>();

        // InputActionAsset を結線
        var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");
        if (inputAsset != null)
        {
            var so = new SerializedObject(playerGO.GetComponent<FieldPlayerController>());
            so.FindProperty("_inputActions").objectReferenceValue = inputAsset;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log("[FieldSceneAutoBuilder] プレイヤー構築完了。");
        return playerGO;
    }

    // ================================================================
    // カメラ構築
    // ================================================================

    private static void BuildCamera(Transform playerTransform)
    {
        // ── Main Camera ──
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        var camera = cameraGO.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 60f;
        cameraGO.transform.position = new Vector3(0f, 5f, -8f);
        cameraGO.AddComponent<AudioListener>();

        // ── CinemachineBrain ──
        cameraGO.AddComponent<CinemachineBrain>();

        // ── CinemachineCamera (TPS) ──
        var vcamGO = new GameObject("FieldVCam");
        var vcam = vcamGO.AddComponent<CinemachineCamera>();
        vcam.Lens = new LensSettings
        {
            FieldOfView = 55f,
            NearClipPlane = 0.1f,
            FarClipPlane = 500f,
        };

        // ThirdPersonFollow Body コンポーネント
        var tpFollow = vcamGO.AddComponent<CinemachineThirdPersonFollow>();
        tpFollow.Damping = new Vector3(0.1f, 0.25f, 0.15f);
        tpFollow.ShoulderOffset = new Vector3(0.5f, 0f, 0f);
        tpFollow.CameraDistance = 5f;
        tpFollow.CameraSide = 1f;

        // ── FieldCameraController ──
        var camController = vcamGO.AddComponent<FieldCameraController>();

        // InputActionAsset を結線
        var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");

        var camSO = new SerializedObject(camController);
        camSO.FindProperty("_vcam").objectReferenceValue = vcam;
        if (inputAsset != null)
        {
            camSO.FindProperty("_inputActions").objectReferenceValue = inputAsset;
        }
        camSO.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[FieldSceneAutoBuilder] カメラ構築完了。");
    }

    // ================================================================
    // 敵シンボル構築
    // ================================================================

    private static void BuildEnemySymbols()
    {
        var enemyParent = new GameObject("--- Enemy Symbols ---");

        // EnemyData / CharacterStats を読み込み
        EnemyData[] allEnemyData = Resources.LoadAll<EnemyData>("");
        CharacterStats[] allStats = Resources.LoadAll<CharacterStats>("");

        for (int i = 0; i < ENEMY_POSITIONS.Length; i++)
        {
            var enemyGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyGO.name = $"EnemySymbol_{i}";
            enemyGO.transform.position = ENEMY_POSITIONS[i] + Vector3.up;
            enemyGO.transform.SetParent(enemyParent.transform);

            // 赤マテリアル
            var renderer = enemyGO.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                var mat = new Material(shader);
                mat.color = new Color(1f, 0.3f, 0.3f);
                renderer.sharedMaterial = mat;
            }

            // NavMeshAgent
            var agent = enemyGO.AddComponent<NavMeshAgent>();
            agent.speed = 2f;
            agent.stoppingDistance = 0.5f;

            // EnemySymbol
            var symbol = enemyGO.AddComponent<EnemySymbol>();

            // EnemyData / CharacterStats を結線（利用可能なものを順番に割り当て）
            var symbolSO = new SerializedObject(symbol);
            if (allEnemyData.Length > 0)
            {
                int dataIdx = i % allEnemyData.Length;
                symbolSO.FindProperty("_enemyData").objectReferenceValue = allEnemyData[dataIdx];
            }

            // Stats — 敵用の Stats を探す（Player 以外）
            CharacterStats enemyStat = FindEnemyStats(allStats, i);
            if (enemyStat != null)
            {
                symbolSO.FindProperty("_enemyStats").objectReferenceValue = enemyStat;
            }
            symbolSO.ApplyModifiedPropertiesWithoutUndo();

            // SphereCollider（トリガー）をエンカウント判定用に追加
            // CreatePrimitive の CapsuleCollider をトリガーに変更
            var existingCollider = enemyGO.GetComponent<CapsuleCollider>();
            if (existingCollider != null)
            {
                Object.DestroyImmediate(existingCollider);
            }

            var triggerCollider = enemyGO.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 1.2f;
        }

        Debug.Log($"[FieldSceneAutoBuilder] 敵シンボル {ENEMY_POSITIONS.Length}体 を配置完了。");
    }

    /// <summary>敵用の CharacterStats を検索する。</summary>
    private static CharacterStats FindEnemyStats(CharacterStats[] allStats, int index)
    {
        // "enemy" を含む ID のものを敵として扱う
        var enemyStatsList = new List<CharacterStats>();
        foreach (var s in allStats)
        {
            if (s != null && s.Id != null && s.Id.Contains("enemy"))
            {
                enemyStatsList.Add(s);
            }
        }

        // 敵 Stats が無い場合は全体からフォールバック
        if (enemyStatsList.Count > 0)
        {
            return enemyStatsList[index % enemyStatsList.Count];
        }
        else if (allStats.Length > 0)
        {
            return allStats[index % allStats.Length];
        }

        return null;
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
}
#endif
