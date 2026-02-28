// ============================================================
// JustInputAction.cs
// ジャスト入力（ヒットストップ中の追加入力）システム。
// 武器ヒット時にヒットストップを発生させ、プレイヤーの追加入力を待つ。
// 成功時: ダメージ倍率 ×2.5、部位破壊値 +50、各種フィードバック
// 失敗時: 通常ダメージで処理
// ============================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ジャスト入力システムの中核。
/// <see cref="WeaponColliderHandler"/> からヒット通知を受け取り、
/// ヒットストップ中にプレイヤーの追加入力を判定する。
/// </summary>
public sealed class JustInputAction : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float FRAME_DURATION = 1f / 60f; // 1フレーム = 1/60秒

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────
    [Header("設定")]
    [SerializeField] private JustInputConfig _config;
    [SerializeField] private PlayerController _playerController;

    [Header("フィードバック — ビジュアル")]
    [SerializeField] private Animator _cylinderAnimator;
    [SerializeField] private ParticleSystem _sparkEffect;
    [SerializeField] private ParticleSystem _blueFlameEffect;

    [Header("フィードバック — オーディオ")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _justSuccessSE;
    [SerializeField] private AudioClip _justFailSE;

    [Header("フィードバック — カメラ")]
    [SerializeField] private CameraShakeHandler _cameraShakeHandler;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>ジャスト入力成功時。</summary>
    public event Action OnJustInputSuccess;

    /// <summary>ジャスト入力失敗時（時間切れ）。</summary>
    public event Action OnJustInputFailed;

    /// <summary>ヒットストップ開始時。</summary>
    public event Action OnHitStopStarted;

    /// <summary>ヒットストップ終了時。</summary>
    public event Action OnHitStopEnded;

    // ──────────────────────────────────────────────
    // プロパティ
    // ──────────────────────────────────────────────

    /// <summary>現在ヒットストップ中かどうか。</summary>
    public bool IsHitStopActive { get; private set; }

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private float _hitStopTimer;
    private float _hitStopAcceptDuration;
    private bool _isWaitingForInput;
    private IDamageable _pendingTarget;
    private DamageInfo _pendingDamageInfo;

    // Animator パラメータハッシュ
    private static readonly int ANIM_CYLINDER_ROTATE = Animator.StringToHash("CylinderRotate");

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void OnDisable()
    {
        // 安全復帰: ヒットストップ中に無効化された場合に timeScale を復元
        if (IsHitStopActive)
        {
            ForceEndHitStop(applyNormalDamage: false);
        }

        GameManager.ForceRestoreTimeScale();
    }

    private void Update()
    {
        if (!_isWaitingForInput) return;

        // unscaledDeltaTime ベースのタイマー
        _hitStopTimer += Time.unscaledDeltaTime;

        // ジャスト入力判定（ポーリング）
        if (IsShootButtonPressed())
        {
            OnJustInputSucceeded();
            return;
        }

        // 時間切れ判定
        if (_hitStopTimer >= _hitStopAcceptDuration)
        {
            OnJustInputTimedOut();
        }
    }

    // ──────────────────────────────────────────────
    // 公開 API — WeaponColliderHandler から呼び出し
    // ──────────────────────────────────────────────

    /// <summary>
    /// 武器ヒット通知のエントリポイント。
    /// WeaponColliderHandler がヒットを検出した際に呼び出す。
    /// </summary>
    /// <param name="target">ヒットした対象の IDamageable。</param>
    /// <param name="baseDamageInfo">基本ダメージ情報。</param>
    public void NotifyWeaponHit(IDamageable target, DamageInfo baseDamageInfo)
    {
        if (target == null) return;
        if (_config == null)
        {
            Debug.LogWarning("[JustInputAction] JustInputConfig が未設定です。通常ダメージを適用します。");
            ApplyNormalDamage(target, baseDamageInfo);
            return;
        }

        // 既にヒットストップ中の場合は通常ダメージ
        if (IsHitStopActive)
        {
            ApplyNormalDamage(target, baseDamageInfo);
            return;
        }

        _pendingTarget = target;
        _pendingDamageInfo = baseDamageInfo;

        StartHitStop();
    }

    // ──────────────────────────────────────────────
    // ヒットストップ開始
    // ──────────────────────────────────────────────

    private void StartHitStop()
    {
        IsHitStopActive = true;
        _isWaitingForInput = true;
        _hitStopTimer = 0f;

        // 受付時間 = 基本ヒットストップ時間 + 武器ボーナスフレーム
        float bonusTime = 0f;
        if (_playerController != null && _playerController.EquippedWeapon != null)
        {
            bonusTime = _playerController.EquippedWeapon.JustInputFrameBonus * FRAME_DURATION;
        }
        _hitStopAcceptDuration = _config.HitStopDuration + bonusTime;

        // timeScale を低速に変更
        Time.timeScale = _config.HitStopTimeScale;
        Time.fixedDeltaTime = 0.02f * _config.HitStopTimeScale;

        // 攻撃フェーズをヒットストップに変更
        if (_playerController != null)
        {
            _playerController.ForceAttackPhase(AttackPhase.HitStop);
        }

        OnHitStopStarted?.Invoke();
    }

    // ──────────────────────────────────────────────
    // ジャスト入力 — 成功
    // ──────────────────────────────────────────────

    private void OnJustInputSucceeded()
    {
        _isWaitingForInput = false;

        // timeScale を即座に復帰
        GameManager.ForceRestoreTimeScale();

        // ダメージ計算: 倍率 ×2.5、部位破壊値 +50
        float multiplier = _config.JustDamageMultiplier;
        int finalDamage = Mathf.RoundToInt(_pendingDamageInfo.BaseDamage * multiplier);
        int partBreak = _pendingDamageInfo.BasePartBreakValue + _config.JustPartBreakBonus;

        HitResult result = new HitResult(
            _pendingDamageInfo.BaseDamage,
            finalDamage,
            partBreak,
            _pendingDamageInfo.HitPoint,
            _pendingDamageInfo.HitNormal,
            true,
            multiplier,
            _pendingDamageInfo.Attacker
        );

        // ダメージ適用
        _pendingTarget.TakeDamage(result);

        // フィードバック実行
        PlayJustSuccessFeedback();

        // 攻撃フェーズを Active に戻す
        if (_playerController != null)
        {
            _playerController.ForceAttackPhase(AttackPhase.Active);
        }

        IsHitStopActive = false;
        OnHitStopEnded?.Invoke();
        OnJustInputSuccess?.Invoke();

        ClearPending();
    }

    // ──────────────────────────────────────────────
    // ジャスト入力 — 失敗（時間切れ）
    // ──────────────────────────────────────────────

    private void OnJustInputTimedOut()
    {
        _isWaitingForInput = false;

        // timeScale を復帰
        GameManager.ForceRestoreTimeScale();

        // 通常ダメージ適用
        ApplyNormalDamage(_pendingTarget, _pendingDamageInfo);

        // 失敗時SE
        if (AudioManager.Instance != null && _justFailSE != null)
        {
            AudioManager.Instance.PlaySE(_justFailSE);
        }
        else if (_audioSource != null && _justFailSE != null)
        {
            _audioSource.PlayOneShot(_justFailSE);
        }

        // 攻撃フェーズを Active に戻す
        if (_playerController != null)
        {
            _playerController.ForceAttackPhase(AttackPhase.Active);
        }

        IsHitStopActive = false;
        OnHitStopEnded?.Invoke();
        OnJustInputFailed?.Invoke();

        ClearPending();
    }

    // ──────────────────────────────────────────────
    // 強制終了（OnDisable 等からの安全復帰）
    // ──────────────────────────────────────────────

    private void ForceEndHitStop(bool applyNormalDamage)
    {
        _isWaitingForInput = false;
        GameManager.ForceRestoreTimeScale();

        if (applyNormalDamage && _pendingTarget != null)
        {
            ApplyNormalDamage(_pendingTarget, _pendingDamageInfo);
        }

        IsHitStopActive = false;
        OnHitStopEnded?.Invoke();
        ClearPending();
    }

    // ──────────────────────────────────────────────
    // 通常ダメージ適用
    // ──────────────────────────────────────────────

    private void ApplyNormalDamage(IDamageable target, DamageInfo info)
    {
        if (target == null) return;

        HitResult result = new HitResult(
            info.BaseDamage,
            info.BaseDamage,
            info.BasePartBreakValue,
            info.HitPoint,
            info.HitNormal,
            false,
            1f,
            info.Attacker
        );

        target.TakeDamage(result);
    }

    // ──────────────────────────────────────────────
    // 入力ポーリング
    // ──────────────────────────────────────────────

    /// <summary>
    /// ジャスト入力の射撃ボタンが押されたかをポーリングする。
    /// Gamepad: 右トリガー / Mouse: 右ボタン / Keyboard: F キー
    /// </summary>
    private bool IsShootButtonPressed()
    {
        // Gamepad
        if (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            return true;
        }

        // Mouse
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            return true;
        }

        // Keyboard
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    // ──────────────────────────────────────────────
    // フィードバック — ジャスト成功
    // ──────────────────────────────────────────────

    private void PlayJustSuccessFeedback()
    {
        // シリンダー回転アニメーション
        if (_cylinderAnimator != null)
        {
            _cylinderAnimator.SetTrigger(ANIM_CYLINDER_ROTATE);
        }

        // パーティクルエフェクト
        if (_sparkEffect != null)
        {
            _sparkEffect.transform.position = _pendingDamageInfo.HitPoint;
            _sparkEffect.Play();
        }

        if (_blueFlameEffect != null)
        {
            _blueFlameEffect.transform.position = _pendingDamageInfo.HitPoint;
            _blueFlameEffect.Play();
        }

        // SE
        if (AudioManager.Instance != null && _justSuccessSE != null)
        {
            AudioManager.Instance.PlaySE(_justSuccessSE);
        }
        else if (_audioSource != null && _justSuccessSE != null)
        {
            _audioSource.PlayOneShot(_justSuccessSE);
        }

        // カメラシェイク
        if (_cameraShakeHandler != null)
        {
            _cameraShakeHandler.Shake(_config.CameraShakeIntensity, _config.CameraShakeDuration);
        }

        // コントローラー振動
        StartCoroutine(RumbleCoroutine(
            _config.RumbleLowFrequency,
            _config.RumbleHighFrequency,
            _config.RumbleDuration
        ));
    }

    // ──────────────────────────────────────────────
    // コントローラー振動コルーチン
    // ──────────────────────────────────────────────

    private IEnumerator RumbleCoroutine(float lowFreq, float highFreq, float duration)
    {
        if (Gamepad.current is IDualMotorRumble rumble)
        {
            rumble.SetMotorSpeeds(lowFreq, highFreq);
            yield return new WaitForSecondsRealtime(duration);
            rumble.SetMotorSpeeds(0f, 0f);
        }
    }

    // ──────────────────────────────────────────────
    // クリーンアップ
    // ──────────────────────────────────────────────

    private void ClearPending()
    {
        _pendingTarget = null;
        _pendingDamageInfo = default;
    }
}
