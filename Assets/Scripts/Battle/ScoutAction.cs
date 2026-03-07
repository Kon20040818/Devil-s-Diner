using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// スカウト（Scout）コマンドの実行アクション。
/// 対象の敵の残りHP割合が低いほど成功確率が上がり、
/// 成功すると敵がバトルから除外されて「雇用リスト」に入る。
/// </summary>
public sealed class ScoutAction : MonoBehaviour
{
    [Header("スカウト確率カーブ")]
    [Tooltip("敵HP100%時の成功確率")]
    [SerializeField] private float _minChance = 0.10f;

    [Tooltip("敵HP0%時の成功確率")]
    [SerializeField] private float _maxChance = 0.90f;

    [Tooltip("確率が急上昇し始めるHP割合（これ以下で加速）")]
    [SerializeField] private float _criticalHPRatio = 0.3f;

    [Header("演出")]
    [SerializeField] private float _animDuration = 0.5f;

    /// <summary>
    /// スカウトを実行するコルーチン。
    /// </summary>
    /// <param name="target">スカウト対象の敵。</param>
    /// <param name="onComplete">完了コールバック（成功=true, 失敗=false）。</param>
    public IEnumerator ExecuteActionCoroutine(CharacterBattleController target, Action<bool> onComplete)
    {
        if (target == null || !target.IsAlive)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        // ── 成功確率の計算 ──
        float hpRatio = target.MaxHP > 0
            ? (float)target.CurrentHP / target.MaxHP
            : 1f;

        // HP割合が _criticalHPRatio 以下で急上昇するカーブ
        // hpRatio=1.0 → _minChance, hpRatio=0.0 → _maxChance
        // _criticalHPRatio 以下では加速度的に上昇
        float chance;
        if (hpRatio <= _criticalHPRatio)
        {
            // 瀕死域: _criticalHPRatio→0 で midChance→_maxChance
            float midChance = Mathf.Lerp(_maxChance, _minChance, _criticalHPRatio);
            float t = hpRatio / _criticalHPRatio; // 1→0
            chance = Mathf.Lerp(_maxChance, midChance, t);
        }
        else
        {
            // 通常域: 1.0→_criticalHPRatio で _minChance→midChance
            float midChance = Mathf.Lerp(_maxChance, _minChance, _criticalHPRatio);
            float t = (hpRatio - _criticalHPRatio) / (1f - _criticalHPRatio); // 0→1
            chance = Mathf.Lerp(midChance, _minChance, t);
        }

        float roll = UnityEngine.Random.Range(0f, 1f);
        bool success = roll <= chance;

        Debug.Log($"[ScoutAction] {target.DisplayName} をスカウト！ HP: {target.CurrentHP}/{target.MaxHP} ({hpRatio:P0}) 成功確率: {chance:P0} 判定: {roll:F3} → {(success ? "成功！" : "失敗...")}");

        // TODO: スカウト演出エフェクト再生
        // TODO: SE再生

        if (success)
        {
            Debug.Log($"<color=cyan>[ScoutAction] スカウト成功！ {target.DisplayName} を雇用した！</color>");
        }
        else
        {
            Debug.Log($"<color=red>[ScoutAction] スカウト失敗... {target.DisplayName} は怒っている！</color>");
        }

        yield return new WaitForSeconds(_animDuration);

        onComplete?.Invoke(success);
    }
}
