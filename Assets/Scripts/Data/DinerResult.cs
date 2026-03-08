// ============================================================
// DinerResult.cs
// 店舗営業の結果構造体。
// ============================================================

/// <summary>
/// 1日の店舗営業の結果データ。
/// <see cref="DinerService.RunService"/> が返す。
/// </summary>
public struct DinerResult
{
    /// <summary>売上合計（ゴールド）。</summary>
    public int TotalRevenue;

    /// <summary>チップ合計（ゴールド）。</summary>
    public int TotalTips;

    /// <summary>接客した客数。</summary>
    public int CustomersServed;

    /// <summary>平均満足度。</summary>
    public float AverageSatisfaction;

    /// <summary>評判の変動値。</summary>
    public int ReputationChange;

    /// <summary>総収入（売上+チップ）。</summary>
    public int TotalEarnings => TotalRevenue + TotalTips;
}
