// ============================================================
// YadaCommentator.cs
// 相棒カラス「矢田」が店内イベントに対してランダムなコメントを表示する。
// CustomerReactionHandler の Perfect 反応、OrderQueue への注文、
// フェーズ変更などのイベントを購読し、吹き出しUIに表示する。
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 相棒カラス「矢田」のコメンテーター。
/// 店内イベントに応じたランダムなフレーバーテキストを吹き出しUIに表示する。
/// </summary>
public sealed class YadaCommentator : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float COMMENT_DISPLAY_DURATION = 3f;
    private const float COMMENT_COOLDOWN = 5f;

    // ──────────────────────────────────────────────
    // コメントテーブル
    // ──────────────────────────────────────────────

    private static readonly string[] PERFECT_COMMENTS = new string[]
    {
        "カァ！完璧な料理だ！",
        "さすがだな、マスター！",
        "この仕上がり…天才か？",
        "客も大喜びだぜ！",
        "パーフェクト！最高だ！"
    };

    private static readonly string[] ORDER_COMMENTS = new string[]
    {
        "お客さんが来たぜ！",
        "注文が入ったな。",
        "腕の見せ所だぜ！",
        "忙しくなってきたな！"
    };

    private static readonly string[] BUSINESS_START_COMMENTS = new string[]
    {
        "営業開始だ！張り切っていくぜ！",
        "今日も繁盛させるぞ！",
        "さぁ、仕事の時間だ！"
    };

    private static readonly string[] MIDNIGHT_COMMENTS = new string[]
    {
        "今日もお疲れさん！",
        "なかなかの稼ぎだな！",
        "明日はもっと稼ぐぜ！"
    };

    private static readonly string[] MORNING_COMMENTS = new string[]
    {
        "おはよう、マスター！",
        "今日も狩りに行くか？",
        "準備は万全か？"
    };

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("UI 参照")]
    [SerializeField] private GameObject _speechBubble;
    [SerializeField] private Text _commentText;

    [Header("参照")]
    [SerializeField] private DinerManager _dinerManager;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private float _cooldownTimer;
    private Coroutine _displayCoroutine;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (_speechBubble != null)
        {
            _speechBubble.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // フェーズ変更イベント
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }

        // 営業開始イベント
        if (_dinerManager != null)
        {
            _dinerManager.OnBusinessStarted += HandleBusinessStarted;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }

        if (_dinerManager != null)
        {
            _dinerManager.OnBusinessStarted -= HandleBusinessStarted;
        }

        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
            _displayCoroutine = null;
        }
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }
    }

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 外部からコメントを表示させる（CustomerReactionHandler 等からの呼び出し用）。
    /// </summary>
    public void NotifyPerfectDish()
    {
        TryShowComment(PERFECT_COMMENTS);
    }

    /// <summary>
    /// 外部からオーダー通知を受ける。
    /// </summary>
    public void NotifyNewOrder()
    {
        TryShowComment(ORDER_COMMENTS);
    }

    // ──────────────────────────────────────────────
    // イベントハンドラ
    // ──────────────────────────────────────────────

    private void HandlePhaseChanged(GameManager.GamePhase newPhase)
    {
        switch (newPhase)
        {
            case GameManager.GamePhase.Morning:
                ShowComment(MORNING_COMMENTS[Random.Range(0, MORNING_COMMENTS.Length)]);
                break;
            case GameManager.GamePhase.Midnight:
                ShowComment(MIDNIGHT_COMMENTS[Random.Range(0, MIDNIGHT_COMMENTS.Length)]);
                break;
        }
    }

    private void HandleBusinessStarted()
    {
        ShowComment(BUSINESS_START_COMMENTS[Random.Range(0, BUSINESS_START_COMMENTS.Length)]);
    }

    // ──────────────────────────────────────────────
    // 表示ロジック
    // ──────────────────────────────────────────────

    private void TryShowComment(string[] comments)
    {
        if (_cooldownTimer > 0f) return;

        ShowComment(comments[Random.Range(0, comments.Length)]);
    }

    private void ShowComment(string comment)
    {
        if (_speechBubble == null || _commentText == null) return;

        _cooldownTimer = COMMENT_COOLDOWN;

        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }

        _displayCoroutine = StartCoroutine(DisplayCommentCoroutine(comment));
    }

    private IEnumerator DisplayCommentCoroutine(string comment)
    {
        _commentText.text = comment;
        _speechBubble.SetActive(true);

        // SE
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("yada_comment");
        }

        yield return new WaitForSeconds(COMMENT_DISPLAY_DURATION);

        _speechBubble.SetActive(false);
        _displayCoroutine = null;
    }
}
