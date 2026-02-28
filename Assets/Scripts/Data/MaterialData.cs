// ============================================================
// MaterialData.cs
// 素材データの ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>素材データ。敵ドロップ、採集、購入で取得する。</summary>
[CreateAssetMenu(fileName = "MAT_New", menuName = "DevilsDiner/MaterialData")]
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
