// ============================================================
// BuffDurationTracker.cs
// 食事バフの持続ターン数を追跡し、期限切れ時に
// SkillEffectApplier から効果を除去する独立コンポーネント。
// BattleManager と同じ GameObject にアタッチして使用する。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 食事バフの持続ターン管理。毎ターン終了時に呼ばれ、
/// デクリメント → 期限切れ除去 → リジェネ適用を行う。
/// </summary>
public sealed class BuffDurationTracker : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 内部データ構造
    // ──────────────────────────────────────────────

    /// <summary>アクティブなバフ1件分。</summary>
    [Serializable]
    public struct ActiveBuff
    {
        /// <summary>バフ量（SkillEffectApplier に加算した値）。</summary>
        public float Amount;
        /// <summary>残りターン数。</summary>
        public int RemainingTurns;
        /// <summary>スカウトボーナス加算量。</summary>
        public float ScoutBonus;
    }

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private readonly Dictionary<DishCategory, ActiveBuff> _activeBuffs
        = new Dictionary<DishCategory, ActiveBuff>();

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>バフが適用されたとき (カテゴリ, 残ターン)。</summary>
    public event Action<DishCategory, int> OnBuffApplied;

    /// <summary>バフが期限切れで除去されたとき。</summary>
    public event Action<DishCategory> OnBuffExpired;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 料理使用時にバフを登録する。
    /// 同カテゴリの既存バフがある場合はターン数のみリフレッシュ（量スタックなし）。
    /// </summary>
    public void RegisterBuff(DishInstance dish)
    {
        if (dish.Data == null) return;

        DishCategory category = dish.Category;
        float buffAmount = dish.BuffAmount;
        float scoutBonus = dish.ScoutBonus;
        int duration = dish.BuffDurationTurns;

        if (_activeBuffs.TryGetValue(category, out ActiveBuff existing))
        {
            // 同カテゴリ: ターン数リフレッシュのみ
            existing.RemainingTurns = duration;
            _activeBuffs[category] = existing;
            Debug.Log($"[BuffDurationTracker] {category} バフをリフレッシュ: 残{duration}ターン");
        }
        else
        {
            // 新規登録
            _activeBuffs[category] = new ActiveBuff
            {
                Amount         = buffAmount,
                RemainingTurns = duration,
                ScoutBonus     = scoutBonus
            };
            Debug.Log($"[BuffDurationTracker] {category} バフ登録: 量={buffAmount:F2} 残{duration}ターン");
        }

        OnBuffApplied?.Invoke(category, duration);
    }

    /// <summary>
    /// ターン終了時に呼ぶ。リジェネ適用 → デクリメント → 期限切れ除去。
    /// </summary>
    /// <param name="activeCharacter">現在のアクティブキャラクター。</param>
    public void ProcessTurnEnd(CharacterBattleController activeCharacter)
    {
        // 1. RegenPerTurn 適用（味方キャラのターンのみ）
        if (activeCharacter != null && activeCharacter.IsAlive
            && activeCharacter.CharacterFaction == CharacterBattleController.Faction.Player)
        {
            int regen = SkillEffectApplier.RegenPerTurn;
            if (regen > 0)
            {
                int hpBefore = activeCharacter.CurrentHP;
                activeCharacter.Heal(regen);
                int actualHeal = activeCharacter.CurrentHP - hpBefore;
                if (actualHeal > 0)
                {
                    Debug.Log($"[BuffDurationTracker] リジェネ: {activeCharacter.DisplayName} +{actualHeal} HP");
                }
            }
        }

        // 2. デクリメント + 期限切れ処理
        var expiredCategories = new List<DishCategory>();
        var categories = new List<DishCategory>(_activeBuffs.Keys);

        foreach (var category in categories)
        {
            var buff = _activeBuffs[category];
            buff.RemainingTurns--;

            if (buff.RemainingTurns <= 0)
            {
                expiredCategories.Add(category);
            }
            else
            {
                _activeBuffs[category] = buff;
            }
        }

        // 3. 期限切れバフを SkillEffectApplier から差し引いて除去
        foreach (var category in expiredCategories)
        {
            var buff = _activeBuffs[category];
            RemoveBuffFromApplier(category, buff);
            _activeBuffs.Remove(category);
            OnBuffExpired?.Invoke(category);
            Debug.Log($"[BuffDurationTracker] {category} バフ期限切れ！ 効果を除去。");
        }
    }

    /// <summary>現在アクティブなバフ情報を返す（UI用）。</summary>
    public IReadOnlyDictionary<DishCategory, ActiveBuff> GetActiveBuffs() => _activeBuffs;

    /// <summary>全バフをクリアして SkillEffectApplier からも除去する。</summary>
    public void ClearAll()
    {
        foreach (var kvp in _activeBuffs)
        {
            RemoveBuffFromApplier(kvp.Key, kvp.Value);
            OnBuffExpired?.Invoke(kvp.Key);
        }
        _activeBuffs.Clear();
    }

    // ──────────────────────────────────────────────
    // 内部ヘルパー
    // ──────────────────────────────────────────────

    /// <summary>バフ効果を SkillEffectApplier から差し引く。</summary>
    private void RemoveBuffFromApplier(DishCategory category, ActiveBuff buff)
    {
        switch (category)
        {
            case DishCategory.Meat:
                SkillEffectApplier.AttackMultiplier -= buff.Amount;
                break;
            case DishCategory.Fish:
                SkillEffectApplier.SpeedMultiplier -= buff.Amount;
                break;
            case DishCategory.Salad:
                SkillEffectApplier.DefenseMultiplier -= buff.Amount;
                break;
            case DishCategory.Dessert:
                int regenAmount = Mathf.RoundToInt(buff.Amount * 100f);
                SkillEffectApplier.RegenPerTurn -= regenAmount;
                break;
        }
        SkillEffectApplier.ScoutChanceBonus -= buff.ScoutBonus;
    }
}
