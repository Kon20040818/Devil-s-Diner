// ============================================================
// MasterDataImporter.cs
// Assets/MasterData/ 配下の CSV / JSON ファイルを読み込み、
// ScriptableObject アセットを自動生成・更新するエディタツール。
//
// メニュー: DevilsDiner > Import Master Data (CSV/JSON)
//
// 対応ファイル:
//   ingredients.csv  → IngredientData
//   dishes.csv       → DishData
//   recipes.json     → RecipeData
//   furniture.csv    → FurnitureData
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
    private const string MASTER_DIR     = "Assets/MasterData";
    private const string ING_CSV        = MASTER_DIR + "/ingredients.csv";
    private const string DISH_CSV       = MASTER_DIR + "/dishes.csv";
    private const string RECIPE_JSON    = MASTER_DIR + "/recipes.json";
    private const string FURNITURE_CSV  = MASTER_DIR + "/furniture.csv";

    // ── 出力パス ──
    private const string DATA_ROOT      = "Assets/Resources/Data";
    private const string ING_DIR        = DATA_ROOT + "/Ingredients";
    private const string DISH_DIR       = DATA_ROOT + "/Dishes";
    private const string RECIPE_DIR     = DATA_ROOT + "/Recipes";
    private const string FURNITURE_DIR  = DATA_ROOT + "/Furniture";
    private const string QUALITY_DIR    = DATA_ROOT;

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
        if (File.Exists(ING_CSV))
        {
            int count = ImportIngredients();
            total += count;
            Debug.Log($"[MasterDataImporter] IngredientData: {count} 件インポート");
        }
        else
        {
            Debug.LogWarning($"[MasterDataImporter] {ING_CSV} が見つかりません。スキップ。");
        }

        // 2. QualityScaleTable（Dish の前に必要）
        var qualityTable = EnsureQualityScaleTable();

        // 3. Dishes
        if (File.Exists(DISH_CSV))
        {
            int count = ImportDishes(qualityTable);
            total += count;
            Debug.Log($"[MasterDataImporter] DishData: {count} 件インポート");
        }
        else
        {
            Debug.LogWarning($"[MasterDataImporter] {DISH_CSV} が見つかりません。スキップ。");
        }

        // 4. Recipes
        if (File.Exists(RECIPE_JSON))
        {
            int count = ImportRecipes();
            total += count;
            Debug.Log($"[MasterDataImporter] RecipeData: {count} 件インポート");
        }
        else
        {
            Debug.LogWarning($"[MasterDataImporter] {RECIPE_JSON} が見つかりません。スキップ。");
        }

        // 5. Furniture
        if (File.Exists(FURNITURE_CSV))
        {
            int count = ImportFurniture();
            total += count;
            Debug.Log($"[MasterDataImporter] FurnitureData: {count} 件インポート");
        }
        else
        {
            Debug.LogWarning($"[MasterDataImporter] {FURNITURE_CSV} が見つかりません。スキップ。");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MasterDataImporter] 完了！ 合計 {total} アセット処理。");
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

            // ItemData 基底
            so.FindProperty("_itemID").stringValue      = id;
            so.FindProperty("_displayName").stringValue = row["DisplayName"];
            so.FindProperty("_description").stringValue = row["Description"];
            so.FindProperty("_sellPrice").intValue      = ParseInt(row["ShopPrice"]) / 2;

            // DishData 固有
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

        // 素材・料理のルックアップテーブルを構築
        var ingLookup = BuildLookup<IngredientData>(ING_DIR);
        var dishLookup = BuildLookup<DishData>(DISH_DIR);

        int count = 0;

        foreach (var entry in entries)
        {
            string path = $"{RECIPE_DIR}/{entry.id}.asset";

            // 既存の旧スキーマを削除
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

            // OutputDish
            if (dishLookup.TryGetValue(entry.dishId, out var dish))
            {
                so.FindProperty("_outputDish").objectReferenceValue = dish;
            }
            else
            {
                Debug.LogWarning($"[MasterDataImporter] DishData '{entry.dishId}' が見つかりません。レシピ '{entry.id}' の OutputDish は未設定。");
            }

            // Ingredients
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

    /// <summary>
    /// 簡易 CSV パーサー。1行目をヘッダーとして辞書のリストを返す。
    /// カンマ区切り。ダブルクォート内のカンマには対応しない（マスターデータには不要）。
    /// </summary>
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

    /// <summary>
    /// JSON 配列を Unity の JsonUtility でデシリアライズするラッパー。
    /// JsonUtility は配列トップレベルを扱えないため、ラッパーオブジェクトで包む。
    /// </summary>
    private static class JsonHelper
    {
        public static T[] FromJsonArray<T>(string json)
        {
            // JsonUtility は配列トップレベル非対応のためラッパーで包む
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

    /// <summary>JSON レシピエントリ。</summary>
    [Serializable]
    private class RecipeEntry
    {
        public string id;
        public string displayName;
        public string description;
        public string dishId;
        public int chefLevel;
        public string[][] ingredients; // [["ING_Meat","2"], ...]
    }

    // ──────────────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────────────

    /// <summary>既存アセットをロード、なければ新規作成。</summary>
    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    /// <summary>指定ディレクトリ内の全アセットを ID → アセット の辞書にする。</summary>
    private static Dictionary<string, T> BuildLookup<T>(string dir) where T : ScriptableObject
    {
        var lookup = new Dictionary<string, T>();
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
