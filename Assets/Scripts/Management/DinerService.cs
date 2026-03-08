// ============================================================
// DinerService.cs
// 店舗営業の即時結果計算。
// メニュー（DishInstance 配列）を受け取り、売上・満足度を即時算出する。
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// 店舗営業シミュレーション（即時結果計算版）。
/// ManagementScene で「営業開始」ボタン押下時に呼ばれ、
/// 提供メニューとスタッフバフから売上・評判を一括計算する。
/// </summary>
public sealed class DinerService : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("営業パラメータ")]
    [SerializeField, Tooltip("基本客数"), Min(1)]
    private int _baseCustomerCount = 5;

    [SerializeField, Tooltip("満足度→チップ変換倍率")]
    private float _tipMultiplier = 0.1f;

    [SerializeField, Tooltip("満足度→評判変換倍率")]
    private float _reputationMultiplier = 0.05f;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>営業結果確定時。</summary>
    public event Action<DinerResult> OnServiceEnd;

    // ──────────────────────────────────────────────
    // 営業実行
    // ──────────────────────────────────────────────

    /// <summary>
    /// 提供メニューから営業結果を即時計算する。
    /// </summary>
    /// <param name="menu">今日提供する料理の配列。</param>
    /// <param name="calendarEvent">当日のカレンダーイベント（なければ null）。</param>
    public DinerResult RunService(DishInstance[] menu, CalendarEventData calendarEvent = null)
    {
        if (menu == null || menu.Length == 0)
        {
            Debug.LogWarning("[DinerService] メニューが空のため営業できません。");
            return default;
        }

        StaffBuffSummary staffBuffs = default;
        if (GameManager.Instance != null && GameManager.Instance.Staff != null)
        {
            staffBuffs = GameManager.Instance.Staff.GetActiveBonuses();
        }

        int totalRevenue = 0;
        int totalTips = 0;
        float totalSatisfaction = 0f;
        int customersServed = _baseCustomerCount;

        // 各客に対しランダムにメニューからオーダー
        for (int i = 0; i < customersServed; i++)
        {
            DishInstance dish = menu[UnityEngine.Random.Range(0, menu.Length)];

            int satisfaction = CalculateSatisfaction(dish, staffBuffs, calendarEvent);
            int price = dish.ShopPrice;
            int tip = Mathf.RoundToInt(satisfaction * _tipMultiplier);

            totalRevenue += price;
            totalTips += tip;
            totalSatisfaction += satisfaction;
        }

        var result = new DinerResult
        {
            TotalRevenue = totalRevenue,
            TotalTips = totalTips,
            CustomersServed = customersServed,
            AverageSatisfaction = customersServed > 0 ? totalSatisfaction / customersServed : 0f,
            ReputationChange = Mathf.RoundToInt(totalSatisfaction * _reputationMultiplier)
        };

        // ゴールド加算
        int totalEarnings = result.TotalRevenue + result.TotalTips;
        if (GameManager.Instance != null && totalEarnings > 0)
        {
            GameManager.Instance.AddGold(totalEarnings);
        }

        // メニューの料理をインベントリから消費
        if (GameManager.Instance != null)
        {
            foreach (var dish in menu)
            {
                GameManager.Instance.Inventory.RemoveDish(dish, 1);
            }
        }

        Debug.Log($"[DinerService] 営業完了！ 売上: {result.TotalRevenue}G, チップ: {result.TotalTips}G, " +
                  $"客数: {result.CustomersServed}, 平均満足度: {result.AverageSatisfaction:F1}");

        OnServiceEnd?.Invoke(result);
        return result;
    }

    // ──────────────────────────────────────────────
    // 満足度計算
    // ──────────────────────────────────────────────

    /// <summary>
    /// 個別の満足度を計算する。
    /// 基本満足度 × (1 + スタッフバフ) × カレンダーボーナス
    /// </summary>
    private int CalculateSatisfaction(
        DishInstance dish,
        StaffBuffSummary staffBuffs,
        CalendarEventData calendarEvent)
    {
        float baseSatisfaction = dish.Satisfaction;

        // スタッフバフ
        float staffMultiplier = 1f + staffBuffs.SatisfactionBonus;

        // カレンダーボーナス
        float calendarMultiplier = 1f;
        if (calendarEvent != null)
        {
            if (!calendarEvent.BonusCategoryEnabled || calendarEvent.BonusCategory == dish.Category)
            {
                calendarMultiplier = calendarEvent.SatisfactionMultiplier;
            }
        }

        return Mathf.RoundToInt(baseSatisfaction * staffMultiplier * calendarMultiplier);
    }
}
