// ============================================================
// MealBuffApplier.cs
// 食事コマンドで料理を使用した際、カテゴリに応じたバフを
// SkillEffectApplier の静的プロパティに書き込む独立コンポーネント。
// BattleManager と同じ GameObject にアタッチして使用する。
// ============================================================
using UnityEngine;

/// <summary>
/// 料理カテゴリに応じたバフを適用する。
/// <see cref="SkillEffectApplier"/> の静的プロパティへ値を加算する。
/// </summary>
public sealed class MealBuffApplier : MonoBehaviour
{
    /// <summary>
    /// 指定 DishInstance のカテゴリに基づいてバフを適用する。
    /// </summary>
    /// <param name="dish">使用する料理インスタンス。</param>
    public void ApplyBuff(DishInstance dish)
    {
        if (dish.Data == null) return;

        float buffAmount  = dish.BuffAmount;
        float scoutBonus  = dish.ScoutBonus;

        switch (dish.Category)
        {
            case DishCategory.Meat:
                SkillEffectApplier.AttackMultiplier += buffAmount;
                Debug.Log($"[MealBuffApplier] {dish} → ATK倍率 +{buffAmount:F2} (計: {SkillEffectApplier.AttackMultiplier:F2})");
                break;

            case DishCategory.Fish:
                SkillEffectApplier.SpeedMultiplier += buffAmount;
                Debug.Log($"[MealBuffApplier] {dish} → SPD倍率 +{buffAmount:F2} (計: {SkillEffectApplier.SpeedMultiplier:F2})");
                break;

            case DishCategory.Salad:
                SkillEffectApplier.DefenseMultiplier += buffAmount;
                Debug.Log($"[MealBuffApplier] {dish} → DEF倍率 +{buffAmount:F2} (計: {SkillEffectApplier.DefenseMultiplier:F2})");
                break;

            case DishCategory.Dessert:
                int regenAmount = Mathf.RoundToInt(buffAmount * 100f);
                SkillEffectApplier.RegenPerTurn += regenAmount;
                Debug.Log($"[MealBuffApplier] {dish} → リジェネ +{regenAmount}/ターン (計: {SkillEffectApplier.RegenPerTurn})");
                break;
        }

        // 全カテゴリ共通: スカウトボーナス加算
        SkillEffectApplier.ScoutChanceBonus += scoutBonus;
        Debug.Log($"[MealBuffApplier] スカウトボーナス +{scoutBonus:F3} (計: {SkillEffectApplier.ScoutChanceBonus:F3})");
    }
}
