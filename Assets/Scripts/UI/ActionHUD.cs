// ============================================================
// ActionHUD.cs
// ActionScene のHUDオーバーレイ。
// 15分カウントダウンタイマー、ジャスト入力成功フィードバック、
// コンボ表示、アイテム取得ログ、タイマー連動BGMピッチ変更を管理。
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ActionScene の HUD オーバーレイ。
/// タイマー、JUST! フィードバック、コンボ表示、
/// アイテム取得ログ、BGMピッチ制御を管理する。
/// </summary>
public sealed class ActionHUD : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float ACTION_TIME_LIMIT = 900f; // 15分 = 900秒
    private const float BGM_PITCH_THRESHOLD_5MIN = 300f; // 残り5分
    private const float BGM_PITCH_THRESHOLD_1MIN = 60f;  // 残り1分
    private const float BGM_PITCH_NORMAL = 1.0f;
    private const float BGM_PITCH_5MIN = 1.1f;
    private const float BGM_PITCH_1MIN = 1.25f;

    private const float ITEM_LOG_DISPLAY_DURATION = 3f;
    private const int MAX_ITEM_LOG_ENTRIES = 5;

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("タイマー")]
    [SerializeField] private Text _timerText;

    [Header("ジャスト入力成功表示")]
    [SerializeField] private GameObject _justSuccessDisplay;
    [SerializeField] private float _justDisplayDuration = 1.0f;

    [Header("参照")]
    [SerializeField] private JustInputAction _justInputAction;

    [Header("HPバー")]
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private PlayerHealth _playerHealth;

    [Header("コンボ表示")]
    [SerializeField] private Text _comboText;
    [SerializeField] private ComboManager _comboManager;

    [Header("アイテム取得ログ")]
    [SerializeField] private Transform _itemLogContainer;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private float _remainingTime;
    private bool _timerExpired;
    private Coroutine _justDisplayCoroutine;
    private int _bgmPitchLevel; // 0=normal, 1=5min, 2=1min
    private readonly List<GameObject> _activeLogEntries = new List<GameObject>();

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _remainingTime = ACTION_TIME_LIMIT;
        _timerExpired = false;
        _bgmPitchLevel = 0;

        // 初期状態: JUST! 表示を非表示にする
        if (_justSuccessDisplay != null)
        {
            _justSuccessDisplay.SetActive(false);
        }

        // HPバー初期化
        if (_hpSlider != null && _playerHealth != null)
        {
            _hpSlider.minValue = 0;
            _hpSlider.maxValue = _playerHealth.MaxHP;
            _hpSlider.value = _playerHealth.CurrentHP;
        }

        // コンボ表示初期化
        if (_comboText != null)
        {
            _comboText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (_justInputAction != null)
        {
            _justInputAction.OnJustInputSuccess += HandleJustInputSuccess;
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnHPChanged += HandleHPChanged;
        }

        if (_comboManager != null)
        {
            _comboManager.OnComboChanged += HandleComboChanged;
        }

        // アイテム取得ログ購読
        if (GameManager.Instance != null && GameManager.Instance.Inventory != null)
        {
            GameManager.Instance.Inventory.OnMaterialAdded += HandleMaterialAdded;
        }
    }

    private void OnDisable()
    {
        if (_justInputAction != null)
        {
            _justInputAction.OnJustInputSuccess -= HandleJustInputSuccess;
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnHPChanged -= HandleHPChanged;
        }

        if (_comboManager != null)
        {
            _comboManager.OnComboChanged -= HandleComboChanged;
        }

        if (GameManager.Instance != null && GameManager.Instance.Inventory != null)
        {
            GameManager.Instance.Inventory.OnMaterialAdded -= HandleMaterialAdded;
        }

        // コルーチン停止と表示非表示
        if (_justDisplayCoroutine != null)
        {
            StopCoroutine(_justDisplayCoroutine);
            _justDisplayCoroutine = null;
        }

        if (_justSuccessDisplay != null)
        {
            _justSuccessDisplay.SetActive(false);
        }

        // BGMピッチ復帰
        RestoreBGMPitch();
    }

    private void Update()
    {
        if (_timerExpired) return;

        UpdateTimer();
        UpdateBGMPitch();
    }

    // ──────────────────────────────────────────────
    // タイマーロジック
    // ──────────────────────────────────────────────

    private void UpdateTimer()
    {
        _remainingTime -= Time.deltaTime;

        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            _timerExpired = true;

            UpdateTimerDisplay();
            RestoreBGMPitch();

            // フェーズを Evening に遷移
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AdvancePhase();
            }

            return;
        }

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (_timerText == null) return;

        int totalSeconds = Mathf.CeilToInt(_remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        _timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // ──────────────────────────────────────────────
    // BGMピッチ制御
    // ──────────────────────────────────────────────

    private void UpdateBGMPitch()
    {
        if (AudioManager.Instance == null) return;

        if (_remainingTime <= BGM_PITCH_THRESHOLD_1MIN && _bgmPitchLevel < 2)
        {
            _bgmPitchLevel = 2;
            SetBGMPitch(BGM_PITCH_1MIN);
        }
        else if (_remainingTime <= BGM_PITCH_THRESHOLD_5MIN && _bgmPitchLevel < 1)
        {
            _bgmPitchLevel = 1;
            SetBGMPitch(BGM_PITCH_5MIN);
        }
    }

    private void SetBGMPitch(float pitch)
    {
        if (AudioManager.Instance == null) return;

        // AudioManager の BGM AudioSource にアクセスしてピッチ変更
        AudioSource[] sources = AudioManager.Instance.GetComponents<AudioSource>();
        if (sources.Length > 0)
        {
            sources[0].pitch = pitch;
        }
    }

    private void RestoreBGMPitch()
    {
        if (_bgmPitchLevel > 0)
        {
            SetBGMPitch(BGM_PITCH_NORMAL);
            _bgmPitchLevel = 0;
        }
    }

    // ──────────────────────────────────────────────
    // JUST! 表示ロジック
    // ──────────────────────────────────────────────

    private void HandleJustInputSuccess()
    {
        if (_justSuccessDisplay == null) return;

        // 既に表示中のコルーチンがあれば停止してリスタート
        if (_justDisplayCoroutine != null)
        {
            StopCoroutine(_justDisplayCoroutine);
        }

        _justDisplayCoroutine = StartCoroutine(ShowJustDisplayCoroutine());
    }

    private IEnumerator ShowJustDisplayCoroutine()
    {
        _justSuccessDisplay.SetActive(true);

        yield return new WaitForSecondsRealtime(_justDisplayDuration);

        _justSuccessDisplay.SetActive(false);
        _justDisplayCoroutine = null;
    }

    // ──────────────────────────────────────────────
    // HPバーロジック
    // ──────────────────────────────────────────────

    private void HandleHPChanged(int currentHP, int maxHP)
    {
        if (_hpSlider == null) return;

        _hpSlider.maxValue = maxHP;
        _hpSlider.value = currentHP;
    }

    // ──────────────────────────────────────────────
    // コンボ表示ロジック
    // ──────────────────────────────────────────────

    private void HandleComboChanged(int comboCount)
    {
        if (_comboText == null) return;

        if (comboCount <= 0)
        {
            _comboText.gameObject.SetActive(false);
            return;
        }

        _comboText.gameObject.SetActive(true);
        _comboText.text = $"{comboCount} HITS!";
    }

    // ──────────────────────────────────────────────
    // アイテム取得ログ
    // ──────────────────────────────────────────────

    private void HandleMaterialAdded(MaterialData material, int newCount)
    {
        if (_itemLogContainer == null || material == null) return;

        StartCoroutine(ShowItemLogEntry(material.MaterialName));
    }

    private IEnumerator ShowItemLogEntry(string itemName)
    {
        // ログエントリ生成
        GameObject logEntry = new GameObject("ItemLogEntry");
        logEntry.transform.SetParent(_itemLogContainer, false);

        Text logText = logEntry.AddComponent<Text>();
        logText.text = $"+ {itemName}";
        logText.fontSize = 24;
        logText.color = new Color(0.4f, 1f, 0.4f, 1f);
        logText.alignment = TextAnchor.MiddleLeft;
        logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (logText.font == null)
        {
            logText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        RectTransform logRect = logEntry.GetComponent<RectTransform>();
        logRect.sizeDelta = new Vector2(300f, 30f);

        _activeLogEntries.Add(logEntry);

        // 最大表示数を超えたら古いものを削除
        while (_activeLogEntries.Count > MAX_ITEM_LOG_ENTRIES)
        {
            GameObject oldest = _activeLogEntries[0];
            _activeLogEntries.RemoveAt(0);
            Destroy(oldest);
        }

        // フェードアウト
        yield return new WaitForSeconds(ITEM_LOG_DISPLAY_DURATION);

        float elapsed = 0f;
        float fadeDuration = 0.5f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            logText.color = new Color(logText.color.r, logText.color.g, logText.color.b, alpha);
            yield return null;
        }

        _activeLogEntries.Remove(logEntry);
        Destroy(logEntry);
    }
}
