// ============================================================
// ManagementSceneAutoBuilder.cs
// テスト用マネジメントシーンを全自動構築するエディタ拡張。
// メニュー「DevilsDiner > Build Test Management Scene」で実行する。
// ============================================================
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// テスト用マネジメントシーンをワンクリックで全自動構築するエディタ拡張。
/// Floor / Seats / Spawn-Exit Points / Diner Manager / SeatManager /
/// HUD Canvas (CookingUI, PhaseControlUI, MidnightResultUI) / GameManager を生成し、
/// CookingConfig ScriptableObject アセットの作成・結線まで行う。
/// </summary>
public static class ManagementSceneAutoBuilder
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const string MENU_PATH = "DevilsDiner/Build Test Management Scene";

    private const string COOKING_CONFIG_ASSET_PATH = "Assets/Data/Config/CookingConfig.asset";

    private const string MATERIAL_ASSET_PATH_PREFIX = "Assets/Data/Materials/";
    private const string RECIPE_ASSET_PATH_PREFIX = "Assets/Data/Recipes/";
    private const string FURNITURE_ASSET_PATH_PREFIX = "Assets/Data/Furniture/";
    private const string SKILL_ASSET_PATH_PREFIX = "Assets/Data/Skills/";
    private const string ENEMY_ASSET_PATH_PREFIX = "Assets/Data/Enemies/";
    private const string MAP_ASSET_PATH_PREFIX = "Assets/Data/Maps/";
    private const string WEAPON_ASSET_PATH_PREFIX = "Assets/Data/Weapons/";

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
            // ── Step 1: ScriptableObject アセット作成 ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating ScriptableObject assets...", (float)currentStep / totalSteps);
            CookingConfig cookingConfig = EnsureCookingConfigAsset();

            MaterialData matMeat = EnsureMaterialDataAsset("MAT_Meat", "肉", 1);
            MaterialData matVegetable = EnsureMaterialDataAsset("MAT_Vegetable", "野菜", 1);
            RecipeData recipeSteak = EnsureRecipeDataAsset("RCP_Steak", "ステーキ", 150, matMeat, 2);
            RecipeData recipeStew = EnsureRecipeDataAsset("RCP_Stew", "シチュー", 200, matMeat, 1, matVegetable, 1);
            RecipeData recipeSalad = EnsureRecipeDataAsset("RCP_Salad", "サラダ", 80, matVegetable, 2);

            FurnitureData frnTable = EnsureFurnitureDataAsset("FRN_Table", "木のテーブル", FurnitureData.FurnitureType.Table, 200, 5f);
            FurnitureData frnChair = EnsureFurnitureDataAsset("FRN_Chair", "木のイス", FurnitureData.FurnitureType.Chair, 100, 3f);
            FurnitureData frnLamp = EnsureFurnitureDataAsset("FRN_Lamp", "ランタン", FurnitureData.FurnitureType.Lighting, 150, 4f);

            // テスト用素材をインベントリに初期追加するヘルパーオブジェクトをメモ
            // (実際にはランタイム時に GameManager.Instance.Inventory.AddMaterial で追加される)

            // ── Step 2: Floor ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating Floor...", (float)currentStep / totalSteps);
            GameObject floor = CreateFloor();

            // ── Step 3: Seats ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating Seats...", (float)currentStep / totalSteps);
            SeatNode[] seatNodes = CreateSeats();

            // ── Step 4: Spawn / Exit Points ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating Spawn/Exit Points...", (float)currentStep / totalSteps);
            Transform spawnPoint;
            Transform exitPoint;
            CreateSpawnExitPoints(out spawnPoint, out exitPoint);

            // ── Step 5: SeatManager ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating SeatManager...", (float)currentStep / totalSteps);
            SeatManager seatManager = CreateSeatManager(seatNodes);

            // ── Step 6: Diner Manager ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating Diner Manager...", (float)currentStep / totalSteps);
            FurnitureData[] testFurniture = new FurnitureData[] { frnTable, frnChair, frnLamp };
            GameObject dinerManagerObj = CreateDinerManager(
                cookingConfig, seatManager, spawnPoint, exitPoint, testFurniture);

            // ── Step 7: HUD Canvas ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating HUD Canvas...", (float)currentStep / totalSteps);
            GameObject hudCanvas = CreateHUDCanvas(
                dinerManagerObj.GetComponent<CookingMinigame>(), cookingConfig,
                new RecipeData[] { recipeSteak, recipeStew, recipeSalad },
                testFurniture);

            // フォント取得（ProgressionUI 生成用に保持）
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // ── Step 8: GameManager ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating GameManager...", (float)currentStep / totalSteps);
            EnsureGameManager();

            // ── Step 9: CustomerPrefab (CustomerReactionHandler) ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating CustomerPrefab with CustomerReactionHandler...",
                (float)currentStep / totalSteps);
            // (CustomerPrefab は CreateDinerManager 内で生成・結線済み)

            // ── Step 10: SkillData テストアセット作成 ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating test SkillData assets...", (float)currentStep / totalSteps);
            SkillData sklHPUp = EnsureSkillDataAsset(
                "SKL_HPUp", "体力強化", "最大HPが20アップ",
                SkillData.SkillType.MaxHPUp, 500, 20f, null);
            SkillData sklAttack = EnsureSkillDataAsset(
                "SKL_AttackUp", "攻撃力強化", "攻撃力が15%アップ",
                SkillData.SkillType.AttackUp, 800, 15f, null);
            SkillData sklJustFrame = EnsureSkillDataAsset(
                "SKL_JustFrame", "ジャスト入力延長", "ジャスト入力の受付フレームが3フレーム延長（攻撃力強化が前提）",
                SkillData.SkillType.JustFrameExtend, 1200, 3f, sklAttack);

            // ── Step 11: ProgressionUI (スキルツリー + 店舗拡張) ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating ProgressionUI...", (float)currentStep / totalSteps);
            SkillData[] testSkills = new SkillData[] { sklHPUp, sklAttack, sklJustFrame };
            CreateProgressionUI(hudCanvas, testSkills, font);

            // ── Step 12: 量産用ダミーアセット（強敵・高難度レシピ・高額家具・マップ） ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating mass-production assets...", (float)currentStep / totalSteps);

            // 強敵ダミーデータ
            MaterialData matBone = EnsureMaterialDataAsset("MAT_Bone", "竜の骨", 3);
            MaterialData matScale = EnsureMaterialDataAsset("MAT_Scale", "毒鱗", 2);
            EnsureEnemyDataAsset("ENM_DragonLord", "竜王", 5000, 150, matBone, matBone);
            EnsureEnemyDataAsset("ENM_PoisonHydra", "毒ヒドラ", 3500, 120, matScale, matScale);

            // 高難度レシピダミーデータ
            RecipeData recipeDragonSteak = EnsureRecipeDataAsset("RCP_DragonSteak", "竜王のステーキ", 500, matBone, 3);
            RecipeData recipePoisonStew = EnsureRecipeDataAsset("RCP_PoisonStew", "毒鱗のシチュー", 350, matScale, 2, matVegetable, 2);

            // 高額家具ダミーデータ
            EnsureFurnitureDataAsset("FRN_Chandelier", "シャンデリア", FurnitureData.FurnitureType.Lighting, 2000, 15f);
            EnsureFurnitureDataAsset("FRN_ThroneSeat", "玉座の椅子", FurnitureData.FurnitureType.Chair, 3000, 20f);

            // マップデータ
            EnsureMapDataAsset("MAP_Desert", "砂塵のスラム街",
                "荒廃した砂漠の街。弱い魔物が徘徊する。",
                MapData.EnvironmentType.Desert, 1, 1);
            EnsureMapDataAsset("MAP_Swamp", "瘴気の毒沼",
                "毒霧に包まれた危険な沼地。中級の魔物が潜む。",
                MapData.EnvironmentType.Swamp, 2, 3);
            EnsureMapDataAsset("MAP_Forest", "妖精の森",
                "不思議な光に満ちた森。素材が豊富。",
                MapData.EnvironmentType.Forest, 2, 2);
            EnsureMapDataAsset("MAP_Volcano", "灼熱の火山",
                "溶岩が流れる過酷な火山帯。強敵が多い。",
                MapData.EnvironmentType.Volcano, 3, 4);
            EnsureMapDataAsset("MAP_Castle", "魔王城",
                "最終決戦の舞台。最強の魔物が待ち構える。",
                MapData.EnvironmentType.Castle, 5, 5);

            // ── Step 13: 回避スキル・武器データ・WeaponShopUI ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating dodge skills, weapon data & WeaponShopUI...", (float)currentStep / totalSteps);

            // 回避系スキルアセット
            SkillData sklDodgeDist = EnsureSkillDataAsset(
                "SKL_DodgeDistance", "回避距離アップ", "回避距離が2ユニット増加",
                SkillData.SkillType.DodgeDistanceUp, 800, 2.0f, null);
            SkillData sklDodgeInv = EnsureSkillDataAsset(
                "SKL_DodgeInvincible", "回避無敵延長", "回避無敵比率が15%増加（回避距離アップが前提）",
                SkillData.SkillType.DodgeInvincibleUp, 1200, 15f, sklDodgeDist);

            // 武器データアセット
            WeaponData wpnStarter = EnsureWeaponDataAsset("WPN_Starter", "初心者の剣", 0, 50, 5, 1);
            WeaponData wpnStandard = EnsureWeaponDataAsset("WPN_Standard", "鋼のガンブレード", 800, 100, 10, 2);
            WeaponData wpnHeavy = EnsureWeaponDataAsset("WPN_Heavy", "ヘビーシリンダー・ブレード", 3000, 180, 20, 0);
            WeaponData wpnLight = EnsureWeaponDataAsset("WPN_Light", "ライトニング・ブレード", 1500, 80, 5, 5);

            WeaponData[] testWeapons = new WeaponData[] { wpnStarter, wpnStandard, wpnHeavy, wpnLight };

            // WeaponShopUI パネル生成
            CreateWeaponShopUI(hudCanvas, testWeapons, font);

            // ── Step 14: MoneyPopUp + YadaCommentator ──
            currentStep++;
            EditorUtility.DisplayProgressBar("Build Test Management Scene",
                "Creating MoneyPopUp, YadaCommentator...", (float)currentStep / totalSteps);

            CreateMoneyPopUpAndYada(dinerManagerObj, hudCanvas, font);

            // ── 完了 ──
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[ManagementSceneAutoBuilder] Test Management Scene を構築しました。\n" +
                "  - Floor (20x1x20, NavMeshSurface attempted)\n" +
                "  - Seats x4 (SeatNode)\n" +
                "  - CustomerSpawnPoint / CustomerExitPoint\n" +
                "  - SeatManager (_seats wired)\n" +
                "  - Diner Manager (DinerManager + CookingMinigame + CustomerSpawner + OrderQueue + ManagementSceneBootstrap)\n" +
                "  - CustomerPrefab (CustomerAI + CustomerReactionHandler + ReactionParticle)\n" +
                "  - HousingManager (家具配置 + ComfortScore)\n" +
                "  - Dummy Furniture: FRN_Table, FRN_Chair, FRN_Lamp\n" +
                "  - HUD Canvas (CookingUI + PhaseControlUI + MidnightResultUI)\n" +
                "  - RecipeSelectUI (レシピ選択パネル)\n" +
                "  - HousingShopUI (家具ショップ + トグルボタン)\n" +
                "  - ProgressionUI (スキルツリー + 店舗拡張パネル)\n" +
                "  - Dummy Skills: SKL_HPUp, SKL_AttackUp, SKL_JustFrame\n" +
                "  - Dummy Materials: MAT_Meat, MAT_Vegetable, MAT_Bone, MAT_Scale\n" +
                "  - Dummy Recipes: RCP_Steak, RCP_Stew, RCP_Salad, RCP_DragonSteak, RCP_PoisonStew\n" +
                "  - Dummy Enemies: ENM_DragonLord, ENM_PoisonHydra\n" +
                "  - Dummy Furniture: FRN_Chandelier, FRN_ThroneSeat\n" +
                "  - Maps: MAP_Desert, MAP_Swamp, MAP_Forest, MAP_Volcano, MAP_Castle\n" +
                "  - Dodge Skills: SKL_DodgeDistance, SKL_DodgeInvincible\n" +
                "  - Weapons: WPN_Starter, WPN_Standard, WPN_Heavy, WPN_Light\n" +
                "  - WeaponShopUI (武器ショップ + トグルボタン)\n" +
                "  - MoneyPopUp (支払いポップアップ)\n" +
                "  - YadaCommentator (相棒カラス実況)\n" +
                "  - MidnightResultUI (段階的リザルト演出: チャリン→ジャララッ→バンッ)\n" +
                "  - GameManager (singleton)\n" +
                $"  - CookingConfig: {COOKING_CONFIG_ASSET_PATH}\n\n" +
                "========== 永続化テスト手順 ==========\n" +
                "1. Play モードを開始する\n" +
                "2. [F2] を数回押してゴールドを追加する（例: 5回 → 5000G）\n" +
                "3. Morning/Evening フェーズで ProgressionUI が表示される\n" +
                "   - 「スキルツリー」タブでスキルを解放する（例: 体力強化 500G, 攻撃力強化 800G）\n" +
                "   - 「店舗拡張」タブで店舗をアップグレードする（Lv.2 = 1000G）\n" +
                "4. [F4] を押して手動セーブを実行する\n" +
                "   → コンソールに「セーブ完了」が表示されることを確認\n" +
                "5. Play モードを停止する\n" +
                "6. 再度 Play モードを開始する\n" +
                "7. [F5] を押して手動ロードを実行する\n" +
                "   → コンソールに「ロード完了」「スキルを復元しました」が表示されることを確認\n" +
                "8. 以下を検証する:\n" +
                "   - 所持金がセーブ時の値に復元されている\n" +
                "   - 解放したスキルが [UNLOCKED] 表示になっている\n" +
                "   - 店舗レベルがセーブ時の値に復元されている\n" +
                "======================================");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ══════════════════════════════════════════════
    // ScriptableObject アセット作成
    // ══════════════════════════════════════════════

    private static CookingConfig EnsureCookingConfigAsset()
    {
        CookingConfig existing = AssetDatabase.LoadAssetAtPath<CookingConfig>(COOKING_CONFIG_ASSET_PATH);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Config");

        CookingConfig asset = ScriptableObject.CreateInstance<CookingConfig>();
        AssetDatabase.CreateAsset(asset, COOKING_CONFIG_ASSET_PATH);

        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_baseSpeed", 1.5f);
        SetSerializedField(so, "_baseSuccessWidth", 0.2f);
        SetSerializedField(so, "_perfectZoneRatio", 0.3f);
        SetSerializedField(so, "_burntMeatPrice", 10);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    // ══════════════════════════════════════════════
    // 1. Floor
    // ══════════════════════════════════════════════

    private static GameObject CreateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(20f, 1f, 20f);

        Undo.RegisterCreatedObjectUndo(floor, "Create Floor");

        // NavMeshSurface をリフレクションで追加（AI Navigation パッケージが入っている場合のみ）
        System.Type navMeshSurfaceType = System.Type.GetType(
            "Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (navMeshSurfaceType != null)
        {
            floor.AddComponent(navMeshSurfaceType);
            Debug.Log("[ManagementSceneAutoBuilder] NavMeshSurface を Floor に追加しました。");
        }
        else
        {
            Debug.LogWarning(
                "[ManagementSceneAutoBuilder] AI Navigation パッケージが未インストールのため " +
                "NavMeshSurface を追加できませんでした。");
        }

        return floor;
    }

    // ══════════════════════════════════════════════
    // 2. Seats
    // ══════════════════════════════════════════════

    private static SeatNode[] CreateSeats()
    {
        float[] xPositions = { -3f, -1f, 1f, 3f };
        SeatNode[] seatNodes = new SeatNode[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject seatObj = new GameObject($"Seat_{i}");
            seatObj.transform.position = new Vector3(xPositions[i], 0f, 2f);
            Undo.RegisterCreatedObjectUndo(seatObj, $"Create Seat_{i}");

            seatNodes[i] = seatObj.AddComponent<SeatNode>();
        }

        return seatNodes;
    }

    // ══════════════════════════════════════════════
    // 3. Spawn / Exit Points
    // ══════════════════════════════════════════════

    private static void CreateSpawnExitPoints(out Transform spawnPoint, out Transform exitPoint)
    {
        GameObject spawnObj = new GameObject("CustomerSpawnPoint");
        spawnObj.transform.position = new Vector3(0f, 0f, -8f);
        Undo.RegisterCreatedObjectUndo(spawnObj, "Create CustomerSpawnPoint");
        spawnPoint = spawnObj.transform;

        GameObject exitObj = new GameObject("CustomerExitPoint");
        exitObj.transform.position = new Vector3(0f, 0f, -10f);
        Undo.RegisterCreatedObjectUndo(exitObj, "Create CustomerExitPoint");
        exitPoint = exitObj.transform;
    }

    // ══════════════════════════════════════════════
    // 4. SeatManager
    // ══════════════════════════════════════════════

    private static SeatManager CreateSeatManager(SeatNode[] seatNodes)
    {
        GameObject smObj = new GameObject("SeatManager");
        Undo.RegisterCreatedObjectUndo(smObj, "Create SeatManager");

        SeatManager seatManager = smObj.AddComponent<SeatManager>();

        // SeatManager._seats → 全 SeatNode 配列
        SetPrivateFieldViaReflection(seatManager, "_seats", seatNodes);

        return seatManager;
    }

    // ══════════════════════════════════════════════
    // 5. Diner Manager
    // ══════════════════════════════════════════════

    private static GameObject CreateDinerManager(
        CookingConfig cookingConfig,
        SeatManager seatManager,
        Transform spawnPoint,
        Transform exitPoint,
        FurnitureData[] testFurniture)
    {
        GameObject dmObj = new GameObject("--- Diner Manager ---");
        Undo.RegisterCreatedObjectUndo(dmObj, "Create Diner Manager");

        // コンポーネント追加
        DinerManager dinerManager = dmObj.AddComponent<DinerManager>();
        CookingMinigame cookingMinigame = dmObj.AddComponent<CookingMinigame>();
        CustomerSpawner customerSpawner = dmObj.AddComponent<CustomerSpawner>();
        dmObj.AddComponent<OrderQueue>();
        dmObj.AddComponent<ManagementSceneBootstrap>();
        HousingManager housingManager = dmObj.AddComponent<HousingManager>();

        // CookingMinigame._config → CookingConfig
        SetPrivateFieldViaReflection(cookingMinigame, "_config", cookingConfig);

        // CustomerSpawner._seatManager → SeatManager
        SetPrivateFieldViaReflection(customerSpawner, "_seatManager", seatManager);

        // CustomerSpawner._dinerManager → DinerManager
        SetPrivateFieldViaReflection(customerSpawner, "_dinerManager", dinerManager);

        // CustomerSpawner._spawnPoint → CustomerSpawnPoint.transform
        SetPrivateFieldViaReflection(customerSpawner, "_spawnPoint", spawnPoint);

        // CustomerSpawner._exitPoint → CustomerExitPoint.transform
        SetPrivateFieldViaReflection(customerSpawner, "_exitPoint", exitPoint);

        // HousingManager._dinerManager → DinerManager
        SetPrivateFieldViaReflection(housingManager, "_dinerManager", dinerManager);

        // DinerManager._housingManager → HousingManager
        SetPrivateFieldViaReflection(dinerManager, "_housingManager", housingManager);

        // テスト用家具を DinerManager の配置リストに追加
        if (testFurniture != null)
        {
            for (int i = 0; i < testFurniture.Length; i++)
            {
                if (testFurniture[i] != null)
                {
                    dinerManager.PlaceFurniture(testFurniture[i]);
                }
            }
        }

        // ── テスト用 Customer プレハブ ──
        GameObject customerPrefab = CreateTestCustomerPrefab();
        SetPrivateFieldViaReflection(customerSpawner, "_customerPrefab", customerPrefab);

        return dmObj;
    }

    // ══════════════════════════════════════════════
    // 5b. テスト用 Customer プレハブ
    // ══════════════════════════════════════════════

    private static GameObject CreateTestCustomerPrefab()
    {
        // Create a Capsule to represent the customer
        GameObject customer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        customer.name = "CustomerPrefab";
        customer.transform.position = new Vector3(0f, -100f, 0f); // Off-screen storage

        Undo.RegisterCreatedObjectUndo(customer, "Create CustomerPrefab");

        // CustomerAI (RequireComponent adds NavMeshAgent)
        CustomerAI customerAI = customer.AddComponent<CustomerAI>();

        // CustomerReactionHandler
        CustomerReactionHandler reactionHandler = customer.AddComponent<CustomerReactionHandler>();
        SetPrivateFieldViaReflection(reactionHandler, "_customerAI", customerAI);

        // テスト用パーティクルシステム (子オブジェクト)
        GameObject particleObj = new GameObject("ReactionParticle");
        Undo.RegisterCreatedObjectUndo(particleObj, "Create ReactionParticle");
        particleObj.transform.SetParent(customer.transform);
        particleObj.transform.localPosition = Vector3.up * 1.5f; // 頭上

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        // パーティクル設定
        var main = ps.main;
        main.startSpeed = 3f;
        main.startSize = 0.3f;
        main.startLifetime = 1.0f;
        main.startColor = new Color(1f, 0.85f, 0f, 1f); // ゴールド色
        main.maxParticles = 50;
        main.loop = false;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 30)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        // Wire the particle system to CustomerReactionHandler
        SetPrivateFieldViaReflection(reactionHandler, "_reactionParticle", ps);

        // Renderer reference for color changes
        Renderer renderer = customer.GetComponent<Renderer>();
        if (renderer != null)
        {
            SetPrivateFieldViaReflection(reactionHandler, "_targetRenderer", renderer);
        }

        return customer;
    }

    // ══════════════════════════════════════════════
    // 6. HUD Canvas
    // ══════════════════════════════════════════════

    private static GameObject CreateHUDCanvas(
        CookingMinigame cookingMinigame,
        CookingConfig cookingConfig,
        RecipeData[] testRecipes,
        FurnitureData[] testFurniture)
    {
        // ── Canvas 本体 ──
        GameObject canvasObj = new GameObject("Management HUD");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Management HUD");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObj.AddComponent<GraphicRaycaster>();

        // フォント取得
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // ── CookingUI セクション ──
        CreateCookingUI(canvasObj, cookingMinigame, cookingConfig, font);

        // ── PhaseControlUI セクション ──
        CreatePhaseControlUI(canvasObj, font);

        // ── MidnightResultUI セクション ──
        CreateMidnightResultUI(canvasObj, font);

        // ── RecipeSelectUI セクション ──
        CreateRecipeSelectUI(canvasObj, cookingMinigame, testRecipes, font);

        // ── HousingShopUI セクション ──
        CreateHousingShopUI(canvasObj, testFurniture, font);

        return canvasObj;
    }

    // ──────────────────────────────────────────────
    // 6a. CookingUI
    // ──────────────────────────────────────────────

    private static void CreateCookingUI(
        GameObject canvasObj,
        CookingMinigame cookingMinigame,
        CookingConfig cookingConfig,
        Font font)
    {
        // CookingUI コンポーネントのホルダー（Canvas 直下）
        GameObject cookingUIObj = new GameObject("CookingUI");
        Undo.RegisterCreatedObjectUndo(cookingUIObj, "Create CookingUI");
        cookingUIObj.transform.SetParent(canvasObj.transform, false);

        RectTransform cookingUIRect = cookingUIObj.AddComponent<RectTransform>();
        cookingUIRect.anchorMin = Vector2.zero;
        cookingUIRect.anchorMax = Vector2.one;
        cookingUIRect.sizeDelta = Vector2.zero;

        CookingUI cookingUI = cookingUIObj.AddComponent<CookingUI>();

        // ── CookingGaugePanel (Image, gray background, inactive by default) ──
        GameObject gaugePanelObj = new GameObject("CookingGaugePanel");
        Undo.RegisterCreatedObjectUndo(gaugePanelObj, "Create CookingGaugePanel");
        gaugePanelObj.transform.SetParent(cookingUIObj.transform, false);

        Image gaugePanelImage = gaugePanelObj.AddComponent<Image>();
        gaugePanelImage.color = Color.gray;

        RectTransform gaugePanelRect = gaugePanelObj.GetComponent<RectTransform>();
        gaugePanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        gaugePanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        gaugePanelRect.sizeDelta = new Vector2(420f, 50f);
        gaugePanelRect.anchoredPosition = Vector2.zero;

        gaugePanelObj.SetActive(false);

        // ── SuccessZone (Image, green) ──
        GameObject successZoneObj = new GameObject("SuccessZone");
        Undo.RegisterCreatedObjectUndo(successZoneObj, "Create SuccessZone");
        successZoneObj.transform.SetParent(gaugePanelObj.transform, false);

        Image successZoneImage = successZoneObj.AddComponent<Image>();
        successZoneImage.color = Color.green;

        RectTransform successZoneRect = successZoneObj.GetComponent<RectTransform>();
        successZoneRect.anchorMin = new Vector2(0.5f, 0.5f);
        successZoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        successZoneRect.sizeDelta = new Vector2(80f, 30f);
        successZoneRect.anchoredPosition = Vector2.zero;

        // ── PerfectZone (Image, yellow) ──
        GameObject perfectZoneObj = new GameObject("PerfectZone");
        Undo.RegisterCreatedObjectUndo(perfectZoneObj, "Create PerfectZone");
        perfectZoneObj.transform.SetParent(gaugePanelObj.transform, false);

        Image perfectZoneImage = perfectZoneObj.AddComponent<Image>();
        perfectZoneImage.color = Color.yellow;

        RectTransform perfectZoneRect = perfectZoneObj.GetComponent<RectTransform>();
        perfectZoneRect.anchorMin = new Vector2(0.5f, 0.5f);
        perfectZoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        perfectZoneRect.sizeDelta = new Vector2(24f, 30f);
        perfectZoneRect.anchoredPosition = Vector2.zero;

        // ── GaugeNeedle (Image, white) ──
        GameObject gaugeNeedleObj = new GameObject("GaugeNeedle");
        Undo.RegisterCreatedObjectUndo(gaugeNeedleObj, "Create GaugeNeedle");
        gaugeNeedleObj.transform.SetParent(gaugePanelObj.transform, false);

        Image gaugeNeedleImage = gaugeNeedleObj.AddComponent<Image>();
        gaugeNeedleImage.color = Color.white;

        RectTransform gaugeNeedleRect = gaugeNeedleObj.GetComponent<RectTransform>();
        gaugeNeedleRect.anchorMin = new Vector2(0.5f, 0.5f);
        gaugeNeedleRect.anchorMax = new Vector2(0.5f, 0.5f);
        gaugeNeedleRect.sizeDelta = new Vector2(4f, 40f);
        gaugeNeedleRect.anchoredPosition = Vector2.zero;

        // ── ResultText (Text, "Perfect", 48pt, center) ──
        GameObject resultTextObj = new GameObject("ResultText");
        Undo.RegisterCreatedObjectUndo(resultTextObj, "Create ResultText");
        resultTextObj.transform.SetParent(gaugePanelObj.transform, false);

        Text resultText = resultTextObj.AddComponent<Text>();
        resultText.text = "Perfect";
        resultText.fontSize = 48;
        resultText.color = Color.white;
        resultText.alignment = TextAnchor.MiddleCenter;
        resultText.font = font;

        RectTransform resultTextRect = resultTextObj.GetComponent<RectTransform>();
        resultTextRect.anchorMin = new Vector2(0.5f, 1f);
        resultTextRect.anchorMax = new Vector2(0.5f, 1f);
        resultTextRect.pivot = new Vector2(0.5f, 0f);
        resultTextRect.sizeDelta = new Vector2(300f, 60f);
        resultTextRect.anchoredPosition = new Vector2(0f, 10f);

        // ── CookingUI フィールド結線 ──
        SetPrivateFieldViaReflection(cookingUI, "_gaugeNeedle", gaugeNeedleImage);
        SetPrivateFieldViaReflection(cookingUI, "_successZone", successZoneImage);
        SetPrivateFieldViaReflection(cookingUI, "_perfectZone", perfectZoneImage);
        SetPrivateFieldViaReflection(cookingUI, "_resultText", resultText);
        SetPrivateFieldViaReflection(cookingUI, "_gaugePanel", gaugePanelObj);
        SetPrivateFieldViaReflection(cookingUI, "_cookingMinigame", cookingMinigame);
        SetPrivateFieldViaReflection(cookingUI, "_cookingConfig", cookingConfig);
    }

    // ──────────────────────────────────────────────
    // 6b. PhaseControlUI
    // ──────────────────────────────────────────────

    private static void CreatePhaseControlUI(GameObject canvasObj, Font font)
    {
        // PhaseControlUI コンポーネントのホルダー
        GameObject phaseUIObj = new GameObject("PhaseControlUI");
        Undo.RegisterCreatedObjectUndo(phaseUIObj, "Create PhaseControlUI");
        phaseUIObj.transform.SetParent(canvasObj.transform, false);

        RectTransform phaseUIRect = phaseUIObj.AddComponent<RectTransform>();
        phaseUIRect.anchorMin = Vector2.zero;
        phaseUIRect.anchorMax = Vector2.one;
        phaseUIRect.sizeDelta = Vector2.zero;

        PhaseControlUI phaseControlUI = phaseUIObj.AddComponent<PhaseControlUI>();

        // ── ReadyButton ──
        GameObject readyBtnObj = CreateButton("ReadyButton", "準備完了", font,
            phaseUIObj.transform, new Vector2(-120f, -480f), new Vector2(200f, 60f));

        // ── CloseButton ──
        GameObject closeBtnObj = CreateButton("CloseButton", "営業終了", font,
            phaseUIObj.transform, new Vector2(120f, -480f), new Vector2(200f, 60f));

        // ── PhaseControlUI フィールド結線 ──
        SetPrivateFieldViaReflection(phaseControlUI, "_readyButton",
            readyBtnObj.GetComponent<Button>());
        SetPrivateFieldViaReflection(phaseControlUI, "_closeButton",
            closeBtnObj.GetComponent<Button>());
    }

    // ──────────────────────────────────────────────
    // 6c. MidnightResultUI
    // ──────────────────────────────────────────────

    private static void CreateMidnightResultUI(GameObject canvasObj, Font font)
    {
        // MidnightResultUI コンポーネントのホルダー
        GameObject midnightUIObj = new GameObject("MidnightResultUI");
        Undo.RegisterCreatedObjectUndo(midnightUIObj, "Create MidnightResultUI");
        midnightUIObj.transform.SetParent(canvasObj.transform, false);

        RectTransform midnightUIRect = midnightUIObj.AddComponent<RectTransform>();
        midnightUIRect.anchorMin = Vector2.zero;
        midnightUIRect.anchorMax = Vector2.one;
        midnightUIRect.sizeDelta = Vector2.zero;

        MidnightResultUI midnightResultUI = midnightUIObj.AddComponent<MidnightResultUI>();

        // ── ResultPanel (inactive by default) ──
        GameObject resultPanelObj = new GameObject("ResultPanel");
        Undo.RegisterCreatedObjectUndo(resultPanelObj, "Create ResultPanel");
        resultPanelObj.transform.SetParent(midnightUIObj.transform, false);

        Image resultPanelImage = resultPanelObj.AddComponent<Image>();
        resultPanelImage.color = new Color(0f, 0f, 0f, 0.8f);

        RectTransform resultPanelRect = resultPanelObj.GetComponent<RectTransform>();
        resultPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultPanelRect.sizeDelta = new Vector2(600f, 400f);
        resultPanelRect.anchoredPosition = Vector2.zero;

        resultPanelObj.SetActive(false);

        // ── DayLabel ──
        GameObject dayLabelObj = new GameObject("DayLabel");
        Undo.RegisterCreatedObjectUndo(dayLabelObj, "Create DayLabel");
        dayLabelObj.transform.SetParent(resultPanelObj.transform, false);

        Text dayLabel = dayLabelObj.AddComponent<Text>();
        dayLabel.text = "Day 1 終了";
        dayLabel.fontSize = 48;
        dayLabel.color = Color.white;
        dayLabel.alignment = TextAnchor.MiddleCenter;
        dayLabel.font = font;

        RectTransform dayLabelRect = dayLabelObj.GetComponent<RectTransform>();
        dayLabelRect.anchorMin = new Vector2(0.5f, 1f);
        dayLabelRect.anchorMax = new Vector2(0.5f, 1f);
        dayLabelRect.pivot = new Vector2(0.5f, 1f);
        dayLabelRect.sizeDelta = new Vector2(500f, 70f);
        dayLabelRect.anchoredPosition = new Vector2(0f, -20f);

        // ── GoldLabel ──
        GameObject goldLabelObj = new GameObject("GoldLabel");
        Undo.RegisterCreatedObjectUndo(goldLabelObj, "Create GoldLabel");
        goldLabelObj.transform.SetParent(resultPanelObj.transform, false);

        Text goldLabel = goldLabelObj.AddComponent<Text>();
        goldLabel.text = "所持金: 500 G";
        goldLabel.fontSize = 36;
        goldLabel.color = Color.white;
        goldLabel.alignment = TextAnchor.MiddleCenter;
        goldLabel.font = font;

        RectTransform goldLabelRect = goldLabelObj.GetComponent<RectTransform>();
        goldLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
        goldLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        goldLabelRect.sizeDelta = new Vector2(500f, 50f);
        goldLabelRect.anchoredPosition = new Vector2(0f, 30f);

        // ── RevenueLabel ──
        GameObject revenueLabelObj = new GameObject("RevenueLabel");
        Undo.RegisterCreatedObjectUndo(revenueLabelObj, "Create RevenueLabel");
        revenueLabelObj.transform.SetParent(resultPanelObj.transform, false);

        Text revenueLabel = revenueLabelObj.AddComponent<Text>();
        revenueLabel.text = "基本売上: 0 G";
        revenueLabel.fontSize = 36;
        revenueLabel.color = Color.white;
        revenueLabel.alignment = TextAnchor.MiddleCenter;
        revenueLabel.font = font;

        RectTransform revenueLabelRect = revenueLabelObj.GetComponent<RectTransform>();
        revenueLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
        revenueLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        revenueLabelRect.sizeDelta = new Vector2(500f, 50f);
        revenueLabelRect.anchoredPosition = new Vector2(0f, 10f);

        // ── TipLabel ──
        GameObject tipLabelObj = new GameObject("TipLabel");
        Undo.RegisterCreatedObjectUndo(tipLabelObj, "Create TipLabel");
        tipLabelObj.transform.SetParent(resultPanelObj.transform, false);

        Text tipLabel = tipLabelObj.AddComponent<Text>();
        tipLabel.text = "チップ: +0 G";
        tipLabel.fontSize = 30;
        tipLabel.color = new Color(0.5f, 1f, 0.5f, 1f);
        tipLabel.alignment = TextAnchor.MiddleCenter;
        tipLabel.font = font;

        RectTransform tipLabelRect = tipLabelObj.GetComponent<RectTransform>();
        tipLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
        tipLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        tipLabelRect.sizeDelta = new Vector2(500f, 40f);
        tipLabelRect.anchoredPosition = new Vector2(0f, -30f);

        tipLabelObj.SetActive(false);

        // ── TotalLabel ──
        GameObject totalLabelObj = new GameObject("TotalLabel");
        Undo.RegisterCreatedObjectUndo(totalLabelObj, "Create TotalLabel");
        totalLabelObj.transform.SetParent(resultPanelObj.transform, false);

        Text totalLabel = totalLabelObj.AddComponent<Text>();
        totalLabel.text = "総売上: 0 G";
        totalLabel.fontSize = 42;
        totalLabel.color = new Color(1f, 0.85f, 0f, 1f);
        totalLabel.alignment = TextAnchor.MiddleCenter;
        totalLabel.font = font;

        RectTransform totalLabelRect = totalLabelObj.GetComponent<RectTransform>();
        totalLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
        totalLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        totalLabelRect.sizeDelta = new Vector2(500f, 60f);
        totalLabelRect.anchoredPosition = new Vector2(0f, -80f);

        totalLabelObj.SetActive(false);

        // ── NextDayButton ──
        GameObject nextDayBtnObj = CreateButton("NextDayButton", "次の日へ", font,
            resultPanelObj.transform, new Vector2(0f, -150f), new Vector2(200f, 60f));

        // ── MidnightResultUI フィールド結線 ──
        SetPrivateFieldViaReflection(midnightResultUI, "_resultPanel", resultPanelObj);
        SetPrivateFieldViaReflection(midnightResultUI, "_dayLabel", dayLabel);
        SetPrivateFieldViaReflection(midnightResultUI, "_goldLabel", goldLabel);
        SetPrivateFieldViaReflection(midnightResultUI, "_revenueLabel", revenueLabel);
        SetPrivateFieldViaReflection(midnightResultUI, "_tipLabel", tipLabel);
        SetPrivateFieldViaReflection(midnightResultUI, "_totalLabel", totalLabel);
        SetPrivateFieldViaReflection(midnightResultUI, "_nextDayButton",
            nextDayBtnObj.GetComponent<Button>());
    }

    // ──────────────────────────────────────────────
    // 6d. RecipeSelectUI
    // ──────────────────────────────────────────────

    private static void CreateRecipeSelectUI(
        GameObject canvasObj,
        CookingMinigame cookingMinigame,
        RecipeData[] testRecipes,
        Font font)
    {
        // RecipeSelectUI コンポーネントのホルダー
        GameObject recipeUIObj = new GameObject("RecipeSelectUI");
        Undo.RegisterCreatedObjectUndo(recipeUIObj, "Create RecipeSelectUI");
        recipeUIObj.transform.SetParent(canvasObj.transform, false);

        RectTransform recipeUIRect = recipeUIObj.AddComponent<RectTransform>();
        recipeUIRect.anchorMin = Vector2.zero;
        recipeUIRect.anchorMax = Vector2.one;
        recipeUIRect.sizeDelta = Vector2.zero;

        RecipeSelectUI recipeSelectUI = recipeUIObj.AddComponent<RecipeSelectUI>();

        // ── RecipeListPanel ──
        GameObject panelObj = new GameObject("RecipeListPanel");
        Undo.RegisterCreatedObjectUndo(panelObj, "Create RecipeListPanel");
        panelObj.transform.SetParent(recipeUIObj.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0.35f, 1f);
        panelRect.offsetMin = new Vector2(20f, 20f);
        panelRect.offsetMax = new Vector2(0f, -20f);

        // ── タイトルテキスト ──
        GameObject titleObj = new GameObject("Title");
        Undo.RegisterCreatedObjectUndo(titleObj, "Create RecipeTitle");
        titleObj.transform.SetParent(panelObj.transform, false);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "レシピ選択";
        titleText.fontSize = 32;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.font = font;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 50f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);

        // ── ButtonContainer ──
        GameObject containerObj = new GameObject("ButtonContainer");
        Undo.RegisterCreatedObjectUndo(containerObj, "Create ButtonContainer");
        containerObj.transform.SetParent(panelObj.transform, false);

        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.offsetMin = new Vector2(10f, 10f);
        containerRect.offsetMax = new Vector2(-10f, -70f);

        panelObj.SetActive(false); // 初期非表示

        // ── RecipeSelectUI フィールド結線 ──
        SetPrivateFieldViaReflection(recipeSelectUI, "_availableRecipes", testRecipes);
        SetPrivateFieldViaReflection(recipeSelectUI, "_cookingMinigame", cookingMinigame);
        SetPrivateFieldViaReflection(recipeSelectUI, "_recipeListPanel", panelObj);
        SetPrivateFieldViaReflection(recipeSelectUI, "_buttonContainer", containerObj.transform);
    }

    // ──────────────────────────────────────────────
    // 6e. HousingShopUI
    // ──────────────────────────────────────────────

    private static void CreateHousingShopUI(
        GameObject canvasObj,
        FurnitureData[] testFurniture,
        Font font)
    {
        // HousingShopUI コンポーネントのホルダー
        GameObject shopUIObj = new GameObject("HousingShopUI");
        Undo.RegisterCreatedObjectUndo(shopUIObj, "Create HousingShopUI");
        shopUIObj.transform.SetParent(canvasObj.transform, false);

        RectTransform shopUIRect = shopUIObj.AddComponent<RectTransform>();
        shopUIRect.anchorMin = Vector2.zero;
        shopUIRect.anchorMax = Vector2.one;
        shopUIRect.sizeDelta = Vector2.zero;

        HousingShopUI housingShopUI = shopUIObj.AddComponent<HousingShopUI>();

        // ── ShopPanel (右側、初期非表示) ──
        GameObject panelObj = new GameObject("ShopPanel");
        Undo.RegisterCreatedObjectUndo(panelObj, "Create ShopPanel");
        panelObj.transform.SetParent(shopUIObj.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.15f, 0.05f, 0.9f);  // ダークグリーン

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.65f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.offsetMin = new Vector2(0f, 20f);
        panelRect.offsetMax = new Vector2(-20f, -20f);

        // ── ショップタイトル ──
        GameObject titleObj = new GameObject("ShopTitle");
        Undo.RegisterCreatedObjectUndo(titleObj, "Create ShopTitle");
        titleObj.transform.SetParent(panelObj.transform, false);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "家具ショップ";
        titleText.fontSize = 28;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.font = font;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 40f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);

        // ── GoldLabel ──
        GameObject goldLabelObj = new GameObject("GoldLabel");
        Undo.RegisterCreatedObjectUndo(goldLabelObj, "Create ShopGoldLabel");
        goldLabelObj.transform.SetParent(panelObj.transform, false);

        Text goldLabel = goldLabelObj.AddComponent<Text>();
        goldLabel.text = "所持金: 500 G";
        goldLabel.fontSize = 20;
        goldLabel.color = new Color(1f, 0.9f, 0.3f, 1f);  // ゴールド色
        goldLabel.alignment = TextAnchor.MiddleLeft;
        goldLabel.font = font;

        RectTransform goldLabelRect = goldLabelObj.GetComponent<RectTransform>();
        goldLabelRect.anchorMin = new Vector2(0f, 1f);
        goldLabelRect.anchorMax = new Vector2(1f, 1f);
        goldLabelRect.pivot = new Vector2(0.5f, 1f);
        goldLabelRect.sizeDelta = new Vector2(0f, 30f);
        goldLabelRect.anchoredPosition = new Vector2(0f, -55f);
        goldLabelRect.offsetMin = new Vector2(15f, goldLabelRect.offsetMin.y);

        // ── ComfortLabel ──
        GameObject comfortLabelObj = new GameObject("ComfortLabel");
        Undo.RegisterCreatedObjectUndo(comfortLabelObj, "Create ShopComfortLabel");
        comfortLabelObj.transform.SetParent(panelObj.transform, false);

        Text comfortLabel = comfortLabelObj.AddComponent<Text>();
        comfortLabel.text = "合計居心地度: 0.0";
        comfortLabel.fontSize = 18;
        comfortLabel.color = new Color(0.6f, 1f, 0.6f, 0.9f);  // ライトグリーン
        comfortLabel.alignment = TextAnchor.MiddleLeft;
        comfortLabel.font = font;

        RectTransform comfortLabelRect = comfortLabelObj.GetComponent<RectTransform>();
        comfortLabelRect.anchorMin = new Vector2(0f, 1f);
        comfortLabelRect.anchorMax = new Vector2(1f, 1f);
        comfortLabelRect.pivot = new Vector2(0.5f, 1f);
        comfortLabelRect.sizeDelta = new Vector2(0f, 25f);
        comfortLabelRect.anchoredPosition = new Vector2(0f, -85f);
        comfortLabelRect.offsetMin = new Vector2(15f, comfortLabelRect.offsetMin.y);

        // ── ButtonContainer ──
        GameObject containerObj = new GameObject("ShopButtonContainer");
        Undo.RegisterCreatedObjectUndo(containerObj, "Create ShopButtonContainer");
        containerObj.transform.SetParent(panelObj.transform, false);

        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.offsetMin = new Vector2(10f, 10f);
        containerRect.offsetMax = new Vector2(-10f, -115f);

        panelObj.SetActive(false);  // 初期非表示

        // ── ToggleButton (画面右下) ──
        GameObject toggleBtnObj = CreateButton("ShopToggleButton", "家具ショップ", font,
            canvasObj.transform, new Vector2(-120f, -480f), new Vector2(180f, 50f));

        // ToggleButton の位置を右下に調整
        RectTransform toggleRect = toggleBtnObj.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0f);
        toggleRect.anchorMax = new Vector2(1f, 0f);
        toggleRect.pivot = new Vector2(1f, 0f);
        toggleRect.anchoredPosition = new Vector2(-20f, 20f);

        // ── HousingShopUI フィールド結線 ──
        SetPrivateFieldViaReflection(housingShopUI, "_shopItems", testFurniture);
        SetPrivateFieldViaReflection(housingShopUI, "_shopPanel", panelObj);
        SetPrivateFieldViaReflection(housingShopUI, "_buttonContainer", containerObj.transform);
        SetPrivateFieldViaReflection(housingShopUI, "_goldLabel", goldLabel);
        SetPrivateFieldViaReflection(housingShopUI, "_comfortLabel", comfortLabel);
        SetPrivateFieldViaReflection(housingShopUI, "_toggleButton", toggleBtnObj.GetComponent<Button>());
    }

    // ══════════════════════════════════════════════
    // 7. GameManager
    // ══════════════════════════════════════════════

    private static void EnsureGameManager()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log("[ManagementSceneAutoBuilder] GameManager.Instance は既に存在します。スキップ。");
            return;
        }

        // シーン内に GameManager コンポーネントを持つオブジェクトがないか検索
        GameManager existingGM = Object.FindFirstObjectByType<GameManager>();
        if (existingGM != null)
        {
            Debug.Log("[ManagementSceneAutoBuilder] GameManager はシーン内に既に存在します。スキップ。");
            return;
        }

        GameObject gmObj = new GameObject("GameManager");
        Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
        gmObj.AddComponent<GameManager>();
    }

    // ══════════════════════════════════════════════
    // ダミー MaterialData / RecipeData 作成
    // ══════════════════════════════════════════════

    private static MaterialData EnsureMaterialDataAsset(string id, string materialName, int rarity)
    {
        string path = MATERIAL_ASSET_PATH_PREFIX + id + ".asset";
        MaterialData existing = AssetDatabase.LoadAssetAtPath<MaterialData>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Materials");

        MaterialData asset = ScriptableObject.CreateInstance<MaterialData>();
        AssetDatabase.CreateAsset(asset, path);

        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", id);
        SetSerializedField(so, "_materialName", materialName);
        SetSerializedField(so, "_rarity", rarity);
        SetSerializedField(so, "_gaugeSpeedMultiplier", 1f);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    private static RecipeData EnsureRecipeDataAsset(
        string id, string recipeName, int basePrice,
        params object[] materialsAndAmounts)
    {
        string path = RECIPE_ASSET_PATH_PREFIX + id + ".asset";
        RecipeData existing = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Recipes");

        RecipeData asset = ScriptableObject.CreateInstance<RecipeData>();
        AssetDatabase.CreateAsset(asset, path);

        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", id);
        SetSerializedField(so, "_recipeName", recipeName);
        SetSerializedField(so, "_basePrice", basePrice);

        // RequiredMaterials 配列を設定
        SerializedProperty matListProp = so.FindProperty("_requiredMaterials");
        if (matListProp != null)
        {
            matListProp.ClearArray();
            int index = 0;
            for (int i = 0; i < materialsAndAmounts.Length; i += 2)
            {
                MaterialData mat = materialsAndAmounts[i] as MaterialData;
                int amount = (int)materialsAndAmounts[i + 1];

                matListProp.InsertArrayElementAtIndex(index);
                SerializedProperty elem = matListProp.GetArrayElementAtIndex(index);
                elem.FindPropertyRelative("Material").objectReferenceValue = mat;
                elem.FindPropertyRelative("Amount").intValue = amount;
                index++;
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    // ══════════════════════════════════════════════
    // ダミー FurnitureData 作成
    // ══════════════════════════════════════════════

    private static FurnitureData EnsureFurnitureDataAsset(
        string id, string furnitureName, FurnitureData.FurnitureType type, int price, float comfortBonus)
    {
        string path = FURNITURE_ASSET_PATH_PREFIX + id + ".asset";
        FurnitureData existing = AssetDatabase.LoadAssetAtPath<FurnitureData>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Furniture");

        FurnitureData asset = ScriptableObject.CreateInstance<FurnitureData>();
        AssetDatabase.CreateAsset(asset, path);

        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", id);
        SetSerializedField(so, "_furnitureName", furnitureName);
        // Enum: use int value
        SerializedProperty typeProp = so.FindProperty("_type");
        if (typeProp != null) typeProp.enumValueIndex = (int)type;
        SetSerializedField(so, "_price", price);
        SetSerializedField(so, "_comfortBonus", comfortBonus);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    // ══════════════════════════════════════════════
    // ヘルパー — ボタン生成
    // ══════════════════════════════════════════════

    /// <summary>Button + Text の子オブジェクトを持つ UI ボタンを生成する。</summary>
    private static GameObject CreateButton(
        string name, string label, Font font,
        Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(btnObj, $"Create {name}");
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = size;
        btnRect.anchoredPosition = anchoredPosition;

        // ── ボタン内テキスト ──
        GameObject textObj = new GameObject("Text");
        Undo.RegisterCreatedObjectUndo(textObj, $"Create {name}/Text");
        textObj.transform.SetParent(btnObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = font;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        return btnObj;
    }

    // ══════════════════════════════════════════════
    // ヘルパー — リフレクション
    // ══════════════════════════════════════════════

    /// <summary>リフレクションで MonoBehaviour のプライベートフィールドに値を設定する。</summary>
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
                $"[ManagementSceneAutoBuilder] {target.GetType().Name} にフィールド '{fieldName}' が見つかりません。");
            return;
        }

        field.SetValue(target, value);

        if (target is Object unityObj)
        {
            EditorUtility.SetDirty(unityObj);
        }
    }

    // ══════════════════════════════════════════════
    // ヘルパー — SerializedObject フィールド設定
    // ══════════════════════════════════════════════

    /// <summary>SerializedObject 経由でフィールドを設定する（float）。</summary>
    private static void SetSerializedField(SerializedObject so, string fieldName, float value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.floatValue = value;
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

    /// <summary>SerializedObject 経由でフィールドを設定する（文字列）。</summary>
    private static void SetSerializedField(SerializedObject so, string fieldName, string value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.stringValue = value;
        }
    }

    // ══════════════════════════════════════════════
    // ヘルパー — フォルダ作成
    // ══════════════════════════════════════════════

    /// <summary>フォルダが存在しなければ再帰的に作成する。</summary>
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

    // ══════════════════════════════════════════════
    // ダミー SkillData 作成
    // ══════════════════════════════════════════════

    private static SkillData EnsureSkillDataAsset(
        string id, string skillName, string description,
        SkillData.SkillType type, int cost, float value, SkillData prerequisite)
    {
        string path = SKILL_ASSET_PATH_PREFIX + id + ".asset";
        SkillData existing = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Skills");

        SkillData asset = ScriptableObject.CreateInstance<SkillData>();
        AssetDatabase.CreateAsset(asset, path);

        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", id);
        SetSerializedField(so, "_skillName", skillName);
        SetSerializedField(so, "_description", description);

        SerializedProperty typeProp = so.FindProperty("_type");
        if (typeProp != null) typeProp.enumValueIndex = (int)type;

        SetSerializedField(so, "_cost", cost);

        SerializedProperty valueProp = so.FindProperty("_value");
        if (valueProp != null) valueProp.floatValue = value;

        if (prerequisite != null)
        {
            SerializedProperty prereqProp = so.FindProperty("_prerequisite");
            if (prereqProp != null) prereqProp.objectReferenceValue = prerequisite;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    // ══════════════════════════════════════════════
    // ダミー EnemyData 作成
    // ══════════════════════════════════════════════

    private static EnemyData EnsureEnemyDataAsset(
        string id, string enemyName, int maxHP, int baseAttack,
        MaterialData dropNormal, MaterialData dropJust)
    {
        string path = ENEMY_ASSET_PATH_PREFIX + id + ".asset";
        EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Enemies");

        EnemyData asset = ScriptableObject.CreateInstance<EnemyData>();
        AssetDatabase.CreateAsset(asset, path);

        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", id);
        SetSerializedField(so, "_enemyName", enemyName);
        SetSerializedField(so, "_maxHP", maxHP);
        SetSerializedField(so, "_baseAttack", baseAttack);

        SerializedProperty dropNormalProp = so.FindProperty("_dropItemNormal");
        if (dropNormalProp != null) dropNormalProp.objectReferenceValue = dropNormal;

        SerializedProperty dropJustProp = so.FindProperty("_dropItemJust");
        if (dropJustProp != null) dropJustProp.objectReferenceValue = dropJust;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    // ══════════════════════════════════════════════
    // ダミー MapData 作成
    // ══════════════════════════════════════════════

    private static MapData EnsureMapDataAsset(
        string id, string mapName, string description,
        MapData.EnvironmentType environment, int requiredShopLevel, int recommendedLevel)
    {
        string path = MAP_ASSET_PATH_PREFIX + id + ".asset";
        MapData existing = AssetDatabase.LoadAssetAtPath<MapData>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Maps");

        MapData asset = ScriptableObject.CreateInstance<MapData>();
        AssetDatabase.CreateAsset(asset, path);

        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", id);
        SetSerializedField(so, "_mapName", mapName);
        SetSerializedField(so, "_description", description);

        SerializedProperty envProp = so.FindProperty("_environment");
        if (envProp != null) envProp.enumValueIndex = (int)environment;

        SetSerializedField(so, "_requiredShopLevel", requiredShopLevel);
        SetSerializedField(so, "_recommendedLevel", recommendedLevel);
        SetSerializedField(so, "_sceneName", "ActionScene");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    // ══════════════════════════════════════════════
    // ダミー WeaponData 作成
    // ══════════════════════════════════════════════

    private static WeaponData EnsureWeaponDataAsset(string id, string name, int price, int baseDamage, int partBreak, int justBonus)
    {
        string path = WEAPON_ASSET_PATH_PREFIX + id + ".asset";
        WeaponData existing = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Weapons");

        WeaponData asset = ScriptableObject.CreateInstance<WeaponData>();
        AssetDatabase.CreateAsset(asset, path);
        SerializedObject so = new SerializedObject(asset);
        SetSerializedField(so, "_id", id);
        SetSerializedField(so, "_weaponName", name);
        SetSerializedField(so, "_price", price);
        SetSerializedField(so, "_baseDamage", baseDamage);
        SetSerializedField(so, "_basePartBreakValue", partBreak);
        SetSerializedField(so, "_justInputFrameBonus", justBonus);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    // ──────────────────────────────────────────────
    // 6f. WeaponShopUI
    // ──────────────────────────────────────────────

    private static void CreateWeaponShopUI(
        GameObject canvasObj,
        WeaponData[] testWeapons,
        Font font)
    {
        // WeaponShopUI コンポーネントのホルダー
        GameObject shopUIObj = new GameObject("WeaponShopUI");
        Undo.RegisterCreatedObjectUndo(shopUIObj, "Create WeaponShopUI");
        shopUIObj.transform.SetParent(canvasObj.transform, false);

        RectTransform shopUIRect = shopUIObj.AddComponent<RectTransform>();
        shopUIRect.anchorMin = Vector2.zero;
        shopUIRect.anchorMax = Vector2.one;
        shopUIRect.sizeDelta = Vector2.zero;

        WeaponShopUI weaponShopUI = shopUIObj.AddComponent<WeaponShopUI>();

        // ── ShopPanel (右側、初期非表示、ダークレッド) ──
        GameObject panelObj = new GameObject("WeaponShopPanel");
        Undo.RegisterCreatedObjectUndo(panelObj, "Create WeaponShopPanel");
        panelObj.transform.SetParent(shopUIObj.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.05f, 0.05f, 0.9f);  // ダークレッド

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.65f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.offsetMin = new Vector2(0f, 20f);
        panelRect.offsetMax = new Vector2(-20f, -20f);

        // ── ショップタイトル ──
        GameObject titleObj = new GameObject("WeaponShopTitle");
        Undo.RegisterCreatedObjectUndo(titleObj, "Create WeaponShopTitle");
        titleObj.transform.SetParent(panelObj.transform, false);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "武器ショップ";
        titleText.fontSize = 28;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.font = font;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 40f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);

        // ── GoldLabel ──
        GameObject goldLabelObj = new GameObject("WeaponGoldLabel");
        Undo.RegisterCreatedObjectUndo(goldLabelObj, "Create WeaponGoldLabel");
        goldLabelObj.transform.SetParent(panelObj.transform, false);

        Text goldLabel = goldLabelObj.AddComponent<Text>();
        goldLabel.text = "所持金: 0 G";
        goldLabel.fontSize = 20;
        goldLabel.color = new Color(1f, 0.9f, 0.3f, 1f);  // ゴールド色
        goldLabel.alignment = TextAnchor.MiddleLeft;
        goldLabel.font = font;

        RectTransform goldLabelRect = goldLabelObj.GetComponent<RectTransform>();
        goldLabelRect.anchorMin = new Vector2(0f, 1f);
        goldLabelRect.anchorMax = new Vector2(1f, 1f);
        goldLabelRect.pivot = new Vector2(0.5f, 1f);
        goldLabelRect.sizeDelta = new Vector2(0f, 30f);
        goldLabelRect.anchoredPosition = new Vector2(0f, -55f);
        goldLabelRect.offsetMin = new Vector2(15f, goldLabelRect.offsetMin.y);

        // ── EquippedLabel ──
        GameObject equippedLabelObj = new GameObject("EquippedLabel");
        Undo.RegisterCreatedObjectUndo(equippedLabelObj, "Create EquippedLabel");
        equippedLabelObj.transform.SetParent(panelObj.transform, false);

        Text equippedLabel = equippedLabelObj.AddComponent<Text>();
        equippedLabel.text = "装備中: ---";
        equippedLabel.fontSize = 18;
        equippedLabel.color = new Color(1f, 0.6f, 0.6f, 0.9f);  // ライトレッド
        equippedLabel.alignment = TextAnchor.MiddleLeft;
        equippedLabel.font = font;

        RectTransform equippedLabelRect = equippedLabelObj.GetComponent<RectTransform>();
        equippedLabelRect.anchorMin = new Vector2(0f, 1f);
        equippedLabelRect.anchorMax = new Vector2(1f, 1f);
        equippedLabelRect.pivot = new Vector2(0.5f, 1f);
        equippedLabelRect.sizeDelta = new Vector2(0f, 25f);
        equippedLabelRect.anchoredPosition = new Vector2(0f, -85f);
        equippedLabelRect.offsetMin = new Vector2(15f, equippedLabelRect.offsetMin.y);

        // ── ButtonContainer (スクロール領域) ──
        GameObject scrollObj = new GameObject("WeaponScrollArea");
        Undo.RegisterCreatedObjectUndo(scrollObj, "Create WeaponScrollArea");
        scrollObj.transform.SetParent(panelObj.transform, false);

        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(10f, 10f);
        scrollRect.offsetMax = new Vector2(-10f, -115f);

        GameObject containerObj = new GameObject("WeaponButtonContainer");
        Undo.RegisterCreatedObjectUndo(containerObj, "Create WeaponButtonContainer");
        containerObj.transform.SetParent(scrollObj.transform, false);

        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        panelObj.SetActive(false);  // 初期非表示

        // ── ToggleButton (画面右下、家具ショップの上にオフセット) ──
        GameObject toggleBtnObj = CreateButton("WeaponShopToggleButton", "武器ショップ", font,
            canvasObj.transform, new Vector2(-120f, -480f), new Vector2(180f, 50f));

        RectTransform toggleRect = toggleBtnObj.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0f);
        toggleRect.anchorMax = new Vector2(1f, 0f);
        toggleRect.pivot = new Vector2(1f, 0f);
        toggleRect.anchoredPosition = new Vector2(-20f, 80f);  // 家具ショップの上

        // ── WeaponShopUI フィールド結線 ──
        SetPrivateFieldViaReflection(weaponShopUI, "_shopWeapons", testWeapons);
        SetPrivateFieldViaReflection(weaponShopUI, "_shopPanel", panelObj);
        SetPrivateFieldViaReflection(weaponShopUI, "_buttonContainer", containerObj.transform);
        SetPrivateFieldViaReflection(weaponShopUI, "_goldLabel", goldLabel);
        SetPrivateFieldViaReflection(weaponShopUI, "_equippedLabel", equippedLabel);
        SetPrivateFieldViaReflection(weaponShopUI, "_toggleButton", toggleBtnObj.GetComponent<Button>());
    }

    // ══════════════════════════════════════════════
    // 8. ProgressionUI（スキルツリー + 店舗拡張）
    // ══════════════════════════════════════════════

    private static void CreateProgressionUI(
        GameObject canvasObj, SkillData[] testSkills, Font font)
    {
        // ── ProgressionUI ルート ──
        GameObject progressionObj = new GameObject("ProgressionUI");
        Undo.RegisterCreatedObjectUndo(progressionObj, "Create ProgressionUI");
        progressionObj.transform.SetParent(canvasObj.transform, false);

        RectTransform rootRect = progressionObj.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        ProgressionUI progressionUI = progressionObj.AddComponent<ProgressionUI>();

        // ── スキルツリーパネル (画面左側) ──
        GameObject skillPanel = new GameObject("SkillPanel");
        Undo.RegisterCreatedObjectUndo(skillPanel, "Create SkillPanel");
        skillPanel.transform.SetParent(progressionObj.transform, false);

        Image skillPanelImage = skillPanel.AddComponent<Image>();
        skillPanelImage.color = new Color(0.1f, 0.05f, 0.2f, 0.9f);   // ダークパープル

        RectTransform skillPanelRect = skillPanel.GetComponent<RectTransform>();
        skillPanelRect.anchorMin = new Vector2(0f, 0f);
        skillPanelRect.anchorMax = new Vector2(0.45f, 1f);
        skillPanelRect.offsetMin = new Vector2(20f, 80f);
        skillPanelRect.offsetMax = new Vector2(0f, -20f);

        // スキルタイトル
        GameObject skillTitleObj = new GameObject("SkillTitle");
        Undo.RegisterCreatedObjectUndo(skillTitleObj, "Create SkillTitle");
        skillTitleObj.transform.SetParent(skillPanel.transform, false);

        Text skillTitleText = skillTitleObj.AddComponent<Text>();
        skillTitleText.text = "スキルツリー";
        skillTitleText.fontSize = 28;
        skillTitleText.color = Color.white;
        skillTitleText.alignment = TextAnchor.UpperCenter;
        skillTitleText.font = font;

        RectTransform skillTitleRect = skillTitleObj.GetComponent<RectTransform>();
        skillTitleRect.anchorMin = new Vector2(0f, 1f);
        skillTitleRect.anchorMax = new Vector2(1f, 1f);
        skillTitleRect.pivot = new Vector2(0.5f, 1f);
        skillTitleRect.sizeDelta = new Vector2(0f, 40f);
        skillTitleRect.anchoredPosition = new Vector2(0f, -10f);

        // スキルボタンコンテナ
        GameObject skillContainer = new GameObject("SkillButtonContainer");
        Undo.RegisterCreatedObjectUndo(skillContainer, "Create SkillButtonContainer");
        skillContainer.transform.SetParent(skillPanel.transform, false);

        RectTransform skillContainerRect = skillContainer.AddComponent<RectTransform>();
        skillContainerRect.anchorMin = new Vector2(0f, 0f);
        skillContainerRect.anchorMax = new Vector2(1f, 1f);
        skillContainerRect.offsetMin = new Vector2(10f, 10f);
        skillContainerRect.offsetMax = new Vector2(-10f, -60f);

        // ── 店舗拡張パネル (画面右側) ──
        GameObject expansionPanel = new GameObject("ExpansionPanel");
        Undo.RegisterCreatedObjectUndo(expansionPanel, "Create ExpansionPanel");
        expansionPanel.transform.SetParent(progressionObj.transform, false);

        Image expansionPanelImage = expansionPanel.AddComponent<Image>();
        expansionPanelImage.color = new Color(0.05f, 0.15f, 0.1f, 0.9f);  // ダークグリーン

        RectTransform expansionPanelRect = expansionPanel.GetComponent<RectTransform>();
        expansionPanelRect.anchorMin = new Vector2(0.55f, 0f);
        expansionPanelRect.anchorMax = new Vector2(1f, 1f);
        expansionPanelRect.offsetMin = new Vector2(0f, 80f);
        expansionPanelRect.offsetMax = new Vector2(-20f, -20f);

        // 店舗レベルラベル
        GameObject shopLevelObj = new GameObject("ShopLevelLabel");
        Undo.RegisterCreatedObjectUndo(shopLevelObj, "Create ShopLevelLabel");
        shopLevelObj.transform.SetParent(expansionPanel.transform, false);

        Text shopLevelText = shopLevelObj.AddComponent<Text>();
        shopLevelText.text = "Lv.1 - ボロ酒場";
        shopLevelText.fontSize = 28;
        shopLevelText.color = Color.white;
        shopLevelText.alignment = TextAnchor.UpperCenter;
        shopLevelText.font = font;

        RectTransform shopLevelRect = shopLevelObj.GetComponent<RectTransform>();
        shopLevelRect.anchorMin = new Vector2(0f, 1f);
        shopLevelRect.anchorMax = new Vector2(1f, 1f);
        shopLevelRect.pivot = new Vector2(0.5f, 1f);
        shopLevelRect.sizeDelta = new Vector2(0f, 40f);
        shopLevelRect.anchoredPosition = new Vector2(0f, -10f);

        // アップグレードコストラベル
        GameObject upgradeCostObj = new GameObject("UpgradeCostLabel");
        Undo.RegisterCreatedObjectUndo(upgradeCostObj, "Create UpgradeCostLabel");
        upgradeCostObj.transform.SetParent(expansionPanel.transform, false);

        Text upgradeCostText = upgradeCostObj.AddComponent<Text>();
        upgradeCostText.text = "次のレベル: 1000G";
        upgradeCostText.fontSize = 22;
        upgradeCostText.color = new Color(1f, 0.9f, 0.3f, 1f);  // ゴールド色
        upgradeCostText.alignment = TextAnchor.MiddleCenter;
        upgradeCostText.font = font;

        RectTransform upgradeCostRect = upgradeCostObj.GetComponent<RectTransform>();
        upgradeCostRect.anchorMin = new Vector2(0f, 1f);
        upgradeCostRect.anchorMax = new Vector2(1f, 1f);
        upgradeCostRect.pivot = new Vector2(0.5f, 1f);
        upgradeCostRect.sizeDelta = new Vector2(0f, 30f);
        upgradeCostRect.anchoredPosition = new Vector2(0f, -60f);

        // アップグレードボタン
        GameObject upgradeBtnObj = new GameObject("UpgradeButton");
        Undo.RegisterCreatedObjectUndo(upgradeBtnObj, "Create UpgradeButton");
        upgradeBtnObj.transform.SetParent(expansionPanel.transform, false);

        Image upgradeBtnImage = upgradeBtnObj.AddComponent<Image>();
        upgradeBtnImage.color = new Color(0.2f, 0.5f, 0.2f, 1f);

        Button upgradeBtn = upgradeBtnObj.AddComponent<Button>();
        upgradeBtn.targetGraphic = upgradeBtnImage;

        RectTransform upgradeBtnRect = upgradeBtnObj.GetComponent<RectTransform>();
        upgradeBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
        upgradeBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
        upgradeBtnRect.sizeDelta = new Vector2(240f, 50f);
        upgradeBtnRect.anchoredPosition = new Vector2(0f, 20f);

        // アップグレードボタンテキスト
        GameObject upgradeBtnTextObj = new GameObject("Text");
        Undo.RegisterCreatedObjectUndo(upgradeBtnTextObj, "Create UpgradeBtnText");
        upgradeBtnTextObj.transform.SetParent(upgradeBtnObj.transform, false);

        Text upgradeBtnText = upgradeBtnTextObj.AddComponent<Text>();
        upgradeBtnText.text = "店舗拡張";
        upgradeBtnText.fontSize = 24;
        upgradeBtnText.color = Color.white;
        upgradeBtnText.alignment = TextAnchor.MiddleCenter;
        upgradeBtnText.font = font;

        RectTransform upgradeBtnTextRect = upgradeBtnTextObj.GetComponent<RectTransform>();
        upgradeBtnTextRect.anchorMin = Vector2.zero;
        upgradeBtnTextRect.anchorMax = Vector2.one;
        upgradeBtnTextRect.sizeDelta = Vector2.zero;

        // ── タブボタン (画面下部) ──
        GameObject skillTabObj = CreateButton("SkillTabButton", "スキルツリー", font,
            progressionObj.transform, new Vector2(-120f, 0f), new Vector2(200f, 50f));
        RectTransform skillTabRect = skillTabObj.GetComponent<RectTransform>();
        skillTabRect.anchorMin = new Vector2(0.3f, 0f);
        skillTabRect.anchorMax = new Vector2(0.3f, 0f);
        skillTabRect.pivot = new Vector2(0.5f, 0f);
        skillTabRect.anchoredPosition = new Vector2(0f, 20f);

        GameObject expansionTabObj = CreateButton("ExpansionTabButton", "店舗拡張", font,
            progressionObj.transform, new Vector2(120f, 0f), new Vector2(200f, 50f));
        RectTransform expansionTabRect = expansionTabObj.GetComponent<RectTransform>();
        expansionTabRect.anchorMin = new Vector2(0.7f, 0f);
        expansionTabRect.anchorMax = new Vector2(0.7f, 0f);
        expansionTabRect.pivot = new Vector2(0.5f, 0f);
        expansionTabRect.anchoredPosition = new Vector2(0f, 20f);

        // ── 所持金ラベル (画面上部中央) ──
        GameObject goldObj = new GameObject("GoldLabel");
        Undo.RegisterCreatedObjectUndo(goldObj, "Create ProgressionGoldLabel");
        goldObj.transform.SetParent(progressionObj.transform, false);

        Text goldText = goldObj.AddComponent<Text>();
        goldText.text = "所持金: 0G";
        goldText.fontSize = 24;
        goldText.color = new Color(1f, 0.9f, 0.3f, 1f);
        goldText.alignment = TextAnchor.MiddleCenter;
        goldText.font = font;

        RectTransform goldRect = goldObj.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0.3f, 1f);
        goldRect.anchorMax = new Vector2(0.7f, 1f);
        goldRect.pivot = new Vector2(0.5f, 1f);
        goldRect.sizeDelta = new Vector2(0f, 40f);
        goldRect.anchoredPosition = new Vector2(0f, 0f);

        // ── ProgressionUI フィールド結線 ──
        SetPrivateFieldViaReflection(progressionUI, "_skillPanel", skillPanel);
        SetPrivateFieldViaReflection(progressionUI, "_skillButtonContainer", skillContainer.transform);
        SetPrivateFieldViaReflection(progressionUI, "_skillTitleLabel", skillTitleText);
        SetPrivateFieldViaReflection(progressionUI, "_expansionPanel", expansionPanel);
        SetPrivateFieldViaReflection(progressionUI, "_shopLevelLabel", shopLevelText);
        SetPrivateFieldViaReflection(progressionUI, "_upgradeCostLabel", upgradeCostText);
        SetPrivateFieldViaReflection(progressionUI, "_upgradeButton", upgradeBtn);
        SetPrivateFieldViaReflection(progressionUI, "_skillTabButton", skillTabObj.GetComponent<Button>());
        SetPrivateFieldViaReflection(progressionUI, "_expansionTabButton", expansionTabObj.GetComponent<Button>());
        SetPrivateFieldViaReflection(progressionUI, "_goldLabel", goldText);

        // ── SkillManager にテストスキルを設定 ──
        // (SkillManager は GameManager.Awake で自動アタッチされるが、
        //  エディタ上での結線のためシーン内に SkillManager を持つ GameManager を検索)
        SkillManager skillManager = Object.FindFirstObjectByType<SkillManager>();
        if (skillManager == null)
        {
            // GameManager オブジェクトに SkillManager を追加
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                skillManager = gm.gameObject.AddComponent<SkillManager>();
            }
        }

        if (skillManager != null)
        {
            SetPrivateFieldViaReflection(skillManager, "_availableSkills", testSkills);
        }

        // ShopExpansionManager を確保
        ShopExpansionManager shopExpansionManager = Object.FindFirstObjectByType<ShopExpansionManager>();
        if (shopExpansionManager == null)
        {
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                shopExpansionManager = gm.gameObject.AddComponent<ShopExpansionManager>();
            }
        }

        // CustomerSpawner を結線
        if (shopExpansionManager != null)
        {
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            if (spawner != null)
            {
                SetPrivateFieldViaReflection(shopExpansionManager, "_customerSpawner", spawner);
            }
        }

        expansionPanel.SetActive(false);  // 初期はスキルパネル表示

        Debug.Log("[ManagementSceneAutoBuilder] ProgressionUI を生成しました（スキルツリー + 店舗拡張）。");
    }

    // ══════════════════════════════════════════════
    // 9. MoneyPopUp + YadaCommentator
    // ══════════════════════════════════════════════

    private static void CreateMoneyPopUpAndYada(
        GameObject dinerManagerObj, GameObject hudCanvas, Font font)
    {
        DinerManager dinerManager = dinerManagerObj.GetComponent<DinerManager>();

        // ── MoneyPopUp 用ワールドCanvas ──
        GameObject worldCanvasObj = new GameObject("WorldCanvas_MoneyPopUp");
        Undo.RegisterCreatedObjectUndo(worldCanvasObj, "Create WorldCanvas_MoneyPopUp");

        Canvas worldCanvas = worldCanvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        worldCanvas.sortingOrder = 10; // 通常HUDより前面

        CanvasScaler scaler = worldCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // MoneyPopUp コンポーネント（Diner Manager に追加）
        MoneyPopUp moneyPopUp = dinerManagerObj.AddComponent<MoneyPopUp>();
        SetPrivateFieldViaReflection(moneyPopUp, "_worldCanvas", worldCanvas);

        // DinerManager に MoneyPopUp を結線
        if (dinerManager != null)
        {
            SetPrivateFieldViaReflection(dinerManager, "_moneyPopUp", moneyPopUp);
        }

        // MidnightResultUI を DinerManager に結線
        MidnightResultUI midnightResultUI = Object.FindFirstObjectByType<MidnightResultUI>();
        if (dinerManager != null && midnightResultUI != null)
        {
            SetPrivateFieldViaReflection(dinerManager, "_midnightResultUI", midnightResultUI);
        }

        // ── YadaCommentator ──
        GameObject yadaObj = new GameObject("--- Yada Commentator ---");
        Undo.RegisterCreatedObjectUndo(yadaObj, "Create YadaCommentator");

        YadaCommentator yada = yadaObj.AddComponent<YadaCommentator>();

        // 吹き出しUI（画面左下）
        GameObject bubbleObj = new GameObject("YadaSpeechBubble");
        Undo.RegisterCreatedObjectUndo(bubbleObj, "Create YadaSpeechBubble");
        bubbleObj.transform.SetParent(hudCanvas.transform, false);

        Image bubbleImage = bubbleObj.AddComponent<Image>();
        bubbleImage.color = new Color(0.15f, 0.1f, 0.25f, 0.9f);

        RectTransform bubbleRect = bubbleObj.GetComponent<RectTransform>();
        bubbleRect.anchorMin = new Vector2(0f, 0f);
        bubbleRect.anchorMax = new Vector2(0f, 0f);
        bubbleRect.pivot = new Vector2(0f, 0f);
        bubbleRect.anchoredPosition = new Vector2(20f, 20f);
        bubbleRect.sizeDelta = new Vector2(350f, 60f);

        // コメントテキスト
        GameObject commentTextObj = new GameObject("YadaCommentText");
        Undo.RegisterCreatedObjectUndo(commentTextObj, "Create YadaCommentText");
        commentTextObj.transform.SetParent(bubbleObj.transform, false);

        Text commentText = commentTextObj.AddComponent<Text>();
        commentText.text = "";
        commentText.fontSize = 22;
        commentText.color = Color.white;
        commentText.alignment = TextAnchor.MiddleLeft;
        commentText.font = font;

        RectTransform commentTextRect = commentTextObj.GetComponent<RectTransform>();
        commentTextRect.anchorMin = Vector2.zero;
        commentTextRect.anchorMax = Vector2.one;
        commentTextRect.offsetMin = new Vector2(50f, 5f);  // カラスアイコン分のオフセット
        commentTextRect.offsetMax = new Vector2(-10f, -5f);

        // カラスラベル（簡易アイコン代替）
        GameObject crowLabelObj = new GameObject("CrowLabel");
        Undo.RegisterCreatedObjectUndo(crowLabelObj, "Create CrowLabel");
        crowLabelObj.transform.SetParent(bubbleObj.transform, false);

        Text crowLabel = crowLabelObj.AddComponent<Text>();
        crowLabel.text = "矢田";
        crowLabel.fontSize = 16;
        crowLabel.color = new Color(0.8f, 0.6f, 1f, 1f);
        crowLabel.alignment = TextAnchor.MiddleCenter;
        crowLabel.font = font;

        RectTransform crowLabelRect = crowLabelObj.GetComponent<RectTransform>();
        crowLabelRect.anchorMin = new Vector2(0f, 0f);
        crowLabelRect.anchorMax = new Vector2(0f, 1f);
        crowLabelRect.pivot = new Vector2(0f, 0.5f);
        crowLabelRect.anchoredPosition = new Vector2(5f, 0f);
        crowLabelRect.sizeDelta = new Vector2(40f, 0f);

        bubbleObj.SetActive(false); // 初期非表示

        // YadaCommentator フィールド結線
        SetPrivateFieldViaReflection(yada, "_speechBubble", bubbleObj);
        SetPrivateFieldViaReflection(yada, "_commentText", commentText);
        SetPrivateFieldViaReflection(yada, "_dinerManager", dinerManager);

        Debug.Log("[ManagementSceneAutoBuilder] MoneyPopUp + YadaCommentator を生成しました。");
    }
}
