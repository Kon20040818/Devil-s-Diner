// ============================================================
// BattleUIManager.cs
// バトルUIの統合管理。BattleManager のイベントを購読し、
// 各UIコンポーネントへの通知を一元化する。
// スターレイル風UIへの全面改修版。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// バトルUI全体を統合管理するコンポーネント。
/// BattleCanvas の最上位にアタッチし、子UIへの参照を保持する。
/// </summary>
public sealed class BattleUIManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("UI参照")]
    [SerializeField] private ActionTimelineUI _timelineUI;
    [SerializeField] private SkillCommandUI _skillCommandUI;
    [SerializeField] private UltimatePortraitUI _ultimatePortraitUI;
    [SerializeField] private CharacterStatusUI _characterStatusUI;
    [SerializeField] private DamageNumberUI _damageNumberUI;
    [SerializeField] private BattleEffectsUI _battleEffectsUI;

    [Header("トグルボタン")]
    [SerializeField] private Button _autoToggleButton;
    [SerializeField] private Button _speedToggleButton;
    [SerializeField] private Text _autoToggleText;
    [SerializeField] private Text _speedToggleText;

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private BattleManager _battleManager;
    private BattleCameraManager _cameraManager;
    private CharacterBattleController _pendingUltimateCharacter;
    private readonly List<EnemyStatusUI> _enemyStatusUIs = new List<EnemyStatusUI>();

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>SkillCommandUI を外部から設定する。</summary>
    public void SetSkillCommandUI(SkillCommandUI skillCmd)
    {
        _skillCommandUI = skillCmd;
    }

    /// <summary>UltimatePortraitUI を外部から設定する。</summary>
    public void SetUltimatePortraitUI(UltimatePortraitUI ultimateUI)
    {
        _ultimatePortraitUI = ultimateUI;
    }

    /// <summary>BattleManager を注入してイベント購読を開始する。</summary>
    public void Initialize(BattleManager battleManager)
    {
        _battleManager = battleManager;
        _cameraManager = FindFirstObjectByType<BattleCameraManager>();

        // イベント購読
        _battleManager.OnPhaseChanged += HandlePhaseChanged;
        _battleManager.OnActiveCharacterChanged += HandleActiveCharacterChanged;
        _battleManager.OnSPChanged += HandleSPChanged;
        _battleManager.OnBattleEnd += HandleBattleEnd;
        _battleManager.OnDamageDealt += HandleDamageDealt;
        _battleManager.OnUltimateActivated += HandleUltimateActivated;
        _battleManager.OnSkillExecuted += HandleSkillExecuted;
        _battleManager.Queue.OnQueueUpdated += HandleQueueUpdated;

        // ── ActionTimelineUI ──
        if (_timelineUI != null) _timelineUI.Initialize(_battleManager);

        // ── CharacterStatusUI (HP表示のみ) ──
        if (_characterStatusUI != null) _characterStatusUI.Initialize(_battleManager);

        // ── SkillCommandUI ──
        if (_skillCommandUI != null)
        {
            _skillCommandUI.Initialize(_battleManager);
            _skillCommandUI.OnCommandSelected += HandleCommandSelected;
            _skillCommandUI.Hide();
        }

        // ── UltimatePortraitUI ──
        if (_ultimatePortraitUI != null)
        {
            _ultimatePortraitUI.Initialize(_battleManager, _battleManager.PlayerParty);
            _ultimatePortraitUI.OnUltimateRequested += HandleUltimateRequested;
        }

        // ── DamageNumberUI ──
        if (_damageNumberUI != null)
        {
            _damageNumberUI.Initialize(Camera.main);
        }

        // ── BattleEffectsUI ──
        if (_battleEffectsUI != null)
        {
            _battleEffectsUI.Initialize();
        }

        // ── EnemyStatusUI (各敵に生成) ──
        if (_battleManager.EnemyParty != null)
        {
            foreach (var enemy in _battleManager.EnemyParty)
            {
                if (enemy == null) continue;
                var statusUI = enemy.gameObject.AddComponent<EnemyStatusUI>();
                statusUI.Initialize(enemy);
                _enemyStatusUIs.Add(statusUI);

                // 靭性破壊エフェクト
                enemy.OnToughnessBreak += HandleToughnessBreak;
            }
        }

        // ── Auto/Speed トグルボタン ──
        if (_autoToggleButton != null)
        {
            _autoToggleButton.onClick.AddListener(HandleAutoToggle);
            UpdateAutoToggleVisual();
        }
        if (_speedToggleButton != null)
        {
            _speedToggleButton.onClick.AddListener(HandleSpeedToggle);
            UpdateSpeedToggleVisual();
        }

        // SP初期表示
        HandleSPChanged(_battleManager.CurrentSP, _battleManager.MaxSP);
    }

    // ──────────────────────────────────────────────
    // イベントハンドラ
    // ──────────────────────────────────────────────

    private void HandlePhaseChanged(BattleManager.BattlePhase phase)
    {
        switch (phase)
        {
            case BattleManager.BattlePhase.BattleStart:
                if (_battleEffectsUI != null)
                    StartCoroutine(_battleEffectsUI.PlayBattleStartEffect());
                break;

            case BattleManager.BattlePhase.PlayerCommand:
                if (_skillCommandUI != null)
                {
                    _skillCommandUI.Show(
                        _battleManager.ActiveCharacter,
                        _battleManager.CanUseSkill());
                }
                if (_battleEffectsUI != null)
                    _battleEffectsUI.PlayTurnStartFlash();
                break;

            case BattleManager.BattlePhase.Executing:
            case BattleManager.BattlePhase.EnemyAction:
            case BattleManager.BattlePhase.TurnEnd:
                if (_skillCommandUI != null) _skillCommandUI.Hide();
                break;

            case BattleManager.BattlePhase.Victory:
                if (_skillCommandUI != null) _skillCommandUI.Hide();
                if (_battleEffectsUI != null)
                    StartCoroutine(_battleEffectsUI.PlayVictoryEffect());
                break;

            case BattleManager.BattlePhase.Defeat:
                if (_skillCommandUI != null) _skillCommandUI.Hide();
                if (_battleEffectsUI != null)
                    StartCoroutine(_battleEffectsUI.PlayDefeatEffect());
                break;
        }
    }

    private void HandleCommandSelected(CharacterBattleController.ActionType actionType)
    {
        if (_skillCommandUI != null)
        {
            _skillCommandUI.EnterTargetSelection(actionType, _battleManager.EnemyParty);
        }
    }

    private void HandleActiveCharacterChanged(CharacterBattleController character)
    {
        if (_timelineUI != null) _timelineUI.Refresh();
    }

    private void HandleSPChanged(int current, int max)
    {
        if (_skillCommandUI != null)
        {
            _skillCommandUI.UpdateSP(current, max);
        }
    }

    private void HandleQueueUpdated()
    {
        if (_timelineUI != null) _timelineUI.Refresh();
    }

    private void HandleDamageDealt(CharacterBattleController.DamageResult result)
    {
        if (_damageNumberUI != null)
            _damageNumberUI.SpawnDamageNumber(result);
    }

    private void HandleToughnessBreak(CharacterBattleController character)
    {
        if (_battleEffectsUI != null) _battleEffectsUI.PlayBreakFlash();
        if (_cameraManager != null) _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_BREAK);
    }

    private void HandleUltimateRequested(CharacterBattleController character)
    {
        // UltimatePortraitUI がターゲット選択→ExecuteUltimate を直接呼ぶので
        // ここでは特別な処理は不要（ポートレートUI内で完結）
    }

    private void HandleUltimateActivated(CharacterBattleController character)
    {
        // 必殺技カットイン演出 + フラッシュ
        if (_battleEffectsUI != null)
        {
            _battleEffectsUI.PlayUltimateFlash();
            _battleEffectsUI.PlayUltimateCutIn(character.DisplayName);
        }
    }

    private void HandleSkillExecuted(CharacterBattleController character, string skillName)
    {
        // スキル名表示演出
        if (_battleEffectsUI != null)
        {
            _battleEffectsUI.PlaySkillNameDisplay(
                $"{character.DisplayName} - {skillName}");
        }
    }

    private void HandleBattleEnd(bool victory)
    {
        if (_skillCommandUI != null) _skillCommandUI.Hide();
        Debug.Log($"[BattleUIManager] バトル終了: {(victory ? "勝利" : "敗北")}");
    }

    // ──────────────────────────────────────────────
    // Auto / Speed トグル
    // ──────────────────────────────────────────────

    private void HandleAutoToggle()
    {
        if (_battleManager != null)
        {
            _battleManager.ToggleAutoBattle();
            UpdateAutoToggleVisual();
        }
    }

    private void HandleSpeedToggle()
    {
        if (_battleManager != null)
        {
            _battleManager.ToggleSpeed();
            UpdateSpeedToggleVisual();
        }
    }

    private void UpdateAutoToggleVisual()
    {
        if (_autoToggleText != null)
        {
            bool on = _battleManager != null && _battleManager.IsAutoBattle;
            _autoToggleText.text = on ? "Auto ON" : "Auto";
            _autoToggleText.color = on ? new Color(0.3f, 0.9f, 1f) : Color.white;
        }
    }

    private void UpdateSpeedToggleVisual()
    {
        if (_speedToggleText != null)
        {
            bool on = _battleManager != null && _battleManager.IsDoubleSpeed;
            _speedToggleText.text = on ? "2x" : "1x";
            _speedToggleText.color = on ? new Color(1f, 0.85f, 0.2f) : Color.white;
        }
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void OnDestroy()
    {
        if (_skillCommandUI != null)
        {
            _skillCommandUI.OnCommandSelected -= HandleCommandSelected;
        }

        if (_ultimatePortraitUI != null)
        {
            _ultimatePortraitUI.OnUltimateRequested -= HandleUltimateRequested;
        }

        if (_battleManager != null)
        {
            _battleManager.OnPhaseChanged -= HandlePhaseChanged;
            _battleManager.OnActiveCharacterChanged -= HandleActiveCharacterChanged;
            _battleManager.OnSPChanged -= HandleSPChanged;
            _battleManager.OnBattleEnd -= HandleBattleEnd;
            _battleManager.OnDamageDealt -= HandleDamageDealt;
            _battleManager.OnUltimateActivated -= HandleUltimateActivated;
            _battleManager.OnSkillExecuted -= HandleSkillExecuted;

            if (_battleManager.Queue != null)
            {
                _battleManager.Queue.OnQueueUpdated -= HandleQueueUpdated;
            }

            if (_battleManager.EnemyParty != null)
            {
                foreach (var enemy in _battleManager.EnemyParty)
                {
                    if (enemy != null) enemy.OnToughnessBreak -= HandleToughnessBreak;
                }
            }
        }

        if (_autoToggleButton != null) _autoToggleButton.onClick.RemoveListener(HandleAutoToggle);
        if (_speedToggleButton != null) _speedToggleButton.onClick.RemoveListener(HandleSpeedToggle);
    }
}
