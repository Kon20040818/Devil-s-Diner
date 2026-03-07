// ============================================================
// CharacterStats.cs
// 味方・敵共通のステータスを定義する ScriptableObject。
// スターレイル風の速度ベース行動値システムに対応。
// ============================================================
using UnityEngine;

/// <summary>
/// キャラクターの基本ステータスを定義するデータアセット。
/// 味方キャラ・敵共通で使用する。
/// </summary>
[CreateAssetMenu(fileName = "STAT_New", menuName = "DevilsDiner/CharacterStats")]
public sealed class CharacterStats : ScriptableObject
{
    // ──────────────────────────────────────────────
    // 属性タイプ
    // ──────────────────────────────────────────────

    /// <summary>スキルの対象範囲。</summary>
    public enum TargetingMode
    {
        /// <summary>単体攻撃。</summary>
        Single,
        /// <summary>全敵対象の範囲攻撃。</summary>
        AllEnemies
    }

    /// <summary>属性。弱点・耐性の計算に使用。</summary>
    public enum ElementType
    {
        Physical,
        Fire,
        Ice,
        Lightning,
        Wind,
        Dark
    }

    // ──────────────────────────────────────────────
    // 基本情報
    // ──────────────────────────────────────────────

    [Header("基本情報")]
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _portrait;
    [SerializeField] private ElementType _element = ElementType.Physical;

    // ──────────────────────────────────────────────
    // バトルステータス
    // ──────────────────────────────────────────────

    [Header("バトルステータス")]
    [SerializeField] private int _maxHP = 100;
    [SerializeField] private int _attack = 20;
    [SerializeField] private int _defense = 10;
    [SerializeField] private int _speed = 100;

    [Header("属性耐性 (0.0=等倍, -0.5=弱点, 0.5=耐性, 1.0=無効)")]
    [SerializeField] private float _physicalRes;
    [SerializeField] private float _fireRes;
    [SerializeField] private float _iceRes;
    [SerializeField] private float _lightningRes;
    [SerializeField] private float _windRes;
    [SerializeField] private float _darkRes;

    // ──────────────────────────────────────────────
    // 行動値 (Action Value) 関連
    // ──────────────────────────────────────────────

    [Header("エナジー (EP)")]
    [Tooltip("EP最大値。最大時に必殺技が使用可能")]
    [SerializeField] private int _maxEP = 120;

    [Tooltip("通常攻撃ヒット時のEP獲得量")]
    [SerializeField] private int _epGainOnAttack = 20;

    [Tooltip("被ダメージ時のEP獲得量")]
    [SerializeField] private int _epGainOnHit = 10;

    [Tooltip("スキル使用時のEP獲得量")]
    [SerializeField] private int _epGainOnSkill = 30;

    [Header("スキル倍率")]
    [Tooltip("スキル攻撃のダメージ倍率 (通常攻撃比)")]
    [SerializeField] private float _skillMultiplier = 1.5f;

    [Tooltip("必殺技のダメージ倍率 (通常攻撃比)")]
    [SerializeField] private float _ultimateMultiplier = 3.0f;

    [Header("スキル対象")]
    [Tooltip("スキルの対象範囲。AllEnemies で全体攻撃")]
    [SerializeField] private TargetingMode _skillTargetMode = TargetingMode.Single;

    [Header("靭性(タフネス)")]
    [Tooltip("靭性の最大値。0で靭性システム無効（主に敵用）")]
    [SerializeField] private int _maxToughness;

    [Header("弱点属性リスト")]
    [Tooltip("この敵が弱点を持つ属性。弱点突きで靭性ゲージが減少する")]
    [SerializeField] private ElementType[] _weakElements = new ElementType[0];

    [Header("行動値")]
    [Tooltip("初期行動値 (0で即行動。大きいほど遅い)")]
    [SerializeField] private float _baseActionValue = 10000f;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Portrait => _portrait;
    public ElementType Element => _element;

    public int MaxHP => _maxHP;
    public int Attack => _attack;
    public int Defense => _defense;
    public int Speed => _speed;

    public int MaxEP => _maxEP;
    public int EPGainOnAttack => _epGainOnAttack;
    public int EPGainOnHit => _epGainOnHit;
    public int EPGainOnSkill => _epGainOnSkill;
    public float SkillMultiplier => _skillMultiplier;
    public float UltimateMultiplier => _ultimateMultiplier;
    public TargetingMode SkillTargetMode => _skillTargetMode;

    public int MaxToughness => _maxToughness;
    public ElementType[] WeakElements => _weakElements;

    /// <summary>指定属性が弱点かどうか判定。</summary>
    public bool IsWeakTo(ElementType element)
    {
        if (_weakElements == null) return false;
        foreach (var e in _weakElements)
        {
            if (e == element) return true;
        }
        return false;
    }

    /// <summary>
    /// 速度から行動値を計算する。
    /// 行動値 = BaseActionValue / Speed。速いキャラほど行動値が小さくなる。
    /// </summary>
    public float CalculateActionValue()
    {
        return _speed > 0 ? _baseActionValue / _speed : float.MaxValue;
    }

    /// <summary>指定属性の耐性値を返す。</summary>
    public float GetResistance(ElementType element)
    {
        switch (element)
        {
            case ElementType.Physical:  return _physicalRes;
            case ElementType.Fire:      return _fireRes;
            case ElementType.Ice:       return _iceRes;
            case ElementType.Lightning: return _lightningRes;
            case ElementType.Wind:      return _windRes;
            case ElementType.Dark:      return _darkRes;
            default:                    return 0f;
        }
    }
}
