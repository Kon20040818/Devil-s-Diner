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

        if (GUILayout.Button("テスト用 IngredientData を3つ生成"))
        {
            CreateTestIngredients();
        }

        if (GUILayout.Button("テスト用 DishData を2つ生成"))
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

        if (GUILayout.Button("インベントリをログ出力"))
        {
            LogInventory();
        }

        GUI.enabled = true;
    }

    // ──────────────────────────────────────────────
    // アセット生成
    // ──────────────────────────────────────────────

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

        CreateDish("DISH_DevilSteak",  "悪魔ステーキ",   "ジューシーな悪魔肉のステーキ。", 80,  15, 200);
        CreateDish("DISH_HellCurry",   "地獄カレー",     "辛さが癖になる一品。",           120, 25, 350);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[InventoryTestTool] テスト用 DishData を2つ生成しました。");
    }

    private static void CreateIngredient(string id, string displayName, string desc, int rarity, float dropRate, int sellPrice)
    {
        string path = $"{ASSET_DIR}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<IngredientData>(path) != null) return;

        var data = CreateInstance<IngredientData>();

        // SerializedObject 経由で private フィールドを設定
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

    private static void CreateDish(string id, string displayName, string desc, int hpRecovery, int appeal, int sellPrice)
    {
        string path = $"{ASSET_DIR}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<DishData>(path) != null) return;

        var data = CreateInstance<DishData>();

        var so = new SerializedObject(data);
        so.FindProperty("_itemID").stringValue        = id;
        so.FindProperty("_displayName").stringValue   = displayName;
        so.FindProperty("_description").stringValue   = desc;
        so.FindProperty("_sellPrice").intValue         = sellPrice;
        so.FindProperty("_hpRecoveryAmount").intValue  = hpRecovery;
        so.FindProperty("_appealValue").intValue       = appeal;
        so.FindProperty("_servingTime").floatValue     = 5f;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(data, path);
    }

    // ──────────────────────────────────────────────
    // ランタイムテスト
    // ──────────────────────────────────────────────

    private static void AddTestItemsToInventory()
    {
        var inv = GameManager.Instance.Inventory;

        // Data フォルダからテストアイテムを検索して追加
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

    private static void LogInventory()
    {
        var inv = GameManager.Instance.Inventory;
        var items = inv.GetAllItems();

        Debug.Log($"[InventoryTestTool] === インベントリ内容 ({items.Count} 種) ===");
        foreach (var kvp in items)
        {
            Debug.Log($"  {kvp.Key.DisplayName} (ID:{kvp.Key.ItemID}) x{kvp.Value}");
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
