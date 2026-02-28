// ============================================================
// CookingConfig.cs
// 調理ミニゲームパラメータの ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>調理ミニゲームのパラメータ設定。</summary>
[CreateAssetMenu(fileName = "CookingConfig", menuName = "DevilsDiner/Config/CookingConfig")]
public sealed class CookingConfig : ScriptableObject
{
    [Header("ゲージ設定")]
    [Tooltip("ゲージの基本移動速度")]
    [SerializeField] private float _baseSpeed = 1.0f;

    [Tooltip("成功エリアの基本幅（0〜1の正規化値）")]
    [SerializeField, Range(0.05f, 0.5f)] private float _baseSuccessWidth = 0.2f;

    [Header("Perfect 判定")]
    [Tooltip("成功エリア内でのPerfect判定比率（0〜1）。中央からこの幅以内ならPerfect")]
    [SerializeField, Range(0.1f, 0.5f)] private float _perfectZoneRatio = 0.3f;

    [Header("焦げた肉")]
    [Tooltip("Miss時の焦げた肉の販売価格")]
    [SerializeField] private int _burntMeatPrice = 10;

    public float BaseSpeed         => _baseSpeed;
    public float BaseSuccessWidth  => _baseSuccessWidth;
    public float PerfectZoneRatio  => _perfectZoneRatio;
    public int   BurntMeatPrice    => _burntMeatPrice;
}
