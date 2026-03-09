// ============================================================
// MasterDataImporter.cs
// Assets/MasterData/ 配下の CSV / JSON ファイルを読み込み、
// ScriptableObject アセットを自動生成・更新するエディタツール。
//
// メニュー: DevilsDiner > Import Master Data (CSV/JSON)
//
// 対応ファイル:
//   ingredients.csv    → IngredientData
//   dishes.csv         → DishData
//   recipes.json       → RecipeData
//   furniture.csv      → FurnitureData
//   characters.csv     → CharacterStats
//   enemies.csv        → EnemyData
//   weapons.csv        → WeaponData
//   staff_buffs.csv    → StaffBuffData
//   staff_races.json   → StaffRaceData
//   calendar_events.csv → CalendarEventData
// ============================================================
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CSV / JSON からマスターデータを ScriptableObject にインポートするエディタツール。
/// データの追加・修正は MasterData/ フォルダのファイルを編集するだけで済む。
/// </summary>
public static class MasterDataImporter
{
    // ── 入力パス ──
    private const string MASTER_DIR        = "Assets/MasterData";
    private const string ING_CSV           = MASTER_DIR + "/ingredients.csv";
    private const string DISH_CSV          = MASTER_DIR + "/dishes.csv";
    private const string RECIPE_JSON       = MASTER_DIR + "/recipes.json";
    private const string FURNITURE_CSV     = MASTER_DIR + "/furniture.csv";
    private const string CHAR_CSV          = MASTER_DIR + "/characters.csv";
    private const string ENEMY_CSV         = MASTER_DIR + "/enemies.csv";
    private const string WEAPON_CSV        = MASTER_DIR + "/weapons.csv";
    private const string STAFF_BUFF_CSV    = MASTER_DIR + "/staff_buffs.csv";
    private const string STAFF_RACE_JSON   = MASTER_DIR + "/staff_races.json";
    private const string CALENDAR_CSV      = MASTER_DIR + "/calendar_events.csv";

    // ── 出力パス ──
    private const string DATA_ROOT      = "Assets/Resources/Data";
    private const string ING_DIR        = DATA_ROOT + "/Ingredients";
    private const string DISH_DIR       = DATA_ROOT + "/Dishes";
    private const string RECIPE_DIR     = DATA_ROOT + "/Recipes";
    private const string FURNITURE_DIR  = DATA_ROOT + "/Furniture";
    private const string QUALITY_DIR    = DATA_ROOT;
    private const string CHAR_DIR       = DATA_ROOT + "/Characters";
    private const string ENEMY_DIR      = DATA_ROOT + "/Enemies";
    private const string WEAPON_DIR     = DATA_ROOT + "/Weapons";
    private const string BUFF_DIR       = DATA_ROOT + "/StaffBuffs";
    private const string RACE_DIR       = DATA_ROOT + "/StaffRaces";
    private const string CAL_DIR        = DATA_ROOT + "/CalendarEvents";

    // ──────────────────────────────────────────────
    // エントリポイント
    // ──────────────────────────────────────────────

    [MenuItem("DevilsDiner/Import Master Data (CSV and JSON)")]
    public static void ImportAll()
    {
        EnsureDirectory("Assets/Resources");
        EnsureDirectory(DATA_ROOT);

        int total = 0;

        // 1. Ingredients
        total += ImportIfExists(ING_CSV, ImportIngredients, "IngredientData");

        // 2. QualityScaleTable（Dish の前に必要）
        var qualityTable = EnsureQualityScaleTable();

        // 3. Dishes
        total += ImportIfExists(DISH_CSV, () => ImportDishes(qualityTable), "DishData");

        // 4. Recipes
        total += ImportIfExists(RECIPE_JSON, ImportRecipes, "RecipeData");

        // 5. Furniture
        total += ImportIfExists(FURNITURE_CSV, ImportFurniture, "FurnitureData");

        // 6. CharacterStats
        total += ImportIfExists(CHAR_CSV, ImportCharacters, "CharacterStats");

        // 7. Enemies（CharacterStats の後に実行）
        total += ImportIfExists(ENEMY_CSV, ImportEnemies, "EnemyData");

        // 8. Weapons
        total += ImportIfExists(WEAPON_CSV, ImportWeapons, "WeaponData");

        // 9. StaffBuffs（StaffRace の前に必要）
        total += ImportIfExists(STAFF_BUFF_CSV, ImportStaffBuffs, "StaffBuffData");

        // 10. StaffRaces
        total += ImportIfExists(STAFF_RACE_JSON, ImportStaffRaces, "StaffRaceData");

        // 11. CalendarEvents
        total += ImportIfExists(CALENDAR_CSV, ImportCalendarEvents, "CalendarEventData");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MasterDataImporter] 完了！ 合計 {total} アセット処理。");
    }

    /// <summary>ファイル存在チェック付きインポート実行。</summary>
    private static int ImportIfExists(string filePath, Func<int> importFunc, string label)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[MasterDataImporter] {filePath} が見つかりません。スキップ。");
            return 0;
        }
        int count = importFunc();
        Debug.Log($"[MasterDataImporter] {label}: {count} 件インポート");
        return count;
    }

    // ──────────────────────────────────────────────
    // IngredientData (CSV)
    // ──────────────────────────────────────────────

    private static int ImportIngredients()
    {
        EnsureDirectory(ING_DIR);
        var rows = ParseCsv(ING_CSV);
        int count = 0;

        foreach (var row in rows)
        {
            string id = row["Id"];
            string path = $"{ING_DIR}/{id}.asset";

            var asset = LoadOrCreate<IngredientData>(path);
            var so = new SerializedObject(asset);

            so.FindProperty("_itemID").stringValue          = id;
            so.FindProperty("_displayName").stringValue     = row["DisplayName"];
            so.FindProperty("_description").stringValue     = row["Description"];
            so.FindProperty("_sellPrice").intValue          = ParseInt(row["SellPrice"]);
            so.FindProperty("_rarity").intValue             = ParseInt(row["Rarity"]);
            so.FindProperty("_dropRate").floatValue         = ParseFloat(row["DropRate"]);
            so.FindProperty("_gaugeSpeedMultiplier").floatValue = ParseFloat(row["GaugeSpeed"]);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            count++;
        }

        return count;
    }

    // ──────────────────────────────────────────────
    // DishData (CSV)
    // ──────────────────────────────────────────────

    private static int ImportDishes(QualityScaleTable qualityTable)
    {
        EnsureDirectory(DISH_DIR);
        var rows = ParseCsv(DISH_CSV);
        int count = 0;

        foreach (var row in rows)
        {
            string id = row["Id"];
            string path = $"{DISH_DIR}/{id}.asset";

            var asset = LoadOrCreate<DishData>(path);
            var so = new SerializedObject(asset);

            so.FindProperty("_itemID").stringValue      = id;
            so.FindProperty("_displayName").stringValue = row["DisplayName"];
            so.FindProperty("_description").stringValue = row["Description"];
            so.FindProperty("_sellPrice").intValue      = ParseInt(row["ShopPrice"]) / 2;

            so.FindProperty("_category").enumValueIndex         = ParseCategory(row["Category"]);
            so.FindProperty("_hpRecoveryAmount").intValue       = ParseInt(row["HpRecovery"]);
            so.FindProperty("_baseBuff").floatValue             = ParseFloat(row["BaseBuff"]);
            so.FindProperty("_buffDurationTurns").intValue      = ParseInt(row["BuffDuration"]);
            so.FindProperty("_scoutBonus").floatValue           = ParseFloat(row["ScoutBonus"]);
            so.FindProperty("_shopPrice").intValue              = ParseInt(row["ShopPrice"]);
            so.FindProperty("_baseSatisfaction").intValue       = ParseInt(row["Satisfaction"]);
            so.FindProperty("_servingTime").floatValue          = ParseFloat(row["ServingTime"]);
            so.FindProperty("_qualityTable").objectReferenceValue = qualityTable;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            count++;
        }

        return count;
    }

    // ──────────────────────────────────────────────
    // RecipeData (JSON)
    // ──────────────────────────────────────────────

    private static int ImportRecipes()
    {
        EnsureDirectory(RECIPE_DIR);

        string json = File.ReadAllText(RECIPE_JSON);
        var entries = JsonHelper.FromJsonArray<RecipeEntry>(json);

        var ingLookup = BuildLookup<IngredientData>(ING_DIR);
        var dishLookup = BuildLookup<DishData>(DISH_DIR);

        int count = 0;

        foreach (var entry in entries)
        {
            string path = $"{RECIPE_DIR}/{entry.id}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            var asset = ScriptableObject.CreateInstance<RecipeData>();
            var so = new SerializedObject(asset);

            so.FindProperty("_recipeID").stringValue    = entry.id;
            so.FindProperty("_displayName").stringValue = entry.displayName;
            so.FindProperty("_description").stringValue = entry.description;
            so.FindProperty("_requiredChefLevel").intValue = entry.chefLevel;

            if (dishLookup.TryGetValue(entry.dishId, out var dish))
            {
                so.FindProperty("_outputDish").objectReferenceValue = dish;
            }
            else
            {
                Debug.LogWarning($"[MasterDataImporter] DishData '{entry.dishId}' が見つかりません。レシピ '{entry.id}' の OutputDish は未設定。");
            }

            var ingProp = so.FindProperty("_ingredients");
            ingProp.arraySize = entry.ingredients.Length;
            for (int i = 0; i < entry.ingredients.Length; i++)
            {
                string ingId = entry.ingredients[i][0];
                int amount = int.Parse(entry.ingredients[i][1]);

                var element = ingProp.GetArrayElementAtIndex(i);
                if (ingLookup.TryGetValue(ingId, out var ing))
                {
                    element.FindPropertyRelative("Ingredient").objectReferenceValue = ing;
                }
                else
                {
                    Debug.LogWarning($"[MasterDataImporter] IngredientData '{ingId}' が見つかりません。レシピ '{entry.id}'。");
                }
                element.FindPropertyRelative("Amount").intValue = amount;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            count++;
        }

        return count;
    }

    // ──────────────────────────────────────────────
    // FurnitureData (CSV)
    // ──────────────────────────────────────────────

    private static int ImportFurniture()
    {
        EnsureDirectory(FURNITURE_DIR);
        var rows = ParseCsv(FURNITURE_CSV);
        int count = 0;

        foreach (var row in rows)
        {
            string id = row["Id"];
            string path = $"{FURNITURE_DIR}/{id}.asset";

            var asset = LoadOrCreate<FurnitureData>(path);
            var so = new SerializedObject(asset);

            so.FindProperty("_id").stringValue               = id;
            so.FindProperty("_furnitureName").stringValue     = row["DisplayName"];
            so.FindProperty("_description").stringValue       = row["Description"];
            so.FindProperty("_type").enumValueIndex           = ParseFurnitureType(row["Type"]);
            so.FindProperty("_price").intValue                = ParseInt(row["Price"]);
            so.FindProperty("_satisfactionBonus").floatValue  = ParseFloat(row["SatisfactionBonus"]);
            so.FindProperty("_customerBonus").intValue        = ParseInt(row["CustomerBonus"]);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            count++;
        }

        return count;
    }

    // ──────────────────────────────────────────────
    // CharacterStats (CSV)
    // ──────────────────────────────────────────────

    private static int ImportCharacters()
    {
        EnsureDirectory(CHAR_DIR);
        var rows = ParseCsv(CHAR_CSV);
        int count = 0;

        foreach (var row in rows)
        {
            string id = row["Id"];
            string path = $"{CHAR_DIR}/{id}.asset";

            var asset = LoadOrCreate<CharacterStats>(path);
            var so = new SerializedObject(asset);

            so.FindProperty("_id").stringValue          = id;
            so.FindProperty("_displayName").stringValue = row["DisplayName"];
            so.FindProperty("_element").enumValueIndex  = ParseElement(row["Element"]);
            so.FindProperty("_maxHP").intValue           = ParseInt(row["MaxHP"]);
            so.FindProperty("_attack").intValue          = ParseInt(row["Attack"]);
            so.FindProperty("_defense").intValue         = ParseInt(row["Defense"]);
            so.FindProperty("_speed").intValue           = ParseInt(row["Speed"]);
            so.FindProperty("_maxEP").intValue           = ParseInt(row["MaxEP"]);
            so.FindProperty("_epGainOnAttack").intValue  = ParseInt(row["EPGainOnAttack"]);
            so.FindProperty("_epGainOnHit").intValue     = ParseInt(row["EPGainOnHit"]);
            so.FindProperty("_epGainOnSkill").intValue   = ParseInt(row["EPGainOnSkill"]);
            so.FindProperty("_skillMultiplier").floatValue    = ParseFloat(row["SkillMultiplier"]);
            so.FindProperty("_ultimateMultiplier").floatValue = ParseFloat(row["UltimateMultiplier"]);
            so.FindProperty("_skillTargetMode").enumValueIndex = ParseTargetingMode(row["SkillTargetMode"]);
            so.FindProperty("_maxToughness").intValue    = ParseInt(row["MaxToughness"]);
            so.FindProperty("_baseActionValue").floatValue = ParseFloat(row["BaseActionValue"]);

            // 属性耐性
            so.FindProperty("_physicalRes").floatValue   = ParseFloat(GetOrDefault(row, "PhysicalRes", "0"));
            so.FindProperty("_fireRes").floatValue       = ParseFloat(GetOrDefault(row, "FireRes", "0"));
            so.FindProperty("_iceRes").floatValue        = ParseFloat(GetOrDefault(row, "IceRes", "0"));
            so.FindProperty("_lightningRes").floatValue  = ParseFloat(GetOrDefault(row, "LightningRes", "0"));
            so.FindProperty("_windRes").floatValue       = ParseFloat(GetOrDefault(row, "WindRes", "0"));
            so.FindProperty("_darkRes").floatValue       = ParseFloat(GetOrDefault(row, "DarkRes", "0"));

            // 弱点属性リスト（セミコロン区切り: "Fire;Ice"）
            string weakStr = GetOrDefault(row, "WeakElements", "");
            if (!string.IsNullOrEmpty(weakStr))
            {
                string[] weakParts = weakStr.Split(';');
                var weakProp = so.FindProperty("_weakElements");
                weakProp.arraySize = weakParts.Length;
                for (int i = 0; i < weakParts.Length; i++)
                {
                    weakProp.GetArrayElementAtIndex(i).enumValueIndex = ParseElement(weakParts[i].Trim());
                }
            }
            else
            {
                so.FindProperty("_weakElements").arraySize = 0;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            count++;
        }

        return count;
    }

    // ──────────────────────────────────────────────
    // EnemyData (CSV)
    // ──────────────────────────────────────────────

    private static int ImportEnemies()
    {
        EnsureDirectory(ENEMY_DIR);
        var rows = ParseCsv(ENEMY_CSV);

        // ルックアップ構築
        var ingLookup = BuildLookup<IngredientData>(ING_DIR);
        var raceLookup = BuildLookup<StaffRaceData>(RACE_DIR);

        int count = 0;

        foreach (var row in rows)
        {
            string id = row["Id"];
            string path = $"{ENEMY_DIR}/{id}.asset";

            var asset = LoadOrCreate<EnemyData>(path);
            var so = new SerializedObject(asset);

            so.FindProperty("_id").stringValue          = id;
            so.FindProperty("_enemyName").stringValue   = row["EnemyName"];
            so.FindProperty("_maxHP").intValue           = ParseInt(row["MaxHP"]);
            so.FindProperty("_baseAttack").intValue      = ParseInt(row["BaseAttack"]);
            so.FindProperty("_dropRateNormal").floatValue = ParseFloat(row["DropRateNormal"]);
            so.FindProperty("_dropRateJust").floatValue   = ParseFloat(row["DropRateJust"]);
            so.FindProperty("_goldReward").intValue       = ParseInt(row["GoldReward"]);

            // ドロップアイテム参照
            string normalDrop = GetOrDefault(row, "DropItemNormal", "");
            if (!string.IsNullOrEmpty(normalDrop) && ingLookup.TryGetValue(normalDrop, out var normalIng))
                so.FindProperty("_dropItemNormal").objectReferenceValue = normalIng;

            string justDrop = GetOrDefault(row, "DropItemJust", "");
            if (!string.IsNullOrEmpty(justDrop) && ingLookup.TryGetValue(justDrop, out var justIng))
                so.FindProperty("_dropItemJust").objectReferenceValue = justIng;

            // StaffRace 参照
            string raceId = GetOrDefault(row, "StaffRace", "");
            if (!string.IsNullOrEmpty(raceId) && raceLookup.TryGetValue(raceId, out var race))
                so.FindProperty("_staffRace").objectReferenceValue = race;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            count++;
        }

        return count;
    }

    // ──────────────────────────────────────────────
    // WeaponData (CSV)
    // ──────────────────────────────────────────────

    private static int ImportWeapons()
    {
        EnsureDirectory(WEAPON_DIR);
        var rows = ParseCsv(WEAPON_CSV);
        int count = 0;

        foreach (var row in rows)
        {
            string id = row["Id"];
            string path = $"{WEAPON_DIR}/{id}.asset";

            var asset = LoadOrCreate<WeaponData>(path);
            var so = new SerializedObject(asset);

            // ItemData 基底
            so.FindProperty("_itemID").stringValue      = id;
            so.FindProperty("_displayName").stringValue = row["DisplayName"];
            so.FindProperty("_description").stringValue = row["Description"];
            so.FindProperty("_sellPrice").intValue      = ParseInt(row["SellPrice"]);

            // WeaponData 固有
            so.FindProperty("_baseDamage").intValue          = ParseInt(row["BaseDamage"]);
            so.FindProperty("_basePartBreakValue").intValue  = ParseInt(row["BasePartBreakValue"]);
            so.FindProperty("_justInputFrameBonus").intValue = ParseInt(row["JustInputFrameBonus"]);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            count++;
        }

        return count;
    }

    // ──────────────────────────────────────────────
    // StaffBuffData (CSV)
    // ──────────────────────────────────────────────

    private static int ImportStaffBuffs()
    {
        EnsureDirectory(BUFF_DIR);
        var rows = ParseCsv(STAFF_BUFF_CSV);
        int count = 0;

        foreach (var row in rows)
        {
            string id = row["Id"];
            string path = $"{BUFF_DIR}/{id}.asset";

            var asset = LoadOrCreate<StaffBuffData>(path);
            var so = new SerializedObject(asset);

            so.FindProperty("_buffID").stringValue      = id;
            so.FindProperty("_displayName").stringValue = row["DisplayName"];
            so.FindProperty("_description").stringValue = row["Description"];
            so.FindProperty("_type").enumValueIndex     = ParseStaffBuffType(row["Type"]);
            so.FindProperty("_value").floatValue        = ParseFloat(row["Value"]);
            so.FindProperty("_rarity").intValue         = ParseInt(row["Rarity"]);

            // カテゴリ（CategorySpecialty 用、空欄なら Meat=0 のまま）
            string cat = GetOrDefault(row, "TargetCategory", "");
            if (!string.IsNullOrEmpty(cat))
            {
                so.FindProperty("_targetCategory").enumValueIndex = ParseCategory(cat);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            count++;
        }

        return count;
    }

    // ──────────────────────────────────────────────
    // StaffRaceData (JSON)
    // ──────────────────────────────────────────────

    private static int ImportStaffRaces()
    {
        EnsureDirectory(RACE_DIR);

        string json = File.ReadAllText(STAFF_RACE_JSON);
        var entries = JsonHelper.FromJsonArray<StaffRaceEntry>(json);

        // バフのルックアップ
        var buffLookup = BuildLookup<StaffBuffData>(BUFF_DIR);

        int count = 0;

        foreach (var entry in entries)
        {
            string path = $"{RACE_DIR}/{entry.id}.asset";

            var asset = LoadOrCreate<StaffRaceData>(path);
            var so = new SerializedObject(asset);

            so.FindProperty("_raceID").stringValue          = entry.id;
            so.FindProperty("_raceName").stringValue         = entry.raceName;
            so.FindProperty("_fixedEffect").enumValueIndex   = ParseFixedEffect(entry.fixedEffect);
            so.FindProperty("_fixedEffectValue").floatValue  = entry.fixedEffectValue;
            so.FindProperty("_baseSalary").intValue          = entry.baseSalary;
            so.FindProperty("_minBuffCount").intValue        = entry.minBuffCount;
            so.FindProperty("_maxBuffCount").intValue        = entry.maxBuffCount;

            // PossibleBuffs 配列
            var buffProp = so.FindProperty("_possibleBuffs");
            buffProp.arraySize = entry.possibleBuffs.Length;
            for (int i = 0; i < entry.possibleBuffs.Length; i++)
            {
                string buffId = entry.possibleBuffs[i];
                if (buffLookup.TryGetValue(buffId, out var buff))
                {
                    buffProp.GetArrayElementAtIndex(i).objectReferenceValue = buff;
                }
                else
                {
                    Debug.LogWarning($"[MasterDataImporter] StaffBuffData '{buffId}' が見つかりません。種族 '{entry.id}'。");
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            count++;
        }

        return count;
    }

    // ──────────────────────────────────────────────
    // CalendarEventData (CSV)
    // ──────────────────────────────────────────────

    private static int ImportCalendarEvents()
    {
        EnsureDirectory(CAL_DIR);
        var rows = ParseCsv(CALENDAR_CSV);
        int count = 0;

        foreach (var row in rows)
        {
            string id = row["Id"];
            string path = $"{CAL_DIR}/{id}.asset";

            var asset = LoadOrCreate<CalendarEventData>(path);
            var so = new SerializedObject(asset);

            so.FindProperty("_eventID").stringValue     = id;
            so.FindProperty("_eventName").stringValue   = row["EventName"];
            so.FindProperty("_description").stringValue = row["Description"];

            // TriggerDays（セミコロン区切り: "3;10;17"）
            string daysStr = GetOrDefault(row, "TriggerDays", "");
            if (!string.IsNullOrEmpty(daysStr))
            {
                string[] dayParts = daysStr.Split(';');
                var daysProp = so.FindProperty("_triggerDays");
                daysProp.arraySize = dayParts.Length;
                for (int i = 0; i < dayParts.Length; i++)
                {
                    daysProp.GetArrayElementAtIndex(i).intValue = ParseInt(dayParts[i].Trim());
                }
            }

            so.FindProperty("_bonusCategoryEnabled").boolValue =
                GetOrDefault(row, "BonusCategoryEnabled", "false").ToLower() == "true";
            so.FindProperty("_bonusCategory").enumValueIndex =
                ParseCategory(GetOrDefault(row, "BonusCategory", "Meat"));
            so.FindProperty("_satisfactionMultiplier").floatValue =
                ParseFloat(GetOrDefault(row, "SatisfactionMultiplier", "1"));
            so.FindProperty("_freshnessMultiplier").floatValue =
                ParseFloat(GetOrDefault(row, "FreshnessMultiplier", "1"));

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            count++;
        }

        return count;
    }

    // ──────────────────────────────────────────────
    // QualityScaleTable
    // ──────────────────────────────────────────────

    private static QualityScaleTable EnsureQualityScaleTable()
    {
        string path = $"{QUALITY_DIR}/QualityScaleTable.asset";
        var existing = AssetDatabase.LoadAssetAtPath<QualityScaleTable>(path);
        if (existing != null) return existing;

        EnsureDirectory(QUALITY_DIR);
        var asset = ScriptableObject.CreateInstance<QualityScaleTable>();
        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[MasterDataImporter] 生成: {path}");
        return asset;
    }

    // ──────────────────────────────────────────────
    // CSV パーサー
    // ──────────────────────────────────────────────

    private static List<Dictionary<string, string>> ParseCsv(string filePath)
    {
        var result = new List<Dictionary<string, string>>();
        var lines = File.ReadAllLines(filePath);
        if (lines.Length < 2) return result;

        string[] headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');
            var dict = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                dict[headers[j].Trim()] = values[j].Trim();
            }
            result.Add(dict);
        }

        return result;
    }

    // ──────────────────────────────────────────────
    // JSON ヘルパー
    // ──────────────────────────────────────────────

    private static class JsonHelper
    {
        public static T[] FromJsonArray<T>(string json)
        {
            string wrapped = $"{{\"items\":{json}}}";
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper.items;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }
    }

    [Serializable]
    private class RecipeEntry
    {
        public string id;
        public string displayName;
        public string description;
        public string dishId;
        public int chefLevel;
        public string[][] ingredients;
    }

    [Serializable]
    private class StaffRaceEntry
    {
        public string id;
        public string raceName;
        public string fixedEffect;
        public float fixedEffectValue;
        public int baseSalary;
        public string[] possibleBuffs;
        public int minBuffCount;
        public int maxBuffCount;
    }

    // ──────────────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────────────

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static Dictionary<string, T> BuildLookup<T>(string dir) where T : ScriptableObject
    {
        var lookup = new Dictionary<string, T>();
        if (!AssetDatabase.IsValidFolder(dir)) return lookup;

        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { dir });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                lookup[fileName] = asset;
            }
        }
        return lookup;
    }

    private static string GetOrDefault(Dictionary<string, string> dict, string key, string defaultValue)
    {
        return dict.TryGetValue(key, out string value) ? value : defaultValue;
    }

    // ──────────────────────────────────────────────
    // Enum パーサー
    // ──────────────────────────────────────────────

    private static int ParseCategory(string value)
    {
        return value switch
        {
            "Meat"    => (int)DishCategory.Meat,
            "Fish"    => (int)DishCategory.Fish,
            "Salad"   => (int)DishCategory.Salad,
            "Dessert" => (int)DishCategory.Dessert,
            _         => 0,
        };
    }

    private static int ParseFurnitureType(string value)
    {
        return value switch
        {
            "Table"      => (int)FurnitureData.FurnitureType.Table,
            "Chair"      => (int)FurnitureData.FurnitureType.Chair,
            "Decoration" => (int)FurnitureData.FurnitureType.Decoration,
            "Lighting"   => (int)FurnitureData.FurnitureType.Lighting,
            "Kitchen"    => (int)FurnitureData.FurnitureType.Kitchen,
            _            => 0,
        };
    }

    private static int ParseElement(string value)
    {
        return value switch
        {
            "Physical"  => (int)CharacterStats.ElementType.Physical,
            "Fire"      => (int)CharacterStats.ElementType.Fire,
            "Ice"       => (int)CharacterStats.ElementType.Ice,
            "Lightning" => (int)CharacterStats.ElementType.Lightning,
            "Wind"      => (int)CharacterStats.ElementType.Wind,
            "Dark"      => (int)CharacterStats.ElementType.Dark,
            _           => 0,
        };
    }

    private static int ParseTargetingMode(string value)
    {
        return value switch
        {
            "AllEnemies" => (int)CharacterStats.TargetingMode.AllEnemies,
            _            => (int)CharacterStats.TargetingMode.Single,
        };
    }

    private static int ParseStaffBuffType(string value)
    {
        return value switch
        {
            "CookSpeed"         => (int)StaffBuffType.CookSpeed,
            "QualityBonus"      => (int)StaffBuffType.QualityBonus,
            "SatisfactionBonus" => (int)StaffBuffType.SatisfactionBonus,
            "SalaryReduction"   => (int)StaffBuffType.SalaryReduction,
            "CategorySpecialty" => (int)StaffBuffType.CategorySpecialty,
            "FreshnessBonus"    => (int)StaffBuffType.FreshnessBonus,
            _                   => 0,
        };
    }

    private static int ParseFixedEffect(string value)
    {
        return value switch
        {
            "CookSpeedUp"    => (int)StaffFixedEffect.CookSpeedUp,
            "SatisfactionUp" => (int)StaffFixedEffect.SatisfactionUp,
            "SalaryDiscount" => (int)StaffFixedEffect.SalaryDiscount,
            "QualityUp"      => (int)StaffFixedEffect.QualityUp,
            "DropRateUp"     => (int)StaffFixedEffect.DropRateUp,
            _                => 0,
        };
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int result) ? result : 0;
    }

    private static float ParseFloat(string value)
    {
        return float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float result) ? result : 0f;
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
