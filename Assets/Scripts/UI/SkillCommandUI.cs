// ============================================================
// SkillCommandUI.cs
// Honkai: Star Rail inspired skill command panel (production quality).
// Bottom-center horizontal capsule buttons with SP pip indicators,
// frosted-glass backdrop, glow/scale hover states, and smooth
// target-selection transitions. uGUI Canvas based; all elements
// are procedurally generated in Initialize().
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Star Rail style bottom-center skill command panel.
/// Attach as a child of BattleCanvas, then call Initialize(BattleManager).
/// Two-stage flow: command select -> target select -> fires events.
/// </summary>
public sealed class SkillCommandUI : MonoBehaviour
{
    // =============================================
    // Color Palette - Star Rail dark-blue UI
    // =============================================

    // --- Backdrop ---
    private static readonly Color BACKDROP_TOP    = new Color(0.02f, 0.03f, 0.08f, 0.0f);
    private static readonly Color BACKDROP_BOTTOM = new Color(0.02f, 0.03f, 0.08f, 0.35f);

    // --- Basic Attack button (silver-blue, cool tone) ---
    private static readonly Color ATK_CENTER       = new Color(0.18f, 0.22f, 0.36f, 0.95f);
    private static readonly Color ATK_EDGE         = new Color(0.06f, 0.07f, 0.15f, 0.98f);
    private static readonly Color ATK_BORDER_IDLE  = new Color(0.40f, 0.44f, 0.62f, 0.30f);
    private static readonly Color ATK_BORDER_SEL   = new Color(0.70f, 0.78f, 1.00f, 0.95f);
    private static readonly Color ATK_GLOW         = new Color(0.55f, 0.65f, 1.00f, 0.85f);
    private static readonly Color ATK_TEXT          = new Color(0.82f, 0.85f, 0.95f, 1.0f);
    private static readonly Color ATK_TEXT_DIM      = new Color(0.55f, 0.58f, 0.72f, 0.65f);
    private static readonly Color ATK_ICON          = new Color(0.75f, 0.82f, 1.00f, 0.90f);
    private static readonly Color ATK_SUB           = new Color(0.45f, 0.65f, 0.90f, 0.70f);
    private static readonly Color ATK_INNER_GLOW    = new Color(0.50f, 0.60f, 1.00f, 0.12f);

    // --- Skill button (elemental gold/amber) ---
    private static readonly Color SKL_CENTER       = new Color(0.35f, 0.26f, 0.10f, 0.95f);
    private static readonly Color SKL_EDGE         = new Color(0.12f, 0.08f, 0.03f, 0.98f);
    private static readonly Color SKL_BORDER_IDLE  = new Color(0.72f, 0.55f, 0.20f, 0.35f);
    private static readonly Color SKL_BORDER_SEL   = new Color(1.00f, 0.85f, 0.38f, 1.00f);
    private static readonly Color SKL_GLOW         = new Color(1.00f, 0.82f, 0.30f, 0.90f);
    private static readonly Color SKL_TEXT          = new Color(1.00f, 0.90f, 0.50f, 1.0f);
    private static readonly Color SKL_TEXT_DIM      = new Color(0.65f, 0.55f, 0.30f, 0.60f);
    private static readonly Color SKL_ICON          = new Color(1.00f, 0.88f, 0.50f, 0.95f);
    private static readonly Color SKL_SUB           = new Color(0.85f, 0.70f, 0.35f, 0.65f);
    private static readonly Color SKL_INNER_GLOW    = new Color(1.00f, 0.80f, 0.30f, 0.10f);

    // --- Skill button disabled ---
    private static readonly Color SKL_DIS_CENTER   = new Color(0.12f, 0.12f, 0.16f, 0.70f);
    private static readonly Color SKL_DIS_EDGE     = new Color(0.06f, 0.06f, 0.08f, 0.75f);
    private static readonly Color SKL_DIS_BORDER   = new Color(0.22f, 0.22f, 0.28f, 0.20f);
    private static readonly Color SKL_DIS_TEXT      = new Color(0.32f, 0.32f, 0.38f, 0.45f);

    // --- SP pips ---
    private static readonly Color SP_ACTIVE_COLOR  = new Color(0.00f, 0.90f, 1.00f, 1.0f);
    private static readonly Color SP_EMPTY_COLOR   = new Color(0.18f, 0.22f, 0.28f, 0.50f);
    private static readonly Color SP_GLOW_COLOR    = new Color(0.00f, 0.90f, 1.00f, 0.35f);
    private static readonly Color SP_LABEL         = new Color(0.55f, 0.60f, 0.72f, 0.75f);
    private static readonly Color SP_VALUE         = new Color(0.00f, 0.90f, 1.00f, 0.90f);

    // --- Target selection ---
    private static readonly Color TGT_NAME_COLOR   = new Color(1.0f, 0.92f, 0.55f, 1.0f);
    private static readonly Color TGT_HP_COLOR     = new Color(0.70f, 0.70f, 0.78f, 0.75f);
    private static readonly Color TGT_COUNTER      = new Color(0.45f, 0.45f, 0.55f, 0.60f);
    private static readonly Color TGT_HINT         = new Color(0.38f, 0.38f, 0.48f, 0.50f);
    private static readonly Color TGT_ARROW        = new Color(1.0f, 0.88f, 0.40f, 0.75f);
    private static readonly Color TGT_BG           = new Color(0.03f, 0.04f, 0.09f, 0.88f);
    private static readonly Color TGT_ENTRY_IDLE   = new Color(0.10f, 0.12f, 0.20f, 0.50f);
    private static readonly Color TGT_ENTRY_SEL    = new Color(0.18f, 0.20f, 0.35f, 0.85f);
    private static readonly Color TGT_ENTRY_BORDER = new Color(1.0f, 0.88f, 0.40f, 0.60f);

    // =============================================
    // Layout Constants
    // =============================================

    // Panel (bottom-right, Star Rail style)
    private const float BACKDROP_WIDTH  = 350f;
    private const float BACKDROP_HEIGHT = 140f;

    // Circular buttons (Star Rail uses ~100px diameter circular buttons)
    private const float BTN_WIDTH       = 110f;
    private const float BTN_HEIGHT      = 110f;
    private const float BTN_SPACING     = 24f;
    private const float BTN_BOTTOM_PAD  = 14f;
    private const float BORDER_THICK    = 2f;

    // Icon
    private const float ICON_SIZE       = 32f;
    private const float ICON_LEFT_PAD   = 0f;

    // Text
    private const int FONT_LABEL        = 13;
    private const int FONT_SUB          = 10;
    private const int FONT_SP_LABEL     = 12;
    private const int FONT_SP_VALUE     = 14;
    private const int FONT_TARGET_NAME  = 18;
    private const int FONT_TARGET_HP    = 13;
    private const int FONT_COUNTER      = 12;
    private const int FONT_HINT         = 11;
    private const int FONT_ARROW        = 18;

    // SP pips
    private const float SP_DOT_SIZE     = 10f;
    private const float SP_DOT_SPACING  = 5f;
    private const float SP_ROW_Y        = 126f; // above buttons

    // Target panel
    private const float TGT_PANEL_HEIGHT = 160f;
    private const float TGT_ENTRY_HEIGHT = 38f;
    private const float TGT_ENTRY_WIDTH  = 300f;
    private const float TGT_ENTRY_GAP    = 4f;

    // =============================================
    // Animation Constants
    // =============================================

    private const float ANIM_SCALE_SPEED      = 14f;
    private const float ANIM_SEL_SCALE        = 1.08f;
    private const float ANIM_IDLE_SCALE       = 1.0f;
    private const float ANIM_PRESS_SCALE      = 0.93f;
    private const float ANIM_PRESS_DUR        = 0.12f;
    private const float ANIM_GLOW_PULSE_SPEED = 2.8f;
    private const float ANIM_GLOW_MIN         = 0.50f;
    private const float ANIM_GLOW_MAX         = 1.00f;
    private const float ANIM_BOB_SPEED        = 2.2f;
    private const float ANIM_BOB_AMP          = 2.0f;
    private const float ANIM_SLIDE_OFFSET     = 60f;
    private const float ANIM_SHOW_DUR         = 0.28f;
    private const float ANIM_HIDE_DUR         = 0.18f;
    private const float ANIM_SP_GAIN_DUR      = 0.30f;
    private const float ANIM_SP_CONSUME_DUR   = 0.40f;
    private const float ANIM_TGT_SLIDE_SPEED  = 14f;

    // Shimmer
    private const float SHIMMER_INTERVAL = 7.0f;
    private const float SHIMMER_DUR      = 1.4f;
    private const float SHIMMER_WIDTH_F  = 0.15f;
    private const float SHIMMER_ALPHA    = 0.06f;

    // =============================================
    // Internal types
    // =============================================

    private enum Mode { Command, TargetSelect }

    // =============================================
    // Sprite cache (static, shared across instances)
    // =============================================

    private static Sprite _sWhite;
    private static Sprite _sCircle;
    private static Sprite _sGradientCircle;
    private static Sprite _sCapsule;       // 9-sliced capsule (very rounded)
    private static Sprite _sRoundRect;     // 9-sliced moderate corner
    private static Sprite _sCrossIcon;
    private static Sprite _sStarIcon;
    private static readonly Dictionary<long, Sprite> _sGradientCapsules = new Dictionary<long, Sprite>();

    // =============================================
    // Runtime references
    // =============================================

    private BattleManager _battleManager;
    private CharacterBattleController _currentCharacter;
    private Font _font;

    // =============================================
    // UI element references
    // =============================================

    // Root
    private GameObject _panelRoot;
    private RectTransform _panelRootRect;
    private float _panelBaseY = 0f;

    // Backdrop gradient
    private Image _backdropImage;

    // Shimmer
    private RectTransform _shimmerRect;
    private Image _shimmerImage;

    // Buttons
    private RectTransform _atkBtnRect;
    private RectTransform _sklBtnRect;
    private Image _atkBg;
    private Image _sklBg;
    private Image _atkBorder;
    private Image _sklBorder;
    private Image _atkGlow;
    private Image _sklGlow;
    private Image _atkInnerGlow;
    private Image _sklInnerGlow;
    private Image _atkIcon;
    private Image _sklIcon;
    private Text _atkLabel;
    private Text _sklLabel;
    private Text _atkSub;
    private Text _sklSub;

    // SP row
    private RectTransform _spRowRect;
    private readonly List<Image> _spDots = new List<Image>();
    private readonly List<Image> _spDotGlows = new List<Image>();
    private readonly List<RectTransform> _spDotRects = new List<RectTransform>();
    private Text _spLabelText;
    private Text _spValueText;

    // Target panel
    private GameObject _tgtPanel;
    private RectTransform _tgtPanelRect;
    private readonly List<GameObject> _tgtEntries = new List<GameObject>();
    private readonly List<Image> _tgtEntryBgs = new List<Image>();
    private readonly List<Image> _tgtEntryBorders = new List<Image>();
    private readonly List<Text> _tgtEntryNames = new List<Text>();
    private readonly List<Text> _tgtEntryHPs = new List<Text>();
    private Text _tgtHintText;
    private Image _tgtPanelBg;

    // =============================================
    // Runtime state
    // =============================================

    private Mode _mode = Mode.Command;
    private int _cmdIdx;                // 0=Attack, 1=Skill
    private bool _canUseSkill;
    private bool _isVisible;

    // Target selection
    private CharacterBattleController.ActionType _pendingAction;
    private readonly List<CharacterBattleController> _targets = new List<CharacterBattleController>();
    private int _tgtIdx;

    // SP cache
    private int _cachedSP;
    private int _cachedMaxSP = 5;

    // Anim: button scale
    private float _atkScale = 1f;
    private float _sklScale = 1f;
    private float _atkScaleTarget = 1f;
    private float _sklScaleTarget = 1f;

    // Anim: glow
    private float _atkGlowAlpha;
    private float _sklGlowAlpha;
    private float _atkGlowTarget;
    private float _sklGlowTarget;

    // Anim: inner glow
    private float _atkInnerAlpha;
    private float _sklInnerAlpha;
    private float _atkInnerTarget;
    private float _sklInnerTarget;

    // Anim: press
    private float _atkPressTimer;
    private float _sklPressTimer;
    private bool _atkPressing;
    private bool _sklPressing;

    // Anim: bob
    private float _bobTimer;

    // Anim: slide show/hide
    private float _slideTimer;
    private float _slideDuration;
    private float _slideFrom;
    private float _slideTo;
    private bool _isSliding;
    private bool _hideAfterSlide;

    // Anim: shimmer
    private float _shimmerTimer;

    // Anim: SP pips
    private readonly List<float> _spAnimTimers = new List<float>();
    private readonly List<int> _spAnimTypes = new List<int>(); // 0=none, 1=gain, -1=consume

    // Anim: target panel fade
    private float _tgtPanelAlpha;
    private float _tgtPanelAlphaTarget;

    // =============================================
    // Events
    // =============================================

    /// <summary>Fired when a command button is confirmed (BasicAttack or Skill).</summary>
    public event Action<CharacterBattleController.ActionType> OnCommandSelected;

    /// <summary>Fired when a target is confirmed during target selection.</summary>
    public event Action<CharacterBattleController.ActionType, CharacterBattleController> OnTargetConfirmed;

    // =============================================
    // Public API
    // =============================================

    /// <summary>Inject BattleManager, build all UI elements.</summary>
    public void Initialize(BattleManager battleManager)
    {
        _battleManager = battleManager;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        EnsureSprites();
        BuildUI();

        OnTargetConfirmed += (actionType, target) =>
        {
            _battleManager.ExecutePlayerAction(actionType, target);
        };

        _cachedSP = _battleManager.CurrentSP;
        _cachedMaxSP = _battleManager.MaxSP;
        RefreshSPDots();

        Hide();
    }

    /// <summary>Show in command selection mode with slide-up.</summary>
    public void Show(CharacterBattleController character, bool canUseSkill)
    {
        _currentCharacter = character;
        _canUseSkill = canUseSkill;
        _mode = Mode.Command;
        _cmdIdx = 0;
        _isVisible = true;

        if (_tgtPanel != null)
        {
            _tgtPanel.SetActive(false);
            _tgtPanelAlpha = 0f;
            _tgtPanelAlphaTarget = 0f;
        }

        RefreshButtons();
        RefreshSPDots();

        if (_panelRoot != null)
        {
            _panelRoot.SetActive(true);
            StartSlide(_panelBaseY - ANIM_SLIDE_OFFSET, _panelBaseY, ANIM_SHOW_DUR, false);
        }
    }

    /// <summary>Hide with slide-down animation.</summary>
    public void Hide()
    {
        if (_panelRoot != null && _panelRoot.activeSelf && _isVisible)
        {
            _isVisible = false;
            StartSlide(_panelBaseY, _panelBaseY - ANIM_SLIDE_OFFSET, ANIM_HIDE_DUR, true);
        }
        else
        {
            _isVisible = false;
            _mode = Mode.Command;
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }
    }

    /// <summary>Update SP display with animation triggers.</summary>
    public void UpdateSP(int current, int max)
    {
        int prev = _cachedSP;
        _cachedSP = current;
        _cachedMaxSP = max;
        EnsureDotCount(max);
        RefreshSPDots();
        UpdateSPText();

        for (int i = 0; i < _spAnimTimers.Count; i++)
        {
            bool wasOn = i < prev;
            bool isOn = i < current;
            if (!wasOn && isOn)
            {
                _spAnimTimers[i] = ANIM_SP_GAIN_DUR;
                _spAnimTypes[i] = 1;
            }
            else if (wasOn && !isOn)
            {
                _spAnimTimers[i] = ANIM_SP_CONSUME_DUR;
                _spAnimTypes[i] = -1;
            }
        }
    }

    /// <summary>Enter target selection mode showing alive enemies.</summary>
    public void EnterTargetSelection(
        CharacterBattleController.ActionType action,
        IReadOnlyList<CharacterBattleController> enemies)
    {
        _pendingAction = action;
        _targets.Clear();
        _tgtIdx = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].IsAlive)
                _targets.Add(enemies[i]);
        }

        if (_targets.Count == 0)
        {
            Debug.LogWarning("[SkillCommandUI] No alive targets.");
            return;
        }

        _mode = Mode.TargetSelect;
        RefreshButtons();
        RebuildTargetEntries();

        if (_tgtPanel != null)
        {
            _tgtPanel.SetActive(true);
            _tgtPanelAlphaTarget = 1f;
        }
    }

    // =============================================
    // MonoBehaviour
    // =============================================

    private void Update()
    {
        if (_isSliding)
            TickSlide();

        if (_panelRoot != null && _panelRoot.activeSelf)
            TickShimmer();

        if (!_isVisible) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (_mode == Mode.Command)
            HandleCommandInput(kb);
        else
            HandleTargetInput(kb);

        TickAnimations();
    }

    // =============================================
    // Input - Command
    // =============================================

    private void HandleCommandInput(Keyboard kb)
    {
        bool changed = false;

        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
        {
            if (_cmdIdx != 0) { _cmdIdx = 0; changed = true; }
        }
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
        {
            if (_cmdIdx != 1) { _cmdIdx = 1; changed = true; }
        }

        if (changed) RefreshButtons();

        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            if (_cmdIdx == 0)
            {
                _atkPressTimer = ANIM_PRESS_DUR;
                _atkPressing = true;
            }
            else
            {
                _sklPressTimer = ANIM_PRESS_DUR;
                _sklPressing = true;
            }
            ConfirmCommand();
        }
    }

    // =============================================
    // Input - Target
    // =============================================

    private void HandleTargetInput(Keyboard kb)
    {
        if (_targets.Count == 0) return;

        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
        {
            _tgtIdx = (_tgtIdx - 1 + _targets.Count) % _targets.Count;
            RefreshTargetHighlight();
        }
        else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
        {
            _tgtIdx = (_tgtIdx + 1) % _targets.Count;
            RefreshTargetHighlight();
        }
        // Also support left/right for compatibility
        else if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
        {
            _tgtIdx = (_tgtIdx - 1 + _targets.Count) % _targets.Count;
            RefreshTargetHighlight();
        }
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
        {
            _tgtIdx = (_tgtIdx + 1) % _targets.Count;
            RefreshTargetHighlight();
        }

        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            ConfirmTarget();

        if (kb.escapeKey.wasPressedThisFrame)
            ReturnToCommand();
    }

    // =============================================
    // Command / Target confirmation
    // =============================================

    private void ConfirmCommand()
    {
        CharacterBattleController.ActionType actionType;
        if (_cmdIdx == 0)
        {
            actionType = CharacterBattleController.ActionType.BasicAttack;
        }
        else
        {
            if (!_canUseSkill)
            {
                Debug.Log("[SkillCommandUI] Cannot use skill - insufficient SP.");
                return;
            }
            actionType = CharacterBattleController.ActionType.Skill;
        }

        OnCommandSelected?.Invoke(actionType);
    }

    private void ConfirmTarget()
    {
        if (_targets.Count == 0) return;
        var target = _targets[_tgtIdx];
        Hide();
        OnTargetConfirmed?.Invoke(_pendingAction, target);
    }

    private void ReturnToCommand()
    {
        _mode = Mode.Command;
        _cmdIdx = 0;
        _tgtPanelAlphaTarget = 0f;

        if (_tgtPanel != null)
            _tgtPanel.SetActive(false);

        RefreshButtons();
    }

    // =============================================
    // Slide animation (show/hide)
    // =============================================

    private void StartSlide(float from, float to, float dur, bool hideWhenDone)
    {
        _slideFrom = from;
        _slideTo = to;
        _slideTimer = 0f;
        _slideDuration = dur;
        _isSliding = true;
        _hideAfterSlide = hideWhenDone;

        if (_panelRootRect != null)
        {
            var p = _panelRootRect.anchoredPosition;
            p.y = from;
            _panelRootRect.anchoredPosition = p;
        }
    }

    private void TickSlide()
    {
        _slideTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_slideTimer / _slideDuration);
        float eased = 1f - (1f - t) * (1f - t) * (1f - t); // EaseOutCubic

        float y = Mathf.Lerp(_slideFrom, _slideTo, eased);
        if (_panelRootRect != null)
        {
            var p = _panelRootRect.anchoredPosition;
            p.y = y;
            _panelRootRect.anchoredPosition = p;
        }

        // Also fade the backdrop during slide
        if (_backdropImage != null)
        {
            float fadeAlpha = _hideAfterSlide ? (1f - eased) : eased;
            _backdropImage.color = new Color(
                BACKDROP_BOTTOM.r, BACKDROP_BOTTOM.g, BACKDROP_BOTTOM.b,
                BACKDROP_BOTTOM.a * fadeAlpha);
        }

        if (t >= 1f)
        {
            _isSliding = false;
            if (_hideAfterSlide)
            {
                _mode = Mode.Command;
                if (_panelRoot != null) _panelRoot.SetActive(false);
            }
        }
    }

    // =============================================
    // Shimmer
    // =============================================

    private void TickShimmer()
    {
        _shimmerTimer += Time.unscaledDeltaTime;
        if (_shimmerTimer >= SHIMMER_INTERVAL + SHIMMER_DUR)
            _shimmerTimer = 0f;

        if (_shimmerRect == null || _shimmerImage == null) return;

        if (_shimmerTimer < SHIMMER_DUR)
        {
            float progress = _shimmerTimer / SHIMMER_DUR;
            float sweepPos = Mathf.Lerp(-SHIMMER_WIDTH_F, 1f + SHIMMER_WIDTH_F, progress);

            _shimmerRect.anchorMin = new Vector2(sweepPos - SHIMMER_WIDTH_F, 0f);
            _shimmerRect.anchorMax = new Vector2(sweepPos + SHIMMER_WIDTH_F, 1f);
            _shimmerRect.offsetMin = Vector2.zero;
            _shimmerRect.offsetMax = Vector2.zero;

            float edgeFade = 1f;
            if (progress < 0.2f) edgeFade = progress / 0.2f;
            else if (progress > 0.8f) edgeFade = (1f - progress) / 0.2f;

            _shimmerImage.color = new Color(1f, 1f, 1f, SHIMMER_ALPHA * edgeFade);
            _shimmerImage.enabled = true;
            _shimmerRect.localRotation = Quaternion.Euler(0f, 0f, -20f);
        }
        else
        {
            _shimmerImage.enabled = false;
        }
    }

    // =============================================
    // Animation tick
    // =============================================

    private void TickAnimations()
    {
        float dt = Time.unscaledDeltaTime;
        _bobTimer += dt;

        // --- Press feedback ---
        TickPress(ref _atkPressTimer, ref _atkPressing);
        TickPress(ref _sklPressTimer, ref _sklPressing);

        // --- Button scale ---
        _atkScale = Mathf.MoveTowards(_atkScale, _atkScaleTarget, ANIM_SCALE_SPEED * dt);
        _sklScale = Mathf.MoveTowards(_sklScale, _sklScaleTarget, ANIM_SCALE_SPEED * dt);

        float atkFinal = _atkScale;
        float sklFinal = _sklScale;

        if (_atkPressing)
        {
            float pt = 1f - (_atkPressTimer / ANIM_PRESS_DUR);
            float curve = pt < 0.5f
                ? Mathf.Lerp(1f, ANIM_PRESS_SCALE, pt * 2f)
                : Mathf.Lerp(ANIM_PRESS_SCALE, 1f, (pt - 0.5f) * 2f);
            atkFinal *= curve;
        }
        if (_sklPressing)
        {
            float pt = 1f - (_sklPressTimer / ANIM_PRESS_DUR);
            float curve = pt < 0.5f
                ? Mathf.Lerp(1f, ANIM_PRESS_SCALE, pt * 2f)
                : Mathf.Lerp(ANIM_PRESS_SCALE, 1f, (pt - 0.5f) * 2f);
            sklFinal *= curve;
        }

        // --- Selected bob ---
        float bob = Mathf.Sin(_bobTimer * ANIM_BOB_SPEED * Mathf.PI * 2f) * ANIM_BOB_AMP;

        if (_atkBtnRect != null)
        {
            _atkBtnRect.localScale = Vector3.one * atkFinal;
            bool sel = (_mode == Mode.Command && _cmdIdx == 0) ||
                       (_mode == Mode.TargetSelect && _pendingAction == CharacterBattleController.ActionType.BasicAttack);
            var p = _atkBtnRect.localPosition;
            _atkBtnRect.localPosition = new Vector3(p.x, sel ? bob : 0f, p.z);
        }
        if (_sklBtnRect != null)
        {
            _sklBtnRect.localScale = Vector3.one * sklFinal;
            bool sel = (_mode == Mode.Command && _cmdIdx == 1) ||
                       (_mode == Mode.TargetSelect && _pendingAction == CharacterBattleController.ActionType.Skill);
            var p = _sklBtnRect.localPosition;
            _sklBtnRect.localPosition = new Vector3(p.x, sel ? bob : 0f, p.z);
        }

        // --- Glow pulse ---
        float pulse = ANIM_GLOW_MIN + (ANIM_GLOW_MAX - ANIM_GLOW_MIN) *
            (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * ANIM_GLOW_PULSE_SPEED));

        _atkGlowAlpha = Mathf.MoveTowards(_atkGlowAlpha, _atkGlowTarget, ANIM_SCALE_SPEED * dt);
        _sklGlowAlpha = Mathf.MoveTowards(_sklGlowAlpha, _sklGlowTarget, ANIM_SCALE_SPEED * dt);

        if (_atkGlow != null)
        {
            Color c = ATK_GLOW;
            c.a = _atkGlowAlpha * pulse;
            _atkGlow.color = c;
        }
        if (_sklGlow != null)
        {
            Color c = SKL_GLOW;
            c.a = _sklGlowAlpha * pulse;
            _sklGlow.color = c;
        }

        // --- Inner glow ---
        _atkInnerAlpha = Mathf.MoveTowards(_atkInnerAlpha, _atkInnerTarget, ANIM_SCALE_SPEED * dt);
        _sklInnerAlpha = Mathf.MoveTowards(_sklInnerAlpha, _sklInnerTarget, ANIM_SCALE_SPEED * dt);

        if (_atkInnerGlow != null)
        {
            Color c = ATK_INNER_GLOW;
            c.a = ATK_INNER_GLOW.a * _atkInnerAlpha * pulse;
            _atkInnerGlow.color = c;
        }
        if (_sklInnerGlow != null)
        {
            Color c = SKL_INNER_GLOW;
            c.a = SKL_INNER_GLOW.a * _sklInnerAlpha * pulse;
            _sklInnerGlow.color = c;
        }

        // --- SP pip animations ---
        for (int i = 0; i < _spAnimTimers.Count; i++)
        {
            if (_spAnimTimers[i] <= 0f) continue;
            _spAnimTimers[i] -= dt;
            float rem = Mathf.Max(0f, _spAnimTimers[i]);

            if (_spAnimTypes[i] == 1)
            {
                float t = 1f - (rem / ANIM_SP_GAIN_DUR);
                float scale = 1f + 0.6f * (1f - t) * Mathf.Sin(t * Mathf.PI);
                if (i < _spDotRects.Count && _spDotRects[i] != null)
                    _spDotRects[i].localScale = Vector3.one * Mathf.Max(scale, 1f);
                if (i < _spDots.Count && _spDots[i] != null)
                    _spDots[i].color = Color.Lerp(SP_ACTIVE_COLOR, Color.white, 1f - t);
            }
            else if (_spAnimTypes[i] == -1)
            {
                float t = rem / ANIM_SP_CONSUME_DUR;
                float scale = t > 0.5f
                    ? Mathf.Lerp(0.3f, 1f, (t - 0.5f) * 2f)
                    : Mathf.Lerp(0f, 0.3f, t * 2f);
                if (i < _spDotRects.Count && _spDotRects[i] != null)
                    _spDotRects[i].localScale = Vector3.one * scale;
                if (i < _spDots.Count && _spDots[i] != null)
                {
                    Color dc = SP_ACTIVE_COLOR;
                    dc.a = t;
                    _spDots[i].color = dc;
                }
            }

            if (rem <= 0f)
            {
                _spAnimTypes[i] = 0;
                if (i < _spDotRects.Count && _spDotRects[i] != null)
                    _spDotRects[i].localScale = Vector3.one;
                bool on = i < _cachedSP;
                if (i < _spDots.Count && _spDots[i] != null)
                    _spDots[i].color = on ? SP_ACTIVE_COLOR : SP_EMPTY_COLOR;
            }
        }

        // --- SP glow ring pulse ---
        float spPulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 2.2f);
        for (int i = 0; i < _spDotGlows.Count; i++)
        {
            if (_spDotGlows[i] == null) continue;
            bool on = i < _cachedSP;
            Color gc = SP_GLOW_COLOR;
            gc.a = on ? SP_GLOW_COLOR.a * spPulse : 0f;
            _spDotGlows[i].color = gc;
        }

        // --- Target panel fade ---
        if (_tgtPanel != null && _tgtPanel.activeSelf)
        {
            _tgtPanelAlpha = Mathf.MoveTowards(_tgtPanelAlpha, _tgtPanelAlphaTarget, 6f * dt);
            var cg = _tgtPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = _tgtPanelAlpha;
        }

        // --- Target entry highlight animation ---
        if (_mode == Mode.TargetSelect)
        {
            for (int i = 0; i < _tgtEntryBgs.Count; i++)
            {
                bool isSel = i == _tgtIdx;
                Color targetBg = isSel ? TGT_ENTRY_SEL : TGT_ENTRY_IDLE;
                if (_tgtEntryBgs[i] != null)
                    _tgtEntryBgs[i].color = Color.Lerp(_tgtEntryBgs[i].color, targetBg, 12f * dt);
                if (i < _tgtEntryBorders.Count && _tgtEntryBorders[i] != null)
                {
                    Color bc = TGT_ENTRY_BORDER;
                    bc.a = isSel ? TGT_ENTRY_BORDER.a * pulse : 0f;
                    _tgtEntryBorders[i].color = Color.Lerp(_tgtEntryBorders[i].color, bc, 12f * dt);
                }
                // Scale selected entry slightly
                if (i < _tgtEntries.Count && _tgtEntries[i] != null)
                {
                    float tgtScale = isSel ? 1.04f : 1.0f;
                    var rt = _tgtEntries[i].GetComponent<RectTransform>();
                    if (rt != null)
                        rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * tgtScale, 10f * dt);
                }
            }
        }
    }

    private void TickPress(ref float timer, ref bool pressing)
    {
        if (!pressing) return;
        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f) { timer = 0f; pressing = false; }
    }

    // =============================================
    // UI Construction
    // =============================================

    private void BuildUI()
    {
        // --- Panel root (bottom-right anchor, Star Rail style) ---
        _panelRoot = new GameObject("SkillCommandPanel");
        _panelRoot.transform.SetParent(transform, false);

        _panelRootRect = _panelRoot.AddComponent<RectTransform>();
        _panelRootRect.anchorMin = new Vector2(1f, 0f);
        _panelRootRect.anchorMax = new Vector2(1f, 0f);
        _panelRootRect.pivot = new Vector2(1f, 0f);
        _panelRootRect.anchoredPosition = new Vector2(-80f, _panelBaseY);
        _panelRootRect.sizeDelta = new Vector2(BACKDROP_WIDTH, BACKDROP_HEIGHT);

        // --- Backdrop gradient (dark at bottom, transparent at top) ---
        BuildBackdrop();

        // --- Shimmer overlay ---
        BuildShimmer();

        // --- SP pip row (between buttons and top) ---
        BuildSPRow();

        // --- Button container ---
        var btnContainer = MakeChild(_panelRoot, "ButtonContainer");
        var bcr = btnContainer.GetComponent<RectTransform>();
        bcr.anchorMin = new Vector2(0.5f, 0f);
        bcr.anchorMax = new Vector2(0.5f, 0f);
        bcr.pivot = new Vector2(0.5f, 0f);
        bcr.anchoredPosition = new Vector2(0f, BTN_BOTTOM_PAD);
        bcr.sizeDelta = new Vector2(BTN_WIDTH * 2f + BTN_SPACING, BTN_HEIGHT);

        var layout = btnContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = BTN_SPACING;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Attack button (left)
        BuildCapsuleButton(
            btnContainer, "AtkButton", "\u901A\u5E38\u653B\u6483", "SP+1", true,
            out _atkBtnRect, out _atkBg, out _atkBorder, out _atkGlow,
            out _atkInnerGlow, out _atkIcon, out _atkLabel, out _atkSub);

        // Skill button (right)
        BuildCapsuleButton(
            btnContainer, "SklButton", "\u6226\u95D8\u30B9\u30AD\u30EB", "SP-1", false,
            out _sklBtnRect, out _sklBg, out _sklBorder, out _sklGlow,
            out _sklInnerGlow, out _sklIcon, out _sklLabel, out _sklSub);

        // --- Target selection panel ---
        BuildTargetPanel();
    }

    // ---- Backdrop ----

    private void BuildBackdrop()
    {
        var bdObj = MakeChild(_panelRoot, "Backdrop");
        var bdRect = bdObj.GetComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero;
        bdRect.anchorMax = Vector2.one;
        bdRect.offsetMin = new Vector2(-6f, 0f);
        bdRect.offsetMax = new Vector2(6f, 6f);

        _backdropImage = bdObj.AddComponent<Image>();
        _backdropImage.sprite = GetWhiteSprite();
        _backdropImage.type = Image.Type.Simple;
        _backdropImage.color = BACKDROP_BOTTOM;
        _backdropImage.raycastTarget = false;

        // Top gradient fade
        var fadeObj = MakeChild(bdObj, "TopFade");
        var fadeRect = fadeObj.GetComponent<RectTransform>();
        fadeRect.anchorMin = new Vector2(0f, 0.55f);
        fadeRect.anchorMax = new Vector2(1f, 1f);
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        var fadeImg = fadeObj.AddComponent<Image>();
        fadeImg.sprite = GetWhiteSprite();
        fadeImg.type = Image.Type.Simple;
        fadeImg.color = new Color(BACKDROP_TOP.r, BACKDROP_TOP.g, BACKDROP_TOP.b, 0.0f);
        fadeImg.raycastTarget = false;

        // Subtle horizontal line at top of panel for definition
        var lineObj = MakeChild(_panelRoot, "TopLine");
        var lineRect = lineObj.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.05f, 1f);
        lineRect.anchorMax = new Vector2(0.95f, 1f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = new Vector2(0f, 1f);

        var lineImg = lineObj.AddComponent<Image>();
        lineImg.sprite = GetWhiteSprite();
        lineImg.type = Image.Type.Simple;
        lineImg.color = new Color(0.35f, 0.40f, 0.60f, 0.15f);
        lineImg.raycastTarget = false;
    }

    // ---- Shimmer ----

    private void BuildShimmer()
    {
        var container = MakeChild(_panelRoot, "ShimmerClip");
        var cr = container.GetComponent<RectTransform>();
        cr.anchorMin = Vector2.zero;
        cr.anchorMax = Vector2.one;
        cr.offsetMin = Vector2.zero;
        cr.offsetMax = Vector2.zero;

        var maskImg = container.AddComponent<Image>();
        maskImg.sprite = GetWhiteSprite();
        maskImg.type = Image.Type.Simple;
        maskImg.color = new Color(1f, 1f, 1f, 0.002f);
        maskImg.raycastTarget = false;

        var mask = container.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var bar = MakeChild(container, "ShimmerBar");
        _shimmerRect = bar.GetComponent<RectTransform>();
        _shimmerRect.anchorMin = new Vector2(0f, 0f);
        _shimmerRect.anchorMax = new Vector2(0.1f, 1f);
        _shimmerRect.offsetMin = Vector2.zero;
        _shimmerRect.offsetMax = Vector2.zero;
        _shimmerRect.sizeDelta = new Vector2(0f, 60f);

        _shimmerImage = bar.AddComponent<Image>();
        _shimmerImage.sprite = GetWhiteSprite();
        _shimmerImage.type = Image.Type.Simple;
        _shimmerImage.color = Color.clear;
        _shimmerImage.raycastTarget = false;
        _shimmerImage.enabled = false;
    }

    // ---- Capsule button ----

    private void BuildCapsuleButton(
        GameObject parent, string name, string label, string subText, bool isAttack,
        out RectTransform btnRect, out Image bg, out Image border, out Image glow,
        out Image innerGlow, out Image icon, out Text labelTxt, out Text subTxt)
    {
        var btn = MakeChild(parent, name);
        btnRect = btn.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(BTN_WIDTH, BTN_HEIGHT);

        // --- Outer glow (behind everything, larger, pulses when selected) ---
        var glowObj = MakeChild(btn, "OuterGlow");
        var glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-8f, -8f);
        glowRect.offsetMax = new Vector2(8f, 8f);

        glow = glowObj.AddComponent<Image>();
        glow.sprite = GetCapsuleSprite();
        glow.type = Image.Type.Sliced;
        glow.color = Color.clear;
        glow.raycastTarget = false;

        // --- Border (slightly expanded) ---
        var borderObj = MakeChild(btn, "Border");
        var borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-BORDER_THICK, -BORDER_THICK);
        borderRect.offsetMax = new Vector2(BORDER_THICK, BORDER_THICK);

        border = borderObj.AddComponent<Image>();
        border.sprite = GetCapsuleSprite();
        border.type = Image.Type.Sliced;
        border.color = isAttack ? ATK_BORDER_IDLE : SKL_BORDER_IDLE;
        border.raycastTarget = false;

        // --- Background (gradient capsule) ---
        Color cCenter = isAttack ? ATK_CENTER : SKL_CENTER;
        Color cEdge = isAttack ? ATK_EDGE : SKL_EDGE;

        bg = btn.AddComponent<Image>();
        bg.sprite = GetGradientCapsuleSprite(cCenter, cEdge);
        bg.type = Image.Type.Simple;
        bg.color = Color.white;
        bg.raycastTarget = false;

        // --- Inner glow overlay (subtle light on top half, gives glass feel) ---
        var innerGlowObj = MakeChild(btn, "InnerGlow");
        var igRect = innerGlowObj.GetComponent<RectTransform>();
        igRect.anchorMin = new Vector2(0.05f, 0.35f);
        igRect.anchorMax = new Vector2(0.95f, 0.95f);
        igRect.offsetMin = Vector2.zero;
        igRect.offsetMax = Vector2.zero;

        innerGlow = innerGlowObj.AddComponent<Image>();
        innerGlow.sprite = GetGradientCircleSprite();
        innerGlow.color = isAttack ? ATK_INNER_GLOW : SKL_INNER_GLOW;
        innerGlow.raycastTarget = false;
        innerGlow.preserveAspect = false;

        // --- Icon (left side of capsule) ---
        var iconObj = MakeChild(btn, "Icon");
        var iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(ICON_LEFT_PAD, 0f);
        iconRect.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);

        icon = iconObj.AddComponent<Image>();
        icon.sprite = isAttack ? GetCrossIconSprite() : GetStarIconSprite();
        icon.color = isAttack ? ATK_ICON : SKL_ICON;
        icon.raycastTarget = false;
        icon.preserveAspect = true;

        // --- Main label (center-right area) ---
        var lblObj = MakeChild(btn, "Label");
        var lblRect = lblObj.GetComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0f, 0.28f);
        lblRect.anchorMax = new Vector2(1f, 0.82f);
        lblRect.offsetMin = new Vector2(ICON_LEFT_PAD + ICON_SIZE + 6f, 0f);
        lblRect.offsetMax = new Vector2(-12f, 0f);

        labelTxt = lblObj.AddComponent<Text>();
        labelTxt.text = label;
        labelTxt.font = _font;
        labelTxt.fontSize = FONT_LABEL;
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.alignment = TextAnchor.MiddleLeft;
        labelTxt.color = isAttack ? ATK_TEXT : SKL_TEXT;
        labelTxt.raycastTarget = false;

        var lblOutline = lblObj.AddComponent<Outline>();
        lblOutline.effectColor = new Color(0f, 0f, 0f, 0.65f);
        lblOutline.effectDistance = new Vector2(1f, -1f);

        // --- Sub text (below label, SP info) ---
        var subObj = MakeChild(btn, "SubText");
        var subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.30f);
        subRect.offsetMin = new Vector2(ICON_LEFT_PAD + ICON_SIZE + 6f, 4f);
        subRect.offsetMax = new Vector2(-12f, 0f);

        subTxt = subObj.AddComponent<Text>();
        subTxt.text = subText;
        subTxt.font = _font;
        subTxt.fontSize = FONT_SUB;
        subTxt.alignment = TextAnchor.MiddleLeft;
        subTxt.color = isAttack ? ATK_SUB : SKL_SUB;
        subTxt.raycastTarget = false;

        // --- Keyboard hint (small letter in bottom-right corner) ---
        var hintObj = MakeChild(btn, "KeyHint");
        var hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-10f, 6f);
        hintRect.sizeDelta = new Vector2(30f, 18f);

        var hintTxt = hintObj.AddComponent<Text>();
        hintTxt.text = isAttack ? "[A]" : "[D]";
        hintTxt.font = _font;
        hintTxt.fontSize = 12;
        hintTxt.alignment = TextAnchor.LowerRight;
        hintTxt.color = new Color(0.40f, 0.42f, 0.55f, 0.35f);
        hintTxt.raycastTarget = false;
    }

    // ---- SP Row ----

    private void BuildSPRow()
    {
        var row = MakeChild(_panelRoot, "SPRow");
        _spRowRect = row.GetComponent<RectTransform>();
        _spRowRect.anchorMin = new Vector2(0.5f, 0f);
        _spRowRect.anchorMax = new Vector2(0.5f, 0f);
        _spRowRect.pivot = new Vector2(0.5f, 0f);
        _spRowRect.anchoredPosition = new Vector2(0f, SP_ROW_Y);

        float totalW = CalcSPRowWidth(_cachedMaxSP);
        _spRowRect.sizeDelta = new Vector2(totalW, SP_DOT_SIZE + 20f);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = SP_DOT_SPACING;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // SP label "SP"
        var lblObj = MakeChild(row, "SPLabel");
        var lblRect = lblObj.GetComponent<RectTransform>();
        lblRect.sizeDelta = new Vector2(26f, SP_DOT_SIZE + 4f);

        _spLabelText = lblObj.AddComponent<Text>();
        _spLabelText.text = "SP";
        _spLabelText.font = _font;
        _spLabelText.fontSize = FONT_SP_LABEL;
        _spLabelText.fontStyle = FontStyle.Bold;
        _spLabelText.alignment = TextAnchor.MiddleRight;
        _spLabelText.color = SP_LABEL;
        _spLabelText.raycastTarget = false;

        // Dots
        EnsureDotCount(_cachedMaxSP);

        // SP value "3/5"
        var valObj = MakeChild(row, "SPValue");
        var valRect = valObj.GetComponent<RectTransform>();
        valRect.sizeDelta = new Vector2(48f, SP_DOT_SIZE + 4f);

        _spValueText = valObj.AddComponent<Text>();
        _spValueText.font = _font;
        _spValueText.fontSize = FONT_SP_VALUE;
        _spValueText.fontStyle = FontStyle.Bold;
        _spValueText.alignment = TextAnchor.MiddleLeft;
        _spValueText.color = SP_VALUE;
        _spValueText.raycastTarget = false;
        UpdateSPText();
    }

    private float CalcSPRowWidth(int max)
    {
        // "SP" label + dots + "n/m" value
        return 30f + max * (SP_DOT_SIZE + 4f + SP_DOT_SPACING) + 52f;
    }

    private void UpdateSPText()
    {
        if (_spValueText != null)
            _spValueText.text = $"{_cachedSP}/{_cachedMaxSP}";
    }

    // ---- Target Panel ----

    private void BuildTargetPanel()
    {
        _tgtPanel = MakeChild(_panelRoot, "TargetPanel");
        _tgtPanelRect = _tgtPanel.GetComponent<RectTransform>();
        _tgtPanelRect.anchorMin = new Vector2(0.5f, 1f);
        _tgtPanelRect.anchorMax = new Vector2(0.5f, 1f);
        _tgtPanelRect.pivot = new Vector2(0.5f, 0f);
        _tgtPanelRect.anchoredPosition = new Vector2(0f, 8f);
        _tgtPanelRect.sizeDelta = new Vector2(TGT_ENTRY_WIDTH + 40f, TGT_PANEL_HEIGHT);

        // CanvasGroup for fade
        var cg = _tgtPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Background
        var bgObj = MakeChild(_tgtPanel, "Bg");
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        _tgtPanelBg = bgObj.AddComponent<Image>();
        _tgtPanelBg.sprite = GetRoundRectSprite();
        _tgtPanelBg.type = Image.Type.Sliced;
        _tgtPanelBg.color = TGT_BG;
        _tgtPanelBg.raycastTarget = false;

        // Title
        var titleObj = MakeChild(_tgtPanel, "Title");
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -6f);
        titleRect.sizeDelta = new Vector2(0f, 26f);

        var titleTxt = titleObj.AddComponent<Text>();
        titleTxt.text = "\u30BF\u30FC\u30B2\u30C3\u30C8\u9078\u629E";
        titleTxt.font = _font;
        titleTxt.fontSize = 15;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.50f, 0.52f, 0.65f, 0.60f);
        titleTxt.raycastTarget = false;

        // Hint at bottom
        var hintObj = MakeChild(_tgtPanel, "Hint");
        var hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 6f);
        hintRect.sizeDelta = new Vector2(0f, 20f);

        _tgtHintText = hintObj.AddComponent<Text>();
        _tgtHintText.text = "\u2191\u2193:\u9078\u629E  Enter:\u6C7A\u5B9A  Esc:\u623B\u308B";
        _tgtHintText.font = _font;
        _tgtHintText.fontSize = FONT_HINT;
        _tgtHintText.alignment = TextAnchor.MiddleCenter;
        _tgtHintText.color = TGT_HINT;
        _tgtHintText.raycastTarget = false;

        _tgtPanel.SetActive(false);
    }

    /// <summary>Rebuild the list of target entries based on current target list.</summary>
    private void RebuildTargetEntries()
    {
        // Clear old entries
        foreach (var e in _tgtEntries)
        {
            if (e != null) Destroy(e);
        }
        _tgtEntries.Clear();
        _tgtEntryBgs.Clear();
        _tgtEntryBorders.Clear();
        _tgtEntryNames.Clear();
        _tgtEntryHPs.Clear();

        if (_tgtPanel == null) return;

        // Resize panel to fit entries
        float contentHeight = _targets.Count * (TGT_ENTRY_HEIGHT + TGT_ENTRY_GAP) + 54f;
        _tgtPanelRect.sizeDelta = new Vector2(TGT_ENTRY_WIDTH + 40f, contentHeight);

        // Entry container (vertical list, centered)
        var containerObj = MakeChild(_tgtPanel, "Entries");
        var containerRect = containerObj.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0f, -32f);
        containerRect.sizeDelta = new Vector2(TGT_ENTRY_WIDTH, 0f);

        var vlg = containerObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = TGT_ENTRY_GAP;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        _tgtEntries.Add(containerObj);

        for (int i = 0; i < _targets.Count; i++)
        {
            var target = _targets[i];
            bool isSel = i == _tgtIdx;

            var entry = MakeChild(containerObj, $"Entry_{i}");
            var entryRect = entry.GetComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(TGT_ENTRY_WIDTH, TGT_ENTRY_HEIGHT);

            var layoutElem = entry.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = TGT_ENTRY_HEIGHT;

            // Entry border (behind bg)
            var ebObj = MakeChild(entry, "Border");
            var ebRect = ebObj.GetComponent<RectTransform>();
            ebRect.anchorMin = Vector2.zero;
            ebRect.anchorMax = Vector2.one;
            ebRect.offsetMin = new Vector2(-2f, -2f);
            ebRect.offsetMax = new Vector2(2f, 2f);

            var eBorder = ebObj.AddComponent<Image>();
            eBorder.sprite = GetRoundRectSprite();
            eBorder.type = Image.Type.Sliced;
            eBorder.color = isSel ? TGT_ENTRY_BORDER : Color.clear;
            eBorder.raycastTarget = false;
            _tgtEntryBorders.Add(eBorder);

            // Entry background
            var eBgImg = entry.AddComponent<Image>();
            eBgImg.sprite = GetRoundRectSprite();
            eBgImg.type = Image.Type.Sliced;
            eBgImg.color = isSel ? TGT_ENTRY_SEL : TGT_ENTRY_IDLE;
            eBgImg.raycastTarget = false;
            _tgtEntryBgs.Add(eBgImg);

            // Selection indicator (bright left bar)
            var barObj = MakeChild(entry, "SelBar");
            var barRect = barObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0.15f);
            barRect.anchorMax = new Vector2(0f, 0.85f);
            barRect.pivot = new Vector2(0f, 0.5f);
            barRect.anchoredPosition = new Vector2(4f, 0f);
            barRect.sizeDelta = new Vector2(3f, 0f);

            var barImg = barObj.AddComponent<Image>();
            barImg.sprite = GetWhiteSprite();
            barImg.color = TGT_ARROW;
            barImg.raycastTarget = false;

            // Enemy name
            var nameObj = MakeChild(entry, "Name");
            var nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(0.65f, 1f);
            nameRect.offsetMin = new Vector2(16f, 0f);
            nameRect.offsetMax = new Vector2(0f, 0f);

            var nameText = nameObj.AddComponent<Text>();
            nameText.text = target.DisplayName;
            nameText.font = _font;
            nameText.fontSize = FONT_TARGET_NAME;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = isSel ? TGT_NAME_COLOR : new Color(TGT_NAME_COLOR.r, TGT_NAME_COLOR.g, TGT_NAME_COLOR.b, 0.55f);
            nameText.raycastTarget = false;
            _tgtEntryNames.Add(nameText);

            var nameOutline = nameObj.AddComponent<Outline>();
            nameOutline.effectColor = new Color(0f, 0f, 0f, 0.5f);
            nameOutline.effectDistance = new Vector2(1f, -1f);

            // HP text
            var hpObj = MakeChild(entry, "HP");
            var hpRect = hpObj.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0.65f, 0f);
            hpRect.anchorMax = new Vector2(1f, 1f);
            hpRect.offsetMin = new Vector2(0f, 0f);
            hpRect.offsetMax = new Vector2(-12f, 0f);

            var hpText = hpObj.AddComponent<Text>();
            hpText.text = $"HP {target.CurrentHP}/{target.MaxHP}";
            hpText.font = _font;
            hpText.fontSize = FONT_TARGET_HP;
            hpText.alignment = TextAnchor.MiddleRight;
            hpText.color = TGT_HP_COLOR;
            hpText.raycastTarget = false;
            _tgtEntryHPs.Add(hpText);

            _tgtEntries.Add(entry);
        }
    }

    /// <summary>Refresh visual highlight on target entries.</summary>
    private void RefreshTargetHighlight()
    {
        for (int i = 0; i < _tgtEntryNames.Count; i++)
        {
            bool isSel = i == _tgtIdx;
            if (_tgtEntryNames[i] != null)
            {
                _tgtEntryNames[i].color = isSel
                    ? TGT_NAME_COLOR
                    : new Color(TGT_NAME_COLOR.r, TGT_NAME_COLOR.g, TGT_NAME_COLOR.b, 0.55f);
            }
        }
    }

    // =============================================
    // SP Dot management
    // =============================================

    private void EnsureDotCount(int max)
    {
        if (_spRowRect == null) return;

        // Remove excess
        while (_spDots.Count > max)
        {
            int last = _spDots.Count - 1;
            var dot = _spDots[last];
            _spDots.RemoveAt(last);
            if (dot != null && dot.gameObject != null)
                Destroy(dot.transform.parent.gameObject);

            if (_spDotGlows.Count > last) _spDotGlows.RemoveAt(last);
            if (_spDotRects.Count > last) _spDotRects.RemoveAt(last);
            if (_spAnimTimers.Count > last) _spAnimTimers.RemoveAt(last);
            if (_spAnimTypes.Count > last) _spAnimTypes.RemoveAt(last);
        }

        // Add missing
        while (_spDots.Count < max)
        {
            int idx = _spDots.Count;

            var container = MakeChild(_spRowRect.gameObject, $"Dot_{idx}");
            var cRect = container.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(SP_DOT_SIZE + 6f, SP_DOT_SIZE + 6f);

            // Glow (behind)
            var glObj = MakeChild(container, "Glow");
            var glRect = glObj.GetComponent<RectTransform>();
            glRect.anchorMin = new Vector2(0.5f, 0.5f);
            glRect.anchorMax = new Vector2(0.5f, 0.5f);
            glRect.pivot = new Vector2(0.5f, 0.5f);
            glRect.anchoredPosition = Vector2.zero;
            glRect.sizeDelta = new Vector2(SP_DOT_SIZE + 10f, SP_DOT_SIZE + 10f);

            var glImg = glObj.AddComponent<Image>();
            glImg.sprite = GetGradientCircleSprite();
            glImg.color = new Color(SP_GLOW_COLOR.r, SP_GLOW_COLOR.g, SP_GLOW_COLOR.b, 0f);
            glImg.raycastTarget = false;
            _spDotGlows.Add(glImg);

            // Dot
            var dotObj = MakeChild(container, "Circle");
            var dotRect = dotObj.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;
            dotRect.sizeDelta = new Vector2(SP_DOT_SIZE, SP_DOT_SIZE);

            var dotImg = dotObj.AddComponent<Image>();
            dotImg.sprite = GetCircleSprite();
            dotImg.color = SP_EMPTY_COLOR;
            dotImg.raycastTarget = false;

            _spDots.Add(dotImg);
            _spDotRects.Add(cRect);
            _spAnimTimers.Add(0f);
            _spAnimTypes.Add(0);
        }

        // Ensure value text stays last
        if (_spValueText != null)
            _spValueText.transform.SetAsLastSibling();

        _spRowRect.sizeDelta = new Vector2(CalcSPRowWidth(max), SP_DOT_SIZE + 20f);
    }

    private void RefreshSPDots()
    {
        for (int i = 0; i < _spDots.Count; i++)
        {
            bool on = i < _cachedSP;
            if (_spDots[i] != null && (i >= _spAnimTimers.Count || _spAnimTimers[i] <= 0f))
                _spDots[i].color = on ? SP_ACTIVE_COLOR : SP_EMPTY_COLOR;

            if (i < _spDotGlows.Count && _spDotGlows[i] != null)
            {
                Color gc = SP_GLOW_COLOR;
                gc.a = on ? SP_GLOW_COLOR.a : 0f;
                _spDotGlows[i].color = gc;
            }
        }
        UpdateSPText();
    }

    // =============================================
    // Button display refresh
    // =============================================

    private void RefreshButtons()
    {
        if (_atkBg == null || _sklBg == null) return;

        bool inTarget = _mode == Mode.TargetSelect;

        // --- Attack button ---
        {
            bool sel = inTarget
                ? _pendingAction == CharacterBattleController.ActionType.BasicAttack
                : _cmdIdx == 0;

            _atkBg.sprite = GetGradientCapsuleSprite(ATK_CENTER, ATK_EDGE);
            _atkBg.color = Color.white;
            _atkBorder.color = sel ? ATK_BORDER_SEL : ATK_BORDER_IDLE;
            _atkLabel.color = sel ? ATK_TEXT : ATK_TEXT_DIM;
            _atkIcon.color = sel ? ATK_ICON : new Color(ATK_ICON.r, ATK_ICON.g, ATK_ICON.b, 0.40f);
            _atkSub.text = "SP+1";
            _atkSub.color = sel ? ATK_SUB : new Color(ATK_SUB.r, ATK_SUB.g, ATK_SUB.b, 0.35f);

            _atkScaleTarget = sel ? ANIM_SEL_SCALE : ANIM_IDLE_SCALE;
            _atkGlowTarget = sel ? ATK_GLOW.a : 0f;
            _atkInnerTarget = sel ? 1f : 0.3f;
        }

        // --- Skill button ---
        if (!_canUseSkill && !inTarget)
        {
            _sklBg.sprite = GetGradientCapsuleSprite(SKL_DIS_CENTER, SKL_DIS_EDGE);
            _sklBg.color = Color.white;
            _sklBorder.color = SKL_DIS_BORDER;
            _sklLabel.color = SKL_DIS_TEXT;
            _sklIcon.color = SKL_DIS_TEXT;
            _sklSub.text = "SP-1";
            _sklSub.color = SKL_DIS_TEXT;

            _sklScaleTarget = ANIM_IDLE_SCALE;
            _sklGlowTarget = 0f;
            _sklInnerTarget = 0f;
        }
        else
        {
            bool sel = inTarget
                ? _pendingAction == CharacterBattleController.ActionType.Skill
                : _cmdIdx == 1;

            _sklBg.sprite = GetGradientCapsuleSprite(SKL_CENTER, SKL_EDGE);
            _sklBg.color = Color.white;
            _sklBorder.color = sel ? SKL_BORDER_SEL : SKL_BORDER_IDLE;
            _sklLabel.color = sel ? SKL_TEXT : SKL_TEXT_DIM;
            _sklIcon.color = sel ? SKL_ICON : new Color(SKL_ICON.r, SKL_ICON.g, SKL_ICON.b, 0.40f);
            _sklSub.text = "SP-1";
            _sklSub.color = sel ? SKL_SUB : new Color(SKL_SUB.r, SKL_SUB.g, SKL_SUB.b, 0.35f);

            _sklScaleTarget = sel ? ANIM_SEL_SCALE : ANIM_IDLE_SCALE;
            _sklGlowTarget = sel ? SKL_GLOW.a : 0f;
            _sklInnerTarget = sel ? 1f : 0.3f;
        }
    }

    // =============================================
    // Procedural Sprite Generation
    // =============================================

    private static void EnsureSprites()
    {
        GetWhiteSprite();
        GetCircleSprite();
        GetGradientCircleSprite();
        GetCapsuleSprite();
        GetRoundRectSprite();
        GetCrossIconSprite();
        GetStarIconSprite();
    }

    /// <summary>4x4 solid white sprite.</summary>
    private static Sprite GetWhiteSprite()
    {
        if (_sWhite != null) return _sWhite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _sWhite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        return _sWhite;
    }

    /// <summary>Filled circle sprite for SP dots.</summary>
    private static Sprite GetCircleSprite()
    {
        if (_sCircle != null) return _sCircle;
        int res = 48;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float center = res * 0.5f;
        float radius = center - 2f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                    tex.SetPixel(x, y, Color.white);
                else if (dist <= radius + 1.5f)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (dist - radius) / 1.5f)));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        _sCircle = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
        return _sCircle;
    }

    /// <summary>Gradient circle (bright center, transparent edge) for glow halos.</summary>
    private static Sprite GetGradientCircleSprite()
    {
        if (_sGradientCircle != null) return _sGradientCircle;
        int res = 64;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float center = res * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float nd = dist / radius;
                if (nd <= 1f)
                {
                    float a = 1f - nd * nd;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        _sGradientCircle = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
        return _sGradientCircle;
    }

    /// <summary>
    /// Capsule sprite (very rounded ends, 9-slice). Wider aspect than tall.
    /// The corner radius is nearly half the height for true capsule shape.
    /// </summary>
    private static Sprite GetCapsuleSprite()
    {
        if (_sCapsule != null) return _sCapsule;

        int w = 96;
        int h = 48;
        int cornerR = 22; // nearly half height for capsule shape
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };

        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                float alpha = 1f;
                Vector2 cc = Vector2.zero;
                bool inCorner = false;

                if (px < cornerR && py < cornerR) { cc = new Vector2(cornerR, cornerR); inCorner = true; }
                else if (px >= w - cornerR && py < cornerR) { cc = new Vector2(w - cornerR - 1, cornerR); inCorner = true; }
                else if (px < cornerR && py >= h - cornerR) { cc = new Vector2(cornerR, h - cornerR - 1); inCorner = true; }
                else if (px >= w - cornerR && py >= h - cornerR) { cc = new Vector2(w - cornerR - 1, h - cornerR - 1); inCorner = true; }

                if (inCorner)
                {
                    float d = Vector2.Distance(new Vector2(px, py), cc);
                    if (d > cornerR + 0.5f) alpha = 0f;
                    else if (d > cornerR - 0.5f) alpha = 1f - (d - (cornerR - 0.5f));
                }

                tex.SetPixel(px, py, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            }
        }
        tex.Apply();

        int border = cornerR + 1;
        _sCapsule = Sprite.Create(
            tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        return _sCapsule;
    }

    /// <summary>Moderate rounded rectangle (9-slice) for panels and target entries.</summary>
    private static Sprite GetRoundRectSprite()
    {
        if (_sRoundRect != null) return _sRoundRect;
        int res = 48;
        int cornerR = 10;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };

        for (int py = 0; py < res; py++)
        {
            for (int px = 0; px < res; px++)
            {
                float alpha = 1f;
                Vector2 cc = Vector2.zero;
                bool inCorner = false;

                if (px < cornerR && py < cornerR) { cc = new Vector2(cornerR, cornerR); inCorner = true; }
                else if (px >= res - cornerR && py < cornerR) { cc = new Vector2(res - cornerR - 1, cornerR); inCorner = true; }
                else if (px < cornerR && py >= res - cornerR) { cc = new Vector2(cornerR, res - cornerR - 1); inCorner = true; }
                else if (px >= res - cornerR && py >= res - cornerR) { cc = new Vector2(res - cornerR - 1, res - cornerR - 1); inCorner = true; }

                if (inCorner)
                {
                    float d = Vector2.Distance(new Vector2(px, py), cc);
                    if (d > cornerR + 0.5f) alpha = 0f;
                    else if (d > cornerR - 0.5f) alpha = 1f - (d - (cornerR - 0.5f));
                }

                tex.SetPixel(px, py, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            }
        }
        tex.Apply();

        int b = cornerR + 1;
        _sRoundRect = Sprite.Create(
            tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        return _sRoundRect;
    }

    /// <summary>
    /// Capsule sprite with baked radial gradient (center color -> edge color).
    /// NOT 9-sliced (use as Simple). Image.color = white.
    /// </summary>
    private static Sprite GetGradientCapsuleSprite(Color cCenter, Color cEdge)
    {
        long key = ColorKey(cCenter) * 31L + ColorKey(cEdge);
        if (_sGradientCapsules.TryGetValue(key, out var cached) && cached != null)
            return cached;

        int w = 128;
        int h = 48;
        int cornerR = 22;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };

        float cx = w * 0.5f;
        float cy = h * 0.5f;
        // Use the semi-diagonal for gradient normalization
        float maxDist = Mathf.Sqrt(cx * cx + cy * cy);

        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                // Capsule alpha
                float alpha = 1f;
                Vector2 cc = Vector2.zero;
                bool inCorner = false;

                if (px < cornerR && py < cornerR) { cc = new Vector2(cornerR, cornerR); inCorner = true; }
                else if (px >= w - cornerR && py < cornerR) { cc = new Vector2(w - cornerR - 1, cornerR); inCorner = true; }
                else if (px < cornerR && py >= h - cornerR) { cc = new Vector2(cornerR, h - cornerR - 1); inCorner = true; }
                else if (px >= w - cornerR && py >= h - cornerR) { cc = new Vector2(w - cornerR - 1, h - cornerR - 1); inCorner = true; }

                if (inCorner)
                {
                    float d = Vector2.Distance(new Vector2(px, py), cc);
                    if (d > cornerR + 0.5f) alpha = 0f;
                    else if (d > cornerR - 0.5f) alpha = 1f - (d - (cornerR - 0.5f));
                }

                // Radial gradient
                float dx = px - cx + 0.5f;
                float dy = py - cy + 0.5f;
                float nd = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / maxDist);
                float gt = nd * nd; // quadratic for softer center

                Color c = Color.Lerp(cCenter, cEdge, gt);
                c.a = Mathf.Min(c.a, Mathf.Clamp01(alpha));
                tex.SetPixel(px, py, c);
            }
        }
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        _sGradientCapsules[key] = spr;
        return spr;
    }

    /// <summary>X-cross icon (sword slash style, thicker lines with glow).</summary>
    private static Sprite GetCrossIconSprite()
    {
        if (_sCrossIcon != null) return _sCrossIcon;
        int res = 80;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };

        var clear = new Color[res * res];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        float margin = 16f;
        float thick = 3.5f;

        // Two diagonal lines forming an X
        DrawLine(tex, res, margin, margin, res - margin, res - margin, thick, Color.white);
        DrawLine(tex, res, margin, res - margin, res - margin, margin, thick, Color.white);

        // Glow halo around the lines (thicker, transparent)
        DrawLine(tex, res, margin, margin, res - margin, res - margin, thick + 4f, new Color(1f, 1f, 1f, 0.15f));
        DrawLine(tex, res, margin, res - margin, res - margin, margin, thick + 4f, new Color(1f, 1f, 1f, 0.15f));

        // Center diamond accent
        float c = res * 0.5f;
        float accentR = 6f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = Mathf.Abs(x - c + 0.5f);
                float dy = Mathf.Abs(y - c + 0.5f);
                if (dx + dy <= accentR)
                {
                    float edge = accentR - (dx + dy);
                    float a = Mathf.Clamp01(edge);
                    Color ex = tex.GetPixel(x, y);
                    if (a > ex.a) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
        }

        tex.Apply();
        _sCrossIcon = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
        return _sCrossIcon;
    }

    /// <summary>4-pointed star / cross icon (skill ability style).</summary>
    private static Sprite GetStarIconSprite()
    {
        if (_sStarIcon != null) return _sStarIcon;
        int res = 80;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };

        var clear = new Color[res * res];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        float center = res * 0.5f;
        float outerR = 34f;
        float innerR = 9f;
        int points = 4;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float px = x - center + 0.5f;
                float py = y - center + 0.5f;
                float angle = Mathf.Atan2(py, px);
                float dist = Mathf.Sqrt(px * px + py * py);

                float cosVal = Mathf.Cos(angle * points);
                float starR = innerR + (outerR - innerR) * Mathf.Pow(Mathf.Max(0f, cosVal), 0.55f);

                if (dist <= starR)
                {
                    float edge = starR - dist;
                    float alpha = Mathf.Clamp01(edge * 1.8f);
                    float brightness = 0.75f + 0.25f * (1f - dist / outerR);
                    tex.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
                }
                // Subtle outer glow
                else if (dist <= starR + 3f)
                {
                    float fade = 1f - (dist - starR) / 3f;
                    Color ex = tex.GetPixel(x, y);
                    float a = fade * 0.12f;
                    if (a > ex.a)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
        }

        // Bright center dot
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float px = x - center + 0.5f;
                float py = y - center + 0.5f;
                float dist = Mathf.Sqrt(px * px + py * py);
                if (dist < 4.5f)
                    tex.SetPixel(x, y, Color.white);
            }
        }

        tex.Apply();
        _sStarIcon = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
        return _sStarIcon;
    }

    /// <summary>Draw anti-aliased line on texture.</summary>
    private static void DrawLine(
        Texture2D tex, int res, float x0, float y0, float x1, float y1,
        float thickness, Color color)
    {
        float dx = x1 - x0;
        float dy = y1 - y0;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.001f) return;

        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - thickness - 1));
        int maxX = Mathf.Min(res - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + thickness + 1));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - thickness - 1));
        int maxY = Mathf.Min(res - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + thickness + 1));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float px = x - x0;
                float py = y - y0;
                float t = Mathf.Clamp01((px * dx + py * dy) / (len * len));
                float cx = x0 + t * dx;
                float cy = y0 + t * dy;
                float distX = x - cx;
                float distY = y - cy;
                float dist = Mathf.Sqrt(distX * distX + distY * distY);

                if (dist <= thickness)
                {
                    float alpha = Mathf.Clamp01(1f - (dist - thickness + 1f)) * color.a;
                    Color existing = tex.GetPixel(x, y);
                    Color blended = new Color(
                        Mathf.Max(existing.r, color.r * alpha),
                        Mathf.Max(existing.g, color.g * alpha),
                        Mathf.Max(existing.b, color.b * alpha),
                        Mathf.Max(existing.a, alpha));
                    tex.SetPixel(x, y, blended);
                }
            }
        }
    }

    private static long ColorKey(Color c)
    {
        int r = Mathf.RoundToInt(c.r * 255f);
        int g = Mathf.RoundToInt(c.g * 255f);
        int b = Mathf.RoundToInt(c.b * 255f);
        int a = Mathf.RoundToInt(c.a * 255f);
        return ((long)r << 24) | ((long)g << 16) | ((long)b << 8) | (long)a;
    }

    // =============================================
    // Helpers
    // =============================================

    private static GameObject MakeChild(GameObject parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent.transform, false);
        return obj;
    }
}
