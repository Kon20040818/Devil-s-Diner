// ============================================================
// BattleUIController.cs
// メタファー:リファンタジオ風バトルUI統合コントローラー。
// UI Toolkit (UIDocument) ベースで、コマンドメニュー・行動順ゲージ・
// パーティステータスの3要素を管理する。
// 既存の uGUI 版 UI (SkillCommandUI, ActionTimelineUI, CharacterStatusUI)
// を完全に置き換える。
//
// 【設計方針】
// - rotate は配置コンテナではなくビジュアル子要素にのみ適用
// - AOG はプール方式で VisualElement を再利用
// - TryResolve パターンで UIDocument 遅延構築に対応
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class BattleUIController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 列挙
    // ──────────────────────────────────────────────

    private enum UIMode { Hidden, CommandSelect, TargetSelect }

    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const int COMMAND_COUNT = 5;
    private const int MAX_AOG_ENTRIES = 10;
    private const float LOW_HP_THRESHOLD = 0.3f;
    private const float LAG_SPEED = 2.5f;

    private static readonly CharacterBattleController.ActionType[] SLOT_TO_ACTION =
    {
        CharacterBattleController.ActionType.Skill,        // ARCHETYPE
        CharacterBattleController.ActionType.BasicAttack,   // WEAPON
        CharacterBattleController.ActionType.Skill,        // SYNTHESIS (placeholder)
        CharacterBattleController.ActionType.BasicAttack,   // ITEM (placeholder)
        CharacterBattleController.ActionType.BasicAttack,   // GUARD (placeholder)
    };

    private static readonly string[] CMD_NAMES =
    {
        "cmd-archetype", "cmd-weapon", "cmd-synthesis", "cmd-item", "cmd-guard"
    };

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private UIDocument _uiDocument;
    private BattleManager _battleManager;
    private bool _uiResolved;
    private bool _pendingInit;

    private UIMode _mode = UIMode.Hidden;
    private int _selectedCommandIndex;
    private int _selectedTargetIndex;
    private CharacterBattleController.ActionType _pendingAction;
    private readonly List<CharacterBattleController> _targetList = new List<CharacterBattleController>();

    // ──────────────────────────────────────────────
    // UXML要素キャッシュ
    // ──────────────────────────────────────────────

    private VisualElement _root;

    // Command Menu
    private VisualElement _commandMenu;
    private VisualElement _commandButtons;
    private VisualElement _targetPanel;
    private Label _activeCharName;
    private Label _cmdWatermark;
    private VisualElement _spPips;
    private Label _spValue;
    private Label _cmdHint;
    private readonly VisualElement[] _cmdButtonElements = new VisualElement[COMMAND_COUNT];

    // Action Order Gauge（プール）
    private VisualElement _aogTrack;
    private readonly List<AOGEntry> _aogPool = new List<AOGEntry>();
    private int _aogActiveCount;

    // Party Status
    private VisualElement _partyStatus;
    private readonly List<PartyCard> _partyCards = new List<PartyCard>();

    // ──────────────────────────────────────────────
    // 内部クラス
    // ──────────────────────────────────────────────

    private sealed class AOGEntry
    {
        public VisualElement Root;
        public VisualElement GlowOuter;
        public VisualElement Glow;
        public VisualElement Frame;
        public Label Initial;
    }

    private sealed class PartyCard
    {
        public CharacterBattleController Character;
        public VisualElement Root;
        public Label NameLabel;
        public Label HPText;
        public VisualElement HPFill;
        public VisualElement HPLag;
        public VisualElement EPFill;
        public float LagPercent;
        public float TargetPercent;

        // イベントハンドラ参照（解除用）
        public Action<int, int> HPHandler;
        public Action<int, int> EPHandler;
    }

    // ════════════════════════════════════════════════
    // 公開 API
    // ════════════════════════════════════════════════

    public void Initialize(BattleManager battleManager)
    {
        _battleManager = battleManager;

        if (!TryResolveUI())
        {
            _pendingInit = true;
            return;
        }

        SetupAfterResolve();
    }

    // ════════════════════════════════════════════════
    // Lifecycle
    // ════════════════════════════════════════════════

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _uiDocument.sortingOrder = 100;
    }

    private void OnEnable()
    {
        _uiResolved = false;
    }

    private void Update()
    {
        if (_pendingInit && _battleManager != null)
        {
            if (TryResolveUI())
            {
                _pendingInit = false;
                SetupAfterResolve();
            }
            return;
        }

        HandleInput();
        UpdatePartyCardLag();
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnPhaseChanged -= HandlePhaseChanged;
            _battleManager.OnActiveCharacterChanged -= HandleActiveCharacterChanged;
            _battleManager.OnSPChanged -= HandleSPChanged;
            _battleManager.OnBattleEnd -= HandleBattleEnd;

            if (_battleManager.Queue != null)
                _battleManager.Queue.OnQueueUpdated -= HandleQueueUpdated;
        }

        // パーティカードのイベント解除（メソッド参照で確実に解除）
        foreach (var card in _partyCards)
        {
            if (card.Character == null) continue;
            if (card.HPHandler != null) card.Character.OnHPChanged -= card.HPHandler;
            if (card.EPHandler != null) card.Character.OnEPChanged -= card.EPHandler;
        }
    }

    // ════════════════════════════════════════════════
    // UI解決（TryResolveパターン）
    // ════════════════════════════════════════════════

    private bool TryResolveUI()
    {
        if (_uiResolved) return _root != null;
        if (_uiDocument == null) return false;

        var rootVE = _uiDocument.rootVisualElement;
        if (rootVE == null) return false;

        _root = rootVE.Q<VisualElement>("battle-ui-root");
        if (_root == null) return false;

        _commandMenu = _root.Q<VisualElement>("command-menu");
        _commandButtons = _root.Q<VisualElement>("command-buttons");
        _targetPanel = _root.Q<VisualElement>("target-panel");
        _activeCharName = _root.Q<Label>("active-char-name");
        _cmdWatermark = _root.Q<Label>("cmd-watermark");
        _spPips = _root.Q<VisualElement>("sp-pips");
        _spValue = _root.Q<Label>("sp-value");
        _cmdHint = _root.Q<Label>("cmd-hint");

        // コマンドボタンを全部nullチェック付きで取得
        bool allFound = true;
        for (int i = 0; i < COMMAND_COUNT; i++)
        {
            _cmdButtonElements[i] = _root.Q<VisualElement>(CMD_NAMES[i]);
            if (_cmdButtonElements[i] == null) allFound = false;

            int idx = i;
            _cmdButtonElements[i]?.RegisterCallback<ClickEvent>(_ =>
            {
                if (_mode == UIMode.CommandSelect)
                {
                    _selectedCommandIndex = idx;
                    UpdateCommandHighlight();
                    ConfirmCommand();
                }
            });
        }

        _aogTrack = _root.Q<VisualElement>("aog-track");
        _partyStatus = _root.Q<VisualElement>("party-status");

        if (_commandMenu == null || _aogTrack == null || _partyStatus == null || !allFound)
        {
            Debug.LogWarning("[BattleUIController] 一部のUI要素が見つかりません。次フレームでリトライします。");
            return false;
        }

        _uiResolved = true;
        return true;
    }

    private void SetupAfterResolve()
    {
        // rootVisualElement にサイズを強制（NaN対策）
        var rootVE = _uiDocument.rootVisualElement;
        rootVE.style.flexGrow = 1;
        rootVE.style.position = Position.Absolute;
        rootVE.style.left = 0;
        rootVE.style.top = 0;
        rootVE.style.right = 0;
        rootVE.style.bottom = 0;

        _battleManager.OnPhaseChanged += HandlePhaseChanged;
        _battleManager.OnActiveCharacterChanged += HandleActiveCharacterChanged;
        _battleManager.OnSPChanged += HandleSPChanged;
        _battleManager.OnBattleEnd += HandleBattleEnd;

        if (_battleManager.Queue != null)
            _battleManager.Queue.OnQueueUpdated += HandleQueueUpdated;

        BuildSPPips();
        HandleSPChanged(_battleManager.CurrentSP, _battleManager.MaxSP);
        BuildPartyStatusCards();
        PreallocateAOGPool();
        RefreshActionOrder();

        Debug.Log("[BattleUIController] Metaphor UI 初期化完了。");
    }

    // ════════════════════════════════════════════════
    // イベントハンドラ
    // ════════════════════════════════════════════════

    private void HandlePhaseChanged(BattleManager.BattlePhase phase)
    {
        switch (phase)
        {
            case BattleManager.BattlePhase.PlayerCommand:
                ShowCommandMenu();
                break;

            case BattleManager.BattlePhase.Executing:
            case BattleManager.BattlePhase.EnemyAction:
            case BattleManager.BattlePhase.TurnEnd:
            case BattleManager.BattlePhase.Victory:
            case BattleManager.BattlePhase.Defeat:
                HideCommandMenu();
                break;
        }
    }

    private void HandleActiveCharacterChanged(CharacterBattleController character)
    {
        foreach (var card in _partyCards)
        {
            if (card.Character == character)
                card.Root.AddToClassList("ps-active");
            else
                card.Root.RemoveFromClassList("ps-active");
        }
        RefreshActionOrder();
    }

    private void HandleSPChanged(int current, int max)
    {
        UpdateSPDisplay(current, max);
    }

    private void HandleBattleEnd(bool victory)
    {
        HideCommandMenu();
    }

    private void HandleQueueUpdated()
    {
        RefreshActionOrder();
    }

    // ════════════════════════════════════════════════
    // コマンドメニュー
    // ════════════════════════════════════════════════

    private void ShowCommandMenu()
    {
        if (_commandMenu == null) return;

        _mode = UIMode.CommandSelect;
        _selectedCommandIndex = 0;

        var active = _battleManager.ActiveCharacter;
        if (_activeCharName != null && active != null)
            _activeCharName.text = active.DisplayName?.ToUpper() ?? "---";

        // ウォーターマーク更新（キャラ名の巨大背景文字）
        if (_cmdWatermark != null && active != null)
            _cmdWatermark.text = active.DisplayName?.ToUpper() ?? "COMMAND";

        UpdateCommandAvailability();
        UpdateCommandHighlight();

        _targetPanel?.AddToClassList("hidden");
        _commandButtons?.RemoveFromClassList("hidden");
        if (_cmdHint != null) _cmdHint.text = "[Z] Confirm  [X] Cancel  [\u2191\u2193] Select";

        _commandMenu.RemoveFromClassList("hidden");
    }

    private void HideCommandMenu()
    {
        _mode = UIMode.Hidden;
        _commandMenu?.AddToClassList("hidden");
    }

    private void UpdateCommandAvailability()
    {
        bool canSkill = _battleManager.CanUseSkill();

        for (int i = 0; i < COMMAND_COUNT; i++)
        {
            if (_cmdButtonElements[i] == null) continue;

            bool disabled = SLOT_TO_ACTION[i] == CharacterBattleController.ActionType.Skill && !canSkill;

            if (disabled)
                _cmdButtonElements[i].AddToClassList("is-disabled");
            else
                _cmdButtonElements[i].RemoveFromClassList("is-disabled");
        }
    }

    private void UpdateCommandHighlight()
    {
        for (int i = 0; i < COMMAND_COUNT; i++)
        {
            if (_cmdButtonElements[i] == null) continue;

            if (i == _selectedCommandIndex)
                _cmdButtonElements[i].AddToClassList("is-active");
            else
                _cmdButtonElements[i].RemoveFromClassList("is-active");
        }
    }

    // ════════════════════════════════════════════════
    // ターゲット選択
    // ════════════════════════════════════════════════

    private void EnterTargetSelection()
    {
        _targetList.Clear();
        _selectedTargetIndex = 0;

        var enemies = _battleManager.EnemyParty;
        if (enemies == null) return;

        foreach (var e in enemies)
        {
            if (e != null && e.IsAlive)
                _targetList.Add(e);
        }

        if (_targetList.Count == 0)
        {
            Debug.LogWarning("[BattleUIController] 生存中のターゲットがいません。");
            return;
        }

        _mode = UIMode.TargetSelect;
        _commandButtons?.AddToClassList("hidden");
        _targetPanel?.RemoveFromClassList("hidden");

        RebuildTargetEntries();
        UpdateTargetHighlight();

        if (_cmdHint != null) _cmdHint.text = "[Z] Confirm  [X] Back  [\u2191\u2193] Select";
    }

    private void RebuildTargetEntries()
    {
        if (_targetPanel == null) return;
        _targetPanel.Clear();

        // ターゲットもコマンドと同じ扇状配置にする
        // コマンドボタンと同じ弧の位置に重ねて表示
        int[] xOffsets = { 30, 70, 130, 210, 290 };
        int[] yOffsets = { 100, 180, 260, 330, 390 };
        int[] rotations = { -18, -12, -7, -3, 0 };

        for (int i = 0; i < _targetList.Count; i++)
        {
            var target = _targetList[i];
            int slot = Mathf.Min(i, xOffsets.Length - 1);

            var entry = new VisualElement();
            entry.AddToClassList("target-entry");
            // absolute配置 + translate で扇状に散らす
            entry.style.translate = new Translate(xOffsets[slot], yOffsets[slot]);
            entry.style.rotate = new Rotate(Angle.Degrees(rotations[slot]));

            var visual = new VisualElement();
            visual.AddToClassList("target-entry-visual");

            var bg = new VisualElement();
            bg.AddToClassList("target-entry-bg");

            var nameLabel = new Label(target.DisplayName?.ToUpper() ?? "???");
            nameLabel.AddToClassList("target-entry-name");

            var hpLabel = new Label($"HP {target.CurrentHP}/{target.MaxHP}");
            hpLabel.AddToClassList("target-entry-hp");

            visual.Add(bg);
            visual.Add(nameLabel);
            visual.Add(hpLabel);
            entry.Add(visual);

            int idx = i;
            entry.RegisterCallback<ClickEvent>(_ =>
            {
                _selectedTargetIndex = idx;
                UpdateTargetHighlight();
                ConfirmTarget();
            });

            _targetPanel.Add(entry);
        }
    }

    private void UpdateTargetHighlight()
    {
        if (_targetPanel == null) return;

        for (int i = 0; i < _targetPanel.childCount; i++)
        {
            var child = _targetPanel[i];
            if (i == _selectedTargetIndex)
                child.AddToClassList("is-active");
            else
                child.RemoveFromClassList("is-active");
        }
    }

    private void ReturnToCommandMode()
    {
        _mode = UIMode.CommandSelect;
        _targetPanel?.AddToClassList("hidden");
        _commandButtons?.RemoveFromClassList("hidden");
        UpdateCommandHighlight();
        if (_cmdHint != null) _cmdHint.text = "[Z] Confirm  [X] Cancel  [\u2191\u2193] Select";
    }

    // ════════════════════════════════════════════════
    // 入力処理
    // ════════════════════════════════════════════════

    private void HandleInput()
    {
        if (_mode == UIMode.Hidden) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (_mode == UIMode.CommandSelect)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                NavigateCommand(-1);
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                NavigateCommand(+1);

            if (kb.zKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                ConfirmCommand();
        }
        else if (_mode == UIMode.TargetSelect)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                NavigateTarget(-1);
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                NavigateTarget(+1);

            if (kb.zKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                ConfirmTarget();

            if (kb.xKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
                ReturnToCommandMode();
        }
    }

    private void NavigateCommand(int delta)
    {
        _selectedCommandIndex = (_selectedCommandIndex + delta + COMMAND_COUNT) % COMMAND_COUNT;
        UpdateCommandHighlight();
    }

    private void ConfirmCommand()
    {
        if (_selectedCommandIndex < 0 || _selectedCommandIndex >= COMMAND_COUNT) return;

        if (_cmdButtonElements[_selectedCommandIndex] != null &&
            _cmdButtonElements[_selectedCommandIndex].ClassListContains("is-disabled"))
        {
            Debug.Log("[BattleUIController] このコマンドは使用不可です。");
            return;
        }

        _pendingAction = SLOT_TO_ACTION[_selectedCommandIndex];
        EnterTargetSelection();
    }

    private void NavigateTarget(int delta)
    {
        if (_targetList.Count == 0) return;
        _selectedTargetIndex = (_selectedTargetIndex + delta + _targetList.Count) % _targetList.Count;
        UpdateTargetHighlight();
    }

    private void ConfirmTarget()
    {
        if (_targetList.Count == 0 || _selectedTargetIndex >= _targetList.Count) return;

        var target = _targetList[_selectedTargetIndex];
        Debug.Log($"[BattleUIController] アクション実行: {_pendingAction} → {target.DisplayName}");

        HideCommandMenu();
        _battleManager.ExecutePlayerAction(_pendingAction, target);
    }

    // ════════════════════════════════════════════════
    // SP 表示
    // ════════════════════════════════════════════════

    private void BuildSPPips()
    {
        if (_spPips == null) return;
        _spPips.Clear();

        int max = _battleManager.MaxSP;
        for (int i = 0; i < max; i++)
        {
            var pip = new VisualElement();
            pip.AddToClassList("sp-pip");
            _spPips.Add(pip);
        }
    }

    private void UpdateSPDisplay(int current, int max)
    {
        if (_spPips != null)
        {
            for (int i = 0; i < _spPips.childCount; i++)
            {
                if (i < current)
                    _spPips[i].AddToClassList("sp-pip-active");
                else
                    _spPips[i].RemoveFromClassList("sp-pip-active");
            }
        }

        if (_spValue != null)
            _spValue.text = $"{current}/{max}";
    }

    // ════════════════════════════════════════════════
    // Action Order Gauge（プール方式）
    // ════════════════════════════════════════════════

    private void PreallocateAOGPool()
    {
        for (int i = 0; i < MAX_AOG_ENTRIES; i++)
        {
            var aogEntry = CreateAOGEntry();
            aogEntry.Root.style.display = DisplayStyle.None;
            _aogTrack.Add(aogEntry.Root);
            _aogPool.Add(aogEntry);
        }
        _aogActiveCount = 0;
    }

    private AOGEntry CreateAOGEntry()
    {
        var entry = new AOGEntry();

        entry.Root = new VisualElement();
        entry.Root.AddToClassList("aog-entry");

        // 後光エフェクト（背面、アクティブ時のみ表示）
        entry.GlowOuter = new VisualElement();
        entry.GlowOuter.AddToClassList("aog-glow-outer");
        entry.GlowOuter.style.display = DisplayStyle.None;

        entry.Glow = new VisualElement();
        entry.Glow.AddToClassList("aog-glow");
        entry.Glow.style.display = DisplayStyle.None;

        entry.Frame = new VisualElement();
        entry.Frame.AddToClassList("aog-entry-frame");

        entry.Initial = new Label("?");
        entry.Initial.AddToClassList("aog-entry-initial");

        entry.Frame.Add(entry.Initial);
        entry.Root.Add(entry.GlowOuter);
        entry.Root.Add(entry.Glow);
        entry.Root.Add(entry.Frame);

        return entry;
    }

    private void RefreshActionOrder()
    {
        if (_aogTrack == null || _battleManager == null || _battleManager.Queue == null) return;

        var order = _battleManager.Queue.GetOrderPreview(MAX_AOG_ENTRIES);
        int count = Mathf.Min(order.Count, _aogPool.Count);

        for (int i = 0; i < _aogPool.Count; i++)
        {
            var poolEntry = _aogPool[i];

            if (i < count)
            {
                var character = order[i];
                bool isActive = (i == 0);
                bool isPlayer = character.CharacterFaction == CharacterBattleController.Faction.Player;

                // クラスリセット
                poolEntry.Root.RemoveFromClassList("aog-entry-player");
                poolEntry.Root.RemoveFromClassList("aog-entry-enemy");
                poolEntry.Root.RemoveFromClassList("aog-active");

                poolEntry.Root.AddToClassList(isPlayer ? "aog-entry-player" : "aog-entry-enemy");
                if (isActive) poolEntry.Root.AddToClassList("aog-active");

                // 後光はアクティブ時のみ表示
                poolEntry.Glow.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
                poolEntry.GlowOuter.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;

                string displayName = character.DisplayName ?? "?";
                poolEntry.Initial.text = displayName.Length > 0 ? displayName.Substring(0, 1) : "?";

                poolEntry.Root.style.display = DisplayStyle.Flex;
            }
            else
            {
                poolEntry.Root.style.display = DisplayStyle.None;
            }
        }

        _aogActiveCount = count;
    }

    // ════════════════════════════════════════════════
    // Party Status Cards
    // ════════════════════════════════════════════════

    private void BuildPartyStatusCards()
    {
        if (_partyStatus == null || _battleManager == null) return;
        _partyStatus.Clear();
        _partyCards.Clear();

        var party = _battleManager.PlayerParty;
        if (party == null) return;

        foreach (var character in party)
        {
            if (character == null) continue;
            var card = CreatePartyCard(character);
            _partyStatus.Add(card.Root);
            _partyCards.Add(card);
        }
    }

    private PartyCard CreatePartyCard(CharacterBattleController character)
    {
        var card = new PartyCard
        {
            Character = character,
            LagPercent = 100f,
            TargetPercent = 100f
        };

        // 配置コンテナ（rotateなし）
        card.Root = new VisualElement();
        card.Root.AddToClassList("ps-card");

        if (_battleManager.ActiveCharacter == character)
            card.Root.AddToClassList("ps-active");

        // ビジュアルラッパー（rotateはここ）
        var visual = new VisualElement();
        visual.AddToClassList("ps-card-visual");

        var inner = new VisualElement();
        inner.AddToClassList("ps-card-inner");

        // ウォーターマーク（巨大薄文字、背面レイヤー）
        var watermark = new Label(character.DisplayName?.ToUpper() ?? "");
        watermark.AddToClassList("ps-watermark");
        inner.Add(watermark);

        // ヘッダー
        var header = new VisualElement();
        header.AddToClassList("ps-card-header");

        card.NameLabel = new Label(character.DisplayName?.ToUpper() ?? "---");
        card.NameLabel.AddToClassList("ps-char-name");

        card.HPText = new Label($"{character.CurrentHP}/{character.MaxHP}");
        card.HPText.AddToClassList("ps-hp-text");

        header.Add(card.NameLabel);
        header.Add(card.HPText);
        inner.Add(header);

        // HP バー
        var hpBg = new VisualElement();
        hpBg.AddToClassList("ps-hp-bar-bg");
        hpBg.style.position = Position.Relative;

        float hpPercent = character.MaxHP > 0 ? (float)character.CurrentHP / character.MaxHP * 100f : 0f;

        card.HPLag = new VisualElement();
        card.HPLag.AddToClassList("ps-hp-bar-lag");
        card.HPLag.style.width = Length.Percent(hpPercent);
        card.LagPercent = hpPercent;
        card.TargetPercent = hpPercent;

        card.HPFill = new VisualElement();
        card.HPFill.AddToClassList("ps-hp-bar-fill");
        card.HPFill.style.width = Length.Percent(hpPercent);

        hpBg.Add(card.HPLag);
        hpBg.Add(card.HPFill);
        inner.Add(hpBg);

        // EP バー
        var epBg = new VisualElement();
        epBg.AddToClassList("ps-ep-bar-bg");
        epBg.style.position = Position.Relative;

        card.EPFill = new VisualElement();
        card.EPFill.AddToClassList("ps-ep-bar-fill");
        float epPercent = character.MaxEP > 0 ? (float)character.CurrentEP / character.MaxEP * 100f : 0f;
        card.EPFill.style.width = Length.Percent(epPercent);

        epBg.Add(card.EPFill);
        inner.Add(epBg);

        visual.Add(inner);
        card.Root.Add(visual);

        // イベント（メソッド参照で保持→OnDestroyで解除可能）
        card.HPHandler = (hp, max) => UpdateCardHP(card, hp, max);
        card.EPHandler = (ep, max) => UpdateCardEP(card, ep, max);
        character.OnHPChanged += card.HPHandler;
        character.OnEPChanged += card.EPHandler;

        UpdateCardState(card);
        return card;
    }

    private void UpdateCardHP(PartyCard card, int hp, int max)
    {
        float percent = max > 0 ? (float)hp / max * 100f : 0f;
        card.TargetPercent = percent;
        card.HPFill.style.width = Length.Percent(percent);
        card.HPText.text = $"{hp}/{max}";
        UpdateCardState(card);
    }

    private void UpdateCardEP(PartyCard card, int ep, int max)
    {
        float percent = max > 0 ? (float)ep / max * 100f : 0f;
        card.EPFill.style.width = Length.Percent(percent);
    }

    private void UpdateCardState(PartyCard card)
    {
        if (card.Character == null) return;

        if (!card.Character.IsAlive)
        {
            card.Root.AddToClassList("ps-dead");
            card.Root.RemoveFromClassList("ps-low-hp");
            return;
        }

        card.Root.RemoveFromClassList("ps-dead");

        float ratio = card.Character.MaxHP > 0
            ? (float)card.Character.CurrentHP / card.Character.MaxHP
            : 1f;

        if (ratio <= LOW_HP_THRESHOLD)
            card.Root.AddToClassList("ps-low-hp");
        else
            card.Root.RemoveFromClassList("ps-low-hp");
    }

    private void UpdatePartyCardLag()
    {
        float dt = Time.unscaledDeltaTime;

        foreach (var card in _partyCards)
        {
            if (Mathf.Abs(card.LagPercent - card.TargetPercent) > 0.1f)
            {
                card.LagPercent = Mathf.Lerp(card.LagPercent, card.TargetPercent, dt * LAG_SPEED);
                card.HPLag.style.width = Length.Percent(card.LagPercent);
            }
            else if (card.LagPercent != card.TargetPercent)
            {
                card.LagPercent = card.TargetPercent;
                card.HPLag.style.width = Length.Percent(card.LagPercent);
            }
        }
    }
}
