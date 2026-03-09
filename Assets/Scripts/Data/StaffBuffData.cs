// ============================================================
// StaffBuffData.cs
// スタッフのランダムバフマスターデータ。ハクスラ要素の核。
// ============================================================
using UnityEngine;

/// <summary>
/// スタッフに付与されるランダムバフの定義。
/// <see cref="StaffRaceData.PossibleBuffs"/> から重み付き抽選される。
/// </summary>
[CreateAssetMenu(fileName = "SBUF_New", menuName = "DevilsDiner/Staff/StaffBuffData")]
public sealed class StaffBuffData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private string _buffID;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea(1, 2)] private string _description;
    [SerializeField] private Sprite _icon;

    [Header("効果")]
    [SerializeField] private StaffBuffType _type = StaffBuffType.QualityBonus;
    [SerializeField, Tooltip("効果量（型に応じて倍率 or 加算値）")]
    private float _value = 0.1f;

    [Header("カテゴリ限定（CategorySpecialty 用）")]
    [SerializeField, Tooltip("CategorySpecialty の場合に対象となるカテゴリ")]
    private DishCategory _targetCategory;

    [Header("レアリティ（抽選重み）")]
    [SerializeField, Range(1, 5), Tooltip("1=コモン(出やすい), 5=レジェンド(出にくい)")]
    private int _rarity = 1;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    public string BuffID => _buffID;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public StaffBuffType Type => _type;
    public float Value => _value;
    public DishCategory TargetCategory => _targetCategory;
    public int Rarity => _rarity;

    /// <summary>抽選時の重み。レアリティが高いほど出にくい。</summary>
    public float SelectionWeight => 1f / _rarity;
}
