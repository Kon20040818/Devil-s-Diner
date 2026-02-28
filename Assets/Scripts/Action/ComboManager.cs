// ============================================================
// ComboManager.cs
// ジャスト入力成功時のコンボカウンターを管理する。
// 一定時間入力がないか被弾時にリセット。
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// ジャスト入力成功時にコンボ数を加算し、
/// タイムアウトまたは被弾でリセットするコンボマネージャー。
/// ActionHUD に現在のコンボ数を通知する。
/// </summary>
public sealed class ComboManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float COMBO_TIMEOUT = 5f;

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("参照")]
    [SerializeField] private JustInputAction _justInputAction;
    [SerializeField] private PlayerHealth _playerHealth;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>コンボ数が変化したとき。引数は現在のコンボ数。</summary>
    public event Action<int> OnComboChanged;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private int _currentCombo;
    private float _comboTimer;

    /// <summary>現在のコンボ数。</summary>
    public int CurrentCombo => _currentCombo;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

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
    }

    private void Update()
    {
        if (_currentCombo <= 0) return;

        _comboTimer += Time.deltaTime;
        if (_comboTimer >= COMBO_TIMEOUT)
        {
            ResetCombo();
        }
    }

    // ──────────────────────────────────────────────
    // ハンドラ
    // ──────────────────────────────────────────────

    private void HandleJustInputSuccess()
    {
        _currentCombo++;
        _comboTimer = 0f;
        OnComboChanged?.Invoke(_currentCombo);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("combo_up");
        }
    }

    private void HandleHPChanged(int currentHP, int maxHP)
    {
        // HP が減少した場合（被弾）にコンボリセット
        // OnHPChanged は回復時にも呼ばれるため、コンボが0なら無視
        if (_currentCombo > 0)
        {
            ResetCombo();
        }
    }

    // ──────────────────────────────────────────────
    // 内部メソッド
    // ──────────────────────────────────────────────

    private void ResetCombo()
    {
        _currentCombo = 0;
        _comboTimer = 0f;
        OnComboChanged?.Invoke(_currentCombo);
    }
}
