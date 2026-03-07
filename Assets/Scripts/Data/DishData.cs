// ============================================================
// DishData.cs
// 料理アイテムの ScriptableObject。
// ItemData を継承し、HP回復量やアピール値などの料理固有パラメータを持つ。
// ============================================================
using UnityEngine;

/// <summary>
/// 料理（Dish）データ。調理ミニゲームで作成する。
/// 戦闘中の Meal コマンドで消費してHP回復したり、
/// 経営フェーズで客に提供してアピール値を得る。
/// </summary>
[CreateAssetMenu(fileName = "DISH_New", menuName = "DevilsDiner/Item/DishData")]
public sealed class DishData : ItemData
{
    [Header("戦闘効果")]
    [SerializeField] private int _hpRecoveryAmount = 50;

    [Header("経営パラメータ")]
    [SerializeField, Tooltip("客へのアピール値（満足度に影響）")]
    private int _appealValue = 10;
    [SerializeField, Tooltip("提供までの所要時間（秒）")]
    private float _servingTime = 5f;

    /// <summary>戦闘中の HP 回復量。</summary>
    public int HPRecoveryAmount => _hpRecoveryAmount;

    /// <summary>客へのアピール値。</summary>
    public int AppealValue => _appealValue;

    /// <summary>提供までの所要時間（秒）。</summary>
    public float ServingTime => _servingTime;
}
