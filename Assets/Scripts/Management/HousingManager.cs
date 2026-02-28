// ============================================================
// HousingManager.cs
// 家具の配置管理と居心地度（ComfortScore）の算出を行う。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 家具の配置を管理し、居心地度（ComfortScore）を算出する。
/// ComfortScore に基づいてチップボーナス率やスポーン間隔の短縮率を提供する。
/// </summary>
public sealed class HousingManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    /// <summary>居心地ポイント1あたりのチップボーナス率（2%）。</summary>
    private const float TIP_BONUS_PER_COMFORT = 0.02f;

    /// <summary>居心地ポイント1あたりのスポーン間隔短縮率（1%）。</summary>
    private const float SPAWN_REDUCTION_PER_COMFORT = 0.01f;

    /// <summary>スポーン間隔の最小倍率（基本間隔の30%が下限）。</summary>
    private const float MIN_SPAWN_INTERVAL_MULTIPLIER = 0.3f;

    // ──────────────────────────────────────────────
    // シリアライズフィールド
    // ──────────────────────────────────────────────

    [SerializeField] private DinerManager _dinerManager;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>ComfortScore が変化したとき。引数は新しいスコア。</summary>
    public event Action<float> OnComfortScoreChanged;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    private readonly List<FurnitureData> _placedFurniture = new List<FurnitureData>();

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>配置済み家具の ComfortBonus 合計値。</summary>
    public float ComfortScore
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < _placedFurniture.Count; i++)
            {
                if (_placedFurniture[i] != null)
                {
                    total += _placedFurniture[i].ComfortBonus;
                }
            }
            return total;
        }
    }

    /// <summary>
    /// ComfortScore に基づくチップボーナス率。
    /// 例: ComfortScore = 10 → 20% ボーナス。
    /// </summary>
    public float TipBonusRate => ComfortScore * TIP_BONUS_PER_COMFORT;

    /// <summary>
    /// ComfortScore に基づくスポーン間隔の倍率。
    /// 1.0 = 短縮なし、0.3 = 最小（70%短縮）。
    /// </summary>
    public float SpawnIntervalMultiplier =>
        Mathf.Max(MIN_SPAWN_INTERVAL_MULTIPLIER, 1f - ComfortScore * SPAWN_REDUCTION_PER_COMFORT);

    /// <summary>配置済み家具リスト（読み取り専用）。</summary>
    public IReadOnlyList<FurnitureData> PlacedFurniture => _placedFurniture;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 家具を配置リストに追加し、DinerManager と同期する。
    /// </summary>
    public void PlaceFurniture(FurnitureData furniture)
    {
        if (furniture == null) return;

        _placedFurniture.Add(furniture);

        if (_dinerManager != null)
        {
            _dinerManager.PlaceFurniture(furniture);
        }

        OnComfortScoreChanged?.Invoke(ComfortScore);
    }

    /// <summary>
    /// 家具を配置リストから除去し、DinerManager と同期する。
    /// </summary>
    public void RemoveFurniture(FurnitureData furniture)
    {
        if (furniture == null) return;

        if (!_placedFurniture.Remove(furniture)) return;

        if (_dinerManager != null)
        {
            _dinerManager.RemoveFurniture(furniture);
        }

        OnComfortScoreChanged?.Invoke(ComfortScore);
    }

    /// <summary>
    /// インベントリの全家具を配置リストに読み込む。
    /// Evening フェーズ開始時や初期化時に呼び出す。
    /// </summary>
    public void InitializeFromInventory()
    {
        if (GameManager.Instance == null || GameManager.Instance.Inventory == null)
        {
            Debug.LogWarning("[HousingManager] GameManager または Inventory が見つかりません。");
            return;
        }

        _placedFurniture.Clear();

        IReadOnlyList<FurnitureData> inventoryFurniture = GameManager.Instance.Inventory.Furniture;
        for (int i = 0; i < inventoryFurniture.Count; i++)
        {
            if (inventoryFurniture[i] != null)
            {
                _placedFurniture.Add(inventoryFurniture[i]);
            }
        }

        // DinerManager の配置リストも同期
        if (_dinerManager != null)
        {
            for (int i = 0; i < _placedFurniture.Count; i++)
            {
                _dinerManager.PlaceFurniture(_placedFurniture[i]);
            }
        }

        Debug.Log($"[HousingManager] 家具初期化完了 — {_placedFurniture.Count} 個配置, ComfortScore: {ComfortScore:F1}");
        OnComfortScoreChanged?.Invoke(ComfortScore);
    }

    // ──────────────────────────────────────────────
    // 内部メソッド
    // ──────────────────────────────────────────────

    /// <summary>
    /// フェーズ変更ハンドラ。Evening フェーズ開始時にインベントリから家具を読み込む。
    /// </summary>
    private void HandlePhaseChanged(GameManager.GamePhase newPhase)
    {
        if (newPhase == GameManager.GamePhase.Evening)
        {
            InitializeFromInventory();
        }
    }
}
