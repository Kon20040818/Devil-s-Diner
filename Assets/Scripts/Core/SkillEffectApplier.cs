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
    /// <summary>攻撃力のスキル補正倍率。ダメージ計算時に参照する。</summary>
    public static float AttackMultiplier { get; private set; } = 1f;

    /// <summary>ドロップ率のスキル補正加算値。DropResolver で参照する。</summary>
    public static float DropRateBonus { get; private set; }

    private void Start()
    {
        ResetStaticFields();
    }

    private void ResetStaticFields()
    {
        AttackMultiplier = 1f;
        DropRateBonus = 0f;
    }
}
