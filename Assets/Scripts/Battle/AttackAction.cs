// ============================================================
// AttackAction.cs
// 固定ヒット連動型ジャストアタック（目押し強化）システム。
// 空の GameObject にアタッチし、Space キーで攻撃開始＆目押し入力。
// 他スクリプトへの依存なし。単独で動作テスト可能。
// ============================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AttackAction : MonoBehaviour
{
    // ── Inspector 設定 ──────────────────────────────────
    [Header("Hit Timings (seconds from attack start)")]
    [SerializeField] private float[] _hitTimings = { 0.5f, 1.2f, 1.8f };

    [Header("Input Window (seconds before each hit)")]
    [SerializeField] private float _inputWindow = 0.2f;

    [Header("Just Attack Damage Multiplier")]
    [SerializeField] private float _justMultiplier = 1.5f;

    [Header("Hit-Stop Duration (real-time seconds)")]
    [SerializeField] private float _hitStopDuration = 0.05f;

    [Header("Wait after last hit before ActionEnd")]
    [SerializeField] private float _endDelay = 1.0f;

    [Header("Base Damage")]
    [SerializeField] private int _baseDamage = 100;

    // ── Callback ────────────────────────────────────────
    /// <summary>全ヒット処理完了後に発火するデリゲート。</summary>
    public Action OnActionEnd;

    // ── Runtime State ───────────────────────────────────
    private bool _isRunning;
    private bool _isAcceptingInput;
    private bool _justTriggered;

    /// <summary>攻撃実行中か。</summary>
    public bool IsRunning => _isRunning;

    /// <summary>ジャスト倍率。</summary>
    public float JustMultiplier => _justMultiplier;

    /// <summary>ヒット数。</summary>
    public int HitCount => _hitTimings != null ? _hitTimings.Length : 0;

    // ── Public API ──────────────────────────────────────

    /// <summary>攻撃コルーチンを開始する（単独テスト用）。</summary>
    public void ExecuteAttack()
    {
        if (_isRunning) return;
        StartCoroutine(AttackSequence());
    }

    /// <summary>
    /// BattleManager 連携用コルーチン。各ヒット時に onHit コールバックで
    /// (hitIndex, isJust) を通知する。呼び出し元で yield return する。
    /// </summary>
    public IEnumerator ExecuteAttackCoroutine(Action<int, bool> onHit)
    {
        if (_isRunning) yield break;
        yield return StartCoroutine(AttackSequenceIntegrated(onHit));
    }

    // ── 入力検知（Input System / キーボード + ゲームパッド対応）──
    private void Update()
    {
        bool confirmPressed = false;

        var kb = Keyboard.current;
        if (kb != null)
        {
            confirmPressed = kb.spaceKey.wasPressedThisFrame
                          || kb.zKey.wasPressedThisFrame
                          || kb.enterKey.wasPressedThisFrame;
        }

        var gp = Gamepad.current;
        if (!confirmPressed && gp != null)
        {
            confirmPressed = gp.buttonSouth.wasPressedThisFrame;
        }

        if (!confirmPressed) return;

        // 攻撃未実行中：開始
        if (!_isRunning)
        {
            ExecuteAttack();
            return;
        }

        // 攻撃実行中：入力受付ウィンドウ内でジャスト判定
        if (_isAcceptingInput && !_justTriggered)
        {
            _justTriggered = true;
            _isAcceptingInput = false;
        }
    }

    // ── Core Coroutine ──────────────────────────────────
    private IEnumerator AttackSequence()
    {
        _isRunning = true;
        float timeSinceAttackStart = 0f;

        Debug.Log("[AttackAction] === Attack Start ===");

        for (int i = 0; i < _hitTimings.Length; i++)
        {
            float hitTime = _hitTimings[i];
            float windowOpen = hitTime - _inputWindow;

            // フラグリセット
            _justTriggered = false;
            _isAcceptingInput = false;

            // ── ヒットタイミングまで待機（入力ウィンドウ管理込み） ──
            while (timeSinceAttackStart < hitTime)
            {
                timeSinceAttackStart += Time.deltaTime;

                // 入力ウィンドウ開放判定
                if (!_isAcceptingInput && !_justTriggered && timeSinceAttackStart >= windowOpen)
                {
                    _isAcceptingInput = true;
                    Debug.Log($"[AttackAction] Hit {i + 1}: Input window OPEN  (t={timeSinceAttackStart:F3}s)");
                }

                yield return null;
            }

            // ── ヒット到達 ──
            _isAcceptingInput = false;

            if (_justTriggered)
            {
                // ジャスト成功
                int damage = Mathf.RoundToInt(_baseDamage * _justMultiplier);
                Debug.Log($"<color=cyan>[AttackAction] Hit {i + 1}: JUST ATTACK! {_justMultiplier}x → {damage} damage (t={timeSinceAttackStart:F3}s)</color>");

                // ヒットストップ
                yield return StartCoroutine(HitStop());
            }
            else
            {
                // 通常ヒット
                Debug.Log($"[AttackAction] Hit {i + 1}: Normal Attack → {_baseDamage} damage (t={timeSinceAttackStart:F3}s)");
            }
        }

        // ── 終了待機 ──
        Debug.Log($"[AttackAction] All hits done. Waiting {_endDelay}s before end...");
        yield return new WaitForSeconds(_endDelay);

        Debug.Log("[AttackAction] === Attack End ===");
        _isRunning = false;
        OnActionEnd?.Invoke();
    }

    // ── BattleManager 連携用コルーチン ─────────────────
    private IEnumerator AttackSequenceIntegrated(Action<int, bool> onHit)
    {
        _isRunning = true;
        float timeSinceAttackStart = 0f;

        Debug.Log("[AttackAction] === Attack Start (Integrated) ===");

        for (int i = 0; i < _hitTimings.Length; i++)
        {
            float hitTime = _hitTimings[i];
            float windowOpen = hitTime - _inputWindow;

            _justTriggered = false;
            _isAcceptingInput = false;

            while (timeSinceAttackStart < hitTime)
            {
                timeSinceAttackStart += Time.deltaTime;

                if (!_isAcceptingInput && !_justTriggered && timeSinceAttackStart >= windowOpen)
                {
                    _isAcceptingInput = true;
                }

                yield return null;
            }

            _isAcceptingInput = false;

            // ヒット通知（BattleManager 側でダメージ処理を行う）
            onHit?.Invoke(i, _justTriggered);

            if (_justTriggered)
            {
                Debug.Log($"<color=cyan>[AttackAction] Hit {i + 1}: JUST! (t={timeSinceAttackStart:F3}s)</color>");
                yield return StartCoroutine(HitStop());
            }
            else
            {
                Debug.Log($"[AttackAction] Hit {i + 1}: Normal (t={timeSinceAttackStart:F3}s)");
            }
        }

        yield return new WaitForSeconds(_endDelay);

        Debug.Log("[AttackAction] === Attack End (Integrated) ===");
        _isRunning = false;
        OnActionEnd?.Invoke();
    }

    // ── Hit-Stop ────────────────────────────────────────
    private IEnumerator HitStop()
    {
        float savedTimeScale = Time.timeScale;
        Time.timeScale = 0.1f;

        // realtime で待機（timeScale の影響を受けない）
        float elapsed = 0f;
        while (elapsed < _hitStopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = savedTimeScale;
    }
}
