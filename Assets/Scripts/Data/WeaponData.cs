// ============================================================
// WeaponData.cs
// 武器データの ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>武器データ。攻撃力やジャスト入力ボーナスを定義する。</summary>
[CreateAssetMenu(fileName = "WPN_New", menuName = "DevilsDiner/WeaponData")]
public sealed class WeaponData : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _weaponName;
    [SerializeField] private int _price = 500;
    [SerializeField] private int _baseDamage = 100;
    [SerializeField] private int _basePartBreakValue = 10;
    [SerializeField] private int _justInputFrameBonus;
    [SerializeField] private AnimatorOverrideController _animatorOverride;

    public string                    Id                    => _id;
    public string                    WeaponName            => _weaponName;
    public int                       Price                 => _price;
    public int                       BaseDamage            => _baseDamage;
    public int                       BasePartBreakValue    => _basePartBreakValue;
    public int                       JustInputFrameBonus   => _justInputFrameBonus;
    public AnimatorOverrideController AnimatorOverride      => _animatorOverride;
}
