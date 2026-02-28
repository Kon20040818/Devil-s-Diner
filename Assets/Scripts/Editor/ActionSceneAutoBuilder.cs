// ============================================================
// ActionSceneAutoBuilder.cs
// テスト用アクションシーンを全自動構築するエディタ拡張。
// メニュー「DevilsDiner > Build Test Action Scene」で実行する。
// ============================================================
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// テスト用アクションシーンをワンクリックで全自動構築するエディタ拡張。
/// Floor / Player / Enemy / Camera / HUD Canvas / GameManager / Scene Manager を生成し、
/// ダミー ScriptableObject アセットの作成・結線まで行う。
/// </summary>
public static class ActionSceneAutoBuilder
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const string MENU_PATH = "DevilsDiner/Build Test Action Scene";

    private const string WEAPON_ASSET_PATH = "Assets/Data/Weapons/WPN_Test.asset";
    private const string ENEMY_ASSET_PATH = "Assets/Data/Enemies/ENM_Test.asset";
    private const string CACTUS_ENEMY_ASSET_PATH = "Assets/Data/Enemies/ENM_Cactus.asset";
    private const string BOSS_ENEMY_ASSET_PATH = "Assets/Data/Enemies/ENM_Boss.asset";
    private const string WEAPON_HEAVY_ASSET_PATH = "Assets/Data/Weapons/WPN_Heavy.asset";
    private const string WEAPON_LIGHT_ASSET_PATH = "Assets/Data/Weapons/WPN_Light.asset";
    private const string CONFIG_ASSET_PATH = "Assets/Data/Config/JustInputConfig.asset";

    private const string TAG_PLAYER = "Player";
    private const string TAG_ENEMY_HURTBOX = "EnemyHurtbox";

    // ──────────────────────────────────────────────
    // メニュー実行
    // ──────────────────────────────────────────────

    [MenuItem(MENU_PATH)]
    private static void Execute()
    {
        int totalSteps = 14;
        int currentStep = 0;

        try
        {
            // ── Step 0: タグ作成 ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Ensuring tags exist...", (float)currentStep / totalSteps);
            EnsureTagExists(TAG_ENEMY_HURTBOX);

            // ── Step 1: ダミー ScriptableObject アセット作成 ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Creating ScriptableObject assets...", (float)currentStep / totalSteps);
            WeaponData weaponData = EnsureWeaponDataAsset();
            EnemyData cactusEnemyData = EnsureCactusEnemyDataAsset();
            EnemyData bossEnemyData = EnsureBossEnemyDataAsset();
            JustInputConfig justInputConfig = EnsureJustInputConfigAsset();

            // ── Step 2: Terrain (立体マップ) ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Creating Terrain...", (float)currentStep / totalSteps);
            GameObject terrain = CreateTerrain();

            // ── Step 3: Player ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Creating Player...", (float)currentStep / totalSteps);
            GameObject player = CreatePlayer(weaponData);

            // ── Step 4: CactusEnemy ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Creating CactusEnemy...", (float)currentStep / totalSteps);
            GameObject cactusEnemy = CreateCactusEnemy(cactusEnemyData);

            // ── Step 5: BossEnemy ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Creating BossEnemy...", (float)currentStep / totalSteps);
            GameObject bossEnemy = CreateBossEnemy(bossEnemyData);

            // ── Step 6: Main Camera ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Setting up Main Camera...", (float)currentStep / totalSteps);
            GameObject mainCamera = SetupMainCamera(player.transform);

            // ── Step 7: HUD Canvas ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Creating HUD Canvas...", (float)currentStep / totalSteps);
            GameObject hudCanvas = CreateHUDCanvas();

            // ── Step 8: GameManager ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Creating GameManager...", (float)currentStep / totalSteps);
            EnsureGameManager();

            // ── Step 9: Scene Manager ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Creating Scene Manager...", (float)currentStep / totalSteps);
            GameObject sceneManager = CreateSceneManager(justInputConfig);

            // ── Step 10: ReturnPortal ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Creating ReturnPortal...", (float)currentStep / totalSteps);
            CreateReturnPortal();

            // ── Step 11: 参照結線 ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Wiring component references...", (float)currentStep / totalSteps);
            WireReferences(player, mainCamera, hudCanvas, sceneManager, weaponData);

            // ── Step 12: 追加武器アセット作成 ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Action Scene", "Creating additional weapon assets...", (float)currentStep / totalSteps);
            EnsureWeaponDataAsset(WEAPON_HEAVY_ASSET_PATH, "WPN_Heavy", "ヘビーシリンダー・ブレード", 3000, 180, 20, 0);
            EnsureWeaponDataAsset(WEAPON_LIGHT_ASSET_PATH, "WPN_Light", "ライトニング・ブレード", 1500, 80, 5, 5);

            // ── 完了 ──
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[ActionSceneAutoBuilder] Test Action Scene を構築しました。\n" +
                "  - Terrain (立体マップ: ground + platforms + slopes + walls, NavMeshSurface attempted)\n" +
                "  - Player (Capsule + PlayerController + PlayerHealth + Weapon child)\n" +
                "  - CactusEnemy (Sphere + CactusEnemy + EnemyHurtbox + EnemyAttackBox)\n" +
                "  - BossEnemy (Cube + BossEnemy + BossAttackWarning + EnemyHurtbox + EnemyAttackBox)\n" +
                "  - Main Camera (TPSCameraController + CameraShakeHandler)\n" +
                "  - HUD Canvas (ActionHUD + TimerText + JustSuccessDisplay + HPSlider)\n" +
                "  - GameManager (singleton)\n" +
                "  - Scene Manager (ActionSceneBootstrap + JustInputAction)\n" +
                "  - SkillEffectApplier (on Scene Manager)\n" +
                "  - ReturnPortal (Cube trigger at corner)\n" +
                $"  - WeaponData: {WEAPON_ASSET_PATH}\n" +
                $"  - WPN_Heavy: {WEAPON_HEAVY_ASSET_PATH}\n" +
                $"  - WPN_Light: {WEAPON_LIGHT_ASSET_PATH}\n" +
                $"  - CactusEnemyData: {CACTUS_ENEMY_ASSET_PATH}\n" +
                $"  - BossEnemyData: {BOSS_ENEMY_ASSET_PATH}\n" +
                $"  - JustInputConfig: {CONFIG_ASSET_PATH}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ══════════════════════════════════════════════
    // タグ作成
    // ══════════════════════════════════════════════

    /// <summary>指定タグが存在しなければ TagManager に追加する。</summary>
    private static void EnsureTagExists(string tagName)
    {
        // 既に存在する場合はスキップ
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                return;
            }
        }

        // 空きスロットを探すか末尾に追加
        int insertIndex = -1;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (string.IsNullOrEmpty(tagsProp.GetArrayElementAtIndex(i).stringValue))
            {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex >= 0)
        {
            tagsProp.GetArrayElementAtIndex(insertIndex).stringValue = tagName;
        }
        else
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        }

        tagManager.ApplyModifiedProperties();
        Debug.Log($"[ActionSceneAutoBuilder] Tag '{tagName}' を作成しました。");
    }

    // ══════════════════════════════════════════════
    // ScriptableObject アセット作成
    // ══════════════════════════════════════════════

    private static WeaponData EnsureWeaponDataAsset()
    {
        WeaponData existing = AssetDatabase.LoadAssetAtPath<WeaponData>(WEAPON_ASSET_PATH);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Weapons");

        WeaponData asset = ScriptableObject.CreateInstance<WeaponData>();

        // SerializedObject でフィールドを設定
        AssetDatabase.CreateAsset(asset, WEAPON_ASSET_PATH);
        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", "WPN_Test");
        SetSerializedField(so, "_weaponName", "Test Weapon");
        SetSerializedField(so, "_price", 0);
        SetSerializedField(so, "_baseDamage", 100);
        SetSerializedField(so, "_basePartBreakValue", 10);
        SetSerializedField(so, "_justInputFrameBonus", 2);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    private static WeaponData EnsureWeaponDataAsset(string path, string id, string weaponName, int price, int damage, int partBreak, int justBonus)
    {
        WeaponData existing = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Weapons");

        WeaponData asset = ScriptableObject.CreateInstance<WeaponData>();

        AssetDatabase.CreateAsset(asset, path);
        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", id);
        SetSerializedField(so, "_weaponName", weaponName);
        SetSerializedField(so, "_price", price);
        SetSerializedField(so, "_baseDamage", damage);
        SetSerializedField(so, "_basePartBreakValue", partBreak);
        SetSerializedField(so, "_justInputFrameBonus", justBonus);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    private static EnemyData EnsureEnemyDataAsset()
    {
        EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_ASSET_PATH);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Enemies");

        EnemyData asset = ScriptableObject.CreateInstance<EnemyData>();

        AssetDatabase.CreateAsset(asset, ENEMY_ASSET_PATH);
        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", "ENM_Test");
        SetSerializedField(so, "_enemyName", "Test Enemy");
        SetSerializedField(so, "_maxHP", 500);
        SetSerializedField(so, "_baseAttack", 10);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    private static EnemyData EnsureCactusEnemyDataAsset()
    {
        EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(CACTUS_ENEMY_ASSET_PATH);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Enemies");

        EnemyData asset = ScriptableObject.CreateInstance<EnemyData>();

        AssetDatabase.CreateAsset(asset, CACTUS_ENEMY_ASSET_PATH);
        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", "ENM_Cactus");
        SetSerializedField(so, "_enemyName", "サボテン魔人");
        SetSerializedField(so, "_maxHP", 300);
        SetSerializedField(so, "_baseAttack", 8);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    private static EnemyData EnsureBossEnemyDataAsset()
    {
        EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(BOSS_ENEMY_ASSET_PATH);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Enemies");

        EnemyData asset = ScriptableObject.CreateInstance<EnemyData>();

        AssetDatabase.CreateAsset(asset, BOSS_ENEMY_ASSET_PATH);
        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", "ENM_Boss");
        SetSerializedField(so, "_enemyName", "牛頭の保安官");
        SetSerializedField(so, "_maxHP", 2000);
        SetSerializedField(so, "_baseAttack", 25);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    private static JustInputConfig EnsureJustInputConfigAsset()
    {
        JustInputConfig existing = AssetDatabase.LoadAssetAtPath<JustInputConfig>(CONFIG_ASSET_PATH);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Config");

        JustInputConfig asset = ScriptableObject.CreateInstance<JustInputConfig>();
        AssetDatabase.CreateAsset(asset, CONFIG_ASSET_PATH);
        EditorUtility.SetDirty(asset);

        return asset;
    }

    // ══════════════════════════════════════════════
    // 1. Terrain (立体マップ)
    // ══════════════════════════════════════════════

    private static GameObject CreateTerrain()
    {
        StaticEditorFlags terrainFlags = StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic;

        // ── 親オブジェクト ──────────────────────────
        GameObject terrainParent = new GameObject("Terrain");
        Undo.RegisterCreatedObjectUndo(terrainParent, "Create Terrain");

        // ── メイン地面 (30x1x30) ──────────────────────────
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(terrainParent.transform);
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(30f, 1f, 30f);
        GameObjectUtility.SetStaticEditorFlags(ground, terrainFlags);
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

        // ── 高台プラットフォーム ──────────────────────────
        GameObject platformA = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platformA.name = "Platform_A";
        platformA.transform.SetParent(terrainParent.transform);
        platformA.transform.position = new Vector3(-8f, 0.5f, 6f);
        platformA.transform.localScale = new Vector3(6f, 1f, 5f);
        GameObjectUtility.SetStaticEditorFlags(platformA, terrainFlags);
        Undo.RegisterCreatedObjectUndo(platformA, "Create Platform_A");

        GameObject platformB = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platformB.name = "Platform_B";
        platformB.transform.SetParent(terrainParent.transform);
        platformB.transform.position = new Vector3(9f, 1.0f, -5f);
        platformB.transform.localScale = new Vector3(5f, 1f, 6f);
        GameObjectUtility.SetStaticEditorFlags(platformB, terrainFlags);
        Undo.RegisterCreatedObjectUndo(platformB, "Create Platform_B");

        GameObject platformC = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platformC.name = "Platform_C";
        platformC.transform.SetParent(terrainParent.transform);
        platformC.transform.position = new Vector3(2f, 2.0f, 10f);
        platformC.transform.localScale = new Vector3(4f, 1f, 4f);
        GameObjectUtility.SetStaticEditorFlags(platformC, terrainFlags);
        Undo.RegisterCreatedObjectUndo(platformC, "Create Platform_C");

        // ── スロープ（ランプ） ──────────────────────────
        // スロープ A: 地面 → Platform_A へ接続
        GameObject slopeA = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slopeA.name = "Slope_A";
        slopeA.transform.SetParent(terrainParent.transform);
        slopeA.transform.position = new Vector3(-4.5f, 0.0f, 6f);
        slopeA.transform.localScale = new Vector3(3f, 0.3f, 5f);
        slopeA.transform.rotation = Quaternion.Euler(0f, 0f, 20f);
        GameObjectUtility.SetStaticEditorFlags(slopeA, terrainFlags);
        Undo.RegisterCreatedObjectUndo(slopeA, "Create Slope_A");

        // スロープ B: 地面 → Platform_B へ接続
        GameObject slopeB = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slopeB.name = "Slope_B";
        slopeB.transform.SetParent(terrainParent.transform);
        slopeB.transform.position = new Vector3(5.5f, 0.25f, -5f);
        slopeB.transform.localScale = new Vector3(4f, 0.3f, 5f);
        slopeB.transform.rotation = Quaternion.Euler(0f, 0f, -25f);
        GameObjectUtility.SetStaticEditorFlags(slopeB, terrainFlags);
        Undo.RegisterCreatedObjectUndo(slopeB, "Create Slope_B");

        // ── 遮蔽壁（カバー） ──────────────────────────
        GameObject wallA = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallA.name = "CoverWall_A";
        wallA.transform.SetParent(terrainParent.transform);
        wallA.transform.position = new Vector3(3f, 1f, 2f);
        wallA.transform.localScale = new Vector3(3f, 2f, 0.5f);
        GameObjectUtility.SetStaticEditorFlags(wallA, terrainFlags);
        Undo.RegisterCreatedObjectUndo(wallA, "Create CoverWall_A");

        GameObject wallB = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallB.name = "CoverWall_B";
        wallB.transform.SetParent(terrainParent.transform);
        wallB.transform.position = new Vector3(-5f, 1f, -3f);
        wallB.transform.localScale = new Vector3(0.5f, 2f, 3f);
        GameObjectUtility.SetStaticEditorFlags(wallB, terrainFlags);
        Undo.RegisterCreatedObjectUndo(wallB, "Create CoverWall_B");

        GameObject wallC = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallC.name = "CoverWall_C";
        wallC.transform.SetParent(terrainParent.transform);
        wallC.transform.position = new Vector3(0f, 1f, -8f);
        wallC.transform.localScale = new Vector3(3f, 2f, 0.5f);
        GameObjectUtility.SetStaticEditorFlags(wallC, terrainFlags);
        Undo.RegisterCreatedObjectUndo(wallC, "Create CoverWall_C");

        GameObject wallD = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallD.name = "CoverWall_D";
        wallD.transform.SetParent(terrainParent.transform);
        wallD.transform.position = new Vector3(7f, 1f, 8f);
        wallD.transform.localScale = new Vector3(0.5f, 2f, 3f);
        GameObjectUtility.SetStaticEditorFlags(wallD, terrainFlags);
        Undo.RegisterCreatedObjectUndo(wallD, "Create CoverWall_D");

        // ── NavMeshSurface を親 Terrain に追加（リフレクション） ──────────────────────────
        System.Type navMeshSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (navMeshSurfaceType != null)
        {
            terrainParent.AddComponent(navMeshSurfaceType);
            Debug.Log("[ActionSceneAutoBuilder] NavMeshSurface を Terrain に追加しました。");
        }
        else
        {
            Debug.LogWarning(
                "[ActionSceneAutoBuilder] AI Navigation パッケージが未インストールのため " +
                "NavMeshSurface を追加できませんでした。");
        }

        return terrainParent;
    }

    // ══════════════════════════════════════════════
    // 2. Player
    // ══════════════════════════════════════════════

    private static GameObject CreatePlayer(WeaponData weaponData)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.tag = TAG_PLAYER;

        Undo.RegisterCreatedObjectUndo(player, "Create Player");

        // PlayerController を追加 (RequireComponent で CharacterController, Animator, PlayerInputHandler が自動追加)
        player.AddComponent<PlayerController>();

        // DummyAnimationEventSender を追加
        player.AddComponent<DummyAnimationEventSender>();

        // PlayerHealth を追加（IDamageable 実装）
        player.AddComponent<PlayerHealth>();

        // WeaponData をリフレクションで設定
        PlayerController pc = player.GetComponent<PlayerController>();
        SetPrivateFieldViaReflection(pc, "_equippedWeapon", weaponData);

        // ── 子オブジェクト: Weapon ──
        GameObject weapon = new GameObject("Weapon");
        Undo.RegisterCreatedObjectUndo(weapon, "Create Weapon");
        weapon.transform.SetParent(player.transform);
        weapon.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);

        BoxCollider weaponCollider = weapon.AddComponent<BoxCollider>();
        weaponCollider.size = new Vector3(0.3f, 0.3f, 1.0f);
        weaponCollider.isTrigger = true;

        WeaponColliderHandler wch = weapon.AddComponent<WeaponColliderHandler>();

        // PlayerController._weaponColliderHandler → WeaponColliderHandler
        SetPrivateFieldViaReflection(pc, "_weaponColliderHandler", wch);

        return player;
    }

    // ══════════════════════════════════════════════
    // 3. Enemies
    // ══════════════════════════════════════════════

    private static GameObject CreateCactusEnemy(EnemyData enemyData)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        enemy.name = "CactusEnemy";
        enemy.transform.position = new Vector3(8f, 1f, 5f);
        enemy.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        Undo.RegisterCreatedObjectUndo(enemy, "Create CactusEnemy");

        // CactusEnemy component (inherits EnemyController, RequireComponent adds NavMeshAgent)
        CactusEnemy ce = enemy.AddComponent<CactusEnemy>();

        // EnemyData を設定 (field is in base EnemyController: _enemyData)
        SetPrivateFieldViaReflection(ce, "_enemyData", enemyData);

        // ── 子: EnemyHurtbox ──
        CreateEnemyHurtbox(enemy);

        // ── 子: EnemyAttackBox ──
        CreateEnemyAttackBox(enemy, ce, enemyData);

        return enemy;
    }

    private static GameObject CreateBossEnemy(EnemyData enemyData)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.name = "BossEnemy";
        enemy.transform.position = new Vector3(0f, 1.5f, 12f);
        enemy.transform.localScale = new Vector3(2.5f, 3f, 2.5f);

        Undo.RegisterCreatedObjectUndo(enemy, "Create BossEnemy");

        BossEnemy be = enemy.AddComponent<BossEnemy>();

        SetPrivateFieldViaReflection(be, "_enemyData", enemyData);

        CreateEnemyHurtbox(enemy);
        CreateEnemyAttackBox(enemy, be, enemyData);

        // ── BossAttackWarning (予兆演出) ──
        BossAttackWarning baw = enemy.AddComponent<BossAttackWarning>();
        SetPrivateFieldViaReflection(baw, "_bossEnemy", be);

        // MeshRenderer は Primitive の Cube に自動で付いている
        Renderer bossRenderer = enemy.GetComponent<Renderer>();
        if (bossRenderer != null)
        {
            SetPrivateFieldViaReflection(baw, "_targetRenderer", bossRenderer);
        }

        return enemy;
    }

    // ── 共通ヘルパー: EnemyHurtbox 子オブジェクト作成 ──

    private static void CreateEnemyHurtbox(GameObject enemy)
    {
        GameObject hurtbox = new GameObject("EnemyHurtbox");
        Undo.RegisterCreatedObjectUndo(hurtbox, "Create EnemyHurtbox");
        hurtbox.transform.SetParent(enemy.transform);
        hurtbox.transform.localPosition = Vector3.zero;

        try
        {
            hurtbox.tag = TAG_ENEMY_HURTBOX;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ActionSceneAutoBuilder] Tag '{TAG_ENEMY_HURTBOX}' の設定に失敗: {e.Message}");
        }

        SphereCollider hurtboxCollider = hurtbox.AddComponent<SphereCollider>();
        hurtboxCollider.isTrigger = true;
        hurtboxCollider.radius = 1.0f;
    }

    // ── 共通ヘルパー: EnemyAttackBox 子オブジェクト作成 ──

    private static void CreateEnemyAttackBox(GameObject enemy, EnemyController ec, EnemyData enemyData)
    {
        GameObject attackBox = new GameObject("EnemyAttackBox");
        Undo.RegisterCreatedObjectUndo(attackBox, "Create EnemyAttackBox");
        attackBox.transform.SetParent(enemy.transform);
        attackBox.transform.localPosition = new Vector3(0f, 0f, 1.2f);

        SphereCollider attackCollider = attackBox.AddComponent<SphereCollider>();
        attackCollider.isTrigger = true;
        attackCollider.radius = 0.8f;

        EnemyAttackCollider eac = attackBox.AddComponent<EnemyAttackCollider>();
        SetPrivateFieldViaReflection(eac, "_enemyController", ec);

        if (enemyData != null)
        {
            SerializedObject enemySO = new SerializedObject(enemyData);
            SerializedProperty baseAttackProp = enemySO.FindProperty("_baseAttack");
            if (baseAttackProp != null)
            {
                SetPrivateFieldViaReflection(eac, "_attackDamage", (float)baseAttackProp.intValue);
            }
        }
    }

    // ══════════════════════════════════════════════
    // 4. Main Camera
    // ══════════════════════════════════════════════

    private static GameObject SetupMainCamera(Transform playerTransform)
    {
        // 既存の Main Camera を探す
        GameObject cameraObj = null;
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraObj = mainCam.gameObject;
        }

        if (cameraObj == null)
        {
            cameraObj = new GameObject("Main Camera");
            cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
            Undo.RegisterCreatedObjectUndo(cameraObj, "Create Main Camera");
        }

        cameraObj.transform.position = new Vector3(0f, 3f, -5f);
        cameraObj.transform.LookAt(playerTransform);

        // TPSCameraController を追加
        if (cameraObj.GetComponent<TPSCameraController>() == null)
        {
            cameraObj.AddComponent<TPSCameraController>();
        }

        // CameraShakeHandler
        if (cameraObj.GetComponent<CameraShakeHandler>() == null)
        {
            cameraObj.AddComponent<CameraShakeHandler>();
        }

        return cameraObj;
    }

    // ══════════════════════════════════════════════
    // 5. HUD Canvas
    // ══════════════════════════════════════════════

    private static GameObject CreateHUDCanvas()
    {
        GameObject canvasObj = new GameObject("HUD Canvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create HUD Canvas");

        // Canvas
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // GraphicRaycaster
        canvasObj.AddComponent<GraphicRaycaster>();

        // ActionHUD
        ActionHUD hud = canvasObj.AddComponent<ActionHUD>();

        // ── TimerText ──
        GameObject timerObj = new GameObject("TimerText");
        Undo.RegisterCreatedObjectUndo(timerObj, "Create TimerText");
        timerObj.transform.SetParent(canvasObj.transform, false);

        Text timerText = timerObj.AddComponent<Text>();
        timerText.text = "15:00";
        timerText.fontSize = 36;
        timerText.color = Color.white;
        timerText.alignment = TextAnchor.UpperRight;

        // Font を設定（Unity ビルトインフォント）
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (timerText.font == null)
        {
            timerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        RectTransform timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(1f, 1f);
        timerRect.anchorMax = new Vector2(1f, 1f);
        timerRect.pivot = new Vector2(1f, 1f);
        timerRect.anchoredPosition = new Vector2(-20f, -20f);
        timerRect.sizeDelta = new Vector2(150f, 50f);

        // ── JustSuccessDisplay ──
        GameObject justObj = new GameObject("JustSuccessDisplay");
        Undo.RegisterCreatedObjectUndo(justObj, "Create JustSuccessDisplay");
        justObj.transform.SetParent(canvasObj.transform, false);

        Text justText = justObj.AddComponent<Text>();
        justText.text = "JUST!";
        justText.fontSize = 72;
        justText.color = Color.yellow;
        justText.alignment = TextAnchor.MiddleCenter;

        justText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (justText.font == null)
        {
            justText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        RectTransform justRect = justObj.GetComponent<RectTransform>();
        justRect.anchorMin = new Vector2(0.5f, 0.5f);
        justRect.anchorMax = new Vector2(0.5f, 0.5f);
        justRect.anchoredPosition = new Vector2(0f, 50f);
        justRect.sizeDelta = new Vector2(300f, 100f);

        justObj.SetActive(false);

        // ── HP Slider ──
        GameObject hpSliderObj = new GameObject("HPSlider");
        Undo.RegisterCreatedObjectUndo(hpSliderObj, "Create HPSlider");
        hpSliderObj.transform.SetParent(canvasObj.transform, false);

        RectTransform sliderRect = hpSliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 1f);
        sliderRect.anchoredPosition = new Vector2(20f, -20f);
        sliderRect.sizeDelta = new Vector2(250f, 20f);

        Slider hpSlider = hpSliderObj.AddComponent<Slider>();
        hpSlider.minValue = 0;
        hpSlider.maxValue = 100;
        hpSlider.value = 100;
        hpSlider.interactable = false;

        // Background
        GameObject bgObj = new GameObject("Background");
        Undo.RegisterCreatedObjectUndo(bgObj, "Create HPSlider Background");
        bgObj.transform.SetParent(hpSliderObj.transform, false);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        Undo.RegisterCreatedObjectUndo(fillAreaObj, "Create HPSlider Fill Area");
        fillAreaObj.transform.SetParent(hpSliderObj.transform, false);

        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        // Fill
        GameObject fillObj = new GameObject("Fill");
        Undo.RegisterCreatedObjectUndo(fillObj, "Create HPSlider Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);

        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        hpSlider.fillRect = fillRect;

        // ── ComboText ──
        GameObject comboObj = new GameObject("ComboText");
        Undo.RegisterCreatedObjectUndo(comboObj, "Create ComboText");
        comboObj.transform.SetParent(canvasObj.transform, false);

        Text comboText = comboObj.AddComponent<Text>();
        comboText.text = "0 HITS!";
        comboText.fontSize = 48;
        comboText.color = new Color(1f, 0.6f, 0.1f, 1f);
        comboText.alignment = TextAnchor.MiddleCenter;
        comboText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (comboText.font == null) comboText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform comboRect = comboObj.GetComponent<RectTransform>();
        comboRect.anchorMin = new Vector2(0.5f, 0.5f);
        comboRect.anchorMax = new Vector2(0.5f, 0.5f);
        comboRect.anchoredPosition = new Vector2(0f, 150f);
        comboRect.sizeDelta = new Vector2(300f, 60f);

        comboObj.SetActive(false);

        // ── ItemLogContainer (VerticalLayoutGroup) ──
        GameObject itemLogObj = new GameObject("ItemLogContainer");
        Undo.RegisterCreatedObjectUndo(itemLogObj, "Create ItemLogContainer");
        itemLogObj.transform.SetParent(canvasObj.transform, false);

        RectTransform itemLogRect = itemLogObj.AddComponent<RectTransform>();
        itemLogRect.anchorMin = new Vector2(0f, 0f);
        itemLogRect.anchorMax = new Vector2(0f, 0f);
        itemLogRect.pivot = new Vector2(0f, 0f);
        itemLogRect.anchoredPosition = new Vector2(20f, 20f);
        itemLogRect.sizeDelta = new Vector2(320f, 200f);

        VerticalLayoutGroup vlg = itemLogObj.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.LowerLeft;
        vlg.spacing = 4f;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;

        // ── ActionHUD フィールド結線（リフレクション）──
        SetPrivateFieldViaReflection(hud, "_timerText", timerText);
        SetPrivateFieldViaReflection(hud, "_justSuccessDisplay", justObj);
        SetPrivateFieldViaReflection(hud, "_hpSlider", hpSlider);
        SetPrivateFieldViaReflection(hud, "_comboText", comboText);
        SetPrivateFieldViaReflection(hud, "_itemLogContainer", itemLogObj.transform);

        return canvasObj;
    }

    // ══════════════════════════════════════════════
    // 6. GameManager
    // ══════════════════════════════════════════════

    private static void EnsureGameManager()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log("[ActionSceneAutoBuilder] GameManager.Instance は既に存在します。スキップ。");
            return;
        }

        // シーン内に GameManager コンポーネントを持つオブジェクトがないか検索
        GameManager existingGM = Object.FindFirstObjectByType<GameManager>();
        if (existingGM != null)
        {
            Debug.Log("[ActionSceneAutoBuilder] GameManager はシーン内に既に存在します。スキップ。");
            return;
        }

        GameObject gmObj = new GameObject("GameManager");
        Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
        gmObj.AddComponent<GameManager>();
        // InventoryManager は GameManager.Awake() で自動追加される
    }

    // ══════════════════════════════════════════════
    // 7. Scene Manager
    // ══════════════════════════════════════════════

    private static GameObject CreateSceneManager(JustInputConfig justInputConfig)
    {
        GameObject smObj = new GameObject("--- Scene Manager ---");
        Undo.RegisterCreatedObjectUndo(smObj, "Create Scene Manager");

        smObj.AddComponent<ActionSceneBootstrap>();

        JustInputAction jia = smObj.AddComponent<JustInputAction>();

        // JustInputConfig を設定
        SetPrivateFieldViaReflection(jia, "_config", justInputConfig);

        smObj.AddComponent<SkillEffectApplier>();

        // ComboManager
        smObj.AddComponent<ComboManager>();

        return smObj;
    }

    // ══════════════════════════════════════════════
    // 8. ReturnPortal
    // ══════════════════════════════════════════════

    private static GameObject CreateReturnPortal()
    {
        GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        portal.name = "ReturnPortal";
        portal.transform.position = new Vector3(-12f, 1f, -12f);
        portal.transform.localScale = new Vector3(3f, 3f, 3f);

        Undo.RegisterCreatedObjectUndo(portal, "Create ReturnPortal");

        // コライダーをトリガーに変更
        BoxCollider col = portal.GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // ReturnPortal コンポーネント追加
        portal.AddComponent<ReturnPortal>();

        return portal;
    }

    // ══════════════════════════════════════════════
    // 参照結線
    // ══════════════════════════════════════════════

    /// <summary>
    /// コンポーネント間の参照をリフレクションまたは SerializedObject で結線する。
    /// ActionSceneBootstrap がランタイムで行う結線とは別に、
    /// エディタ上で Inspector に値をセットしておく。
    /// </summary>
    private static void WireReferences(
        GameObject player,
        GameObject mainCamera,
        GameObject hudCanvas,
        GameObject sceneManager,
        WeaponData weaponData)
    {
        PlayerController pc = player.GetComponent<PlayerController>();
        WeaponColliderHandler wch = player.GetComponentInChildren<WeaponColliderHandler>();
        JustInputAction jia = sceneManager.GetComponent<JustInputAction>();
        CameraShakeHandler csh = mainCamera.GetComponent<CameraShakeHandler>();
        ActionHUD hud = hudCanvas.GetComponent<ActionHUD>();

        // PlayerController._cameraTransform → Main Camera
        if (pc != null && mainCamera != null)
        {
            SetPrivateFieldViaReflection(pc, "_cameraTransform", mainCamera.transform);
        }

        // WeaponColliderHandler._playerController → PlayerController
        if (wch != null && pc != null)
        {
            SetPrivateFieldViaReflection(wch, "_playerController", pc);
        }

        // WeaponColliderHandler._justInputAction → JustInputAction
        if (wch != null && jia != null)
        {
            SetPrivateFieldViaReflection(wch, "_justInputAction", jia);
        }

        // JustInputAction._playerController → PlayerController
        if (jia != null && pc != null)
        {
            SetPrivateFieldViaReflection(jia, "_playerController", pc);
        }

        // JustInputAction._cameraShakeHandler → CameraShakeHandler
        if (jia != null && csh != null)
        {
            SetPrivateFieldViaReflection(jia, "_cameraShakeHandler", csh);
        }

        // ActionHUD._justInputAction → JustInputAction
        if (hud != null && jia != null)
        {
            SetPrivateFieldViaReflection(hud, "_justInputAction", jia);
        }

        // ActionHUD._playerHealth → PlayerHealth
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (hud != null && playerHealth != null)
        {
            SetPrivateFieldViaReflection(hud, "_playerHealth", playerHealth);
        }

        // ComboManager 結線
        ComboManager comboMgr = sceneManager.GetComponent<ComboManager>();
        if (comboMgr != null)
        {
            if (jia != null)
            {
                SetPrivateFieldViaReflection(comboMgr, "_justInputAction", jia);
            }
            if (playerHealth != null)
            {
                SetPrivateFieldViaReflection(comboMgr, "_playerHealth", playerHealth);
            }
        }

        // ActionHUD._comboManager → ComboManager
        if (hud != null && comboMgr != null)
        {
            SetPrivateFieldViaReflection(hud, "_comboManager", comboMgr);
        }

        // SerializedObject で永続化を保証
        MarkDirtyAll(pc, wch, jia, csh, hud, comboMgr);
    }

    // ══════════════════════════════════════════════
    // ヘルパー
    // ══════════════════════════════════════════════

    /// <summary>リフレクションで MonoBehaviour のプライベートフィールドに値を設定する。
    /// 継承チェーンを辿って基底クラスのフィールドも検索する。</summary>
    private static void SetPrivateFieldViaReflection(object target, string fieldName, object value)
    {
        if (target == null || value == null) return;

        System.Type type = target.GetType();
        FieldInfo field = null;

        // 継承チェーンを辿ってフィールドを検索
        while (type != null && field == null)
        {
            field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            type = type.BaseType;
        }

        if (field == null)
        {
            Debug.LogWarning(
                $"[ActionSceneAutoBuilder] {target.GetType().Name} にフィールド '{fieldName}' が見つかりません。");
            return;
        }

        field.SetValue(target, value);

        if (target is Object unityObj)
        {
            EditorUtility.SetDirty(unityObj);
        }
    }

    /// <summary>SerializedObject 経由でフィールドを設定する（文字列）。</summary>
    private static void SetSerializedField(SerializedObject so, string fieldName, string value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.stringValue = value;
        }
    }

    /// <summary>SerializedObject 経由でフィールドを設定する（整数）。</summary>
    private static void SetSerializedField(SerializedObject so, string fieldName, int value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.intValue = value;
        }
    }

    /// <summary>フォルダが存在しなければ作成する。</summary>
    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        int lastSlash = folderPath.LastIndexOf('/');
        if (lastSlash < 0) return;

        string parent = folderPath.Substring(0, lastSlash);
        string leaf = folderPath.Substring(lastSlash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, leaf);
    }

    /// <summary>複数の Object を Dirty マークする。</summary>
    private static void MarkDirtyAll(params Object[] objects)
    {
        foreach (Object obj in objects)
        {
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
            }
        }
    }
}
