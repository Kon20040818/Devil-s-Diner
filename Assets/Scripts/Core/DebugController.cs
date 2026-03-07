// ============================================================
// DebugController.cs
// テストプレイ用のデバッグチート機能。
// GameManager と同一 GameObject に自動追加され、DontDestroyOnLoad で永続化。
// NOTE: プロダクションビルドでは無効化またはストリップすること。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// デバッグ用チートコントローラー。
/// F1: テストアイテムを追加
/// F2: 所持金 +1000G
/// F4: 手動セーブ
/// F5: 手動ロード
/// F6: セーブ/ロード往復テスト
/// </summary>
public sealed class DebugController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [SerializeField] private ItemData[] _debugItems;
    [SerializeField] private int _itemAddAmount = 10;
    [SerializeField] private int _goldAddAmount = 1000;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f1Key.wasPressedThisFrame)
        {
            AddDebugItems();
        }

        if (kb.f2Key.wasPressedThisFrame)
        {
            GameManager.Instance.AddGold(_goldAddAmount);
            Debug.Log($"[DebugController] 所持金 +{_goldAddAmount}G (現在: {GameManager.Instance.Gold}G)");
        }

        if (kb.f4Key.wasPressedThisFrame)
        {
            ManualSave();
        }

        if (kb.f5Key.wasPressedThisFrame)
        {
            ManualLoad();
        }

        if (kb.f6Key.wasPressedThisFrame)
        {
            VerifySaveLoadRoundTrip();
        }
    }

    // ──────────────────────────────────────────────
    // 内部メソッド
    // ──────────────────────────────────────────────

    private void AddDebugItems()
    {
        ItemData[] items = _debugItems;

        if (items == null || items.Length == 0)
        {
            items = Resources.LoadAll<ItemData>("");
        }

        if (items == null || items.Length == 0)
        {
            Debug.LogWarning("[DebugController] 追加できる ItemData が見つかりません。");
            return;
        }

        foreach (ItemData item in items)
        {
            GameManager.Instance.Inventory.Add(item, _itemAddAmount);
            Debug.Log($"[DebugController] {item.DisplayName} x{_itemAddAmount} 追加");
        }

        Debug.Log($"[DebugController] デバッグアイテム追加完了！ {items.Length}種 x {_itemAddAmount}個");
    }

    private void ManualSave()
    {
        SaveDataManager saveManager = FindFirstObjectByType<SaveDataManager>();
        if (saveManager != null)
        {
            saveManager.Save();
            Debug.Log("[DebugController] F4: 手動セーブを実行しました。");
        }
        else
        {
            Debug.LogWarning("[DebugController] SaveDataManager が見つかりません。");
        }
    }

    private void ManualLoad()
    {
        SaveDataManager saveManager = FindFirstObjectByType<SaveDataManager>();
        if (saveManager != null)
        {
            if (saveManager.HasSaveData())
            {
                saveManager.Load();
                Debug.Log("[DebugController] F5: 手動ロードを実行しました。");
            }
            else
            {
                Debug.LogWarning("[DebugController] F5: セーブデータが存在しません。先に F4 でセーブしてください。");
            }
        }
        else
        {
            Debug.LogWarning("[DebugController] SaveDataManager が見つかりません。");
        }
    }

    private void VerifySaveLoadRoundTrip()
    {
        GameManager gm = GameManager.Instance;
        SaveDataManager saveManager = FindFirstObjectByType<SaveDataManager>();
        if (gm == null || saveManager == null)
        {
            Debug.LogWarning("[DebugController] F6: GameManager または SaveDataManager が見つかりません。");
            return;
        }

        var inv = gm.Inventory;

        // 1. セーブ前スナップショット
        var beforeItems = new Dictionary<string, int>();
        foreach (var kvp in inv.GetAllItems())
        {
            if (kvp.Key != null)
                beforeItems[kvp.Key.ItemID] = kvp.Value;
        }

        var beforeDishes = new Dictionary<string, int>();
        foreach (var kvp in inv.GetAllDishes())
        {
            string key = $"{kvp.Key.Data.ItemID}:{kvp.Key.Quality}";
            beforeDishes[key] = kvp.Value;
        }

        Debug.Log($"[DebugController] F6: セーブ前 — アイテム {beforeItems.Count} 種, 料理 {beforeDishes.Count} 種");

        // 2. セーブ
        saveManager.Save();

        // 3. クリア
        inv.ClearAll();
        Debug.Log("[DebugController] F6: インベントリをクリアしました。");

        // 4. ロード
        saveManager.Load();

        // 5. ロード後スナップショット & 比較
        var afterItems = new Dictionary<string, int>();
        foreach (var kvp in inv.GetAllItems())
        {
            if (kvp.Key != null)
                afterItems[kvp.Key.ItemID] = kvp.Value;
        }

        var afterDishes = new Dictionary<string, int>();
        foreach (var kvp in inv.GetAllDishes())
        {
            string key = $"{kvp.Key.Data.ItemID}:{kvp.Key.Quality}";
            afterDishes[key] = kvp.Value;
        }

        Debug.Log($"[DebugController] F6: ロード後 — アイテム {afterItems.Count} 種, 料理 {afterDishes.Count} 種");

        // 比較
        bool match = true;

        foreach (var kvp in beforeItems)
        {
            if (!afterItems.TryGetValue(kvp.Key, out int afterAmount) || afterAmount != kvp.Value)
            {
                Debug.LogWarning($"[DebugController] F6: 不一致 [Item] {kvp.Key}: before={kvp.Value} after={afterItems.GetValueOrDefault(kvp.Key, 0)}");
                match = false;
            }
        }
        foreach (var kvp in afterItems)
        {
            if (!beforeItems.ContainsKey(kvp.Key))
            {
                Debug.LogWarning($"[DebugController] F6: 余剰 [Item] {kvp.Key}: after={kvp.Value}");
                match = false;
            }
        }

        foreach (var kvp in beforeDishes)
        {
            if (!afterDishes.TryGetValue(kvp.Key, out int afterAmount) || afterAmount != kvp.Value)
            {
                Debug.LogWarning($"[DebugController] F6: 不一致 [Dish] {kvp.Key}: before={kvp.Value} after={afterDishes.GetValueOrDefault(kvp.Key, 0)}");
                match = false;
            }
        }
        foreach (var kvp in afterDishes)
        {
            if (!beforeDishes.ContainsKey(kvp.Key))
            {
                Debug.LogWarning($"[DebugController] F6: 余剰 [Dish] {kvp.Key}: after={kvp.Value}");
                match = false;
            }
        }

        if (match)
            Debug.Log("[DebugController] F6: セーブ/ロード往復テスト — 全データ一致！ SUCCESS");
        else
            Debug.LogWarning("[DebugController] F6: セーブ/ロード往復テスト — 不一致あり FAILED");
    }
}
