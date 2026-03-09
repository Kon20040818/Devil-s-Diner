// ============================================================
// SampleDataGenerator.cs
// メニュー「DevilsDiner > Generate Sample Staff & Calendar Data」で
// StaffBuffData / StaffRaceData / CalendarEventData を一括生成する。
// 既存 EnemyData への StaffRace 紐付けも行う。
// ============================================================
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// テスト・デバッグ用サンプルデータを一括生成するエディタツール。
/// </summary>
public static class SampleDataGenerator
{
    private const string BUFF_DIR      = "Assets/Data/StaffBuffs";
    private const string RACE_DIR      = "Assets/Data/StaffRaces";
    private const string CAL_DIR       = "Assets/Data/CalendarEvents";
    private const string ENEMY_DIR     = "Assets/Data/Enemies";
    private const string FURNITURE_DIR = "Assets/Data/Furniture";
    private const string DISH_DIR       = "Assets/Data/Dishes";
    private const string RECIPE_DIR    = "Assets/Data/Recipes";
    private const string QUALITY_DIR   = "Assets/Data";
    private const string MATERIAL_DIR  = "Assets/Data/Materials";
    private const string INGREDIENT_DIR = "Assets/Data/Ingredients";

    [MenuItem("DevilsDiner/Generate Sample Staff && Calendar Data")]
    public static void Generate()
    {
        EnsureDirectory(BUFF_DIR);
        EnsureDirectory(RACE_DIR);
        EnsureDirectory(CAL_DIR);

        // ── StaffBuffData 生成 ──
        var buffs = GenerateBuffs();

        // ── StaffRaceData 生成 ──
        var races = GenerateRaces(buffs);

        // ── CalendarEventData 生成 ──
        GenerateCalendarEvents();

        // ── FurnitureData 生成 ──
        GenerateFurniture();

        // ── EnemyData に StaffRace 紐付け ──
        WireEnemyRaces(races);

        // ── IngredientData 生成（MAT_* を ING_* に移行）──
        var ingredients = GenerateIngredients();

        // ── QualityScaleTable 生成 ──
        var qualityTable = GenerateQualityScaleTable();

        // ── DishData 生成 ──
        var dishes = GenerateDishes(qualityTable);

        // ── RecipeData 生成（新スキーマ）──
        GenerateRecipes(dishes, ingredients);

        // ── EnemyData ドロップ枠に IngredientData を結線 ──
        WireEnemyDrops(ingredients);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SampleDataGenerator] サンプルデータ生成完了！");
    }

    // ──────────────────────────────────────────────
    // StaffBuffData
    // ──────────────────────────────────────────────

    private static StaffBuffData[] GenerateBuffs()
    {
        var definitions = new[]
        {
            new BuffDef("SBUF_QualityUp",       "品質向上",       StaffBuffType.QualityBonus,       0.15f, 1, DishCategory.Meat,  false),
            new BuffDef("SBUF_SatisfactionUp",  "接客上手",       StaffBuffType.SatisfactionBonus,  0.10f, 1, DishCategory.Meat,  false),
            new BuffDef("SBUF_FreshnessUp",     "鮮度管理",       StaffBuffType.FreshnessBonus,     0.10f, 2, DishCategory.Meat,  false),
            new BuffDef("SBUF_CookSpeed",       "手際良い",       StaffBuffType.CookSpeed,          0.20f, 2, DishCategory.Meat,  false),
            new BuffDef("SBUF_SalaryDown",      "質素",           StaffBuffType.SalaryReduction,    0.15f, 3, DishCategory.Meat,  false),
            new BuffDef("SBUF_MeatSpecialty",   "肉料理の達人",   StaffBuffType.CategorySpecialty,  0.25f, 3, DishCategory.Meat,  true),
            new BuffDef("SBUF_SaladSpecialty",  "サラダ職人",     StaffBuffType.CategorySpecialty,  0.25f, 3, DishCategory.Salad, true),
            new BuffDef("SBUF_SuperQuality",    "超絶品質",       StaffBuffType.QualityBonus,       0.30f, 5, DishCategory.Meat,  false),
        };

        var results = new StaffBuffData[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            results[i] = CreateBuff(definitions[i]);
        }
        return results;
    }

    private static StaffBuffData CreateBuff(BuffDef def)
    {
        string path = $"{BUFF_DIR}/{def.Id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<StaffBuffData>(path);
        if (existing != null)
        {
            Debug.Log($"[SampleDataGenerator] 既存スキップ: {path}");
            return existing;
        }

        var asset = ScriptableObject.CreateInstance<StaffBuffData>();
        var so = new SerializedObject(asset);
        so.FindProperty("_buffID").stringValue = def.Id;
        so.FindProperty("_displayName").stringValue = def.DisplayName;
        so.FindProperty("_description").stringValue = $"{def.DisplayName}の効果（R{def.Rarity}）";
        so.FindProperty("_type").enumValueIndex = (int)def.Type;
        so.FindProperty("_value").floatValue = def.Value;
        so.FindProperty("_rarity").intValue = def.Rarity;
        if (def.UseCategory)
        {
            so.FindProperty("_targetCategory").enumValueIndex = (int)def.Category;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[SampleDataGenerator] 生成: {path}");
        return asset;
    }

    // ──────────────────────────────────────────────
    // StaffRaceData
    // ──────────────────────────────────────────────

    private static StaffRaceData[] GenerateRaces(StaffBuffData[] buffs)
    {
        // バフ配列インデックス: 0=QualityUp, 1=SatisfactionUp, 2=FreshnessUp, 3=CookSpeed,
        //                       4=SalaryDown, 5=MeatSpecialty, 6=SaladSpecialty, 7=SuperQuality

        var definitions = new[]
        {
            new RaceDef("RACE_Cactus",      "サボテン族",   StaffFixedEffect.SatisfactionUp, 0.10f, 40,
                        new[]{ buffs[1], buffs[3], buffs[4] }, 1, 2),
            new RaceDef("RACE_Boss",        "ボス族",       StaffFixedEffect.QualityUp,      0.15f, 80,
                        new[]{ buffs[0], buffs[2], buffs[5], buffs[7] }, 1, 3),
            new RaceDef("RACE_DragonLord",  "竜族",         StaffFixedEffect.QualityUp,      0.20f, 100,
                        new[]{ buffs[0], buffs[5], buffs[7] }, 2, 3),
            new RaceDef("RACE_PoisonHydra", "ヒドラ族",     StaffFixedEffect.CookSpeedUp,    0.15f, 60,
                        new[]{ buffs[2], buffs[3], buffs[6] }, 1, 2),
            new RaceDef("RACE_Dummy",       "ダミー族",     StaffFixedEffect.SalaryDiscount, 0.20f, 20,
                        new[]{ buffs[1], buffs[4] }, 1, 1),
        };

        var results = new StaffRaceData[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            results[i] = CreateRace(definitions[i]);
        }
        return results;
    }

    private static StaffRaceData CreateRace(RaceDef def)
    {
        string path = $"{RACE_DIR}/{def.Id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<StaffRaceData>(path);
        if (existing != null)
        {
            // 既存でも PossibleBuffs を更新
            var so2 = new SerializedObject(existing);
            SetBuffArray(so2, def.PossibleBuffs);
            so2.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[SampleDataGenerator] 更新: {path}");
            return existing;
        }

        var asset = ScriptableObject.CreateInstance<StaffRaceData>();
        var so = new SerializedObject(asset);
        so.FindProperty("_raceID").stringValue = def.Id;
        so.FindProperty("_raceName").stringValue = def.RaceName;
        so.FindProperty("_fixedEffect").enumValueIndex = (int)def.FixedEffect;
        so.FindProperty("_fixedEffectValue").floatValue = def.FixedEffectValue;
        so.FindProperty("_baseSalary").intValue = def.BaseSalary;
        so.FindProperty("_minBuffCount").intValue = def.MinBuff;
        so.FindProperty("_maxBuffCount").intValue = def.MaxBuff;
        SetBuffArray(so, def.PossibleBuffs);
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[SampleDataGenerator] 生成: {path}");
        return asset;
    }

    private static void SetBuffArray(SerializedObject so, StaffBuffData[] buffs)
    {
        var prop = so.FindProperty("_possibleBuffs");
        prop.arraySize = buffs.Length;
        for (int i = 0; i < buffs.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = buffs[i];
        }
    }

    // ──────────────────────────────────────────────
    // CalendarEventData
    // ──────────────────────────────────────────────

    private static void GenerateCalendarEvents()
    {
        CreateCalendarEvent("CAL_MeatDay",     "肉の日",       "肉料理の売上と品質がアップ！",
                            new[]{ 3, 10, 17 }, true, DishCategory.Meat, 1.5f, 1.3f);
        CreateCalendarEvent("CAL_HarvestFest", "収穫祭",       "サラダ系の満足度が大幅アップ！",
                            new[]{ 7, 14 },     true, DishCategory.Salad, 1.8f, 1.2f);
        CreateCalendarEvent("CAL_GourmetWeek", "グルメウィーク", "全カテゴリの品質がアップ！",
                            new[]{ 5, 12, 19 }, false, DishCategory.Meat, 1.3f, 1.5f);
    }

    private static void CreateCalendarEvent(
        string id, string eventName, string desc,
        int[] triggerDays, bool categoryEnabled, DishCategory category,
        float satisfactionMul, float freshnessMul)
    {
        string path = $"{CAL_DIR}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<CalendarEventData>(path) != null)
        {
            Debug.Log($"[SampleDataGenerator] 既存スキップ: {path}");
            return;
        }

        var asset = ScriptableObject.CreateInstance<CalendarEventData>();
        var so = new SerializedObject(asset);
        so.FindProperty("_eventID").stringValue = id;
        so.FindProperty("_eventName").stringValue = eventName;
        so.FindProperty("_description").stringValue = desc;

        var daysProp = so.FindProperty("_triggerDays");
        daysProp.arraySize = triggerDays.Length;
        for (int i = 0; i < triggerDays.Length; i++)
        {
            daysProp.GetArrayElementAtIndex(i).intValue = triggerDays[i];
        }

        so.FindProperty("_bonusCategoryEnabled").boolValue = categoryEnabled;
        so.FindProperty("_bonusCategory").enumValueIndex = (int)category;
        so.FindProperty("_satisfactionMultiplier").floatValue = satisfactionMul;
        so.FindProperty("_freshnessMultiplier").floatValue = freshnessMul;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[SampleDataGenerator] 生成: {path}");
    }

    // ──────────────────────────────────────────────
    // FurnitureData
    // ──────────────────────────────────────────────

    private static void GenerateFurniture()
    {
        EnsureDirectory(FURNITURE_DIR);

        CreateFurniture("FRN_Table",          "テーブルセット",   "席を増やして来客数アップ",
                        FurnitureData.FurnitureType.Table, 200, 0.1f, 1);
        CreateFurniture("FRN_Decoration",     "装飾品",           "おしゃれな内装で満足度アップ",
                        FurnitureData.FurnitureType.Decoration, 500, 0.2f, 0);
        CreateFurniture("FRN_KitchenUpgrade", "厨房改修",         "設備一新で大幅ボーナス",
                        FurnitureData.FurnitureType.Kitchen, 1000, 0.3f, 2);
    }

    private static void CreateFurniture(
        string id, string furnitureName, string desc,
        FurnitureData.FurnitureType type, int price,
        float satisfactionBonus, int customerBonus)
    {
        string path = $"{FURNITURE_DIR}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<FurnitureData>(path) != null)
        {
            Debug.Log($"[SampleDataGenerator] 既存スキップ: {path}");
            return;
        }

        var asset = ScriptableObject.CreateInstance<FurnitureData>();
        var so = new SerializedObject(asset);
        so.FindProperty("_id").stringValue = id;
        so.FindProperty("_furnitureName").stringValue = furnitureName;
        so.FindProperty("_description").stringValue = desc;
        so.FindProperty("_type").enumValueIndex = (int)type;
        so.FindProperty("_price").intValue = price;
        so.FindProperty("_satisfactionBonus").floatValue = satisfactionBonus;
        so.FindProperty("_customerBonus").intValue = customerBonus;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[SampleDataGenerator] 生成: {path}");
    }

    // ──────────────────────────────────────────────
    // EnemyData 紐付け
    // ──────────────────────────────────────────────

    private static void WireEnemyRaces(StaffRaceData[] races)
    {
        // EnemyData 名 → StaffRaceData のマッピング
        var mapping = new (string enemyFile, string raceId)[]
        {
            ("ENM_Cactus",      "RACE_Cactus"),
            ("ENM_Boss",        "RACE_Boss"),
            ("ENM_DragonLord",  "RACE_DragonLord"),
            ("ENM_PoisonHydra", "RACE_PoisonHydra"),
            ("ENM_Dummy",       "RACE_Dummy"),
        };

        foreach (var (enemyFile, raceId) in mapping)
        {
            string enemyPath = $"{ENEMY_DIR}/{enemyFile}.asset";
            var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(enemyPath);
            if (enemyData == null)
            {
                Debug.LogWarning($"[SampleDataGenerator] EnemyData が見つかりません: {enemyPath}");
                continue;
            }

            string racePath = $"{RACE_DIR}/{raceId}.asset";
            var raceData = AssetDatabase.LoadAssetAtPath<StaffRaceData>(racePath);
            if (raceData == null)
            {
                Debug.LogWarning($"[SampleDataGenerator] StaffRaceData が見つかりません: {racePath}");
                continue;
            }

            var so = new SerializedObject(enemyData);
            so.FindProperty("_staffRace").objectReferenceValue = raceData;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemyData);
            Debug.Log($"[SampleDataGenerator] {enemyFile} → {raceId} 紐付け完了");
        }
    }

    // ──────────────────────────────────────────────
    // IngredientData（素材アイテム生成）
    // ──────────────────────────────────────────────

    /// <summary>
    /// IngredientData アセットを生成する。
    /// 旧 MaterialData (MAT_*) のパラメータを引き継ぎつつ、
    /// ItemData ベースの IngredientData (ING_*) として再生成する。
    /// </summary>
    private static IngredientData[] GenerateIngredients()
    {
        EnsureDirectory(INGREDIENT_DIR);

        var definitions = new[]
        {
            new IngDef("ING_Meat",      "生肉",     "新鮮な肉。ステーキやシチューに。",         1, 0.80f, 1.0f, 20),
            new IngDef("ING_Vegetable", "野菜",     "みずみずしい野菜。サラダやシチューに。",   1, 0.90f, 0.8f, 15),
            new IngDef("ING_Bone",      "骨",       "硬い骨。出汁を取れる珍しい素材。",         2, 0.60f, 1.2f, 30),
            new IngDef("ING_Scale",     "鱗",       "竜の鱗。最高級の調味素材。",               3, 0.40f, 1.5f, 80),
            new IngDef("ING_Poison",    "毒腺",     "毒蛇の毒腺。独特の風味を生み出す。",       3, 0.50f, 1.3f, 60),
            new IngDef("ING_Herb",      "薬草",     "森に自生する薬草。デザートの隠し味に。",   2, 0.70f, 0.9f, 25),
        };

        var results = new IngredientData[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            results[i] = CreateIngredient(definitions[i]);
        }
        return results;
    }

    private static IngredientData CreateIngredient(IngDef def)
    {
        string path = $"{INGREDIENT_DIR}/{def.Id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
        if (existing != null)
        {
            Debug.Log($"[SampleDataGenerator] 既存スキップ: {path}");
            return existing;
        }

        var asset = ScriptableObject.CreateInstance<IngredientData>();
        var so = new SerializedObject(asset);
        // ItemData 基底フィールド
        so.FindProperty("_itemID").stringValue = def.Id;
        so.FindProperty("_displayName").stringValue = def.DisplayName;
        so.FindProperty("_description").stringValue = def.Description;
        so.FindProperty("_sellPrice").intValue = def.SellPrice;
        // IngredientData 固有フィールド
        so.FindProperty("_rarity").intValue = def.Rarity;
        so.FindProperty("_dropRate").floatValue = def.DropRate;
        so.FindProperty("_gaugeSpeedMultiplier").floatValue = def.GaugeSpeedMultiplier;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[SampleDataGenerator] 生成: {path}");
        return asset;
    }

    // ──────────────────────────────────────────────
    // EnemyData ドロップ結線
    // ──────────────────────────────────────────────

    /// <summary>
    /// EnemyData の _dropItemNormal / _dropItemJust に IngredientData を結線する。
    /// </summary>
    private static void WireEnemyDrops(IngredientData[] ingredients)
    {
        // ingredients: 0=Meat, 1=Vegetable, 2=Bone, 3=Scale, 4=Poison, 5=Herb

        var mapping = new (string enemyFile, int normalIdx, int justIdx)[]
        {
            ("ENM_Cactus",      1, 5),  // 通常: 野菜,     ジャスト: 薬草
            ("ENM_Boss",        0, 2),  // 通常: 生肉,     ジャスト: 骨
            ("ENM_DragonLord",  0, 3),  // 通常: 生肉,     ジャスト: 鱗
            ("ENM_PoisonHydra", 2, 4),  // 通常: 骨,       ジャスト: 毒腺
            ("ENM_Dummy",       1, 0),  // 通常: 野菜,     ジャスト: 生肉
        };

        foreach (var (enemyFile, normalIdx, justIdx) in mapping)
        {
            string enemyPath = $"{ENEMY_DIR}/{enemyFile}.asset";
            var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(enemyPath);
            if (enemyData == null)
            {
                Debug.LogWarning($"[SampleDataGenerator] EnemyData が見つかりません: {enemyPath}");
                continue;
            }

            var so = new SerializedObject(enemyData);
            so.FindProperty("_dropItemNormal").objectReferenceValue = ingredients[normalIdx];
            so.FindProperty("_dropItemJust").objectReferenceValue = ingredients[justIdx];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemyData);
            Debug.Log($"[SampleDataGenerator] {enemyFile} ドロップ結線: 通常={ingredients[normalIdx].DisplayName}, ジャスト={ingredients[justIdx].DisplayName}");
        }
    }

    // ──────────────────────────────────────────────
    // QualityScaleTable
    // ──────────────────────────────────────────────

    private static QualityScaleTable GenerateQualityScaleTable()
    {
        string path = $"{QUALITY_DIR}/QualityScaleTable.asset";
        var existing = AssetDatabase.LoadAssetAtPath<QualityScaleTable>(path);
        if (existing != null)
        {
            Debug.Log($"[SampleDataGenerator] 既存スキップ: {path}");
            return existing;
        }

        EnsureDirectory(QUALITY_DIR);
        var asset = ScriptableObject.CreateInstance<QualityScaleTable>();
        // デフォルトのコンストラクタで適切な倍率が設定済み
        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[SampleDataGenerator] 生成: {path}");
        return asset;
    }

    // ──────────────────────────────────────────────
    // DishData
    // ──────────────────────────────────────────────

    private static DishData[] GenerateDishes(QualityScaleTable qualityTable)
    {
        EnsureDirectory(DISH_DIR);

        var definitions = new[]
        {
            new DishDef("DISH_Steak",       "ステーキ",           "ジューシーな肉料理。攻撃力アップ。",
                        DishCategory.Meat,   80, 0.15f, 3, 0.05f, 150, 60, 5f),
            new DishDef("DISH_Salad",        "サラダ",             "新鮮な野菜サラダ。防御力アップ。",
                        DishCategory.Salad,  40, 0.10f, 3, 0.03f, 80,  40, 3f),
            new DishDef("DISH_Stew",         "シチュー",           "温かいシチュー。攻撃力アップ。",
                        DishCategory.Meat,   60, 0.12f, 4, 0.04f, 200, 55, 6f),
            new DishDef("DISH_DragonSteak",  "竜王のステーキ",     "最高級の一品。攻撃力大幅アップ。",
                        DishCategory.Meat,  120, 0.25f, 5, 0.08f, 500, 90, 8f),
            new DishDef("DISH_PoisonStew",   "毒蛇のシチュー",     "独特の風味のシチュー。速度アップ。",
                        DishCategory.Fish,   70, 0.18f, 4, 0.06f, 350, 70, 7f),
            new DishDef("DISH_Pudding",      "プリン",             "甘いデザート。毎ターンHP回復。",
                        DishCategory.Dessert, 30, 0.08f, 5, 0.02f, 120, 65, 4f),
        };

        var results = new DishData[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            results[i] = CreateDish(definitions[i], qualityTable);
        }
        return results;
    }

    private static DishData CreateDish(DishDef def, QualityScaleTable qualityTable)
    {
        string path = $"{DISH_DIR}/{def.Id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DishData>(path);
        if (existing != null)
        {
            Debug.Log($"[SampleDataGenerator] 既存スキップ: {path}");
            return existing;
        }

        var asset = ScriptableObject.CreateInstance<DishData>();
        var so = new SerializedObject(asset);
        // ItemData 基底フィールド
        so.FindProperty("_itemID").stringValue = def.Id;
        so.FindProperty("_displayName").stringValue = def.DisplayName;
        so.FindProperty("_description").stringValue = def.Description;
        so.FindProperty("_sellPrice").intValue = def.ShopPrice / 2;
        // DishData 固有フィールド
        so.FindProperty("_category").enumValueIndex = (int)def.Category;
        so.FindProperty("_hpRecoveryAmount").intValue = def.HpRecovery;
        so.FindProperty("_baseBuff").floatValue = def.BaseBuff;
        so.FindProperty("_buffDurationTurns").intValue = def.BuffDuration;
        so.FindProperty("_scoutBonus").floatValue = def.ScoutBonus;
        so.FindProperty("_shopPrice").intValue = def.ShopPrice;
        so.FindProperty("_baseSatisfaction").intValue = def.Satisfaction;
        so.FindProperty("_servingTime").floatValue = def.ServingTime;
        so.FindProperty("_qualityTable").objectReferenceValue = qualityTable;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[SampleDataGenerator] 生成: {path}");
        return asset;
    }

    // ──────────────────────────────────────────────
    // RecipeData（新スキーマ）
    // ──────────────────────────────────────────────

    private static void GenerateRecipes(DishData[] dishes, IngredientData[] ingredients)
    {
        EnsureDirectory(RECIPE_DIR);

        // ingredients: 0=Meat, 1=Vegetable, 2=Bone, 3=Scale, 4=Poison, 5=Herb
        var meat = ingredients[0];
        var veg  = ingredients[1];
        var bone = ingredients[2];
        var scale = ingredients[3];
        var poison = ingredients[4];
        var herb = ingredients[5];

        // dishes: 0=Steak, 1=Salad, 2=Stew, 3=DragonSteak, 4=PoisonStew, 5=Pudding
        CreateRecipe("RCP_Steak",       "ステーキ",         "肉を焼いた定番料理。",
                     dishes[0], new IngSlot[]{ new IngSlot(meat, 2) }, 1);
        CreateRecipe("RCP_Salad",       "サラダ",           "新鮮な野菜のサラダ。",
                     dishes[1], new IngSlot[]{ new IngSlot(veg, 2) }, 1);
        CreateRecipe("RCP_Stew",        "シチュー",         "肉と野菜の煮込み。",
                     dishes[2], new IngSlot[]{ new IngSlot(meat, 1), new IngSlot(veg, 1) }, 1);
        CreateRecipe("RCP_DragonSteak", "竜王のステーキ",   "最上級の鱗付き肉ステーキ。",
                     dishes[3], new IngSlot[]{ new IngSlot(meat, 3), new IngSlot(scale, 1) }, 3);
        CreateRecipe("RCP_PoisonStew",  "毒蛇のシチュー",   "毒素が旨味に変わる一品。",
                     dishes[4], new IngSlot[]{ new IngSlot(bone, 2), new IngSlot(poison, 1) }, 2);
        CreateRecipe("RCP_Pudding",     "プリン",           "甘くて優しいデザート。",
                     dishes[5], new IngSlot[]{ new IngSlot(veg, 1), new IngSlot(herb, 1) }, 1);
    }

    private static void CreateRecipe(
        string id, string displayName, string description,
        DishData outputDish, IngSlot[] ingredients, int requiredChefLevel)
    {
        string path = $"{RECIPE_DIR}/{id}.asset";

        // 既存の旧スキーマアセットを削除して新スキーマで再生成
        var existing = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
            Debug.Log($"[SampleDataGenerator] 旧レシピ削除: {path}");
        }

        var asset = ScriptableObject.CreateInstance<RecipeData>();
        var so = new SerializedObject(asset);
        so.FindProperty("_recipeID").stringValue = id;
        so.FindProperty("_displayName").stringValue = displayName;
        so.FindProperty("_description").stringValue = description;
        so.FindProperty("_outputDish").objectReferenceValue = outputDish;
        so.FindProperty("_requiredChefLevel").intValue = requiredChefLevel;

        var ingProp = so.FindProperty("_ingredients");
        ingProp.arraySize = ingredients.Length;
        for (int i = 0; i < ingredients.Length; i++)
        {
            var element = ingProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Ingredient").objectReferenceValue = ingredients[i].Data;
            element.FindPropertyRelative("Amount").intValue = ingredients[i].Amount;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[SampleDataGenerator] 生成: {path}");
    }

    // ──────────────────────────────────────────────
    // ヘルパー
    // ──────────────────────────────────────────────

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    // ──────────────────────────────────────────────
    // 内部データ定義
    // ──────────────────────────────────────────────

    private struct BuffDef
    {
        public string Id;
        public string DisplayName;
        public StaffBuffType Type;
        public float Value;
        public int Rarity;
        public DishCategory Category;
        public bool UseCategory;

        public BuffDef(string id, string displayName, StaffBuffType type,
                       float value, int rarity, DishCategory category, bool useCategory)
        {
            Id = id;
            DisplayName = displayName;
            Type = type;
            Value = value;
            Rarity = rarity;
            Category = category;
            UseCategory = useCategory;
        }
    }

    private struct RaceDef
    {
        public string Id;
        public string RaceName;
        public StaffFixedEffect FixedEffect;
        public float FixedEffectValue;
        public int BaseSalary;
        public StaffBuffData[] PossibleBuffs;
        public int MinBuff;
        public int MaxBuff;

        public RaceDef(string id, string raceName, StaffFixedEffect fixedEffect,
                       float fixedEffectValue, int baseSalary,
                       StaffBuffData[] possibleBuffs, int minBuff, int maxBuff)
        {
            Id = id;
            RaceName = raceName;
            FixedEffect = fixedEffect;
            FixedEffectValue = fixedEffectValue;
            BaseSalary = baseSalary;
            PossibleBuffs = possibleBuffs;
            MinBuff = minBuff;
            MaxBuff = maxBuff;
        }
    }

    private struct DishDef
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public DishCategory Category;
        public int HpRecovery;
        public float BaseBuff;
        public int BuffDuration;
        public float ScoutBonus;
        public int ShopPrice;
        public int Satisfaction;
        public float ServingTime;

        public DishDef(string id, string displayName, string description,
                       DishCategory category, int hpRecovery, float baseBuff,
                       int buffDuration, float scoutBonus, int shopPrice,
                       int satisfaction, float servingTime)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Category = category;
            HpRecovery = hpRecovery;
            BaseBuff = baseBuff;
            BuffDuration = buffDuration;
            ScoutBonus = scoutBonus;
            ShopPrice = shopPrice;
            Satisfaction = satisfaction;
            ServingTime = servingTime;
        }
    }

    private struct IngSlot
    {
        public IngredientData Data;
        public int Amount;
        public IngSlot(IngredientData data, int amount) { Data = data; Amount = amount; }
    }

    private struct IngDef
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public int Rarity;
        public float DropRate;
        public float GaugeSpeedMultiplier;
        public int SellPrice;

        public IngDef(string id, string displayName, string description,
                      int rarity, float dropRate, float gaugeSpeedMultiplier, int sellPrice)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Rarity = rarity;
            DropRate = dropRate;
            GaugeSpeedMultiplier = gaugeSpeedMultiplier;
            SellPrice = sellPrice;
        }
    }
}
#endif
