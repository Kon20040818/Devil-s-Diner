// ============================================================
// EnemyAIController.cs
// 敵AIの行動・ターゲット選択を担当する独立コンポーネント。
// BattleManager から呼ばれ、重み付きランダムで意思決定する。
// ============================================================
using UnityEngine;

/// <summary>
/// 敵ターン時の行動選択AIコントローラー。
/// BattleSceneBootstrap で自動生成され、BattleManager.EnemyTurn() から呼び出される。
/// </summary>
public sealed class EnemyAIController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector — 行動重み（プランナー調整可能）
    // ──────────────────────────────────────────────

    [Header("行動重み")]
    [Tooltip("通常攻撃の選択重み")]
    [SerializeField] private float _basicAttackWeight = 1.0f;

    [Tooltip("スキル攻撃の選択重み")]
    [SerializeField] private float _skillWeight = 0.5f;

    [Tooltip("必殺技の選択重み (EP満タン時のみ候補)")]
    [SerializeField] private float _ultimateWeight = 2.0f;

    [Header("ターゲット選択")]
    [Tooltip("ブレイク中のキャラを狙う際の重み倍率")]
    [SerializeField] private float _brokenTargetMultiplier = 2.0f;

    // ──────────────────────────────────────────────
    // AI 意思決定結果
    // ──────────────────────────────────────────────

    /// <summary>AIの意思決定結果。</summary>
    public struct AIDecision
    {
        public CharacterBattleController.ActionType Action;
        public CharacterBattleController Target;
    }

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 行動選択とターゲット選択を行い、結果を返す。
    /// </summary>
    /// <param name="actor">行動する敵キャラクター。</param>
    /// <param name="opponents">攻撃対象の味方パーティ。</param>
    public AIDecision Decide(CharacterBattleController actor, CharacterBattleController[] opponents)
    {
        var decision = new AIDecision
        {
            Action = ChooseAction(actor),
            Target = ChooseTarget(opponents)
        };

        Debug.Log($"[EnemyAI] {actor.DisplayName} → {decision.Action} → {(decision.Target != null ? decision.Target.DisplayName : "なし")}");
        return decision;
    }

    // ──────────────────────────────────────────────
    // 行動選択（重み付きランダム）
    // ──────────────────────────────────────────────

    private CharacterBattleController.ActionType ChooseAction(CharacterBattleController actor)
    {
        float totalWeight = 0f;

        // 通常攻撃: 常に候補
        float wBasic = _basicAttackWeight;
        totalWeight += wBasic;

        // スキル: 常に候補（敵はSP制約なし）
        float wSkill = _skillWeight;
        totalWeight += wSkill;

        // 必殺技: EP満タン時のみ候補
        float wUltimate = 0f;
        if (actor.IsUltimateReady)
        {
            wUltimate = _ultimateWeight;
            totalWeight += wUltimate;
        }

        // 重み付きランダム選択
        float roll = Random.Range(0f, totalWeight);

        if (roll < wBasic)
            return CharacterBattleController.ActionType.BasicAttack;

        roll -= wBasic;
        if (roll < wSkill)
            return CharacterBattleController.ActionType.Skill;

        return CharacterBattleController.ActionType.Ultimate;
    }

    // ──────────────────────────────────────────────
    // ターゲット選択（HP割合ベース重み付き）
    // ──────────────────────────────────────────────

    private CharacterBattleController ChooseTarget(CharacterBattleController[] opponents)
    {
        if (opponents == null || opponents.Length == 0) return null;

        // 生存キャラのみ
        float totalWeight = 0f;
        int aliveCount = 0;

        foreach (var c in opponents)
        {
            if (c == null || !c.IsAlive) continue;
            aliveCount++;
            totalWeight += GetTargetWeight(c);
        }

        if (aliveCount == 0) return null;

        // 重み付きランダム選択
        float roll = Random.Range(0f, totalWeight);

        foreach (var c in opponents)
        {
            if (c == null || !c.IsAlive) continue;

            float w = GetTargetWeight(c);
            if (roll <= w) return c;
            roll -= w;
        }

        // フォールバック: 最後の生存キャラ
        foreach (var c in opponents)
        {
            if (c != null && c.IsAlive) return c;
        }

        return null;
    }

    /// <summary>
    /// ターゲットの重みを計算する。
    /// HP割合が低いほど重みが高い（逆数ベース）。ブレイク中は倍率加算。
    /// </summary>
    private float GetTargetWeight(CharacterBattleController target)
    {
        float hpRatio = target.MaxHP > 0
            ? (float)target.CurrentHP / target.MaxHP
            : 1f;

        // HP割合の逆数（0.1～1.0 の範囲にクランプして逆数化）
        float weight = 1f / Mathf.Max(hpRatio, 0.1f);

        // ブレイク中は追加で狙われやすい
        if (target.IsBroken)
        {
            weight *= _brokenTargetMultiplier;
        }

        return weight;
    }
}
