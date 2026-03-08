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
    private const string BUFF_DIR   = "Assets/Data/StaffBuffs";
    private const string RACE_DIR   = "Assets/Data/StaffRaces";
    private const string CAL_DIR    = "Assets/Data/CalendarEvents";
    private const string ENEMY_DIR  = "Assets/Data/Enemies";

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

        // ── EnemyData に StaffRace 紐付け ──
        WireEnemyRaces(races);

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
}
#endif
