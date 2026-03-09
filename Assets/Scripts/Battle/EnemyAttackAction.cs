// ============================================================
// EnemyAttackAction.cs
// 敵ターンのジャストガード（防御QTE）システム。
// 空の GameObject にアタッチし、ガードボタン（X / □）で防御タイミング入力。
// 他スクリプトへの依存なし。単独で動作テスト可能。
// ============================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class EnemyAttackAction : MonoBehaviour
{
    // ── ガード結果 ──────────────────────────────────
    public enum GuardResult
    {
        JustGuard,
        NormalGuard,
        Failed
    }

    // ── Inspector 設定 ──────────────────────────────────
    [Header("Hit Timings (seconds from attack start)")]
    [SerializeField] private float[] _hitTimings = { 1.0f, 2.5f };

    [Header("Just Guard Window (seconds before hit)")]
    [SerializeField] private float _justGuardWindow = 0.15f;

    [Header("Normal Guard Window (seconds before hit, must be > justGuardWindow)")]
    [SerializeField] private float _normalGuardWindow = 0.35f;

    [Header("Just Guard Damage Multiplier (0 = full block)")]
    [SerializeField] private float _justGuardMultiplier = 0.0f;

    [Header("Normal Guard Damage Multiplier")]
    [SerializeField] private float _normalGuardMultiplier = 0.5f;

    [Header("Hit-Stop Duration on Just Guard (real-time seconds)")]
    [SerializeField] private float _hitStopDuration = 0.05f;

    [Header("Wait after last hit before ActionEnd")]
    [SerializeField] private float _endDelay = 0.8f;

    [Header("Base Damage (standalone test)")]
    [SerializeField] private int _baseDamage = 80;

    // ── Callback ────────────────────────────────────────
    /// <summary>全ヒット処理完了後に発火するデリゲート。</summary>
    public Action OnActionEnd;

    // ── Runtime State ───────────────────────────────────
    private bool _isRunning;
    private float _timeSinceAttackStart;
    private int _currentHitIndex;

    // 入力状態（各ヒットごとにリセット）
    private bool _guardPressed;
    private float _guardPressTime;
    private bool _hasAttemptedGuard; // 早押しペナルティフラグ

    // ── Public Properties ───────────────────────────────
    /// <summary>攻撃実行中か。</summary>
    public bool IsRunning => _isRunning;

    /// <summary>ヒット数。</summary>
    public int HitCount => _hitTimings != null ? _hitTimings.Length : 0;

    /// <summary>ジャストガード倍率。</summary>
    public float JustGuardMultiplier => _justGuardMultiplier;

    /// <summary>通常ガード倍率。</summary>
    public float NormalGuardMultiplier => _normalGuardMultiplier;

    /// <summary>現在入力受付中か（UI表示用）。</summary>
    public bool IsAcceptingGuard { get; private set; }

    // ── Public API ──────────────────────────────────────

    /// <summary>敵攻撃コルーチンを開始する（単独テスト用）。</summary>
    public void ExecuteEnemyAttack()
    {
        if (_isRunning) return;
        StartCoroutine(EnemyAttackSequence());
    }

    /// <summary>
    /// BattleManager 連携用コルーチン。各ヒット時に onHit コールバックで
    /// (hitIndex, guardResult) を通知する。呼び出し元で yield return する。
    /// </summary>
    public IEnumerator ExecuteAttackCoroutine(Action<int, GuardResult> onHit)
    {
        if (_isRunning) yield break;
        yield return StartCoroutine(EnemyAttackSequenceIntegrated(onHit));
    }

    // ── 入力検知（Input System / キーボード + ゲームパッド対応）──
    private void Update()
    {
        if (!_isRunning) return;

        // 既にこのヒットでガード入力済みなら無視
        if (_guardPressed) return;

        bool guardButtonPressed = false;

        var kb = Keyboard.current;
        if (kb != null)
        {
            guardButtonPressed = kb.xKey.wasPressedThisFrame
                              || kb.shiftKey.wasPressedThisFrame;
        }

        var gp = Gamepad.current;
        if (!guardButtonPressed && gp != null)
        {
            guardButtonPressed = gp.buttonWest.wasPressedThisFrame;
        }

        if (!guardButtonPressed) return;

        // ガードボタンが押された
        _guardPressed = true;
        _guardPressTime = _timeSinceAttackStart;

        // 早押しペナルティ判定
        if (_currentHitIndex < _hitTimings.Length)
        {
            float timeToHit = _hitTimings[_currentHitIndex] - _timeSinceAttackStart;
            if (timeToHit > _normalGuardWindow)
            {
                // normalGuardWindow より手前 → 早押しペナルティ
                _hasAttemptedGuard = true;
                Debug.Log($"<color=red>[EnemyAttackAction] Hit {_currentHitIndex + 1}: TOO EARLY! Guard disabled (timeToHit={timeToHit:F3}s)</color>");
            }
        }
    }

    // ── 単独テスト用コルーチン ───────────────────────
    private IEnumerator EnemyAttackSequence()
    {
        _isRunning = true;
        _timeSinceAttackStart = 0f;

        Debug.Log("[EnemyAttackAction] === Enemy Attack Start ===");

        for (int i = 0; i < _hitTimings.Length; i++)
        {
            _currentHitIndex = i;
            ResetGuardState();

            float hitTime = _hitTimings[i];

            // ヒットタイミングまで待機
            while (_timeSinceAttackStart < hitTime)
            {
                _timeSinceAttackStart += Time.deltaTime;

                // ガード受付ウィンドウ表示用フラグ更新
                float timeToHit = hitTime - _timeSinceAttackStart;
                IsAcceptingGuard = timeToHit <= _normalGuardWindow && timeToHit >= 0f;

                yield return null;
            }

            IsAcceptingGuard = false;

            // ガード結果判定
            GuardResult result = EvaluateGuard(hitTime);

            // ダメージ計算
            switch (result)
            {
                case GuardResult.JustGuard:
                {
                    int damage = Mathf.RoundToInt(_baseDamage * _justGuardMultiplier);
                    Debug.Log($"<color=cyan>[EnemyAttackAction] Hit {i + 1}: JUST GUARD! {_justGuardMultiplier}x → {damage} damage</color>");
                    yield return StartCoroutine(HitStop());
                    break;
                }
                case GuardResult.NormalGuard:
                {
                    int damage = Mathf.RoundToInt(_baseDamage * _normalGuardMultiplier);
                    Debug.Log($"<color=yellow>[EnemyAttackAction] Hit {i + 1}: Normal Guard {_normalGuardMultiplier}x → {damage} damage</color>");
                    break;
                }
                case GuardResult.Failed:
                {
                    Debug.Log($"[EnemyAttackAction] Hit {i + 1}: FAILED → {_baseDamage} full damage");
                    break;
                }
            }
        }

        Debug.Log($"[EnemyAttackAction] All hits done. Waiting {_endDelay}s...");
        yield return new WaitForSeconds(_endDelay);

        Debug.Log("[EnemyAttackAction] === Enemy Attack End ===");
        _isRunning = false;
        IsAcceptingGuard = false;
        OnActionEnd?.Invoke();
    }

    // ── BattleManager 連携用コルーチン ─────────────────
    private IEnumerator EnemyAttackSequenceIntegrated(Action<int, GuardResult> onHit)
    {
        _isRunning = true;
        _timeSinceAttackStart = 0f;

        Debug.Log("[EnemyAttackAction] === Enemy Attack Start (Integrated) ===");

        for (int i = 0; i < _hitTimings.Length; i++)
        {
            _currentHitIndex = i;
            ResetGuardState();

            float hitTime = _hitTimings[i];

            while (_timeSinceAttackStart < hitTime)
            {
                _timeSinceAttackStart += Time.deltaTime;

                float timeToHit = hitTime - _timeSinceAttackStart;
                IsAcceptingGuard = timeToHit <= _normalGuardWindow && timeToHit >= 0f;

                yield return null;
            }

            IsAcceptingGuard = false;

            GuardResult result = EvaluateGuard(hitTime);
            onHit?.Invoke(i, result);

            if (result == GuardResult.JustGuard)
            {
                Debug.Log($"<color=cyan>[EnemyAttackAction] Hit {i + 1}: JUST GUARD!</color>");
                yield return StartCoroutine(HitStop());
            }
            else
            {
                string label = result == GuardResult.NormalGuard ? "Normal Guard" : "FAILED";
                Debug.Log($"[EnemyAttackAction] Hit {i + 1}: {label}");
            }
        }

        yield return new WaitForSeconds(_endDelay);

        Debug.Log("[EnemyAttackAction] === Enemy Attack End (Integrated) ===");
        _isRunning = false;
        IsAcceptingGuard = false;
        OnActionEnd?.Invoke();
    }

    // ── ガード判定 ──────────────────────────────────────
    private GuardResult EvaluateGuard(float hitTime)
    {
        // 早押しペナルティ → 強制失敗
        if (_hasAttemptedGuard)
            return GuardResult.Failed;

        // ガード未入力 → 失敗
        if (!_guardPressed)
            return GuardResult.Failed;

        // ボタン押下時点からヒットまでの残り時間
        float timeToHitAtPress = hitTime - _guardPressTime;

        // ジャストガード判定（Hit直前 0〜justGuardWindow 秒）
        if (timeToHitAtPress >= 0f && timeToHitAtPress <= _justGuardWindow)
            return GuardResult.JustGuard;

        // 通常ガード判定（justGuardWindow〜normalGuardWindow 秒）
        if (timeToHitAtPress > _justGuardWindow && timeToHitAtPress <= _normalGuardWindow)
            return GuardResult.NormalGuard;

        // ウィンドウ外（本来は早押しペナルティで弾かれるが念のため）
        return GuardResult.Failed;
    }

    // ── ヒットごとのガード状態リセット ──────────────────
    private void ResetGuardState()
    {
        _guardPressed = false;
        _guardPressTime = 0f;
        _hasAttemptedGuard = false;
    }

    // ── Hit-Stop（ジャストガード成功演出）──────────────
    private IEnumerator HitStop()
    {
        float savedTimeScale = Time.timeScale;
        Time.timeScale = 0.1f;

        float elapsed = 0f;
        while (elapsed < _hitStopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = savedTimeScale;
    }
}
