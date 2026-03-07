using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 食事（Meal）コマンドの実行アクション。
/// 自身のHPを回復し、料理カテゴリに応じたバフを付与してターンを消費する。
/// </summary>
public sealed class MealAction : MonoBehaviour
{
    [Header("デフォルト回復量（DishInstance 未指定時）")]
    [SerializeField] private int _healAmount = 50;

    [Header("演出待機時間（秒）")]
    [SerializeField] private float _animDuration = 0.5f;

    /// <summary>デフォルト回復量（外部参照用）。</summary>
    public int HealAmount => _healAmount;

    // ──────────────────────────────────────────────
    // バフ適用コンポーネント参照
    // ──────────────────────────────────────────────

    private MealBuffApplier _buffApplier;

    /// <summary>バフ適用コンポーネントを外部から設定する。</summary>
    public void SetBuffApplier(MealBuffApplier applier) => _buffApplier = applier;

    // ──────────────────────────────────────────────
    // 旧パス（固定回復、DishInstance なし）
    // ──────────────────────────────────────────────

    /// <summary>
    /// 固定回復量で回復するコルーチン（旧互換）。
    /// </summary>
    public IEnumerator ExecuteActionCoroutine(CharacterBattleController target, Action<int> onComplete)
    {
        if (target == null || !target.IsAlive)
        {
            onComplete?.Invoke(0);
            yield break;
        }

        int hpBefore = target.CurrentHP;
        target.Heal(_healAmount);
        int actualHeal = target.CurrentHP - hpBefore;

        Debug.Log($"[MealAction] {target.DisplayName} は手作り弁当を食べた！ HPが{actualHeal}回復した！ (HP: {target.CurrentHP}/{target.MaxHP})");

        // TODO: 回復エフェクト再生
        // TODO: SE再生

        yield return new WaitForSeconds(_animDuration);

        onComplete?.Invoke(actualHeal);
    }

    // ──────────────────────────────────────────────
    // 新パス（DishInstance 対応 — 回復＋バフ）
    // ──────────────────────────────────────────────

    /// <summary>
    /// DishInstance を使用して回復＋カテゴリバフを付与するコルーチン。
    /// </summary>
    /// <param name="target">回復対象（自身）。</param>
    /// <param name="dish">使用する料理インスタンス。</param>
    /// <param name="onComplete">回復完了時のコールバック（回復量を通知）。</param>
    public IEnumerator ExecuteActionCoroutine(CharacterBattleController target, DishInstance dish, Action<int> onComplete)
    {
        if (target == null || !target.IsAlive)
        {
            onComplete?.Invoke(0);
            yield break;
        }

        // HP回復
        int healAmount = dish.HealAmount;
        int hpBefore = target.CurrentHP;
        target.Heal(healAmount);
        int actualHeal = target.CurrentHP - hpBefore;

        Debug.Log($"[MealAction] {target.DisplayName} は {dish} を食べた！ HPが{actualHeal}回復した！ (HP: {target.CurrentHP}/{target.MaxHP})");

        // カテゴリバフ適用
        if (_buffApplier != null)
        {
            _buffApplier.ApplyBuff(dish);
        }

        // TODO: 回復エフェクト再生
        // TODO: SE再生

        yield return new WaitForSeconds(_animDuration);

        onComplete?.Invoke(actualHeal);
    }
}
