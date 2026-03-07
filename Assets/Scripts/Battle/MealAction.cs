using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 食事（Meal）コマンドの実行アクション。
/// 自身のHPを一定量回復してターンを消費する。
/// </summary>
public sealed class MealAction : MonoBehaviour
{
    [Header("回復量")]
    [SerializeField] private int _healAmount = 50;

    [Header("演出待機時間（秒）")]
    [SerializeField] private float _animDuration = 0.5f;

    /// <summary>回復量（外部参照用）。</summary>
    public int HealAmount => _healAmount;

    /// <summary>
    /// BattleManager 連携用コルーチン。
    /// 回復処理を行い、完了後に onComplete コールバックを呼ぶ。
    /// </summary>
    /// <param name="target">回復対象（自身）。</param>
    /// <param name="onComplete">回復完了時のコールバック（回復量を通知）。</param>
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
}
