// ============================================================
// SkillEffectApplier.cs
// スキル効果の静的パラメータを保持するコンポーネント。
// BattleSceneBootstrap と同じ GameObject にアタッチして使用する。
// ============================================================
using UnityEngine;

/// <summary>
/// スキル効果の静的パラメータを保持するコンポーネント。
/// バトルシステムや DropResolver 等がこの静的値を参照する。
/// </summary>
public sealed class SkillEffectApplier : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // スキル効果パラメータ
    // ──────────────────────────────────────────────

    /// <summary>攻撃力のスキル補正倍率。ダメージ計算時に参照する。</summary>
    public static float AttackMultiplier { get; set; } = 1f;

    /// <summary>ドロップ率のスキル補正加算値。DropResolver で参照する。</summary>
    public static float DropRateBonus { get; set; }

    // ──────────────────────────────────────────────
    // 食事バフパラメータ（MealBuffApplier から書き込まれる）
    // ──────────────────────────────────────────────

    /// <summary>防御力の倍率補正。</summary>
    public static float DefenseMultiplier { get; set; } = 1f;

    /// <summary>速度の倍率補正。</summary>
    public static float SpeedMultiplier { get; set; } = 1f;

    /// <summary>毎ターンHP自動回復量。</summary>
    public static int RegenPerTurn { get; set; }

    /// <summary>スカウト成功率への加算ボーナス。</summary>
    public static float ScoutChanceBonus { get; set; }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Start()
    {
        ResetAll();
    }

    /// <summary>全パラメータを初期値にリセットする。</summary>
    public static void ResetAll()
    {
        AttackMultiplier  = 1f;
        DropRateBonus     = 0f;
        DefenseMultiplier = 1f;
        SpeedMultiplier   = 1f;
        RegenPerTurn      = 0;
        ScoutChanceBonus  = 0f;
    }
}
