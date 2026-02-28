// ============================================================
// CustomerReactionHandler.cs
// 客NPCに付与し、Perfect ランクの料理を食べた際に
// 派手な VFX リアクション（スケール・色・パーティクル）を再生する。
// ============================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 客の食事完了時にランクに応じたリアクション演出を再生するコンポーネント。
/// <see cref="CustomerAI"/> と同じ GameObject にアタッチする想定。
/// </summary>
public sealed class CustomerReactionHandler : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const float PERFECT_REACTION_DURATION = 2.0f;
    private const float PERFECT_SCALE_FACTOR = 1.3f;
    private const float GOOD_SCALE_FACTOR = 1.1f;
    private const float GOOD_REACTION_DURATION = 0.5f;
    private const float SCALE_UP_TIME = 0.3f;
    private const float RESTORE_TIME = 0.5f;

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("参照")]
    [SerializeField] private CustomerAI _customerAI;
    [SerializeField] private ParticleSystem _reactionParticle;
    [SerializeField] private Renderer _targetRenderer;

    [Header("演出設定")]
    [SerializeField] private Color _perfectColor = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private float _flashSpeed = 6f;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    private CustomerAI.CustomerState _previousState;
    private Material _material;
    private Color _originalColor;
    private Vector3 _originalScale;
    private Coroutine _reactionCoroutine;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (_customerAI == null)
        {
            TryGetComponent(out _customerAI);
        }

        if (_targetRenderer == null)
        {
            if (!TryGetComponent(out _targetRenderer))
            {
                _targetRenderer = GetComponentInChildren<Renderer>();
            }
        }

        if (_targetRenderer != null)
        {
            _material = _targetRenderer.material;
            _originalColor = _material.color;
        }

        _originalScale = transform.localScale;
    }

    private void Start()
    {
        if (_customerAI != null)
        {
            _previousState = _customerAI.CurrentState;
        }
    }

    private void Update()
    {
        if (_customerAI == null) return;

        CustomerAI.CustomerState current = _customerAI.CurrentState;

        // Eating → Paying 遷移を検出
        if (_previousState == CustomerAI.CustomerState.Eating
            && current == CustomerAI.CustomerState.Paying)
        {
            OnEatingComplete();
        }

        _previousState = current;
    }

    private void OnDestroy()
    {
        if (_reactionCoroutine != null)
        {
            StopCoroutine(_reactionCoroutine);
            _reactionCoroutine = null;
        }

        // 復帰保証: スケールを元に戻す
        transform.localScale = _originalScale;

        // マテリアルインスタンスの破棄
        if (_material != null)
        {
            Destroy(_material);
        }
    }

    // ──────────────────────────────────────────────
    // 食事完了検出
    // ──────────────────────────────────────────────

    /// <summary>食事完了時のリアクション分岐。</summary>
    private void OnEatingComplete()
    {
        CookedDishData dish = _customerAI.ServedDish;
        if (dish == null) return;

        switch (dish.Rank)
        {
            case CookingRank.Perfect:
                StartReaction(PerfectReactionCoroutine());
                break;

            case CookingRank.Good:
                StartReaction(GoodReactionCoroutine());
                break;

            case CookingRank.Miss:
                // Miss ランクは特別な演出なし
                break;
        }
    }

    /// <summary>既存のリアクションを停止して新しいリアクションを開始する。</summary>
    private void StartReaction(IEnumerator routine)
    {
        if (_reactionCoroutine != null)
        {
            StopCoroutine(_reactionCoroutine);
        }

        _reactionCoroutine = StartCoroutine(routine);
    }

    // ──────────────────────────────────────────────
    // コルーチン — Perfect リアクション
    // ──────────────────────────────────────────────

    /// <summary>
    /// Perfect ランクの派手なリアクション演出。
    /// Phase 1 (0~0.3s)   : バウンス付きスケールアップ
    /// Phase 2 (0.3~1.5s) : パーティクル発射
    /// Phase 3 (0.3~1.5s) : ゴールド色のサインウェーブフラッシュ
    /// Phase 4 (1.5~2.0s) : 元の状態にスムーズ復帰
    /// </summary>
    private IEnumerator PerfectReactionCoroutine()
    {
        Debug.Log("[CustomerReactionHandler] Perfect リアクション開始！");

        Vector3 targetScale = _originalScale * PERFECT_SCALE_FACTOR;
        float elapsed = 0f;

        // ─── Phase 1: バウンス付きスケールアップ (0 ~ SCALE_UP_TIME) ───

        while (elapsed < SCALE_UP_TIME)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SCALE_UP_TIME);

            // バウンス用のイーズアウト: オーバーシュートしてから落ち着く
            float bounce = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            float overshoot = 1f + (PERFECT_SCALE_FACTOR - 1f) * bounce * 1.15f;
            overshoot = Mathf.Lerp(overshoot, PERFECT_SCALE_FACTOR, t);

            transform.localScale = _originalScale * overshoot;
            yield return null;
        }

        transform.localScale = targetScale;

        // ─── Phase 2 & 3: パーティクル & 色フラッシュ (SCALE_UP_TIME ~ 1.5s) ───

        if (_reactionParticle != null)
        {
            _reactionParticle.Play();
        }

        float flashStart = SCALE_UP_TIME;
        float flashEnd = PERFECT_REACTION_DURATION - RESTORE_TIME;
        elapsed = flashStart;

        while (elapsed < flashEnd)
        {
            elapsed += Time.deltaTime;

            if (_material != null)
            {
                float sinT = (Mathf.Sin(elapsed * _flashSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                _material.color = Color.Lerp(_originalColor, _perfectColor, sinT);
            }

            yield return null;
        }

        // ─── Phase 4: スムーズ復帰 (1.5s ~ 2.0s) ───

        float restoreElapsed = 0f;
        Color currentColor = _material != null ? _material.color : _originalColor;
        Vector3 currentScale = transform.localScale;

        while (restoreElapsed < RESTORE_TIME)
        {
            restoreElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(restoreElapsed / RESTORE_TIME);

            // スムーズステップでなめらかに
            float smooth = progress * progress * (3f - 2f * progress);

            transform.localScale = Vector3.Lerp(currentScale, _originalScale, smooth);

            if (_material != null)
            {
                _material.color = Color.Lerp(currentColor, _originalColor, smooth);
            }

            yield return null;
        }

        // 最終状態を確定
        transform.localScale = _originalScale;
        if (_material != null)
        {
            _material.color = _originalColor;
        }

        _reactionCoroutine = null;
        Debug.Log("[CustomerReactionHandler] Perfect リアクション完了。");
    }

    // ──────────────────────────────────────────────
    // コルーチン — Good リアクション
    // ──────────────────────────────────────────────

    /// <summary>
    /// Good ランクの軽いスケールパルス演出。
    /// 0.5秒で 1.1倍に膨らんで元に戻る。
    /// </summary>
    private IEnumerator GoodReactionCoroutine()
    {
        Debug.Log("[CustomerReactionHandler] Good リアクション開始。");

        Vector3 targetScale = _originalScale * GOOD_SCALE_FACTOR;
        float halfDuration = GOOD_REACTION_DURATION * 0.5f;
        float elapsed = 0f;

        // ─── 膨張 ───

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);

            transform.localScale = Vector3.Lerp(_originalScale, targetScale, ease);
            yield return null;
        }

        // ─── 収縮 ───

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);

            transform.localScale = Vector3.Lerp(targetScale, _originalScale, ease);
            yield return null;
        }

        transform.localScale = _originalScale;
        _reactionCoroutine = null;
        Debug.Log("[CustomerReactionHandler] Good リアクション完了。");
    }
}
