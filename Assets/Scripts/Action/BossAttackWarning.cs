// ============================================================
// BossAttackWarning.cs
// ボスの大振り攻撃予備動作を視覚的にプレイヤーに伝える予兆演出。
// マテリアル色の赤色フラッシュとスケールパルスで警告を表示する。
// ============================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// ボスの大振り攻撃の予兆を視覚的に表示するコンポーネント。
/// マテリアルの赤色フラッシュとスケールパルスの二重チャネルで警告を伝える。
/// </summary>
public sealed class BossAttackWarning : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float RESTORE_DURATION = 0.3f;

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("参照")]
    [SerializeField] private BossEnemy _bossEnemy;
    [SerializeField] private Renderer _targetRenderer;

    [Header("色変化設定")]
    [SerializeField] private Color _warningColor = new Color(1f, 0.2f, 0.1f, 1f);
    [SerializeField] private float _flashSpeed = 4f;

    [Header("スケールパルス設定")]
    [SerializeField] private float _pulseIntensity = 0.15f;
    [SerializeField] private float _pulseSpeed = 3f;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    private Color _originalColor;
    private Vector3 _originalScale;
    private bool _isWarningActive;
    private Material _material;
    private Coroutine _warningCoroutine;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (_bossEnemy == null)
        {
            TryGetComponent(out _bossEnemy);
        }

        if (_targetRenderer == null)
        {
            if (!TryGetComponent(out _targetRenderer))
            {
                _targetRenderer = GetComponentInChildren<Renderer>();
            }
        }

        _material = _targetRenderer.material;
        _originalColor = _material.color;
        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        _bossEnemy.OnHeavyAttackWindupStart += HandleWindupStart;
        _bossEnemy.OnHeavyAttackExecute += HandleAttackExecute;
    }

    private void OnDisable()
    {
        _bossEnemy.OnHeavyAttackWindupStart -= HandleWindupStart;
        _bossEnemy.OnHeavyAttackExecute -= HandleAttackExecute;

        if (_warningCoroutine != null)
        {
            StopCoroutine(_warningCoroutine);
            _warningCoroutine = null;
        }

        _material.color = _originalColor;
        transform.localScale = _originalScale;
        _isWarningActive = false;
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
        }
    }

    // ──────────────────────────────────────────────
    // イベントハンドラ
    // ──────────────────────────────────────────────

    private void HandleWindupStart()
    {
        if (_warningCoroutine != null)
        {
            StopCoroutine(_warningCoroutine);
        }

        _isWarningActive = true;
        _warningCoroutine = StartCoroutine(WarningEffectCoroutine());
        Debug.Log("[BossAttackWarning] 大振り攻撃の予兆演出開始！");
    }

    private void HandleAttackExecute()
    {
        _isWarningActive = false;
    }

    // ──────────────────────────────────────────────
    // コルーチン
    // ──────────────────────────────────────────────

    private IEnumerator WarningEffectCoroutine()
    {
        while (_isWarningActive)
        {
            float t = (Mathf.Sin(Time.time * _flashSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            _material.color = Color.Lerp(_originalColor, _warningColor, t);

            float scaleOffset = Mathf.Sin(Time.time * _pulseSpeed * Mathf.PI * 2f) * _pulseIntensity;
            transform.localScale = _originalScale * (1f + scaleOffset);

            yield return null;
        }

        // ─── 警告終了: 元の状態にスムーズに復帰 ───
        float restoreTimer = 0f;
        float restoreDuration = RESTORE_DURATION;
        Color currentColor = _material.color;
        Vector3 currentScale = transform.localScale;

        while (restoreTimer < restoreDuration)
        {
            restoreTimer += Time.deltaTime;
            float progress = restoreTimer / restoreDuration;

            _material.color = Color.Lerp(currentColor, _originalColor, progress);
            transform.localScale = Vector3.Lerp(currentScale, _originalScale, progress);

            yield return null;
        }

        _material.color = _originalColor;
        transform.localScale = _originalScale;
        _warningCoroutine = null;
    }
}
