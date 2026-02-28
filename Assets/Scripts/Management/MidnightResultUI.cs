// ============================================================
// MidnightResultUI.cs
// Midnight フェーズに表示されるリザルト画面。
// 段階的カウントアップ演出（チャリン→ジャララッ→バンッ）で収益を表示。
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Midnight フェーズに表示されるリザルト画面。
/// 当日の収益を段階的にカウントアップ演出し、次の日へ進行するボタンを提供する。
/// </summary>
public sealed class MidnightResultUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("UI 参照")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private Text _dayLabel;
    [SerializeField] private Text _goldLabel;
    [SerializeField] private Text _revenueLabel;
    [SerializeField] private Text _tipLabel;
    [SerializeField] private Text _totalLabel;
    [SerializeField] private Button _nextDayButton;

    [Header("演出設定")]
    [SerializeField] private AnimationCurve _countUpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _baseRevenueCountDuration = 1.5f;
    [SerializeField] private float _tipCountDuration = 1.0f;
    [SerializeField] private float _totalStampDelay = 0.3f;

    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const string DAY_LABEL_FORMAT = "Day {0} 終了";
    private const string GOLD_LABEL_FORMAT = "所持金: {0} G";
    private const string REVENUE_LABEL_FORMAT = "基本売上: {0} G";
    private const string TIP_LABEL_FORMAT = "チップ: +{0} G";
    private const string TOTAL_LABEL_FORMAT = "総売上: {0} G";

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    private int _sessionStartGold;
    private int _dailyRevenue;
    private int _dailyTip;
    private Coroutine _resultCoroutine;

    // ──────────────────────────────────────────────
    // 公開 API — DinerManager から通知
    // ──────────────────────────────────────────────

    /// <summary>支払いを記録する。基本売上とチップを分離して追跡。</summary>
    public void RecordPayment(int baseAmount, int tipAmount)
    {
        _dailyRevenue += baseAmount;
        _dailyTip += tipAmount;
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void OnEnable()
    {
        // 開始時点のゴールドを記録
        _sessionStartGold = GameManager.Instance.Gold;
        _dailyRevenue = 0;
        _dailyTip = 0;

        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        GameManager.Instance.OnGoldChanged += HandleGoldChanged;
        _nextDayButton.onClick.AddListener(OnNextDayClicked);

        // パネルを初期状態で非表示に
        _resultPanel.SetActive(false);

        // ラベル初期化
        if (_tipLabel != null) _tipLabel.gameObject.SetActive(false);
        if (_totalLabel != null) _totalLabel.gameObject.SetActive(false);

        // 既に Midnight フェーズの場合は即座に表示
        if (GameManager.Instance.CurrentPhase == GameManager.GamePhase.Midnight)
        {
            ShowResult();
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            GameManager.Instance.OnGoldChanged -= HandleGoldChanged;
        }

        _nextDayButton.onClick.RemoveListener(OnNextDayClicked);

        if (_resultCoroutine != null)
        {
            StopCoroutine(_resultCoroutine);
            _resultCoroutine = null;
        }
    }

    // ──────────────────────────────────────────────
    // イベントハンドラ
    // ──────────────────────────────────────────────

    private void HandlePhaseChanged(GameManager.GamePhase newPhase)
    {
        switch (newPhase)
        {
            case GameManager.GamePhase.Midnight:
                ShowResult();
                break;

            case GameManager.GamePhase.Morning:
                // 新しい日の開始 — 収益トラッカーをリセットし、パネルを非表示
                _sessionStartGold = GameManager.Instance.Gold;
                _dailyRevenue = 0;
                _dailyTip = 0;
                _resultPanel.SetActive(false);
                break;
        }
    }

    private void HandleGoldChanged(int currentGold)
    {
        // RecordPayment が呼ばれない場合のフォールバック
        int totalEarned = currentGold - _sessionStartGold;
        if (totalEarned > _dailyRevenue + _dailyTip)
        {
            _dailyRevenue = totalEarned - _dailyTip;
        }
    }

    // ──────────────────────────────────────────────
    // リザルト表示（コルーチン演出）
    // ──────────────────────────────────────────────

    private void ShowResult()
    {
        if (_resultCoroutine != null)
        {
            StopCoroutine(_resultCoroutine);
        }

        _resultCoroutine = StartCoroutine(ResultAnimationCoroutine());
    }

    /// <summary>
    /// 段階的リザルト演出コルーチン。
    /// Phase 1: 基本売上カウントアップ (SE: result_count)
    /// Phase 2: チップ加算カウントアップ (SE: result_tip)
    /// Phase 3: 総売上スタンプ表示 (SE: result_bang)
    /// </summary>
    private IEnumerator ResultAnimationCoroutine()
    {
        // 初期設定
        _dayLabel.text = string.Format(DAY_LABEL_FORMAT, GameManager.Instance.CurrentDay);
        _goldLabel.text = string.Format(GOLD_LABEL_FORMAT, GameManager.Instance.Gold);
        _revenueLabel.text = string.Format(REVENUE_LABEL_FORMAT, 0);

        if (_tipLabel != null)
        {
            _tipLabel.text = string.Format(TIP_LABEL_FORMAT, 0);
            _tipLabel.gameObject.SetActive(false);
        }

        if (_totalLabel != null)
        {
            _totalLabel.text = string.Format(TOTAL_LABEL_FORMAT, 0);
            _totalLabel.gameObject.SetActive(false);
            _totalLabel.transform.localScale = Vector3.zero;
        }

        _nextDayButton.gameObject.SetActive(false);
        _resultPanel.SetActive(true);

        // 短い待機
        yield return new WaitForSeconds(0.5f);

        // ── Phase 1: 基本売上カウントアップ (SE: チャリン) ──
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("result_count");
        }

        float elapsed = 0f;
        while (elapsed < _baseRevenueCountDuration)
        {
            elapsed += Time.deltaTime;
            float t = _countUpCurve.Evaluate(Mathf.Clamp01(elapsed / _baseRevenueCountDuration));
            int displayValue = Mathf.RoundToInt(Mathf.Lerp(0, _dailyRevenue, t));
            _revenueLabel.text = string.Format(REVENUE_LABEL_FORMAT, displayValue);
            yield return null;
        }
        _revenueLabel.text = string.Format(REVENUE_LABEL_FORMAT, _dailyRevenue);

        yield return new WaitForSeconds(0.3f);

        // ── Phase 2: チップ加算カウントアップ (SE: ジャララッ) ──
        if (_tipLabel != null && _dailyTip > 0)
        {
            _tipLabel.gameObject.SetActive(true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySE("result_tip");
            }

            elapsed = 0f;
            while (elapsed < _tipCountDuration)
            {
                elapsed += Time.deltaTime;
                float t = _countUpCurve.Evaluate(Mathf.Clamp01(elapsed / _tipCountDuration));
                int displayValue = Mathf.RoundToInt(Mathf.Lerp(0, _dailyTip, t));
                _tipLabel.text = string.Format(TIP_LABEL_FORMAT, displayValue);
                yield return null;
            }
            _tipLabel.text = string.Format(TIP_LABEL_FORMAT, _dailyTip);

            yield return new WaitForSeconds(0.3f);
        }

        // ── Phase 3: 総売上スタンプ (SE: バンッ) ──
        int totalRevenue = _dailyRevenue + _dailyTip;

        if (_totalLabel != null)
        {
            _totalLabel.text = string.Format(TOTAL_LABEL_FORMAT, totalRevenue);
            _totalLabel.gameObject.SetActive(true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySE("result_bang");
            }

            // スタンプ風スケールアニメーション
            float stampDuration = 0.4f;
            elapsed = 0f;

            while (elapsed < stampDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stampDuration);

                // バウンスアニメーション: 大きく→少し戻る→安定
                float scale;
                if (t < 0.5f)
                {
                    scale = Mathf.Lerp(0f, 1.3f, t * 2f);
                }
                else
                {
                    scale = Mathf.Lerp(1.3f, 1.0f, (t - 0.5f) * 2f);
                }

                _totalLabel.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            _totalLabel.transform.localScale = Vector3.one;
        }

        yield return new WaitForSeconds(_totalStampDelay);

        // 所持金ラベル更新
        _goldLabel.text = string.Format(GOLD_LABEL_FORMAT, GameManager.Instance.Gold);

        // ボタン表示
        _nextDayButton.gameObject.SetActive(true);

        _resultCoroutine = null;
    }

    // ──────────────────────────────────────────────
    // ボタン処理
    // ──────────────────────────────────────────────

    private void OnNextDayClicked()
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Midnight) return;

        _resultPanel.SetActive(false);
        GameManager.Instance.AdvancePhase();
    }
}
