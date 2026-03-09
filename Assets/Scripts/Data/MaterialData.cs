// ============================================================
// MaterialData.cs
// 旧互換エイリアス。IngredientData への移行が完了するまで残す。
// 新規コードでは IngredientData を直接使用すること。
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// [Obsolete] IngredientData を使用してください。
/// 旧コードとの互換性のために残しています。
/// </summary>
[Obsolete("MaterialData は廃止予定です。IngredientData を使用してください。")]
[CreateAssetMenu(fileName = "MAT_New", menuName = "DevilsDiner/MaterialData (Legacy)")]
public sealed class MaterialData : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _materialName;
    [SerializeField] private Sprite _icon;
    [SerializeField, Range(1, 5)] private int _rarity = 1;
    [SerializeField] private float _dropRate = 1f;
    [SerializeField] private int _basePrice = 10;
    [SerializeField, Tooltip("調理ゲージ速度倍率")]
    private float _gaugeSpeedMultiplier = 1f;

    public string Id                   => _id;
    public string MaterialName         => _materialName;
    public Sprite Icon                 => _icon;
    public int    Rarity               => _rarity;
    public float  DropRate             => _dropRate;
    public int    BasePrice            => _basePrice;
    public float  GaugeSpeedMultiplier => _gaugeSpeedMultiplier;
}
