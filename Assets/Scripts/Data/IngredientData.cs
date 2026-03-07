// ============================================================
// IngredientData.cs
// 素材アイテムの ScriptableObject。
// ItemData を継承し、レア度・ドロップ率などの素材固有パラメータを持つ。
// ============================================================
using UnityEngine;

/// <summary>
/// 素材（Ingredient）データ。敵ドロップや採集で入手する。
/// 調理レシピの材料として使用される。
/// </summary>
[CreateAssetMenu(fileName = "ING_New", menuName = "DevilsDiner/Item/IngredientData")]
public sealed class IngredientData : ItemData
{
    [Header("素材パラメータ")]
    [SerializeField, Range(1, 5)] private int _rarity = 1;
    [SerializeField, Tooltip("基本ドロップ率 (0.0 ~ 1.0)")]
    private float _dropRate = 1f;
    [SerializeField, Tooltip("調理ゲージ速度倍率")]
    private float _gaugeSpeedMultiplier = 1f;

    /// <summary>レア度（1〜5）。</summary>
    public int Rarity => _rarity;

    /// <summary>基本ドロップ率。</summary>
    public float DropRate => _dropRate;

    /// <summary>調理ミニゲームでのゲージ速度倍率。</summary>
    public float GaugeSpeedMultiplier => _gaugeSpeedMultiplier;
}
