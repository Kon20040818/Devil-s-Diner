// ============================================================
// DummyAnimationEventSender.cs
// Animator / アニメーションクリップが未設定でも攻撃フローをテスト可能にする。
// PlayerController の AnimEvent_* メソッドをタイマーベースで自動呼び出しする。
// ============================================================
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Animator Controller やアニメーションクリップが存在しない状態でも
/// 攻撃フロー（PreCast → Active → Recovery → None）をテスト可能にするダミー。
/// <see cref="PlayerController.OnAttackPhaseChanged"/> を監視し、PreCast 検出時に
/// タイマーベースで各 AnimEvent_* メソッドを順番に呼び出す。
/// </summary>
public sealed class DummyAnimationEventSender : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("参照")]
    [SerializeField] private PlayerController _playerController;

    [Header("フェーズ時間（秒）")]
    [SerializeField] private float _precastDuration = 0.3f;
    [SerializeField] private float _activeDuration = 0.2f;
    [SerializeField] private float _recoveryDuration = 0.3f;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    private Coroutine _activeCoroutine;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (_playerController == null)
        {
            TryGetComponent(out _playerController);
        }

        if (_playerController == null)
        {
            _playerController = GetComponentInParent<PlayerController>();
        }

        if (_playerController == null)
        {
            Debug.LogWarning($"[{nameof(DummyAnimationEventSender)}] PlayerController が見つかりません。", this);
        }
    }

    private void OnEnable()
    {
        if (_playerController != null)
        {
            _playerController.OnAttackPhaseChanged += HandleAttackPhaseChanged;
        }
    }

    private void OnDisable()
    {
        if (_playerController != null)
        {
            _playerController.OnAttackPhaseChanged -= HandleAttackPhaseChanged;
        }

        StopActiveCoroutine();
    }

    // ──────────────────────────────────────────────
    // イベントハンドラー
    // ──────────────────────────────────────────────

    private void HandleAttackPhaseChanged(AttackPhase phase)
    {
        if (phase != AttackPhase.PreCast) return;

        // 重複防止 — 既にコルーチンが走っていれば停止
        StopActiveCoroutine();

        _activeCoroutine = StartCoroutine(SimulateAttackSequence());
    }

    // ──────────────────────────────────────────────
    // コルーチン
    // ──────────────────────────────────────────────

    private IEnumerator SimulateAttackSequence()
    {
        if (_playerController == null) yield break;

        // ── PreCast 待機 ──
        yield return new WaitForSeconds(_precastDuration);

        // ── Active 開始 ──
        _playerController.AnimEvent_ActiveStart();

        // ── Active 待機 ──
        yield return new WaitForSeconds(_activeDuration);

        // ── HitStop が発生していたら解除まで待つ ──
        yield return new WaitUntil(() => _playerController.CurrentAttackPhase != AttackPhase.HitStop);

        // ── Active 終了 → Recovery ──
        _playerController.AnimEvent_ActiveEnd();

        // ── Recovery 待機 ──
        yield return new WaitForSeconds(_recoveryDuration);

        // ── Recovery 終了 → None ──
        _playerController.AnimEvent_RecoveryEnd();

        _activeCoroutine = null;
    }

    // ──────────────────────────────────────────────
    // 内部ヘルパー
    // ──────────────────────────────────────────────

    private void StopActiveCoroutine()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
    }
}
