// ============================================================
// DynamicBattleUIController.cs
// メタファー:リファンタジオ風バトルUI — 3層ジオメトリ + ダイナミック追従
//
// 【構造】
// RadialPivot: rotate:12deg（唯一の回転） — 空間傾斜（右斜め下）
// cmd-slot: inline translate — Cカーブ配置（30px極薄リボン）
// cmd-bg: 回転なし — 黒半透明の土台（将来テクスチャ差替え用）
//
// 【ダイナミック追従】
// ・SmoothDamp による遅延追従（重量感）
// ・カメラ差分による視差効果（parallax-bg / radial-pivot で係数差）
// ・パースペクティブ偽装（カメラ回り込み時の ScaleX / Rotate 微調整）
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class DynamicBattleUIController : MonoBehaviour
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

    // SmoothDamp パラメータ
    private const float SMOOTH_TIME = 0.12f;           // 追従の柔らかさ（秒）
    private const float MAX_SPEED = 4000f;              // 最大追従速度（px/s）

    // 視差効果係数（カメラ差分に対する感度）
    private const float PARALLAX_BG_FACTOR = 0.35f;     // 背景レイヤー（大きく動く）
    private const float PARALLAX_CMD_FACTOR = 0.08f;    // コマンドレイヤー（わずかに動く）

    // パースペクティブ偽装
    private const float PERSPECTIVE_ROTATE_FACTOR = 2.5f;   // カメラYaw差分→Rotate(deg)
    private const float PERSPECTIVE_SCALE_FACTOR = 0.04f;   // カメラYaw差分→ScaleX増減
    private const float PERSPECTIVE_BASE_ROTATE = 8f;       // ベース傾斜角（右斜め下に傾ける）

    // キャラ名・SP・ヒントのオフセット（パネル座標、radial-pivot基準ではなくプレイヤー基準）
    private const float INFO_OFFSET_X = -100f;
    private const float INFO_OFFSET_Y = -260f;
    private const float HINT_OFFSET_X = -140f;
    private const float HINT_OFFSET_Y = 210f;

    // ワールド→スクリーン変換でのキャラ高さ補正 (0.0f = 中心)
    private const float WORLD_Y_OFFSET = 0.0f;
    
    // UIの基準となるカメラ距離（近づいた時にUIを拡大する用）
    private const float REFERENCE_DISTANCE = 8f;

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
        "cmd-attack", "cmd-skill", "cmd-meal", "cmd-scout", "cmd-guard"
    };

    // UXML inline translate値（△○×□ 十字配置 — 中心(-150,-30) 縦150 横220）
    private static readonly Vector2[] SLOT_BASE_TRANSLATE =
    {
        new Vector2(-150f, -180f),  // △ ATTACK 上
        new Vector2(  70f,  -30f),  // ○ SKILL 右
        new Vector2(-370f,  -30f),  // × MEAL 左
        new Vector2(-150f,  120f),  // □ SCOUT 下
        new Vector2(-150f,  200f),  // GUARD 下寄り
    };

    private const float ACTIVE_SHIFT_X = 16f; // is-active 時の右シフト

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

    // SmoothDamp 状態
    private Vector2 _currentPanelPos;
    private Vector2 _panelVelocity;

    // 視差・パースペクティブ用カメラ追跡
    private Vector3 _prevCamEuler;
    private Vector3 _prevCamPos;
    private Vector2 _parallaxBgOffset;
    private float _perspectiveRotate;
    private float _perspectiveScaleX;

    // ──────────────────────────────────────────────
    // UXML要素キャッシュ
    // ──────────────────────────────────────────────

    private VisualElement _root;

    // 視差背景
    private VisualElement _parallaxBg;

    // Command Menu
    private VisualElement _commandMenu;
    private VisualElement _cmdRadialPivot;
    private VisualElement _commandButtons;
    private VisualElement _targetPanel;
    private VisualElement _cmdInfoGroup;
    private Label _activeCharName;
    private Label _cmdWatermark;
    private VisualElement _spPips;
    private Label _spValue;
    private VisualElement _cmdHintWrap;
    private Label _cmdHint;
    private readonly VisualElement[] _cmdButtonElements = new VisualElement[COMMAND_COUNT];

    // Action Order Gauge
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
        UpdateCameraTracking();
        UpdateDynamicPositions();
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

        foreach (var card in _partyCards)
        {
            if (card.Character == null) continue;
            if (card.HPHandler != null) card.Character.OnHPChanged -= card.HPHandler;
            if (card.EPHandler != null) card.Character.OnEPChanged -= card.EPHandler;
        }
    }

    // ════════════════════════════════════════════════
    // UI解決（TryResolve パターン）
    // ════════════════════════════════════════════════

    private bool TryResolveUI()
    {
        if (_uiResolved) return _root != null;
        if (_uiDocument == null) return false;

        var rootVE = _uiDocument.rootVisualElement;
        if (rootVE == null) return false;

        _root = rootVE.Q<VisualElement>("battle-ui-root");
        if (_root == null) return false;

        _parallaxBg      = _root.Q<VisualElement>("parallax-bg");
        _commandMenu      = _root.Q<VisualElement>("command-menu");
        _cmdRadialPivot   = _root.Q<VisualElement>("cmd-radial-pivot");
        _commandButtons   = _root.Q<VisualElement>("command-buttons");
        _targetPanel      = _root.Q<VisualElement>("target-panel");
        _cmdInfoGroup     = _root.Q<VisualElement>("cmd-info-group");
        _activeCharName   = _root.Q<Label>("active-char-name");
        _cmdWatermark     = _root.Q<Label>("cmd-watermark");
        _spPips           = _root.Q<VisualElement>("sp-pips");
        _spValue          = _root.Q<Label>("sp-value");
        _cmdHintWrap      = _root.Q<VisualElement>("cmd-hint-wrap");
        _cmdHint          = _root.Q<Label>("cmd-hint");

        bool allFound = true;
        for (int i = 0; i < COMMAND_COUNT; i++)
        {
            _cmdButtonElements[i] = _root.Q<VisualElement>(CMD_NAMES[i]);
            if (_cmdButtonElements[i] == null) { allFound = false; continue; }

            int idx = i;
            _cmdButtonElements[i].RegisterCallback<ClickEvent>(_ =>
            {
                if (_mode == UIMode.CommandSelect)
                {
                    _selectedCommandIndex = idx;
                    UpdateCommandHighlight();
                    ConfirmCommand();
                }
            });
        }

        _aogTrack    = _root.Q<VisualElement>("aog-track");
        _partyStatus = _root.Q<VisualElement>("party-status");

        if (_commandMenu == null || _cmdRadialPivot == null ||
            _aogTrack == null || _partyStatus == null || !allFound)
        {
            Debug.LogWarning("[DynamicBattleUI] 一部のUI要素が見つかりません。次フレームでリトライします。");
            return false;
        }

        _uiResolved = true;
        return true;
    }

    private void SetupAfterResolve()
    {
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

        // カメラ初期状態を取得
        var cam = Camera.main;
        if (cam != null)
        {
            _prevCamEuler = cam.transform.eulerAngles;
            _prevCamPos = cam.transform.position;
        }

        // 初期位置を即座に設定（SmoothDampの初回ジャンプ防止）
        _currentPanelPos = GetTargetPanelPos();

        BuildSPPips();
        HandleSPChanged(_battleManager.CurrentSP, _battleManager.MaxSP);
        BuildPartyStatusCards();
        PreallocateAOGPool();
        RefreshActionOrder();

        Debug.Log("[DynamicBattleUI] 初期化完了（3層ジオメトリ + SmoothDamp + 視差効果）。");
    }

    // ════════════════════════════════════════════════
    // ワールド→パネル座標変換
    // ════════════════════════════════════════════════

    private Vector2 GetTargetPanelPos()
    {
        var cam = Camera.main;
        if (cam == null || _battleManager == null || _battleManager.ActiveCharacter == null)
            return new Vector2(700f, 540f);

        Vector3 worldPos = _battleManager.ActiveCharacter.transform.position + Vector3.up * WORLD_Y_OFFSET;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0)
            return new Vector2(700f, 540f);

        return ScreenToPanel(new Vector2(screenPos.x, screenPos.y));
    }

    private Vector2 ScreenToPanel(Vector2 screenPos)
    {
        float pw = _root.resolvedStyle.width;
        float ph = _root.resolvedStyle.height;
        if (float.IsNaN(pw) || pw <= 0) pw = 1920f;
        if (float.IsNaN(ph) || ph <= 0) ph = 1080f;

        return new Vector2(
            screenPos.x / Screen.width * pw,
            (1f - screenPos.y / Screen.height) * ph
        );
    }

    // ════════════════════════════════════════════════
    // カメラ差分追跡（視差・パースペクティブ計算）
    // ════════════════════════════════════════════════

    private void UpdateCameraTracking()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 curEuler = cam.transform.eulerAngles;
        Vector3 curPos   = cam.transform.position;

        // オイラー角の差分（-180〜+180 に正規化）
        float dYaw   = Mathf.DeltaAngle(_prevCamEuler.y, curEuler.y);
        float dPitch = Mathf.DeltaAngle(_prevCamEuler.x, curEuler.x);

        // 移動差分
        Vector3 dPos = curPos - _prevCamPos;

        // ── 視差効果: カメラの水平移動/回転で背景をずらす ──
        // Yaw 変化（度）をpx換算して背景に適用
        float parallaxX = -dYaw * 3f;      // yaw 1度 → 3px
        float parallaxY = dPitch * 1.5f;    // pitch 変化も微小に反映

        // ワールド移動の水平成分も加える（カメラローカルright方向）
        float localRightMove = Vector3.Dot(dPos, cam.transform.right);
        parallaxX += -localRightMove * 15f;

        // 背景レイヤーの視差（大きく動く）
        _parallaxBgOffset.x += parallaxX * PARALLAX_BG_FACTOR;
        _parallaxBgOffset.y += parallaxY * PARALLAX_BG_FACTOR;

        // 減衰（フレーム間でゆっくり戻る）
        _parallaxBgOffset *= 0.96f;

        // ── パースペクティブ偽装: Yaw差分でRotateとScaleXを微調整 ──
        float perspTarget = -dYaw * PERSPECTIVE_ROTATE_FACTOR;
        float scaleTarget = 1f + dYaw * PERSPECTIVE_SCALE_FACTOR;
        scaleTarget = Mathf.Clamp(scaleTarget, 0.92f, 1.08f);

        // 滑らかに追従
        _perspectiveRotate = Mathf.Lerp(_perspectiveRotate, perspTarget, Time.unscaledDeltaTime * 8f);
        _perspectiveScaleX = Mathf.Lerp(_perspectiveScaleX, scaleTarget, Time.unscaledDeltaTime * 8f);

        // 減衰（安定時はベースに戻る）
        _perspectiveRotate *= 0.92f;
        _perspectiveScaleX = Mathf.Lerp(_perspectiveScaleX, 1f, Time.unscaledDeltaTime * 3f);

        _prevCamEuler = curEuler;
        _prevCamPos = curPos;
    }

    // ════════════════════════════════════════════════
    // 毎フレーム位置更新（SmoothDamp + 視差 + パースペクティブ）
    // ════════════════════════════════════════════════

    private void UpdateDynamicPositions()
    {
        if (_mode == UIMode.Hidden) return;

        float dt = Time.unscaledDeltaTime;

        // ── SmoothDamp でプレイヤー追従 ──
        Vector2 target = GetTargetPanelPos();
        _currentPanelPos.x = Mathf.SmoothDamp(
            _currentPanelPos.x, target.x, ref _panelVelocity.x, SMOOTH_TIME, MAX_SPEED, dt);
        _currentPanelPos.y = Mathf.SmoothDamp(
            _currentPanelPos.y, target.y, ref _panelVelocity.y, SMOOTH_TIME, MAX_SPEED, dt);

        // ── 距離ベースのスケール計算 ──
        float distanceScale = 1f;
        if (_battleManager != null && _battleManager.ActiveCharacter != null && Camera.main != null)
        {
            float dist = Vector3.Distance(Camera.main.transform.position, _battleManager.ActiveCharacter.transform.position);
            
            // 距離比率（カメラに近いほど大きくなる）
            float rawScale = REFERENCE_DISTANCE / Mathf.Clamp(dist, 1f, 50f);
            
            // あまりにも巨大になりすぎるのを防ぐため、1.0倍～1.5倍の間に緩やかに収める
            distanceScale = Mathf.Clamp(Mathf.Lerp(1.0f, rawScale, 0.35f), 0.7f, 1.4f);
        }

        // ── Layer 1: RadialPivot ──
        // translate = SmoothDamp済みプレイヤー位置 + コマンドレイヤー視差
        if (_cmdRadialPivot != null)
        {
            float cmdParallaxX = _parallaxBgOffset.x * (PARALLAX_CMD_FACTOR / PARALLAX_BG_FACTOR);
            float cmdParallaxY = _parallaxBgOffset.y * (PARALLAX_CMD_FACTOR / PARALLAX_BG_FACTOR);

            _cmdRadialPivot.style.translate = new Translate(
                _currentPanelPos.x + cmdParallaxX,
                _currentPanelPos.y + cmdParallaxY
            );

            // パースペクティブ偽装: rotate と scaleX を微調整 + 距離スケール適用
            float finalRotate = PERSPECTIVE_BASE_ROTATE + _perspectiveRotate;
            _cmdRadialPivot.style.rotate = new Rotate(Angle.Degrees(finalRotate));
            _cmdRadialPivot.style.scale = new Scale(new Vector3(_perspectiveScaleX * distanceScale, distanceScale, 1f));
        }

        // ── 背景視差レイヤー ──
        if (_parallaxBg != null)
        {
            _parallaxBg.style.translate = new Translate(_parallaxBgOffset.x, _parallaxBgOffset.y);
        }

        // ── キャラ名 + SP グループ ──
        if (_cmdInfoGroup != null)
        {
            _cmdInfoGroup.style.translate = new Translate(
                _currentPanelPos.x + INFO_OFFSET_X * distanceScale,
                _currentPanelPos.y + INFO_OFFSET_Y * distanceScale
            );
            _cmdInfoGroup.style.scale = new Scale(new Vector3(distanceScale, distanceScale, 1f));
        }

        // ── ヒント ──
        if (_cmdHintWrap != null)
        {
            _cmdHintWrap.style.translate = new Translate(
                _currentPanelPos.x + HINT_OFFSET_X * distanceScale,
                _currentPanelPos.y + HINT_OFFSET_Y * distanceScale
            );
            _cmdHintWrap.style.scale = new Scale(new Vector3(distanceScale, distanceScale, 1f));
        }

        // ── ターゲットパネル追従 ──
        if (_mode == UIMode.TargetSelect && _targetPanel != null)
        {
            for (int i = 0; i < _targetPanel.childCount; i++)
            {
                _targetPanel[i].style.translate = new Translate(
                    _currentPanelPos.x - 130f * distanceScale,
                    _currentPanelPos.y + (-60f + i * 62f) * distanceScale
                );
                _targetPanel[i].style.scale = new Scale(new Vector3(distanceScale, distanceScale, 1f));
            }
        }
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
    // コマンドメニュー表示/非表示
    // ════════════════════════════════════════════════

    private void ShowCommandMenu()
    {
        if (_commandMenu == null) return;

        _mode = UIMode.CommandSelect;
        _selectedCommandIndex = 0;

        // SmoothDampの初期位置を即座に設定（ジャンプ防止）
        _currentPanelPos = GetTargetPanelPos();
        _panelVelocity = Vector2.zero;

        var active = _battleManager.ActiveCharacter;
        if (_activeCharName != null && active != null)
            _activeCharName.text = active.DisplayName?.ToUpper() ?? "---";
        if (_cmdWatermark != null && active != null)
            _cmdWatermark.text = active.DisplayName?.ToUpper() ?? "COMMAND";

        UpdateCommandAvailability();
        UpdateCommandHighlight();

        _targetPanel?.AddToClassList("hidden");
        _commandButtons?.RemoveFromClassList("hidden");
        if (_cmdHint != null) _cmdHint.text = "[W]△ [D]○ [A]× [S]□  [X] Cancel";

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
            {
                _cmdButtonElements[i].AddToClassList("is-active");
                // is-active 時のスライド: USStranslateが上書きされるのでinlineで合成
                _cmdButtonElements[i].style.translate = new Translate(
                    SLOT_BASE_TRANSLATE[i].x + ACTIVE_SHIFT_X,
                    SLOT_BASE_TRANSLATE[i].y
                );
            }
            else
            {
                _cmdButtonElements[i].RemoveFromClassList("is-active");
                // 非active: USS値に戻す
                _cmdButtonElements[i].style.translate = new Translate(
                    SLOT_BASE_TRANSLATE[i].x,
                    SLOT_BASE_TRANSLATE[i].y
                );
            }
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
            Debug.LogWarning("[DynamicBattleUI] 生存中のターゲットがいません。");
            return;
        }

        _mode = UIMode.TargetSelect;
        _commandButtons?.AddToClassList("hidden");
        _targetPanel?.RemoveFromClassList("hidden");

        RebuildTargetEntries();
        UpdateTargetHighlight();

        if (_cmdHint != null) _cmdHint.text = "[Z] Confirm  [X] Back  [W/S] Select";
    }

    private void RebuildTargetEntries()
    {
        if (_targetPanel == null) return;
        _targetPanel.Clear();

        for (int i = 0; i < _targetList.Count; i++)
        {
            var target = _targetList[i];

            // target-slot: cmd-slotと同構造（透明コンテナ）
            var slot = new VisualElement();
            slot.AddToClassList("target-slot");

            // target-bg: 黒半透明の土台（回転なし）
            var bg = new VisualElement();
            bg.AddToClassList("target-bg");

            // target-text: 敵名（歪みなし）
            var nameLabel = new Label(target.DisplayName?.ToUpper() ?? "???");
            nameLabel.AddToClassList("target-text");

            // target-hp: HP表示
            var hpLabel = new Label($"HP {target.CurrentHP}/{target.MaxHP}");
            hpLabel.AddToClassList("target-hp");

            slot.Add(bg);
            slot.Add(nameLabel);
            slot.Add(hpLabel);

            int idx = i;
            slot.RegisterCallback<ClickEvent>(_ =>
            {
                _selectedTargetIndex = idx;
                UpdateTargetHighlight();
                ConfirmTarget();
            });

            _targetPanel.Add(slot);
        }
    }

    private void UpdateTargetHighlight()
    {
        if (_targetPanel == null) return;

        for (int i = 0; i < _targetPanel.childCount; i++)
        {
            if (i == _selectedTargetIndex)
                _targetPanel[i].AddToClassList("is-active");
            else
                _targetPanel[i].RemoveFromClassList("is-active");
        }
    }

    private void ReturnToCommandMode()
    {
        _mode = UIMode.CommandSelect;
        _targetPanel?.AddToClassList("hidden");
        _commandButtons?.RemoveFromClassList("hidden");
        UpdateCommandHighlight();
        if (_cmdHint != null) _cmdHint.text = "[W]△ [D]○ [A]× [S]□  [X] Cancel";
    }

    // ════════════════════════════════════════════════
    // 入力処理
    // ════════════════════════════════════════════════

    private void HandleInput()
    {
        if (_mode == UIMode.Hidden) return;

        var kb = Keyboard.current;
        var gp = Gamepad.current;

        if (_mode == UIMode.CommandSelect)
        {
            // △○×□ ダイレクト選択（キーボード + ゲームパッド）
            bool cmdTriangle = (kb != null && kb.wKey.wasPressedThisFrame)
                            || (gp != null && gp.buttonNorth.wasPressedThisFrame);
            bool cmdCircle   = (kb != null && kb.dKey.wasPressedThisFrame)
                            || (gp != null && gp.buttonEast.wasPressedThisFrame);
            bool cmdCross    = (kb != null && kb.aKey.wasPressedThisFrame)
                            || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            bool cmdSquare   = (kb != null && kb.sKey.wasPressedThisFrame)
                            || (gp != null && gp.buttonWest.wasPressedThisFrame);
            bool cmdGuard    = (kb != null && (kb.zKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                            || (gp != null && gp.rightShoulder.wasPressedThisFrame);
            bool cmdCancel   = (kb != null && (kb.xKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                            || (gp != null && gp.leftShoulder.wasPressedThisFrame);

            if (cmdTriangle)     SelectAndConfirmCommand(0);  // △ ARCHETYPE
            else if (cmdCircle)  SelectAndConfirmCommand(1);  // ○ WEAPON
            else if (cmdCross)   SelectAndConfirmCommand(2);  // × SYNTHESIS
            else if (cmdSquare)  SelectAndConfirmCommand(3);  // □ ITEM
            else if (cmdGuard)   SelectAndConfirmCommand(4);  // GUARD

            if (cmdCancel) HideCommandMenu();
        }
        else if (_mode == UIMode.TargetSelect)
        {
            bool navUp   = (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame))
                        || (gp != null && gp.dpad.up.wasPressedThisFrame);
            bool navDown = (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame))
                        || (gp != null && gp.dpad.down.wasPressedThisFrame);
            bool confirm = (kb != null && (kb.zKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                        || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            bool cancel  = (kb != null && (kb.xKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                        || (gp != null && gp.buttonEast.wasPressedThisFrame);

            if (navUp)   NavigateTarget(-1);
            else if (navDown) NavigateTarget(+1);

            if (confirm) ConfirmTarget();
            if (cancel)  ReturnToCommandMode();
        }
    }

    private void SelectAndConfirmCommand(int index)
    {
        _selectedCommandIndex = index;
        UpdateCommandHighlight();
        ConfirmCommand();
    }

    private void ConfirmCommand()
    {
        if (_selectedCommandIndex < 0 || _selectedCommandIndex >= COMMAND_COUNT) return;

        if (_cmdButtonElements[_selectedCommandIndex] != null &&
            _cmdButtonElements[_selectedCommandIndex].ClassListContains("is-disabled"))
        {
            Debug.Log("[DynamicBattleUI] このコマンドは使用不可です。");
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
        Debug.Log($"[DynamicBattleUI] アクション実行: {_pendingAction} → {target.DisplayName}");

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
            var entry = CreateAOGEntry();
            entry.Root.style.display = DisplayStyle.None;
            _aogTrack.Add(entry.Root);
            _aogPool.Add(entry);
        }
        _aogActiveCount = 0;
    }

    private AOGEntry CreateAOGEntry()
    {
        var entry = new AOGEntry
        {
            Root      = new VisualElement(),
            GlowOuter = new VisualElement(),
            Glow      = new VisualElement(),
            Frame     = new VisualElement(),
            Initial   = new Label("?")
        };

        entry.Root.AddToClassList("aog-entry");
        entry.GlowOuter.AddToClassList("aog-glow-outer");
        entry.GlowOuter.style.display = DisplayStyle.None;
        entry.Glow.AddToClassList("aog-glow");
        entry.Glow.style.display = DisplayStyle.None;
        entry.Frame.AddToClassList("aog-entry-frame");
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
            var pe = _aogPool[i];

            if (i < count)
            {
                var ch = order[i];
                bool isActive = (i == 0);
                bool isPlayer = ch.CharacterFaction == CharacterBattleController.Faction.Player;

                pe.Root.RemoveFromClassList("aog-entry-player");
                pe.Root.RemoveFromClassList("aog-entry-enemy");
                pe.Root.RemoveFromClassList("aog-active");

                pe.Root.AddToClassList(isPlayer ? "aog-entry-player" : "aog-entry-enemy");
                if (isActive) pe.Root.AddToClassList("aog-active");

                pe.Glow.style.display      = isActive ? DisplayStyle.Flex : DisplayStyle.None;
                pe.GlowOuter.style.display  = isActive ? DisplayStyle.Flex : DisplayStyle.None;

                string dn = ch.DisplayName ?? "?";
                pe.Initial.text = dn.Length > 0 ? dn.Substring(0, 1) : "?";
                pe.Root.style.display = DisplayStyle.Flex;
            }
            else
            {
                pe.Root.style.display = DisplayStyle.None;
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

        foreach (var ch in party)
        {
            if (ch == null) continue;
            var card = CreatePartyCard(ch);
            _partyStatus.Add(card.Root);
            _partyCards.Add(card);
        }
    }

    private PartyCard CreatePartyCard(CharacterBattleController character)
    {
        var card = new PartyCard
        {
            Character     = character,
            LagPercent    = 100f,
            TargetPercent = 100f
        };

        card.Root = new VisualElement();
        card.Root.AddToClassList("ps-card");
        if (_battleManager.ActiveCharacter == character)
            card.Root.AddToClassList("ps-active");

        var visual = new VisualElement();
        visual.AddToClassList("ps-card-visual");

        var inner = new VisualElement();
        inner.AddToClassList("ps-card-inner");

        var wm = new Label(character.DisplayName?.ToUpper() ?? "");
        wm.AddToClassList("ps-watermark");
        inner.Add(wm);

        var header = new VisualElement();
        header.AddToClassList("ps-card-header");

        card.NameLabel = new Label(character.DisplayName?.ToUpper() ?? "---");
        card.NameLabel.AddToClassList("ps-char-name");
        card.HPText = new Label($"{character.CurrentHP}/{character.MaxHP}");
        card.HPText.AddToClassList("ps-hp-text");
        header.Add(card.NameLabel);
        header.Add(card.HPText);
        inner.Add(header);

        // HP Bar
        var hpBg = new VisualElement();
        hpBg.AddToClassList("ps-hp-bar-bg");
        hpBg.style.position = Position.Relative;

        float hpPct = character.MaxHP > 0 ? (float)character.CurrentHP / character.MaxHP * 100f : 0f;
        card.HPLag = new VisualElement();
        card.HPLag.AddToClassList("ps-hp-bar-lag");
        card.HPLag.style.width = Length.Percent(hpPct);
        card.LagPercent = hpPct;
        card.TargetPercent = hpPct;

        card.HPFill = new VisualElement();
        card.HPFill.AddToClassList("ps-hp-bar-fill");
        card.HPFill.style.width = Length.Percent(hpPct);

        hpBg.Add(card.HPLag);
        hpBg.Add(card.HPFill);
        inner.Add(hpBg);

        // EP Bar
        var epBg = new VisualElement();
        epBg.AddToClassList("ps-ep-bar-bg");
        epBg.style.position = Position.Relative;

        card.EPFill = new VisualElement();
        card.EPFill.AddToClassList("ps-ep-bar-fill");
        float epPct = character.MaxEP > 0 ? (float)character.CurrentEP / character.MaxEP * 100f : 0f;
        card.EPFill.style.width = Length.Percent(epPct);
        epBg.Add(card.EPFill);
        inner.Add(epBg);

        visual.Add(inner);
        card.Root.Add(visual);

        card.HPHandler = (hp, max) => UpdateCardHP(card, hp, max);
        card.EPHandler = (ep, max) => UpdateCardEP(card, ep, max);
        character.OnHPChanged += card.HPHandler;
        character.OnEPChanged += card.EPHandler;

        UpdateCardState(card);
        return card;
    }

    private void UpdateCardHP(PartyCard card, int hp, int max)
    {
        float pct = max > 0 ? (float)hp / max * 100f : 0f;
        card.TargetPercent = pct;
        card.HPFill.style.width = Length.Percent(pct);
        card.HPText.text = $"{hp}/{max}";
        UpdateCardState(card);
    }

    private void UpdateCardEP(PartyCard card, int ep, int max)
    {
        float pct = max > 0 ? (float)ep / max * 100f : 0f;
        card.EPFill.style.width = Length.Percent(pct);
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
            ? (float)card.Character.CurrentHP / card.Character.MaxHP : 1f;

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
