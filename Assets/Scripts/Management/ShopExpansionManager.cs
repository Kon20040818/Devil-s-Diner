// ============================================================
// ShopExpansionManager.cs
// 店舗レベルアップ（拡張）を管理する。
// DinerManager オブジェクトに配置、または FindFirstObjectByType で取得。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 店舗拡張（レベルアップ）を管理するコンポーネント。
/// レベルごとのコスト・最大客数・解放マップを定義し、アップグレード処理を提供する。
/// </summary>
public sealed class ShopExpansionManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    /// <summary>レベルごとのアップグレードコスト（インデックス = レベル - 1）。</summary>
    private static readonly int[] UPGRADE_COSTS = { 0, 1000, 3000, 7000, 15000 };

    /// <summary>レベルごとの最大同時客数（インデックス = レベル - 1）。</summary>
    private static readonly int[] MAX_CUSTOMERS_BY_LEVEL = { 10, 20, 30, 40, 50 };

    /// <summary>レベルごとに解放されるマップ ID（インデックス = レベル - 1）。</summary>
    private static readonly string[][] UNLOCKED_MAP_IDS_BY_LEVEL =
    {
        new[] { "MAP_Desert" },
        new[] { "MAP_Desert", "MAP_Forest" },
        new[] { "MAP_Desert", "MAP_Forest", "MAP_Swamp" },
        new[] { "MAP_Desert", "MAP_Forest", "MAP_Swamp", "MAP_Volcano" },
        new[] { "MAP_Desert", "MAP_Forest", "MAP_Swamp", "MAP_Volcano", "MAP_Castle" }
    };

    /// <summary>レベルごとの店舗名（日本語）。</summary>
    private static readonly string[] LEVEL_NAMES =
    {
        "ボロ酒場",
        "小酒場",
        "中規模レストラン",
        "高級レストラン",
        "伝説の名店"
    };

    /// <summary>最大店舗レベル。</summary>
    private const int MAX_LEVEL = 5;

    // ──────────────────────────────────────────────
    // 便利プロパティ（UI 向け）
    // ──────────────────────────────────────────────

    /// <summary>現在の店舗レベル。</summary>
    public int CurrentLevel => GameManager.Instance != null ? GameManager.Instance.ShopLevel : 1;

    /// <summary>現在のレベル名。</summary>
    public string CurrentLevelName => GetCurrentLevelName();

    /// <summary>最大レベルに到達しているか。</summary>
    public bool IsMaxLevel => CurrentLevel >= MAX_LEVEL;

    /// <summary>次のレベルへのアップグレードコスト。最大レベルなら -1。</summary>
    public int NextUpgradeCost => GetUpgradeCost();

    // ──────────────────────────────────────────────
    // シリアライズフィールド
    // ──────────────────────────────────────────────

    /// <summary>客スポーン管理。最大客数の更新に使用する。</summary>
    [SerializeField] private CustomerSpawner _customerSpawner;

    /// <summary>全マップデータ。店舗レベルに応じて解放判定を行う。</summary>
    [SerializeField] private MapData[] _allMaps;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>店舗が拡張されたとき（新レベルを引数で通知）。</summary>
    public event Action<int> OnShopExpanded;

    // ──────────────────────────────────────────────
    // 公開 API — アップグレードコスト
    // ──────────────────────────────────────────────

    /// <summary>
    /// 次のレベルへのアップグレードコストを返す。
    /// 既に最大レベルの場合は -1 を返す。
    /// </summary>
    public int GetUpgradeCost()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return -1;

        int currentLevel = gm.ShopLevel;
        if (currentLevel >= MAX_LEVEL) return -1;

        // 次のレベルのコスト = UPGRADE_COSTS[currentLevel]
        // （currentLevel は 1 始まりなので、インデックスは currentLevel そのまま）
        return UPGRADE_COSTS[Mathf.Clamp(currentLevel, 0, UPGRADE_COSTS.Length - 1)];
    }

    // ──────────────────────────────────────────────
    // 公開 API — アップグレード可否
    // ──────────────────────────────────────────────

    /// <summary>アップグレード可能かを返す。</summary>
    public bool CanUpgrade()
    {
        int cost = GetUpgradeCost();
        if (cost < 0) return false;

        GameManager gm = GameManager.Instance;
        return gm != null && gm.CanAfford(cost);
    }

    // ──────────────────────────────────────────────
    // 公開 API — アップグレード実行
    // ──────────────────────────────────────────────

    /// <summary>
    /// 店舗のアップグレードを試みる。
    /// コスト不足・最大レベルの場合は false を返す。
    /// </summary>
    /// <returns>アップグレードに成功した場合 true。</returns>
    public bool TryUpgrade()
    {
        int cost = GetUpgradeCost();
        if (cost < 0)
        {
            Debug.Log("[ShopExpansionManager] 既に最大レベルです。");
            return false;
        }

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[ShopExpansionManager] GameManager.Instance が null のためアップグレードを中断しました。");
            return false;
        }

        if (!gm.TrySpendGold(cost))
        {
            Debug.Log($"[ShopExpansionManager] ゴールド不足のためアップグレードできません（必要: {cost}, 所持: {gm.Gold}）。");
            return false;
        }

        gm.LevelUpShop();

        // CustomerSpawner の最大客数を更新
        if (_customerSpawner != null)
        {
            _customerSpawner.SetMaxCustomers(GetMaxCustomers());
        }

        Debug.Log($"[ShopExpansionManager] 店舗をレベル {gm.ShopLevel} ({GetCurrentLevelName()}) にアップグレードしました。");
        OnShopExpanded?.Invoke(gm.ShopLevel);
        return true;
    }

    // ──────────────────────────────────────────────
    // 公開 API — レベル情報取得
    // ──────────────────────────────────────────────

    /// <summary>現在のレベルに応じた最大同時客数を返す。</summary>
    public int GetMaxCustomers()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return MAX_CUSTOMERS_BY_LEVEL[0];

        int index = Mathf.Clamp(gm.ShopLevel - 1, 0, MAX_CUSTOMERS_BY_LEVEL.Length - 1);
        return MAX_CUSTOMERS_BY_LEVEL[index];
    }

    /// <summary>現在のレベルで解放されているマップ ID の配列を返す。</summary>
    public string[] GetUnlockedMapIds()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return UNLOCKED_MAP_IDS_BY_LEVEL[0];

        int index = Mathf.Clamp(gm.ShopLevel - 1, 0, UNLOCKED_MAP_IDS_BY_LEVEL.Length - 1);
        return UNLOCKED_MAP_IDS_BY_LEVEL[index];
    }

    /// <summary>
    /// 現在の店舗レベルで解放されている MapData の一覧を返す。
    /// _allMaps が未設定の場合は Resources からフォールバック読み込みを行う。
    /// </summary>
    public List<MapData> GetUnlockedMaps()
    {
        MapData[] maps = _allMaps;
        if (maps == null || maps.Length == 0)
        {
            maps = Resources.LoadAll<MapData>("");
        }

        var unlocked = new List<MapData>();
        int level = CurrentLevel;

        if (maps != null)
        {
            foreach (MapData map in maps)
            {
                if (map != null && map.RequiredShopLevel <= level)
                {
                    unlocked.Add(map);
                }
            }
        }

        return unlocked;
    }

    /// <summary>指定マップが現在の店舗レベルで解放済みかを返す。</summary>
    public bool IsMapUnlocked(MapData map)
    {
        if (map == null) return false;
        return map.RequiredShopLevel <= CurrentLevel;
    }

    /// <summary>現在のレベルに対応する店舗名（日本語）を返す。</summary>
    public string GetCurrentLevelName()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return LEVEL_NAMES[0];

        int index = Mathf.Clamp(gm.ShopLevel - 1, 0, LEVEL_NAMES.Length - 1);
        return LEVEL_NAMES[index];
    }
}
