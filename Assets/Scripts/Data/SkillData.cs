// ============================================================
// SkillData.cs
// スキルツリーの1ノードを定義する ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>
/// スキルツリーの1ノードを表すデータアセット。
/// 各スキルは種類・コスト・効果量・前提スキルを持つ。
/// </summary>
[CreateAssetMenu(fileName = "SKL_New", menuName = "DevilsDiner/SkillData")]
public sealed class SkillData : ScriptableObject
{
    // ──────────────────────────────────────────────
    // 列挙型
    // ──────────────────────────────────────────────

    /// <summary>スキルの種類。</summary>
    public enum SkillType
    {
        /// <summary>最大HPアップ。</summary>
        MaxHPUp,
        /// <summary>ジャスト入力受付フレーム延長。</summary>
        JustFrameExtend,
        /// <summary>攻撃力アップ。</summary>
        AttackUp,
        /// <summary>ドロップ率アップ。</summary>
        DropRateUp,
        /// <summary>調理速度アップ。</summary>
        CookingSpeedUp,
        /// <summary>回避距離アップ。</summary>
        DodgeDistanceUp,
        /// <summary>回避無敵時間延長。</summary>
        DodgeInvincibleUp
    }

    // ──────────────────────────────────────────────
    // シリアライズフィールド
    // ──────────────────────────────────────────────

    /// <summary>スキルの一意な識別子。</summary>
    [SerializeField] private string _id;

    /// <summary>スキルの表示名。</summary>
    [SerializeField] private string _skillName;

    /// <summary>スキルの説明文。</summary>
    [SerializeField] private string _description;

    /// <summary>スキルの種類。</summary>
    [SerializeField] private SkillType _type;

    /// <summary>解放に必要なゴールド。</summary>
    [SerializeField] private int _cost;

    /// <summary>効果量（例: +20 HP, +2 フレーム, +10% 攻撃力）。</summary>
    [SerializeField] private float _value;

    /// <summary>前提スキル。null の場合は前提なし。</summary>
    [SerializeField] private SkillData _prerequisite;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>スキルの一意な識別子。</summary>
    public string Id => _id;

    /// <summary>スキルの表示名。</summary>
    public string SkillName => _skillName;

    /// <summary>スキルの説明文。</summary>
    public string Description => _description;

    /// <summary>スキルの種類。</summary>
    public SkillType Type => _type;

    /// <summary>解放に必要なゴールド。</summary>
    public int Cost => _cost;

    /// <summary>効果量。</summary>
    public float Value => _value;

    /// <summary>前提スキル。null の場合は前提なし。</summary>
    public SkillData Prerequisite => _prerequisite;
}
