// ============================================================
// CookingManager.cs
// 経営パートの料理生産ロジック。
// レシピ選択 → 素材消費 → 品質決定 → DishInstance 生成。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 料理システムのコアロジック。
/// ManagementScene で使用し、レシピ一覧の管理と調理実行を担当する。
/// </summary>
public sealed class CookingManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 品質閾値
    // ──────────────────────────────────────────────

    [Header("品質スコア閾値")]
    [SerializeField, Tooltip("Normal の下限スコア")]
    private float _normalThreshold = 1.0f;
    [SerializeField, Tooltip("Fine の下限スコア")]
    private float _fineThreshold = 2.0f;
    [SerializeField, Tooltip("Exquisite の下限スコア")]
    private float _exquisiteThreshold = 3.5f;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>調理完了時。引数は生成された DishInstance と品質。</summary>
    public event Action<CookResult> OnDishCooked;

    // ──────────────────────────────────────────────
    // 結果構造体
    // ──────────────────────────────────────────────

    /// <summary>調理結果。</summary>
    public struct CookResult
    {
        public DishInstance Dish;
        public float QualityScore;
        public bool Success;
    }

    // ──────────────────────────────────────────────
    // シェフレベル
    // ──────────────────────────────────────────────

    private int _chefLevel = 1;

    /// <summary>現在のシェフレベル。</summary>
    public int ChefLevel
    {
        get => _chefLevel;
        set => _chefLevel = Mathf.Max(1, value);
    }

    // ──────────────────────────────────────────────
    // レシピ一覧
    // ──────────────────────────────────────────────

    /// <summary>全レシピをロードして解放済みのものだけ返す。</summary>
    public List<RecipeData> GetAvailableRecipes()
    {
        RecipeData[] allRecipes = Resources.LoadAll<RecipeData>("");
        var available = new List<RecipeData>();

        foreach (var recipe in allRecipes)
        {
            if (recipe.RequiredChefLevel <= _chefLevel)
            {
                available.Add(recipe);
            }
        }

        return available;
    }

    // ──────────────────────────────────────────────
    // 調理可否チェック
    // ──────────────────────────────────────────────

    /// <summary>指定レシピの素材が足りているか判定する。</summary>
    public bool CanCook(RecipeData recipe)
    {
        if (recipe == null || recipe.OutputDish == null) return false;
        if (recipe.RequiredChefLevel > _chefLevel) return false;
        if (GameManager.Instance == null) return false;

        var inventory = GameManager.Instance.Inventory;
        foreach (var slot in recipe.Ingredients)
        {
            if (slot.Ingredient == null) continue;
            if (!inventory.Has(slot.Ingredient, slot.Amount)) return false;
        }

        return true;
    }

    // ──────────────────────────────────────────────
    // 調理実行
    // ──────────────────────────────────────────────

    /// <summary>
    /// レシピに基づき料理を生成する。
    /// 素材を消費し、品質を決定し、インベントリに追加する。
    /// </summary>
    public CookResult Cook(RecipeData recipe, float dailyFreshnessBuff = 1f, CalendarEventData calendarEvent = null)
    {
        var result = new CookResult { Success = false };

        if (!CanCook(recipe))
        {
            Debug.LogWarning($"[CookingManager] {recipe.DisplayName} の調理条件を満たしていません。");
            return result;
        }

        var inventory = GameManager.Instance.Inventory;

        // 素材消費
        foreach (var slot in recipe.Ingredients)
        {
            if (slot.Ingredient == null) continue;
            inventory.Remove(slot.Ingredient, slot.Amount);
        }

        // 品質決定
        StaffBuffSummary staffBuffs = default;
        if (GameManager.Instance.Staff != null)
        {
            staffBuffs = GameManager.Instance.Staff.GetActiveBonuses();
        }

        float qualityScore = CalculateQualityScore(recipe, dailyFreshnessBuff, calendarEvent, staffBuffs);
        DishQuality quality = ScoreToQuality(qualityScore);

        var dish = new DishInstance(recipe.OutputDish, quality);

        // インベントリに追加
        inventory.AddDish(dish);

        result.Dish = dish;
        result.QualityScore = qualityScore;
        result.Success = true;

        Debug.Log($"[CookingManager] {recipe.DisplayName} 調理完了 → {quality} (スコア: {qualityScore:F2})");

        OnDishCooked?.Invoke(result);
        return result;
    }

    // ──────────────────────────────────────────────
    // 品質算出
    // ──────────────────────────────────────────────

    /// <summary>
    /// 品質スコアを計算する。
    /// スコア = 平均レア度 × 鮮度バフ × カレンダーボーナス × (1 + スタッフバフ)
    /// </summary>
    private float CalculateQualityScore(
        RecipeData recipe,
        float freshnessBuff,
        CalendarEventData calendarEvent,
        StaffBuffSummary staffBuffs)
    {
        float baseScore = recipe.AverageIngredientRarity();

        // 鮮度バフ（バトル成績由来）+ スタッフの鮮度ボーナス
        float freshnessMultiplier = Mathf.Max(0.5f, freshnessBuff + staffBuffs.FreshnessBonus);

        // カレンダーボーナス
        float calendarMultiplier = 1f;
        if (calendarEvent != null)
        {
            // カテゴリ限定チェック
            if (!calendarEvent.BonusCategoryEnabled || calendarEvent.BonusCategory == recipe.Category)
            {
                calendarMultiplier = calendarEvent.FreshnessMultiplier;
            }
        }

        // スタッフバフ（全体品質 + カテゴリ特化）
        float staffMultiplier = 1f + staffBuffs.QualityBonus + staffBuffs.GetCategoryBonus(recipe.Category);

        return baseScore * freshnessMultiplier * calendarMultiplier * staffMultiplier;
    }

    /// <summary>品質スコアを DishQuality に変換する。</summary>
    private DishQuality ScoreToQuality(float score)
    {
        if (score >= _exquisiteThreshold) return DishQuality.Exquisite;
        if (score >= _fineThreshold) return DishQuality.Fine;
        if (score >= _normalThreshold) return DishQuality.Normal;
        return DishQuality.Poor;
    }
}
