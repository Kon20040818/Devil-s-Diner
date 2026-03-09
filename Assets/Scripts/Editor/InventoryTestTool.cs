// ============================================================
// InventoryTestTool.cs
// テスト用アイテム ScriptableObject の自動生成と
// InventoryManager へのテストアイテム追加を行うエディタ拡張。
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// テスト用アイテムの自動生成・インベントリへの追加を行うエディタウィンドウ。
/// メニュー: DevilsDiner → Inventory Test Tool
/// </summary>
public sealed class InventoryTestTool : EditorWindow
{
    private const string ASSET_DIR = "Assets/Data/TestItems";

    [MenuItem("DevilsDiner/Inventory Test Tool")]
    private static void ShowWindow()
    {
        GetWindow<InventoryTestTool>("Inventory Test Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Inventory Test Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ── アセット生成 ──
        EditorGUILayout.LabelField("ScriptableObject 生成", EditorStyles.miniBoldLabel);

        if (GUILayout.Button("品質テーブル(QualityScaleTable)を生成"))
        {
            CreateDefaultQualityScaleTable();
        }

        if (GUILayout.Button("テスト用 IngredientData を3つ生成"))
        {
            CreateTestIngredients();
        }

        if (GUILayout.Button("テスト用 DishData を4つ生成（各カテゴリ）"))
        {
            CreateTestDishes();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ランタイムテスト（Play中のみ）", EditorStyles.miniBoldLabel);

        GUI.enabled = Application.isPlaying && GameManager.Instance != null;

        if (GUILayout.Button("テストアイテムをインベントリに追加"))
        {
            AddTestItemsToInventory();
        }

        if (GUILayout.Button("テスト用 DishInstance をインベントリに追加"))
        {
            AddTestDishInstancesToInventory();
        }

        if (GUILayout.Button("インベントリをログ出力"))
        {
            LogInventory();
        }

        GUI.enabled = true;
    }

    // ──────────────────────────────────────────────
    // アセット生成
    // ──────────────────────────────────────────────

    private static void CreateDefaultQualityScaleTable()
    {
        EnsureDirectory(ASSET_DIR);
        string path = $"{ASSET_DIR}/QualityScaleTable.asset";
        if (AssetDatabase.LoadAssetAtPath<QualityScaleTable>(path) != null)
        {
            Debug.Log("[InventoryTestTool] QualityScaleTable は既に存在します。");
            return;
        }

        var table = CreateInstance<QualityScaleTable>();
        AssetDatabase.CreateAsset(table, path);
        AssetDatabase.SaveAssets();
        Debug.Log("[InventoryTestTool] デフォルト QualityScaleTable を生成しました。");
    }

    private static void CreateTestIngredients()
    {
        EnsureDirectory(ASSET_DIR);

        CreateIngredient("ING_DevilMeat",   "悪魔肉",     "地獄産の上質な肉。",   1, 1.0f, 30);
        CreateIngredient("ING_HellPepper",  "地獄唐辛子", "激辛の希少香辛料。",   3, 0.3f, 120);
        CreateIngredient("ING_SoulSalt",    "魂の塩",     "魂から精製された塩。", 2, 0.6f, 60);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[InventoryTestTool] テスト用 IngredientData を3つ生成しました。");
    }

    private static void CreateTestDishes()
    {
        EnsureDirectory(ASSET_DIR);

        var qualityTable = AssetDatabase.LoadAssetAtPath<QualityScaleTable>(
            $"{ASSET_DIR}/QualityScaleTable.asset");

        CreateDish("DISH_DevilSteak",  "悪魔ステーキ", "ジューシーな悪魔肉のステーキ。",
            80, 200, DishCategory.Meat,    0.15f, 3, 0.05f, 200, 50, 5f, qualityTable);
        CreateDish("DISH_SoulSashimi", "魂の刺身",     "透き通るように美しい刺身。",
            60, 280, DishCategory.Fish,    0.12f, 4, 0.08f, 280, 60, 4f, qualityTable);
        CreateDish("DISH_HellCurry",   "地獄カレー",   "辛さが癖になる一品。",
            120, 350, DishCategory.Salad,  0.10f, 3, 0.03f, 350, 70, 6f, qualityTable);
        CreateDish("DISH_AbyssTart",   "深淵タルト",   "甘い闇の味がする至高のデザート。",
            100, 400, DishCategory.Dessert, 0.08f, 5, 0.04f, 400, 80, 5f, qualityTable);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[InventoryTestTool] テスト用 DishData を4つ生成しました。");
    }

    private static void CreateIngredient(string id, string displayName, string desc, int rarity, float dropRate, int sellPrice)
    {
        string path = $"{ASSET_DIR}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<IngredientData>(path) != null) return;

        var data = CreateInstance<IngredientData>();

        var so = new SerializedObject(data);
        so.FindProperty("_itemID").stringValue       = id;
        so.FindProperty("_displayName").stringValue  = displayName;
        so.FindProperty("_description").stringValue  = desc;
        so.FindProperty("_sellPrice").intValue        = sellPrice;
        so.FindProperty("_rarity").intValue           = rarity;
        so.FindProperty("_dropRate").floatValue       = dropRate;
        so.FindProperty("_gaugeSpeedMultiplier").floatValue = 1f;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(data, path);
    }

    private static void CreateDish(
        string id, string displayName, string desc,
        int hpRecovery, int sellPrice,
        DishCategory category, float baseBuff, int buffDuration,
        float scoutBonus, int shopPrice, int baseSatisfaction,
        float servingTime, QualityScaleTable qualityTable)
    {
        string path = $"{ASSET_DIR}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<DishData>(path) != null) return;

        var data = CreateInstance<DishData>();

        var so = new SerializedObject(data);
        so.FindProperty("_itemID").stringValue            = id;
        so.FindProperty("_displayName").stringValue       = displayName;
        so.FindProperty("_description").stringValue       = desc;
        so.FindProperty("_sellPrice").intValue             = sellPrice;
        so.FindProperty("_hpRecoveryAmount").intValue      = hpRecovery;
        so.FindProperty("_servingTime").floatValue         = servingTime;
        so.FindProperty("_category").enumValueIndex        = (int)category;
        so.FindProperty("_baseBuff").floatValue            = baseBuff;
        so.FindProperty("_buffDurationTurns").intValue     = buffDuration;
        so.FindProperty("_scoutBonus").floatValue          = scoutBonus;
        so.FindProperty("_shopPrice").intValue             = shopPrice;
        so.FindProperty("_baseSatisfaction").intValue      = baseSatisfaction;
        if (qualityTable != null)
            so.FindProperty("_qualityTable").objectReferenceValue = qualityTable;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(data, path);
    }

    // ──────────────────────────────────────────────
    // ランタイムテスト
    // ──────────────────────────────────────────────

    private static void AddTestItemsToInventory()
    {
        var inv = GameManager.Instance.Inventory;

        var guids = AssetDatabase.FindAssets("t:ItemData", new[] { ASSET_DIR });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
            {
                inv.Add(item, 5);
                count++;
            }
        }

        Debug.Log($"[InventoryTestTool] {count} 種のテストアイテムを各5個追加しました。");
    }

    private static void AddTestDishInstancesToInventory()
    {
        var inv = GameManager.Instance.Inventory;

        var guids = AssetDatabase.FindAssets("t:DishData", new[] { ASSET_DIR });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var dishData = AssetDatabase.LoadAssetAtPath<DishData>(path);
            if (dishData == null) continue;

            foreach (DishQuality quality in System.Enum.GetValues(typeof(DishQuality)))
            {
                var instance = new DishInstance(dishData, quality);
                inv.AddDish(instance, 2);
                count++;
            }
        }

        Debug.Log($"[InventoryTestTool] {count} 種の DishInstance を各2個追加しました。");
    }

    private static void LogInventory()
    {
        var inv = GameManager.Instance.Inventory;
        var items = inv.GetAllItems();
        var dishes = inv.GetAllDishes();

        Debug.Log($"[InventoryTestTool] === インベントリ内容 (汎用:{items.Count} 種 + 料理:{dishes.Count} 種) ===");
        foreach (var kvp in items)
        {
            Debug.Log($"  [Item] {kvp.Key.DisplayName} (ID:{kvp.Key.ItemID}) x{kvp.Value}");
        }
        foreach (var kvp in dishes)
        {
            Debug.Log($"  [Dish] {kvp.Key} (HP:{kvp.Key.HealAmount} Buff:{kvp.Key.BuffAmount:F2} Scout:{kvp.Key.ScoutBonus:F3}) x{kvp.Value}");
        }
    }

    // ──────────────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────────────

    private static void EnsureDirectory(string dir)
    {
        if (!AssetDatabase.IsValidFolder(dir))
        {
            string[] parts = dir.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
