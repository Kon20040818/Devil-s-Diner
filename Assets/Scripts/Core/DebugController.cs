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
/// F7: バトル勝利シミュレート（鮮度バフ + ダミーリクルート）
/// F8: 料理シミュレート（最初の利用可能レシピで調理）
/// F9: ManagementScene へ強制遷移
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

        if (kb.f7Key.wasPressedThisFrame)
        {
            SimulateBattleVictory();
        }

        if (kb.f8Key.wasPressedThisFrame)
        {
            SimulateCooking();
        }

        if (kb.f9Key.wasPressedThisFrame)
        {
            JumpToManagementScene();
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

    private void SimulateBattleVictory()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[DebugController] F7: GameManager が見つかりません。");
            return;
        }

        // 鮮度バフをセット
        gm.DailyFreshnessBuff = 1.5f;

        // ダミーリクルートを作成してスタッフマネージャーに送る
        if (gm.Staff != null)
        {
            var dummyRecruit = new RecruitedDemonData
            {
                EnemyName = "Debug_Demon",
                Stats = null,
                Race = null,
                RolledBuffs = new StaffBuffData[0]
            };
            gm.Staff.ReceiveRecruits(new List<RecruitedDemonData> { dummyRecruit });
            Debug.Log("[DebugController] F7: バトル勝利をシミュレート (鮮度×1.5, ダミーリクルート1体)");
        }
        else
        {
            Debug.Log("[DebugController] F7: バトル勝利をシミュレート (鮮度×1.5, StaffManager未初期化のためリクルートなし)");
        }
    }

    private void SimulateCooking()
    {
        var cookMgr = FindFirstObjectByType<CookingManager>();
        if (cookMgr == null)
        {
            cookMgr = new GameObject("CookingManager").AddComponent<CookingManager>();
            Debug.Log("[DebugController] F8: CookingManager を仮生成しました。");
        }

        var recipes = cookMgr.GetAvailableRecipes();
        if (recipes.Count == 0)
        {
            Debug.LogWarning("[DebugController] F8: 利用可能なレシピがありません。素材が不足している可能性があります。");
            return;
        }

        RecipeData recipe = recipes[0];
        if (!cookMgr.CanCook(recipe))
        {
            Debug.LogWarning($"[DebugController] F8: {recipe.DisplayName} の素材が不足しています。");
            return;
        }

        float freshness = GameManager.Instance != null ? GameManager.Instance.DailyFreshnessBuff : 1f;
        var result = cookMgr.Cook(recipe, freshness);
        if (result.Success)
            Debug.Log($"[DebugController] F8: {recipe.DisplayName} を調理 → 品質: {result.Dish.Quality}");
        else
            Debug.LogWarning($"[DebugController] F8: {recipe.DisplayName} の調理に失敗しました。");
    }

    private void JumpToManagementScene()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[DebugController] F9: GameManager が見つかりません。");
            return;
        }

        gm.TransitionToScene("ManagementScene");
        Debug.Log("[DebugController] F9: ManagementScene へ強制遷移");
    }
}
