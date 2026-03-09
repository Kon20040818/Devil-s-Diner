// ============================================================
// QualityScaleTable.cs
// 品質ごとのスケーリング倍率を定義する ScriptableObject。
// DishData から参照され、DishInstance の各パラメータ計算に使用される。
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// 品質ランクごとの倍率テーブル。Inspector で調整可能。
/// </summary>
[CreateAssetMenu(fileName = "QualityScaleTable", menuName = "DevilsDiner/Data/QualityScaleTable")]
public sealed class QualityScaleTable : ScriptableObject
{
    // ──────────────────────────────────────────────
    // データ構造
    // ──────────────────────────────────────────────

    /// <summary>品質ランク1段階分のスケーリング倍率。</summary>
    [Serializable]
    public struct QualityScale
    {
        [Tooltip("HP回復量の倍率")]
        public float HealMultiplier;

        [Tooltip("カテゴリバフ量の倍率")]
        public float BuffMultiplier;

        [Tooltip("スカウトボーナスの倍率")]
        public float ScoutMultiplier;

        [Tooltip("販売価格の倍率")]
        public float PriceMultiplier;

        [Tooltip("顧客満足度の倍率")]
        public float SatisfactionMultiplier;
    }

    // ──────────────────────────────────────────────
    // Inspector フィールド
    // ──────────────────────────────────────────────

    [Header("品質ごとの倍率設定")]

    [SerializeField] private QualityScale _poor = new QualityScale
    {
        HealMultiplier         = 0.5f,
        BuffMultiplier         = 0.5f,
        ScoutMultiplier        = 0.5f,
        PriceMultiplier        = 0.3f,
        SatisfactionMultiplier = 0.5f,
    };

    [SerializeField] private QualityScale _normal = new QualityScale
    {
        HealMultiplier         = 1.0f,
        BuffMultiplier         = 1.0f,
        ScoutMultiplier        = 1.0f,
        PriceMultiplier        = 1.0f,
        SatisfactionMultiplier = 1.0f,
    };

    [SerializeField] private QualityScale _fine = new QualityScale
    {
        HealMultiplier         = 1.3f,
        BuffMultiplier         = 1.3f,
        ScoutMultiplier        = 1.5f,
        PriceMultiplier        = 1.5f,
        SatisfactionMultiplier = 1.4f,
    };

    [SerializeField] private QualityScale _exquisite = new QualityScale
    {
        HealMultiplier         = 1.8f,
        BuffMultiplier         = 1.8f,
        ScoutMultiplier        = 2.0f,
        PriceMultiplier        = 2.5f,
        SatisfactionMultiplier = 2.0f,
    };

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>指定品質のスケーリング倍率を返す。</summary>
    public QualityScale GetScale(DishQuality quality)
    {
        return quality switch
        {
            DishQuality.Poor      => _poor,
            DishQuality.Normal    => _normal,
            DishQuality.Fine      => _fine,
            DishQuality.Exquisite => _exquisite,
            _                     => _normal,
        };
    }
}
