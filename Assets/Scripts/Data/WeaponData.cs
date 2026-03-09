// ============================================================
// WeaponData.cs
// 武器データの ScriptableObject。ItemData を継承。
// ============================================================
using System;
using UnityEngine;

/// <summary>武器データ。攻撃力やジャスト入力ボーナスを定義する。</summary>
[CreateAssetMenu(fileName = "WPN_New", menuName = "DevilsDiner/Item/WeaponData")]
public sealed class WeaponData : ItemData
{
    [Header("武器パラメータ")]
    [SerializeField] private int _baseDamage = 100;
    [SerializeField] private int _basePartBreakValue = 10;
    [SerializeField] private int _justInputFrameBonus;
    [SerializeField] private AnimatorOverrideController _animatorOverride;

    public int                       BaseDamage            => _baseDamage;
    public int                       BasePartBreakValue    => _basePartBreakValue;
    public int                       JustInputFrameBonus   => _justInputFrameBonus;
    public AnimatorOverrideController AnimatorOverride      => _animatorOverride;

    // ── 旧API互換（廃止予定） ──

    /// <summary>[Obsolete] ItemID を使用してください。</summary>
    [Obsolete("Id は廃止予定です。ItemID を使用してください。")]
    public string Id => ItemID;

    /// <summary>[Obsolete] DisplayName を使用してください。</summary>
    [Obsolete("WeaponName は廃止予定です。DisplayName を使用してください。")]
    public string WeaponName => DisplayName;

    /// <summary>[Obsolete] SellPrice を使用してください。</summary>
    [Obsolete("Price は廃止予定です。SellPrice を使用してください。")]
    public int Price => SellPrice;
}
