// ============================================================
// JustInputConfig.cs
// ジャスト入力パラメータの ScriptableObject。
// Inspectorでデザイナーが調整可能。
// ============================================================
using UnityEngine;

/// <summary>ジャスト入力システムのパラメータ設定。</summary>
[CreateAssetMenu(fileName = "JustInputConfig", menuName = "DevilsDiner/Config/JustInputConfig")]
public sealed class JustInputConfig : ScriptableObject
{
    [Header("ヒットストップ")]
    [Tooltip("ヒットストップ中のtimeScale")]
    [SerializeField] private float _hitStopTimeScale = 0.05f;

    [Tooltip("ヒットストップの持続時間（リアルタイム秒）")]
    [SerializeField] private float _hitStopDuration = 0.15f;

    [Header("ジャスト入力 — 大成功")]
    [Tooltip("ジャスト入力成功時のダメージ倍率")]
    [SerializeField] private float _justDamageMultiplier = 2.5f;

    [Tooltip("ジャスト入力成功時の部位破壊値加算")]
    [SerializeField] private int _justPartBreakBonus = 50;

    [Header("カメラシェイク")]
    [Tooltip("ジャスト成功時のカメラシェイク強度")]
    [SerializeField] private float _cameraShakeIntensity = 0.5f;

    [Tooltip("ジャスト成功時のカメラシェイク持続時間")]
    [SerializeField] private float _cameraShakeDuration = 0.1f;

    [Header("コントローラー振動")]
    [Tooltip("振動の強さ（低周波 0〜1）")]
    [SerializeField] private float _rumbleLowFrequency = 0.8f;

    [Tooltip("振動の強さ（高周波 0〜1）")]
    [SerializeField] private float _rumbleHighFrequency = 1.0f;

    [Tooltip("振動の持続時間")]
    [SerializeField] private float _rumbleDuration = 0.2f;

    public float HitStopTimeScale       => _hitStopTimeScale;
    public float HitStopDuration        => _hitStopDuration;
    public float JustDamageMultiplier   => _justDamageMultiplier;
    public int   JustPartBreakBonus     => _justPartBreakBonus;
    public float CameraShakeIntensity   => _cameraShakeIntensity;
    public float CameraShakeDuration    => _cameraShakeDuration;
    public float RumbleLowFrequency     => _rumbleLowFrequency;
    public float RumbleHighFrequency    => _rumbleHighFrequency;
    public float RumbleDuration         => _rumbleDuration;
}
