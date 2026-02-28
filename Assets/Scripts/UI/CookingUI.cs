// ============================================================
// CookingUI.cs
// ManagementScene の調理ミニゲームUI。
// ゲージ表示、成功エリアのビジュアライズ、判定結果表示を行う。
// ============================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 調理ミニゲームの UI 表示。
/// ゲージの針移動、成功 / Perfect ゾーンのビジュアライズ、
/// および判定結果テキストの表示を担当する。
/// </summary>
public sealed class CookingUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float RESULT_DISPLAY_DURATION = 2f;

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("ゲージ要素")]
    [SerializeField] private Image _gaugeNeedle;
    [SerializeField] private Image _successZone;
    [SerializeField] private Image _perfectZone;
    [SerializeField] private Text _resultText;
    [SerializeField] private GameObject _gaugePanel;

    [Header("ゲージ設定")]
    [SerializeField] private float _gaugeWidth = 400f;

    [Header("参照")]
    [SerializeField] private CookingMinigame _cookingMinigame;
    [SerializeField] private CookingConfig _cookingConfig;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private RectTransform _needleRect;
    private RectTransform _successZoneRect;
    private RectTransform _perfectZoneRect;
    private Coroutine _resultCoroutine;
    private bool _wasActive;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        // RectTransform をキャッシュ
        if (_gaugeNeedle != null)
        {
            _needleRect = _gaugeNeedle.GetComponent<RectTransform>();
        }

        if (_successZone != null)
        {
            _successZoneRect = _successZone.GetComponent<RectTransform>();
        }

        if (_perfectZone != null)
        {
            _perfectZoneRect = _perfectZone.GetComponent<RectTransform>();
        }
    }

    private void OnEnable()
    {
        if (_cookingMinigame != null)
        {
            _cookingMinigame.OnCookingCompleted += HandleCookingCompleted;
        }

        HidePanel();
    }

    private void OnDisable()
    {
        if (_cookingMinigame != null)
        {
            _cookingMinigame.OnCookingCompleted -= HandleCookingCompleted;
        }

        if (_resultCoroutine != null)
        {
            StopCoroutine(_resultCoroutine);
            _resultCoroutine = null;
        }
    }

    private void Update()
    {
        if (_cookingMinigame == null) return;

        bool isActive = _cookingMinigame.IsActive;

        // ミニゲーム開始を検知してパネル表示 & ゾーン設定
        if (isActive && !_wasActive)
        {
            ShowPanel();
            SetupZones();
        }

        _wasActive = isActive;

        if (!isActive) return;

        UpdateNeedlePosition();
    }

    // ──────────────────────────────────────────────
    // ゲージ針の位置更新
    // ──────────────────────────────────────────────

    private void UpdateNeedlePosition()
    {
        if (_needleRect == null) return;
        if (_cookingMinigame == null) return;

        // gaugePosition: 0〜1 → X座標: -gaugeWidth/2 〜 +gaugeWidth/2
        float gaugePosition = _cookingMinigame.GaugePosition;
        float xPos = (gaugePosition - 0.5f) * _gaugeWidth;

        Vector2 anchoredPos = _needleRect.anchoredPosition;
        anchoredPos.x = xPos;
        _needleRect.anchoredPosition = anchoredPos;
    }

    // ──────────────────────────────────────────────
    // ゾーン設定
    // ──────────────────────────────────────────────

    private void SetupZones()
    {
        if (_cookingConfig == null) return;

        float successWidth = _gaugeWidth * _cookingConfig.BaseSuccessWidth;
        float perfectWidth = successWidth * _cookingConfig.PerfectZoneRatio;

        // Success ゾーン: ゲージ中央に配置
        if (_successZoneRect != null)
        {
            _successZoneRect.sizeDelta = new Vector2(
                successWidth,
                _successZoneRect.sizeDelta.y
            );
            Vector2 anchoredPos = _successZoneRect.anchoredPosition;
            anchoredPos.x = 0f; // ゲージ中央
            _successZoneRect.anchoredPosition = anchoredPos;
        }

        // Perfect ゾーン: ゲージ中央に配置（Success ゾーンの内側）
        if (_perfectZoneRect != null)
        {
            _perfectZoneRect.sizeDelta = new Vector2(
                perfectWidth,
                _perfectZoneRect.sizeDelta.y
            );
            Vector2 anchoredPos = _perfectZoneRect.anchoredPosition;
            anchoredPos.x = 0f; // ゲージ中央
            _perfectZoneRect.anchoredPosition = anchoredPos;
        }
    }

    // ──────────────────────────────────────────────
    // 結果表示
    // ──────────────────────────────────────────────

    private void HandleCookingCompleted(CookedDishData dishData)
    {
        if (dishData == null) return;

        if (_resultCoroutine != null)
        {
            StopCoroutine(_resultCoroutine);
        }

        _resultCoroutine = StartCoroutine(ShowResultCoroutine(dishData.Rank));
    }

    private IEnumerator ShowResultCoroutine(CookingRank rank)
    {
        // 結果テキストを表示
        if (_resultText != null)
        {
            _resultText.text = rank.ToString();
            _resultText.gameObject.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(RESULT_DISPLAY_DURATION);

        // 結果テキストを非表示
        if (_resultText != null)
        {
            _resultText.gameObject.SetActive(false);
        }

        // ゲージパネルを非表示
        HidePanel();

        _resultCoroutine = null;
    }

    // ──────────────────────────────────────────────
    // パネル表示制御
    // ──────────────────────────────────────────────

    private void ShowPanel()
    {
        if (_gaugePanel != null)
        {
            _gaugePanel.SetActive(true);
        }

        // 結果テキストは初期非表示
        if (_resultText != null)
        {
            _resultText.gameObject.SetActive(false);
        }
    }

    private void HidePanel()
    {
        if (_gaugePanel != null)
        {
            _gaugePanel.SetActive(false);
        }

        if (_resultText != null)
        {
            _resultText.gameObject.SetActive(false);
        }
    }
}
