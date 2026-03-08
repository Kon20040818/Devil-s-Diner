// ============================================================
// StaffBuffSummary.cs
// 全スタッフのバフを集計した結果構造体。
// CookingManager / DinerService が参照する。
// ============================================================
using System.Collections.Generic;

/// <summary>
/// 全配置スタッフのバフ効果を合算した値。
/// <see cref="StaffManager.GetActiveBonuses"/> で取得する。
/// </summary>
public struct StaffBuffSummary
{
    /// <summary>品質スコアへの加算値。</summary>
    public float QualityBonus;

    /// <summary>満足度への加算倍率。</summary>
    public float SatisfactionBonus;

    /// <summary>鮮度バフへの加算倍率。</summary>
    public float FreshnessBonus;

    /// <summary>調理速度ボーナス（将来用）。</summary>
    public float CookSpeedBonus;

    /// <summary>ドロップ率ボーナス（バトルバフ用）。</summary>
    public float DropRateBonus;

    /// <summary>カテゴリ別の品質ボーナス。</summary>
    private Dictionary<DishCategory, float> _categoryBonuses;

    /// <summary>カテゴリ別ボーナスを加算する。</summary>
    public void AddCategoryBonus(DishCategory category, float value)
    {
        if (_categoryBonuses == null)
            _categoryBonuses = new Dictionary<DishCategory, float>();

        if (_categoryBonuses.TryGetValue(category, out float current))
            _categoryBonuses[category] = current + value;
        else
            _categoryBonuses[category] = value;
    }

    /// <summary>指定カテゴリの品質ボーナスを取得する。</summary>
    public float GetCategoryBonus(DishCategory category)
    {
        if (_categoryBonuses == null) return 0f;
        return _categoryBonuses.TryGetValue(category, out float val) ? val : 0f;
    }
}
