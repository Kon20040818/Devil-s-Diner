// ============================================================
// DishData.cs
// 料理アイテムの ScriptableObject（レシピマスター）。
// ItemData を継承し、カテゴリ・基本戦闘効果・バフ・店舗パラメータを持つ。
// 品質（DishQuality）は DishInstance 側で動的に付与される。
// ============================================================
using UnityEngine;

/// <summary>
/// 料理（Dish）データ。調理ミニゲームで作成する。
/// 戦闘中の Meal コマンドで消費してHP回復＋カテゴリバフを付与したり、
/// 経営フェーズで客に提供して売上と満足度を得る。
/// 品質ごとの実効値は <see cref="GetHealAmount"/> 等のメソッドで取得する。
/// </summary>
[CreateAssetMenu(fileName = "DISH_New", menuName = "DevilsDiner/Item/DishData")]
public sealed class DishData : ItemData
{
    // ──────────────────────────────────────────────
    // カテゴリ
    // ──────────────────────────────────────────────

    [Header("カテゴリ")]
    [SerializeField, Tooltip("料理カテゴリ（バフ効果の種類を決定）")]
    private DishCategory _category = DishCategory.Meat;

    // ──────────────────────────────────────────────
    // 戦闘パラメータ（基本値）
    // ──────────────────────────────────────────────

    [Header("戦闘効果（基本値）")]
    [SerializeField, Tooltip("基本HP回復量（品質で倍率が掛かる）")]
    private int _hpRecoveryAmount = 50;

    [SerializeField, Tooltip("カテゴリバフの基本量（品質で倍率が掛かる）")]
    private float _baseBuff = 0.1f;

    [SerializeField, Tooltip("バフの持続ターン数")]
    private int _buffDurationTurns = 3;

    [SerializeField, Tooltip("スカウト成功率への加算ボーナス（品質で倍率が掛かる）")]
    private float _scoutBonus = 0.05f;

    // ──────────────────────────────────────────────
    // 店舗パラメータ（基本値）
    // ──────────────────────────────────────────────

    [Header("店舗パラメータ（基本値）")]
    [SerializeField, Tooltip("店舗での販売価格（品質で倍率が掛かる）")]
    private int _shopPrice = 100;

    [SerializeField, Tooltip("顧客の基本満足度（品質で倍率が掛かる）")]
    private int _baseSatisfaction = 50;

    [SerializeField, Tooltip("提供までの所要時間（秒）")]
    private float _servingTime = 5f;

    // ──────────────────────────────────────────────
    // 品質テーブル参照
    // ──────────────────────────────────────────────

    [Header("品質スケーリング")]
    [SerializeField, Tooltip("品質ごとの倍率テーブル（未設定時は Normal 扱い）")]
    private QualityScaleTable _qualityTable;

    // ──────────────────────────────────────────────
    // 旧互換プロパティ（基本値をそのまま返す）
    // ──────────────────────────────────────────────

    /// <summary>基本HP回復量（品質 Normal 相当）。</summary>
    public int HPRecoveryAmount => _hpRecoveryAmount;

    /// <summary>基本満足度（旧 AppealValue 互換）。</summary>
    public int AppealValue => _baseSatisfaction;

    /// <summary>提供までの所要時間（秒）。</summary>
    public float ServingTime => _servingTime;

    // ──────────────────────────────────────────────
    // 新規プロパティ
    // ──────────────────────────────────────────────

    /// <summary>料理カテゴリ。</summary>
    public DishCategory Category => _category;

    /// <summary>カテゴリバフの基本量。</summary>
    public float BaseBuff => _baseBuff;

    /// <summary>バフの持続ターン数。</summary>
    public int BuffDurationTurns => _buffDurationTurns;

    /// <summary>スカウトボーナス基本値。</summary>
    public float ScoutBonus => _scoutBonus;

    /// <summary>店舗での基本販売価格。</summary>
    public int ShopPrice => _shopPrice;

    /// <summary>顧客の基本満足度。</summary>
    public int BaseSatisfaction => _baseSatisfaction;

    /// <summary>品質スケーリングテーブル。</summary>
    public QualityScaleTable QualityTable => _qualityTable;

    // ──────────────────────────────────────────────
    // 品質適用メソッド
    // ──────────────────────────────────────────────

    /// <summary>品質を加味した HP 回復量を返す。</summary>
    public int GetHealAmount(DishQuality quality)
    {
        float mult = GetMultiplier(quality, s => s.HealMultiplier);
        return Mathf.RoundToInt(_hpRecoveryAmount * mult);
    }

    /// <summary>品質を加味したカテゴリバフ量を返す。</summary>
    public float GetBuffAmount(DishQuality quality)
    {
        float mult = GetMultiplier(quality, s => s.BuffMultiplier);
        return _baseBuff * mult;
    }

    /// <summary>品質を加味したスカウトボーナスを返す。</summary>
    public float GetScoutBonus(DishQuality quality)
    {
        float mult = GetMultiplier(quality, s => s.ScoutMultiplier);
        return _scoutBonus * mult;
    }

    /// <summary>品質を加味した店舗販売価格を返す。</summary>
    public int GetShopPrice(DishQuality quality)
    {
        float mult = GetMultiplier(quality, s => s.PriceMultiplier);
        return Mathf.RoundToInt(_shopPrice * mult);
    }

    /// <summary>品質を加味した顧客満足度を返す。</summary>
    public int GetSatisfaction(DishQuality quality)
    {
        float mult = GetMultiplier(quality, s => s.SatisfactionMultiplier);
        return Mathf.RoundToInt(_baseSatisfaction * mult);
    }

    // ──────────────────────────────────────────────
    // 内部ヘルパー
    // ──────────────────────────────────────────────

    /// <summary>品質テーブルから指定倍率を取得する。テーブル未設定時は 1.0f。</summary>
    private float GetMultiplier(DishQuality quality, System.Func<QualityScaleTable.QualityScale, float> selector)
    {
        if (_qualityTable == null) return 1f;
        return selector(_qualityTable.GetScale(quality));
    }
}
