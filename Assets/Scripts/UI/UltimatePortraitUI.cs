// ============================================================
// UltimatePortraitUI.cs
// Honkai Star Rail inspired ultimate portrait panel.
// Bottom-left circular portrait frames with smooth EP radial ring,
// golden pulsing corona aura, floating diamond sparkles,
// rising luminous particles, leading-edge glow, activation flash
// with shockwave, and target selection overlay.
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Honkai Star Rail style ultimate portrait panel.
/// Displays party member portraits with EP progress rings at the bottom-left.
/// Number keys 1-4 trigger ultimate ability with target selection.
/// </summary>
public sealed class UltimatePortraitUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Layout Constants
    // ──────────────────────────────────────────────

    private const int MAX_PORTRAITS = 4;
    private const float PORTRAIT_CELL_SIZE = 72f;
    private const float EP_RING_OUTER = 66f;
    private const float EP_RING_INNER = 56f;
    private const float INNER_PORTRAIT_SIZE = 50f;
    private const float PORTRAIT_BORDER_SIZE = 56f;
    private const float CORONA_SIZE = 82f;
    private const float OUTER_AURA_SIZE = 96f;
    private const float LAYOUT_SPACING = 10f;
    private const float CONTAINER_X = 36f;
    private const float CONTAINER_Y = 14f;
    private const float KEY_HINT_SIZE = 22f;
    private const float KEY_HINT_FONT_SIZE = 11;
    private const float KEY_HINT_CORNER_RADIUS = 3f;

    // ──────────────────────────────────────────────
    // Animation Constants
    // ──────────────────────────────────────────────

    private const float GLOW_PULSE_SPEED = 2.5f;
    private const float GLOW_PULSE_MIN = 0.15f;
    private const float GLOW_PULSE_MAX = 0.65f;
    private const float CORONA_BREATHE_SPEED = 1.8f;
    private const float CORONA_SCALE_MIN = 0.92f;
    private const float CORONA_SCALE_MAX = 1.08f;
    private const float EP_FILL_LERP_SPEED = 0.4f;
    private const float ACTIVATION_SCALE_PEAK = 1.18f;
    private const float ACTIVATION_DURATION = 0.35f;
    private const float READY_FLASH_DURATION = 0.4f;
    private const float READY_BOUNCE_DURATION = 0.4f;
    private const float READY_BOUNCE_PEAK = 1.15f;
    private const float ACTIVATION_FLASH_DURATION = 0.45f;
    private const float SHOCKWAVE_RING_DURATION = 0.5f;

    // ──────────────────────────────────────────────
    // Sparkle / Particle Constants
    // ──────────────────────────────────────────────

    private const int DIAMOND_SPARKLE_COUNT = 3;
    private const float DIAMOND_FLOAT_SPEED = 1.4f;
    private const float DIAMOND_SIZE = 7f;
    private const float DIAMOND_MAX_ALPHA = 0.85f;
    private const float LEADING_EDGE_SIZE = 10f;

    private const int RISING_PARTICLE_COUNT = 3;
    private const float RISING_PARTICLE_SPAWN_INTERVAL = 0.35f;
    private const float RISING_PARTICLE_LIFETIME = 1.6f;
    private const float RISING_PARTICLE_SIZE = 7f;
    private const float RISING_PARTICLE_RISE_DISTANCE = 70f;

    // ──────────────────────────────────────────────
    // Texture Resolution Constants
    // ──────────────────────────────────────────────

    private const int CIRCLE_RESOLUTION = 128;
    private const int RING_RESOLUTION = 128;
    private const int SOFT_CIRCLE_RESOLUTION = 96;
    private const int DIAMOND_RESOLUTION = 24;
    private const int ROUNDED_RECT_RESOLUTION = 32;

    // ──────────────────────────────────────────────
    // Color Palette (refined Star Rail aesthetic)
    // ──────────────────────────────────────────────

    // EP Ring
    private static readonly Color EP_RING_BG = new Color(0.10f, 0.10f, 0.18f, 0.55f);
    private static readonly Color EP_RING_BORDER = new Color(0.25f, 0.25f, 0.40f, 0.3f);

    // EP Fill gradients (element-based when charging, gold when ready)
    private static readonly Color EP_FILL_DEFAULT = new Color(0.35f, 0.65f, 0.95f, 0.92f);
    private static readonly Color EP_FILL_READY = new Color(1.00f, 0.87f, 0.20f, 1.00f);
    private static readonly Color EP_FILL_READY_BRIGHT = new Color(1.00f, 0.95f, 0.55f, 1.00f);

    // Portrait
    private static readonly Color PORTRAIT_BG = new Color(0.06f, 0.04f, 0.12f, 0.95f);
    private static readonly Color PORTRAIT_BORDER_IDLE = new Color(0.25f, 0.28f, 0.45f, 0.5f);
    private static readonly Color PORTRAIT_BORDER_READY = new Color(0.95f, 0.82f, 0.15f, 0.9f);
    private static readonly Color PORTRAIT_DIM = new Color(0.55f, 0.55f, 0.65f, 1.0f);

    // Corona / Aura (ready state)
    private static readonly Color READY_CORONA_INNER = new Color(1.00f, 0.85f, 0.10f, 0.50f);
    private static readonly Color READY_CORONA_OUTER = new Color(1.00f, 0.70f, 0.05f, 0.20f);
    private static readonly Color READY_GLOW_RING = new Color(1.00f, 0.85f, 0.15f, 0.65f);

    // Key Hint Badge
    private static readonly Color HINT_BG = new Color(0.08f, 0.08f, 0.16f, 0.88f);
    private static readonly Color HINT_BG_READY = new Color(0.70f, 0.58f, 0.08f, 0.92f);
    private static readonly Color HINT_BORDER = new Color(0.30f, 0.35f, 0.55f, 0.45f);
    private static readonly Color HINT_BORDER_READY = new Color(1.00f, 0.87f, 0.20f, 0.90f);
    private static readonly Color HINT_TEXT_IDLE = new Color(0.70f, 0.72f, 0.80f, 0.85f);
    private static readonly Color HINT_TEXT_READY = new Color(1.00f, 1.00f, 1.00f, 1.00f);

    // Target Selection Panel
    private static readonly Color TARGET_PANEL_BG = new Color(0.04f, 0.03f, 0.08f, 0.93f);
    private static readonly Color TARGET_GOLD_ACCENT = new Color(1.00f, 0.85f, 0.15f, 0.85f);
    private static readonly Color TARGET_TEXT_COLOR = new Color(1.00f, 0.88f, 0.30f, 1.00f);
    private static readonly Color TEXT_DIM = new Color(0.50f, 0.50f, 0.58f, 0.70f);

    // Leading Edge
    private static readonly Color LEADING_EDGE_COLOR = new Color(0.95f, 0.97f, 1.00f, 0.95f);

    // Background disc
    private static readonly Color BG_DISC_COLOR = new Color(0.03f, 0.02f, 0.08f, 0.65f);

    // Element colors (for EP ring fill based on character element)
    private static readonly Color ELEMENT_PHYSICAL = new Color(0.75f, 0.75f, 0.80f, 0.92f);
    private static readonly Color ELEMENT_FIRE = new Color(0.95f, 0.40f, 0.20f, 0.92f);
    private static readonly Color ELEMENT_ICE = new Color(0.40f, 0.80f, 0.95f, 0.92f);
    private static readonly Color ELEMENT_LIGHTNING = new Color(0.75f, 0.45f, 0.95f, 0.92f);
    private static readonly Color ELEMENT_WIND = new Color(0.40f, 0.90f, 0.55f, 0.92f);
    private static readonly Color ELEMENT_DARK = new Color(0.60f, 0.35f, 0.75f, 0.92f);

    // ──────────────────────────────────────────────
    // Inner Classes
    // ──────────────────────────────────────────────

    /// <summary>Per-portrait entry holding all references for one character slot.</summary>
    private class PortraitEntry
    {
        public CharacterBattleController Character;
        public GameObject Root;

        // Layered visuals (back to front)
        public Image OuterAuraImage;
        public Image CoronaImage;
        public Image EPRingBgImage;
        public Image EPRingBorderImage;
        public Image EPFillRingImage;
        public Image LeadingEdgeImage;
        public Image GlowRingImage;
        public Image PortraitBorderImage;
        public Image PortraitBgImage;
        public Image PortraitCharImage;
        public Image ReadyFlashImage;
        public Image ActivationFlashImage;
        public Image ShockwaveRingImage;
        public Image[] DiamondSparkles;
        public RisingParticle[] RisingParticles;
        public float RisingParticleTimer;

        // Key hint
        public Image KeyHintBgImage;
        public Image KeyHintBorderImage;
        public Image KeyHintGlowImage;
        public Text KeyHintText;

        // EP state
        public bool IsReady;
        public bool WasReady;
        public float DisplayedFillAmount;
        public float TargetFillAmount;
        public Color ElementFillColor;

        // Animation timers
        public bool IsActivating;
        public float ActivationTimer;
        public bool IsFlashing;
        public float ReadyFlashTimer;
        public bool IsReadyBouncing;
        public float ReadyBounceTimer;
        public bool IsActivationFlashing;
        public float ActivationFlashTimer;
        public bool IsShockwaving;
        public float ShockwaveTimer;

        // Per-sparkle random offsets for organic floating
        public float[] SparklePhaseOffsets;
        public float[] SparkleRadiusOffsets;
    }

    /// <summary>Data for a single rising luminous particle.</summary>
    private class RisingParticle
    {
        public Image Image;
        public float LifeTimer;
        public bool IsAlive;
        public float StartX;
        public float SizeScale;
        public float SwayPhase;
    }

    // ──────────────────────────────────────────────
    // Selection State
    // ──────────────────────────────────────────────

    private enum SelectionState
    {
        None,
        SelectingTarget
    }

    // ──────────────────────────────────────────────
    // Runtime State
    // ──────────────────────────────────────────────

    private BattleManager _battleManager;
    private readonly List<PortraitEntry> _entries = new List<PortraitEntry>();
    private GameObject _container;

    private SelectionState _selectionState = SelectionState.None;
    private PortraitEntry _pendingUltimateEntry;
    private readonly List<CharacterBattleController> _targetCandidates = new List<CharacterBattleController>();
    private int _targetIndex;

    // Target selection UI
    private GameObject _targetPanel;
    private Image _targetPanelBg;
    private Image _targetGoldBorder;
    private Text _targetNameText;
    private Text _targetHintText;

    // ──────────────────────────────────────────────
    // Cached Procedural Sprites
    // ──────────────────────────────────────────────

    private static Sprite _cachedCircleSprite;
    private static Sprite _cachedSoftCircleSprite;
    private static Sprite _cachedRingSprite;
    private static Sprite _cachedDiamondSprite;
    private static Sprite _cachedRoundedRectSprite;
    private static Sprite _cachedThinRingSprite;

    // ──────────────────────────────────────────────
    // Event
    // ──────────────────────────────────────────────

    /// <summary>Fires when an ultimate is requested. Passes the caster character.</summary>
    public event Action<CharacterBattleController> OnUltimateRequested;

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Initialize the portrait UI with the battle manager and player party.
    /// </summary>
    public void Initialize(BattleManager battleManager, IReadOnlyList<CharacterBattleController> playerParty)
    {
        _battleManager = battleManager;

        // Clean up any existing entries
        Cleanup();

        if (playerParty == null || playerParty.Count == 0)
        {
            Debug.LogWarning("[UltimatePortraitUI] playerParty is null or empty.");
            return;
        }

        // Create main container
        _container = CreateContainer();

        int count = Mathf.Min(playerParty.Count, MAX_PORTRAITS);
        for (int i = 0; i < count; i++)
        {
            var character = playerParty[i];
            if (character == null) continue;

            var entry = CreatePortraitEntry(character, i);
            _entries.Add(entry);

            // Subscribe to EP changes
            var localEntry = entry;
            character.OnEPChanged += (ep, max) => UpdateEP(localEntry, ep, max);

            // Apply initial EP value
            float initRatio = character.MaxEP > 0 ? (float)character.CurrentEP / character.MaxEP : 0f;
            entry.DisplayedFillAmount = initRatio;
            entry.TargetFillAmount = initRatio;
            entry.ElementFillColor = GetElementColor(character);
            UpdateEP(entry, character.CurrentEP, character.MaxEP);
        }

        // Create target selection panel (hidden)
        CreateTargetSelectionPanel();
    }

    /// <summary>Hide the entire UI.</summary>
    public void Hide()
    {
        CancelTargetSelection();
        if (_container != null) _container.SetActive(false);
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Update()
    {
        float dt = Time.deltaTime;
        float time = Time.time;

        UpdateEPFillLerp(dt);
        UpdateLeadingEdges(time);
        UpdateGlowEffects(time);
        UpdateCoronaBreathe(time);
        UpdateDiamondSparkles(time);
        UpdateRisingParticles(dt);
        UpdateKeyHintGlow(time);
        UpdateActivationAnimations(dt);
        UpdateReadyFlash(dt);
        UpdateReadyBounce(dt);
        UpdateActivationFlash(dt);
        UpdateShockwave(dt);
        HandleInput();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    // ──────────────────────────────────────────────
    // Input Handling
    // ──────────────────────────────────────────────

    private void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Target selection mode
        if (_selectionState == SelectionState.SelectingTarget)
        {
            HandleTargetSelectionInput(kb);
            return;
        }

        // Number keys 1-4 to activate ultimate
        if (kb.digit1Key.wasPressedThisFrame) TryActivateUltimate(0);
        else if (kb.digit2Key.wasPressedThisFrame) TryActivateUltimate(1);
        else if (kb.digit3Key.wasPressedThisFrame) TryActivateUltimate(2);
        else if (kb.digit4Key.wasPressedThisFrame) TryActivateUltimate(3);
    }

    private void TryActivateUltimate(int index)
    {
        if (index < 0 || index >= _entries.Count) return;

        var entry = _entries[index];
        if (entry.Character == null || !entry.Character.IsAlive) return;
        if (!entry.Character.IsUltimateReady) return;

        // Start activation animation + shockwave flash
        StartActivationAnimation(entry);
        StartActivationFlash(entry);
        StartShockwave(entry);

        // Gather target candidates
        _targetCandidates.Clear();
        if (_battleManager != null && _battleManager.EnemyParty != null)
        {
            foreach (var enemy in _battleManager.EnemyParty)
            {
                if (enemy != null && enemy.IsAlive)
                    _targetCandidates.Add(enemy);
            }
        }

        if (_targetCandidates.Count == 0)
        {
            Debug.LogWarning("[UltimatePortraitUI] No alive targets available.");
            return;
        }

        // Single target: confirm immediately
        if (_targetCandidates.Count == 1)
        {
            ConfirmUltimate(entry, _targetCandidates[0]);
            return;
        }

        // Enter target selection mode
        _pendingUltimateEntry = entry;
        _targetIndex = 0;
        _selectionState = SelectionState.SelectingTarget;
        ShowTargetSelectionPanel();
    }

    private void HandleTargetSelectionInput(Keyboard kb)
    {
        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
        {
            _targetIndex = (_targetIndex - 1 + _targetCandidates.Count) % _targetCandidates.Count;
            UpdateTargetSelectionPanel();
        }
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
        {
            _targetIndex = (_targetIndex + 1) % _targetCandidates.Count;
            UpdateTargetSelectionPanel();
        }

        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            var target = _targetCandidates[_targetIndex];
            var entry = _pendingUltimateEntry;
            CancelTargetSelection();
            ConfirmUltimate(entry, target);
        }

        if (kb.escapeKey.wasPressedThisFrame)
        {
            CancelTargetSelection();
        }
    }

    private void ConfirmUltimate(PortraitEntry entry, CharacterBattleController target)
    {
        Debug.Log($"[UltimatePortraitUI] {entry.Character.DisplayName} ultimate -> {target.DisplayName}");

        OnUltimateRequested?.Invoke(entry.Character);

        if (_battleManager != null)
        {
            _battleManager.ExecuteUltimate(entry.Character, target);
        }
    }

    private void CancelTargetSelection()
    {
        _selectionState = SelectionState.None;
        _pendingUltimateEntry = null;
        _targetCandidates.Clear();

        if (_targetPanel != null) _targetPanel.SetActive(false);
    }

    // ──────────────────────────────────────────────
    // EP Update
    // ──────────────────────────────────────────────

    private void UpdateEP(PortraitEntry entry, int currentEP, int maxEP)
    {
        float ratio = maxEP > 0 ? (float)currentEP / maxEP : 0f;
        bool isNowReady = currentEP >= maxEP && maxEP > 0;

        entry.TargetFillAmount = ratio;

        // Ring fill color: element color when charging, gold when full
        if (entry.EPFillRingImage != null)
        {
            entry.EPFillRingImage.color = isNowReady ? EP_FILL_READY : entry.ElementFillColor;
        }

        // Transition to ready: trigger flash + bounce
        if (isNowReady && !entry.WasReady)
        {
            TriggerReadyFlash(entry);
            StartReadyBounce(entry);
        }

        entry.WasReady = entry.IsReady;
        entry.IsReady = isNowReady;

        UpdatePortraitVisuals(entry);
        UpdateKeyHintBadge(entry);
        UpdatePortraitBorder(entry);
    }

    private void UpdatePortraitVisuals(PortraitEntry entry)
    {
        if (entry.PortraitCharImage == null) return;

        bool hasSprite = entry.Character != null && entry.Character.Stats != null
                         && entry.Character.Stats.Portrait != null;

        if (entry.IsReady)
        {
            entry.PortraitCharImage.color = hasSprite ? Color.white : entry.ElementFillColor;
        }
        else
        {
            if (hasSprite)
            {
                entry.PortraitCharImage.color = PORTRAIT_DIM;
            }
            else
            {
                Color dimmed = entry.ElementFillColor;
                dimmed.r *= 0.5f;
                dimmed.g *= 0.5f;
                dimmed.b *= 0.55f;
                dimmed.a = 0.85f;
                entry.PortraitCharImage.color = dimmed;
            }
        }
    }

    private void UpdatePortraitBorder(PortraitEntry entry)
    {
        if (entry.PortraitBorderImage != null)
        {
            entry.PortraitBorderImage.color = entry.IsReady ? PORTRAIT_BORDER_READY : PORTRAIT_BORDER_IDLE;
        }
    }

    private void UpdateKeyHintBadge(PortraitEntry entry)
    {
        if (entry.KeyHintBgImage != null)
        {
            entry.KeyHintBgImage.color = entry.IsReady ? HINT_BG_READY : HINT_BG;
        }

        if (entry.KeyHintBorderImage != null)
        {
            entry.KeyHintBorderImage.color = entry.IsReady ? HINT_BORDER_READY : HINT_BORDER;
        }

        if (entry.KeyHintText != null)
        {
            entry.KeyHintText.color = entry.IsReady ? HINT_TEXT_READY : HINT_TEXT_IDLE;
        }
    }

    // ──────────────────────────────────────────────
    // EP Fill Lerp (smooth arc fill animation)
    // ──────────────────────────────────────────────

    private void UpdateEPFillLerp(float dt)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.EPFillRingImage == null) continue;

            if (Mathf.Abs(entry.DisplayedFillAmount - entry.TargetFillAmount) > 0.001f)
            {
                // Smooth ease-out lerp for fluid fill
                entry.DisplayedFillAmount = Mathf.Lerp(
                    entry.DisplayedFillAmount,
                    entry.TargetFillAmount,
                    1f - Mathf.Pow(0.02f, dt / EP_FILL_LERP_SPEED));
                entry.EPFillRingImage.fillAmount = entry.DisplayedFillAmount;
            }
            else
            {
                entry.DisplayedFillAmount = entry.TargetFillAmount;
                entry.EPFillRingImage.fillAmount = entry.TargetFillAmount;
            }
        }
    }

    // ──────────────────────────────────────────────
    // Leading Edge (bright dot at fill frontier)
    // ──────────────────────────────────────────────

    private void UpdateLeadingEdges(float time)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.LeadingEdgeImage == null) continue;

            float fill = entry.DisplayedFillAmount;

            // Only show when filling (not empty, not full)
            if (fill <= 0.02f || fill >= 0.98f)
            {
                entry.LeadingEdgeImage.enabled = false;
                continue;
            }

            entry.LeadingEdgeImage.enabled = true;

            // Fill starts at bottom (270 degrees) and goes clockwise
            float angleDeg = 360f * fill;
            float radians = (90f - angleDeg) * Mathf.Deg2Rad;
            float ringRadius = (EP_RING_OUTER + EP_RING_INNER) * 0.25f;
            float px = Mathf.Cos(radians) * ringRadius;
            float py = Mathf.Sin(radians) * ringRadius;

            entry.LeadingEdgeImage.rectTransform.anchoredPosition = new Vector2(px, py);

            // Soft pulsing brightness
            float pulse = 0.80f + 0.20f * Mathf.Sin(time * 10f);
            var col = LEADING_EDGE_COLOR;
            col.a = pulse;
            entry.LeadingEdgeImage.color = col;

            // Scale pulse for liveliness
            float s = 0.9f + 0.2f * Mathf.Sin(time * 8f);
            entry.LeadingEdgeImage.rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }

    // ──────────────────────────────────────────────
    // Ready Bounce (elastic scale when EP fills)
    // ──────────────────────────────────────────────

    private void StartReadyBounce(PortraitEntry entry)
    {
        entry.IsReadyBouncing = true;
        entry.ReadyBounceTimer = 0f;
    }

    private void UpdateReadyBounce(float dt)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (!entry.IsReadyBouncing || entry.Root == null) continue;

            entry.ReadyBounceTimer += dt;
            float progress = entry.ReadyBounceTimer / READY_BOUNCE_DURATION;

            if (progress >= 1f)
            {
                entry.IsReadyBouncing = false;
                if (!entry.IsActivating)
                    entry.Root.transform.localScale = Vector3.one;
            }
            else
            {
                // Elastic overshoot: fast rise, gentle settle with spring
                float scale;
                if (progress < 0.25f)
                {
                    float t = progress / 0.25f;
                    scale = Mathf.Lerp(1.0f, READY_BOUNCE_PEAK, EaseOutQuad(t));
                }
                else
                {
                    float t = (progress - 0.25f) / 0.75f;
                    // Damped spring settle
                    float spring = Mathf.Sin(t * Mathf.PI * 2.5f) * (1f - t) * 0.08f;
                    scale = Mathf.Lerp(READY_BOUNCE_PEAK, 1.0f, EaseOutQuad(t)) + spring;
                }

                if (!entry.IsActivating)
                    entry.Root.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }

    // ──────────────────────────────────────────────
    // Activation Flash (white burst expanding outward)
    // ──────────────────────────────────────────────

    private void StartActivationFlash(PortraitEntry entry)
    {
        entry.IsActivationFlashing = true;
        entry.ActivationFlashTimer = 0f;

        if (entry.ActivationFlashImage != null)
        {
            entry.ActivationFlashImage.enabled = true;
            entry.ActivationFlashImage.color = new Color(1f, 0.95f, 0.8f, 0.9f);
            entry.ActivationFlashImage.rectTransform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }
    }

    private void UpdateActivationFlash(float dt)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (!entry.IsActivationFlashing || entry.ActivationFlashImage == null) continue;

            entry.ActivationFlashTimer += dt;
            float progress = entry.ActivationFlashTimer / ACTIVATION_FLASH_DURATION;

            if (progress >= 1f)
            {
                entry.IsActivationFlashing = false;
                entry.ActivationFlashImage.enabled = false;
                entry.ActivationFlashImage.rectTransform.localScale = Vector3.one;
            }
            else
            {
                // Fast scale up with smooth alpha fade
                float scaleCurve = EaseOutCubic(progress);
                float scale = Mathf.Lerp(0.5f, 3.5f, scaleCurve);
                float alpha = Mathf.Lerp(0.9f, 0f, EaseInQuad(progress));
                entry.ActivationFlashImage.color = new Color(1f, 0.95f, 0.8f, alpha);
                entry.ActivationFlashImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }

    // ──────────────────────────────────────────────
    // Shockwave Ring (expanding ring on activation)
    // ──────────────────────────────────────────────

    private void StartShockwave(PortraitEntry entry)
    {
        entry.IsShockwaving = true;
        entry.ShockwaveTimer = 0f;

        if (entry.ShockwaveRingImage != null)
        {
            entry.ShockwaveRingImage.enabled = true;
            entry.ShockwaveRingImage.color = new Color(1f, 0.87f, 0.2f, 0.7f);
            entry.ShockwaveRingImage.rectTransform.localScale = new Vector3(0.8f, 0.8f, 1f);
        }
    }

    private void UpdateShockwave(float dt)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (!entry.IsShockwaving || entry.ShockwaveRingImage == null) continue;

            entry.ShockwaveTimer += dt;
            float progress = entry.ShockwaveTimer / SHOCKWAVE_RING_DURATION;

            if (progress >= 1f)
            {
                entry.IsShockwaving = false;
                entry.ShockwaveRingImage.enabled = false;
            }
            else
            {
                float scale = Mathf.Lerp(0.8f, 4.0f, EaseOutCubic(progress));
                float alpha = Mathf.Lerp(0.7f, 0f, EaseInQuad(progress));
                entry.ShockwaveRingImage.color = new Color(1f, 0.87f, 0.2f, alpha);
                entry.ShockwaveRingImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }

    // ──────────────────────────────────────────────
    // Rising Particles (luminous sparkles floating up)
    // ──────────────────────────────────────────────

    private void UpdateRisingParticles(float dt)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.RisingParticles == null) continue;

            if (entry.IsReady)
            {
                entry.RisingParticleTimer += dt;
                if (entry.RisingParticleTimer >= RISING_PARTICLE_SPAWN_INTERVAL)
                {
                    entry.RisingParticleTimer -= RISING_PARTICLE_SPAWN_INTERVAL;
                    SpawnRisingParticle(entry);
                }
            }

            for (int p = 0; p < entry.RisingParticles.Length; p++)
            {
                var particle = entry.RisingParticles[p];
                if (particle == null || !particle.IsAlive) continue;

                particle.LifeTimer += dt;
                float life = particle.LifeTimer / RISING_PARTICLE_LIFETIME;

                if (life >= 1f)
                {
                    particle.IsAlive = false;
                    if (particle.Image != null)
                        particle.Image.enabled = false;
                    continue;
                }

                if (particle.Image == null) continue;

                // Smooth upward rise with ease-out deceleration
                float yEased = EaseOutQuad(life);
                float yOffset = yEased * RISING_PARTICLE_RISE_DISTANCE;

                // Gentle sinusoidal horizontal sway
                float xSway = Mathf.Sin(life * Mathf.PI * 1.5f + particle.SwayPhase) * 12f * (1f - life);
                particle.Image.rectTransform.anchoredPosition = new Vector2(
                    particle.StartX + xSway,
                    yOffset - INNER_PORTRAIT_SIZE * 0.25f);

                // Alpha: quick fade in, long gentle fade out
                float alpha;
                if (life < 0.15f)
                    alpha = life / 0.15f;
                else
                    alpha = 1f - Mathf.Pow((life - 0.15f) / 0.85f, 0.7f);
                alpha *= 0.75f;

                // Color: gold with warmth variation
                Color col = Color.Lerp(EP_FILL_READY, EP_FILL_READY_BRIGHT, Mathf.Sin(life * Mathf.PI));
                col.a = alpha;
                particle.Image.color = col;
                particle.Image.enabled = true;

                // Gentle rotation
                float rot = life * 180f + particle.SwayPhase * Mathf.Rad2Deg;
                particle.Image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);

                // Scale: grow then shrink with variation
                float scaleLife = Mathf.Sin(life * Mathf.PI);
                float pScale = (0.4f + scaleLife * 0.7f) * particle.SizeScale;
                particle.Image.rectTransform.localScale = new Vector3(pScale, pScale, 1f);
            }
        }
    }

    private void SpawnRisingParticle(PortraitEntry entry)
    {
        for (int p = 0; p < entry.RisingParticles.Length; p++)
        {
            var particle = entry.RisingParticles[p];
            if (particle == null || particle.IsAlive) continue;

            particle.IsAlive = true;
            particle.LifeTimer = 0f;
            particle.StartX = UnityEngine.Random.Range(-INNER_PORTRAIT_SIZE * 0.35f, INNER_PORTRAIT_SIZE * 0.35f);
            particle.SizeScale = UnityEngine.Random.Range(0.6f, 1.4f);
            particle.SwayPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            return;
        }
    }

    // ──────────────────────────────────────────────
    // Key Hint Glow (pulsing when ready)
    // ──────────────────────────────────────────────

    private void UpdateKeyHintGlow(float time)
    {
        float t = time * GLOW_PULSE_SPEED * 1.8f;
        float glowAlpha = Mathf.Lerp(0.0f, 0.45f, (Mathf.Sin(t) + 1f) * 0.5f);

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.KeyHintGlowImage == null) continue;

            if (entry.IsReady)
            {
                entry.KeyHintGlowImage.enabled = true;
                var col = EP_FILL_READY;
                col.a = glowAlpha;
                entry.KeyHintGlowImage.color = col;
            }
            else
            {
                entry.KeyHintGlowImage.enabled = false;
            }
        }
    }

    // ──────────────────────────────────────────────
    // Glow Pulse (Corona + Glow Ring)
    // ──────────────────────────────────────────────

    private void UpdateGlowEffects(float time)
    {
        float t = time * GLOW_PULSE_SPEED;
        float pulseAlpha = Mathf.Lerp(GLOW_PULSE_MIN, GLOW_PULSE_MAX, (Mathf.Sin(t) + 1f) * 0.5f);
        // Secondary offset pulse for layered glow
        float pulse2 = Mathf.Lerp(0.1f, 0.4f, (Mathf.Sin(t * 1.3f + 1.0f) + 1f) * 0.5f);

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];

            // Inner corona
            if (entry.CoronaImage != null)
            {
                if (entry.IsReady)
                {
                    var c = READY_CORONA_INNER;
                    c.a = pulseAlpha;
                    entry.CoronaImage.color = c;
                    entry.CoronaImage.enabled = true;
                }
                else
                {
                    entry.CoronaImage.enabled = false;
                }
            }

            // Outer aura (second layer, offset phase)
            if (entry.OuterAuraImage != null)
            {
                if (entry.IsReady)
                {
                    var c = READY_CORONA_OUTER;
                    c.a = pulse2;
                    entry.OuterAuraImage.color = c;
                    entry.OuterAuraImage.enabled = true;
                }
                else
                {
                    entry.OuterAuraImage.enabled = false;
                }
            }

            // Glow ring overlay
            if (entry.GlowRingImage != null)
            {
                if (entry.IsReady)
                {
                    var c = READY_GLOW_RING;
                    c.a = Mathf.Lerp(0.2f, 0.55f, (Mathf.Sin(t * 1.5f) + 1f) * 0.5f);
                    entry.GlowRingImage.color = c;
                    entry.GlowRingImage.enabled = true;
                }
                else
                {
                    entry.GlowRingImage.enabled = false;
                }
            }

            // Diamond sparkles visibility
            if (entry.DiamondSparkles != null)
            {
                for (int d = 0; d < entry.DiamondSparkles.Length; d++)
                {
                    if (entry.DiamondSparkles[d] != null)
                        entry.DiamondSparkles[d].enabled = entry.IsReady;
                }
            }
        }
    }

    // ──────────────────────────────────────────────
    // Corona Breathe (subtle scale oscillation)
    // ──────────────────────────────────────────────

    private void UpdateCoronaBreathe(float time)
    {
        float breathe = Mathf.Lerp(CORONA_SCALE_MIN, CORONA_SCALE_MAX,
            (Mathf.Sin(time * CORONA_BREATHE_SPEED) + 1f) * 0.5f);

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];

            if (entry.CoronaImage != null && entry.IsReady)
            {
                entry.CoronaImage.rectTransform.localScale = new Vector3(breathe, breathe, 1f);
            }

            if (entry.OuterAuraImage != null && entry.IsReady)
            {
                // Outer aura breathes inversely for pulsing depth
                float outerBreathe = Mathf.Lerp(CORONA_SCALE_MAX, CORONA_SCALE_MIN,
                    (Mathf.Sin(time * CORONA_BREATHE_SPEED) + 1f) * 0.5f);
                entry.OuterAuraImage.rectTransform.localScale = new Vector3(outerBreathe, outerBreathe, 1f);
            }
        }
    }

    // ──────────────────────────────────────────────
    // Diamond Sparkles (floating upward, organic motion)
    // ──────────────────────────────────────────────

    private void UpdateDiamondSparkles(float time)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.DiamondSparkles == null || !entry.IsReady) continue;

            for (int d = 0; d < entry.DiamondSparkles.Length; d++)
            {
                if (entry.DiamondSparkles[d] == null) continue;

                float phase = entry.SparklePhaseOffsets[d];
                float radiusOffset = entry.SparkleRadiusOffsets[d];

                // Organic floating upward motion with gentle swirl
                float floatT = time * DIAMOND_FLOAT_SPEED + phase;
                float verticalCycle = Mathf.Repeat(floatT, Mathf.PI * 2f) / (Mathf.PI * 2f);

                // Vertical: rise from bottom to top in a smooth loop
                float baseRadius = (EP_RING_OUTER * 0.5f + 8f) + radiusOffset;
                float angleOffset = (float)d / DIAMOND_SPARKLE_COUNT * Mathf.PI * 2f;
                float vertAngle = angleOffset + verticalCycle * Mathf.PI * 2f;

                // Gentle elliptical path slightly biased upward
                float x = Mathf.Cos(vertAngle) * baseRadius * 0.5f;
                float y = Mathf.Sin(vertAngle) * baseRadius * 0.65f + 10f;

                var rt = entry.DiamondSparkles[d].rectTransform;
                rt.anchoredPosition = new Vector2(x, y);

                // Twinkling: smooth fade based on position
                float twinkle = Mathf.Abs(Mathf.Sin(floatT * 2.2f + phase * 3f));
                float sparkleAlpha = twinkle * twinkle * DIAMOND_MAX_ALPHA;
                // Fade out near bottom, bright at top
                float heightFade = Mathf.Clamp01((y + baseRadius) / (baseRadius * 2f));
                sparkleAlpha *= (0.3f + heightFade * 0.7f);

                Color col = Color.Lerp(EP_FILL_READY, EP_FILL_READY_BRIGHT, twinkle);
                col.a = sparkleAlpha;
                entry.DiamondSparkles[d].color = col;

                // Gentle rotation
                rt.localRotation = Quaternion.Euler(0f, 0f, time * 45f + phase * Mathf.Rad2Deg);

                // Scale pulse for sparkle feeling
                float sparkleScale = 0.6f + twinkle * 0.5f;
                rt.localScale = new Vector3(sparkleScale, sparkleScale, 1f);
            }
        }
    }

    // ──────────────────────────────────────────────
    // Activation Animation (scale pulse)
    // ──────────────────────────────────────────────

    private void StartActivationAnimation(PortraitEntry entry)
    {
        entry.IsActivating = true;
        entry.ActivationTimer = 0f;
    }

    private void UpdateActivationAnimations(float dt)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (!entry.IsActivating || entry.Root == null) continue;

            entry.ActivationTimer += dt;
            float progress = entry.ActivationTimer / ACTIVATION_DURATION;

            if (progress >= 1f)
            {
                entry.IsActivating = false;
                entry.Root.transform.localScale = Vector3.one;
            }
            else
            {
                float scale;
                if (progress < 0.3f)
                {
                    float t2 = progress / 0.3f;
                    scale = Mathf.Lerp(1.0f, ACTIVATION_SCALE_PEAK, EaseOutCubic(t2));
                }
                else
                {
                    float t2 = (progress - 0.3f) / 0.7f;
                    // Damped spring return
                    float spring = Mathf.Sin(t2 * Mathf.PI * 2f) * (1f - t2) * 0.05f;
                    scale = Mathf.Lerp(ACTIVATION_SCALE_PEAK, 1.0f, EaseOutQuad(t2)) + spring;
                    scale = Mathf.Max(scale, 0.98f);
                }

                entry.Root.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }

    // ──────────────────────────────────────────────
    // Ready Flash (golden white flash when EP fills)
    // ──────────────────────────────────────────────

    private void TriggerReadyFlash(PortraitEntry entry)
    {
        entry.IsFlashing = true;
        entry.ReadyFlashTimer = 0f;

        if (entry.ReadyFlashImage != null)
        {
            entry.ReadyFlashImage.enabled = true;
            entry.ReadyFlashImage.color = new Color(1f, 0.95f, 0.7f, 0.85f);
        }
    }

    private void UpdateReadyFlash(float dt)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (!entry.IsFlashing || entry.ReadyFlashImage == null) continue;

            entry.ReadyFlashTimer += dt;
            float progress = entry.ReadyFlashTimer / READY_FLASH_DURATION;

            if (progress >= 1f)
            {
                entry.IsFlashing = false;
                entry.ReadyFlashImage.enabled = false;
            }
            else
            {
                // Warm golden flash that fades out smoothly
                float alpha = 0.85f * (1f - EaseInQuad(progress));
                float warmth = 1f - progress * 0.3f;
                entry.ReadyFlashImage.color = new Color(1f, 0.95f * warmth, 0.7f * warmth, alpha);
            }
        }
    }

    // ──────────────────────────────────────────────
    // Element Color Helper
    // ──────────────────────────────────────────────

    private static Color GetElementColor(CharacterBattleController character)
    {
        if (character == null || character.Stats == null)
            return EP_FILL_DEFAULT;

        switch (character.Stats.Element)
        {
            case CharacterStats.ElementType.Physical:  return ELEMENT_PHYSICAL;
            case CharacterStats.ElementType.Fire:      return ELEMENT_FIRE;
            case CharacterStats.ElementType.Ice:       return ELEMENT_ICE;
            case CharacterStats.ElementType.Lightning: return ELEMENT_LIGHTNING;
            case CharacterStats.ElementType.Wind:      return ELEMENT_WIND;
            case CharacterStats.ElementType.Dark:      return ELEMENT_DARK;
            default:                                   return EP_FILL_DEFAULT;
        }
    }

    // ──────────────────────────────────────────────
    // Easing Functions
    // ──────────────────────────────────────────────

    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInQuad(float t) => t * t;

    // ══════════════════════════════════════════════
    // UI CONSTRUCTION
    // ══════════════════════════════════════════════

    // ──────────────────────────────────────────────
    // Container
    // ──────────────────────────────────────────────

    private GameObject CreateContainer()
    {
        var containerObj = new GameObject("UltimatePortraitContainer", typeof(RectTransform));
        containerObj.transform.SetParent(transform, false);

        var rt = containerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, CONTAINER_Y);
        rt.sizeDelta = new Vector2(
            MAX_PORTRAITS * (PORTRAIT_CELL_SIZE + LAYOUT_SPACING),
            OUTER_AURA_SIZE + KEY_HINT_SIZE + 8f);

        var hlg = containerObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = LAYOUT_SPACING;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        int pad = (int)((OUTER_AURA_SIZE - PORTRAIT_CELL_SIZE) * 0.5f);
        hlg.padding = new RectOffset(pad, pad, pad, pad);

        return containerObj;
    }

    // ──────────────────────────────────────────────
    // Portrait Entry
    // ──────────────────────────────────────────────

    private PortraitEntry CreatePortraitEntry(CharacterBattleController character, int index)
    {
        var entry = new PortraitEntry { Character = character };

        // -- Root (layout element) --
        var root = new GameObject($"Portrait_{character.DisplayName}", typeof(RectTransform));
        root.transform.SetParent(_container.transform, false);

        var rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(PORTRAIT_CELL_SIZE, PORTRAIT_CELL_SIZE);

        var rootLE = root.AddComponent<LayoutElement>();
        rootLE.preferredWidth = PORTRAIT_CELL_SIZE;
        rootLE.preferredHeight = PORTRAIT_CELL_SIZE;

        entry.Root = root;

        // Layer order (back to front):
        // 1. Outer Aura (large soft glow, only when ready)
        // 2. Corona (inner glow, only when ready)
        // 3. EP Ring Background (dark ring)
        // 4. EP Ring Border (subtle thin border)
        // 5. EP Fill Ring (radial fill)
        // 6. Leading Edge (bright dot at fill frontier)
        // 7. Glow Ring (golden ring overlay when ready)
        // 8. Portrait Border (thin circle border)
        // 9. Portrait Background (dark inner circle)
        // 10. Portrait Character Image
        // 11. Ready Flash Overlay
        // 12. Activation Flash
        // 13. Shockwave Ring
        // 14. Diamond Sparkles
        // 15. Rising Particles
        // 16. Key Hint Badge

        // 1. Outer Aura
        entry.OuterAuraImage = CreateImageElement(root.transform, "OuterAura",
            CreateSoftCircleSprite(), Color.clear, OUTER_AURA_SIZE, OUTER_AURA_SIZE);
        CenterChild(entry.OuterAuraImage.rectTransform, OUTER_AURA_SIZE);
        entry.OuterAuraImage.enabled = false;

        // 2. Corona
        entry.CoronaImage = CreateImageElement(root.transform, "ReadyCorona",
            CreateSoftCircleSprite(), Color.clear, CORONA_SIZE, CORONA_SIZE);
        CenterChild(entry.CoronaImage.rectTransform, CORONA_SIZE);
        entry.CoronaImage.enabled = false;

        // 3. EP Ring Background
        entry.EPRingBgImage = CreateImageElement(root.transform, "EPRingBg",
            CreateRingSprite(), EP_RING_BG, EP_RING_OUTER, EP_RING_OUTER);
        CenterChild(entry.EPRingBgImage.rectTransform, EP_RING_OUTER);

        // 4. EP Ring Border (thin decorative ring)
        entry.EPRingBorderImage = CreateImageElement(root.transform, "EPRingBorder",
            CreateThinRingSprite(), EP_RING_BORDER, EP_RING_OUTER + 4f, EP_RING_OUTER + 4f);
        CenterChild(entry.EPRingBorderImage.rectTransform, EP_RING_OUTER + 4f);

        // 5. EP Fill Ring (radial fill)
        entry.EPFillRingImage = CreateImageElement(root.transform, "EPFillRing",
            CreateRingSprite(), EP_FILL_DEFAULT, EP_RING_OUTER, EP_RING_OUTER);
        CenterChild(entry.EPFillRingImage.rectTransform, EP_RING_OUTER);
        entry.EPFillRingImage.type = Image.Type.Filled;
        entry.EPFillRingImage.fillMethod = Image.FillMethod.Radial360;
        entry.EPFillRingImage.fillOrigin = (int)Image.Origin360.Bottom;
        entry.EPFillRingImage.fillClockwise = true;
        entry.EPFillRingImage.fillAmount = 0f;

        // 6. Leading Edge
        entry.LeadingEdgeImage = CreateImageElement(root.transform, "LeadingEdge",
            CreateSoftCircleSprite(), LEADING_EDGE_COLOR, LEADING_EDGE_SIZE, LEADING_EDGE_SIZE);
        CenterChild(entry.LeadingEdgeImage.rectTransform, LEADING_EDGE_SIZE);
        entry.LeadingEdgeImage.enabled = false;

        // 7. Glow Ring
        entry.GlowRingImage = CreateImageElement(root.transform, "GlowRing",
            CreateRingSprite(), Color.clear, EP_RING_OUTER, EP_RING_OUTER);
        CenterChild(entry.GlowRingImage.rectTransform, EP_RING_OUTER);
        entry.GlowRingImage.enabled = false;

        // 8. Portrait Border
        entry.PortraitBorderImage = CreateImageElement(root.transform, "PortraitBorder",
            CreateCircleSprite(), PORTRAIT_BORDER_IDLE, PORTRAIT_BORDER_SIZE, PORTRAIT_BORDER_SIZE);
        CenterChild(entry.PortraitBorderImage.rectTransform, PORTRAIT_BORDER_SIZE);

        // 9. Portrait Background
        entry.PortraitBgImage = CreateImageElement(root.transform, "PortraitBg",
            CreateCircleSprite(), PORTRAIT_BG, INNER_PORTRAIT_SIZE, INNER_PORTRAIT_SIZE);
        CenterChild(entry.PortraitBgImage.rectTransform, INNER_PORTRAIT_SIZE);

        // 10. Portrait Character Image
        Color elementCol = GetElementColor(character);
        entry.PortraitCharImage = CreateImageElement(root.transform, "PortraitChar",
            CreateCircleSprite(), elementCol, INNER_PORTRAIT_SIZE, INNER_PORTRAIT_SIZE);
        CenterChild(entry.PortraitCharImage.rectTransform, INNER_PORTRAIT_SIZE);

        if (character.Stats != null && character.Stats.Portrait != null)
        {
            entry.PortraitCharImage.sprite = character.Stats.Portrait;
            entry.PortraitCharImage.color = PORTRAIT_DIM;
        }

        // 11. Ready Flash Overlay
        entry.ReadyFlashImage = CreateImageElement(root.transform, "ReadyFlash",
            CreateCircleSprite(), new Color(1f, 1f, 1f, 0f), INNER_PORTRAIT_SIZE, INNER_PORTRAIT_SIZE);
        CenterChild(entry.ReadyFlashImage.rectTransform, INNER_PORTRAIT_SIZE);
        entry.ReadyFlashImage.enabled = false;

        // 12. Activation Flash
        entry.ActivationFlashImage = CreateImageElement(root.transform, "ActivationFlash",
            CreateSoftCircleSprite(), new Color(1f, 1f, 1f, 0f), INNER_PORTRAIT_SIZE, INNER_PORTRAIT_SIZE);
        CenterChild(entry.ActivationFlashImage.rectTransform, INNER_PORTRAIT_SIZE);
        entry.ActivationFlashImage.enabled = false;

        // 13. Shockwave Ring
        entry.ShockwaveRingImage = CreateImageElement(root.transform, "ShockwaveRing",
            CreateThinRingSprite(), new Color(1f, 0.87f, 0.2f, 0f), EP_RING_OUTER, EP_RING_OUTER);
        CenterChild(entry.ShockwaveRingImage.rectTransform, EP_RING_OUTER);
        entry.ShockwaveRingImage.enabled = false;

        // 14. Diamond Sparkles
        entry.DiamondSparkles = new Image[DIAMOND_SPARKLE_COUNT];
        entry.SparklePhaseOffsets = new float[DIAMOND_SPARKLE_COUNT];
        entry.SparkleRadiusOffsets = new float[DIAMOND_SPARKLE_COUNT];
        for (int d = 0; d < DIAMOND_SPARKLE_COUNT; d++)
        {
            var diamond = CreateImageElement(root.transform, $"Diamond_{d}",
                CreateDiamondSprite(), EP_FILL_READY, DIAMOND_SIZE, DIAMOND_SIZE);
            CenterChild(diamond.rectTransform, DIAMOND_SIZE);
            diamond.enabled = false;
            entry.DiamondSparkles[d] = diamond;
            entry.SparklePhaseOffsets[d] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            entry.SparkleRadiusOffsets[d] = UnityEngine.Random.Range(-8f, 8f);
        }

        // 15. Rising Particles
        entry.RisingParticles = new RisingParticle[RISING_PARTICLE_COUNT];
        for (int p = 0; p < RISING_PARTICLE_COUNT; p++)
        {
            var particleImg = CreateImageElement(root.transform, $"RisingParticle_{p}",
                CreateDiamondSprite(), Color.clear, RISING_PARTICLE_SIZE, RISING_PARTICLE_SIZE);
            CenterChild(particleImg.rectTransform, RISING_PARTICLE_SIZE);
            particleImg.enabled = false;
            entry.RisingParticles[p] = new RisingParticle
            {
                Image = particleImg,
                IsAlive = false,
                LifeTimer = 0f,
                SizeScale = 1f,
                SwayPhase = 0f
            };
        }
        entry.RisingParticleTimer = 0f;

        // 16. Key Hint Badge (below portrait)
        CreateKeyHintBadge(root.transform, entry, index);

        return entry;
    }

    // ──────────────────────────────────────────────
    // Key Hint Badge (clean rounded square)
    // ──────────────────────────────────────────────

    private void CreateKeyHintBadge(Transform parent, PortraitEntry entry, int index)
    {
        var badgeObj = new GameObject("KeyHintBadge", typeof(RectTransform));
        badgeObj.transform.SetParent(parent, false);

        var badgeRT = badgeObj.GetComponent<RectTransform>();
        // Position below the portrait center
        badgeRT.anchorMin = new Vector2(0.5f, 0f);
        badgeRT.anchorMax = new Vector2(0.5f, 0f);
        badgeRT.pivot = new Vector2(0.5f, 1f);
        badgeRT.anchoredPosition = new Vector2(0f, 2f);
        badgeRT.sizeDelta = new Vector2(KEY_HINT_SIZE, KEY_HINT_SIZE);

        // Glow behind badge (only when ready)
        var glowObj = new GameObject("HintGlow", typeof(RectTransform));
        glowObj.transform.SetParent(badgeObj.transform, false);

        entry.KeyHintGlowImage = glowObj.AddComponent<Image>();
        entry.KeyHintGlowImage.sprite = CreateSoftCircleSprite();
        entry.KeyHintGlowImage.color = Color.clear;
        entry.KeyHintGlowImage.raycastTarget = false;
        entry.KeyHintGlowImage.enabled = false;

        var glowRT = glowObj.GetComponent<RectTransform>();
        glowRT.anchorMin = new Vector2(0.5f, 0.5f);
        glowRT.anchorMax = new Vector2(0.5f, 0.5f);
        glowRT.pivot = new Vector2(0.5f, 0.5f);
        glowRT.anchoredPosition = Vector2.zero;
        glowRT.sizeDelta = new Vector2(KEY_HINT_SIZE * 2.0f, KEY_HINT_SIZE * 2.0f);

        // Border (rounded rect, slightly larger)
        var borderObj = new GameObject("HintBorder", typeof(RectTransform));
        borderObj.transform.SetParent(badgeObj.transform, false);

        entry.KeyHintBorderImage = borderObj.AddComponent<Image>();
        entry.KeyHintBorderImage.sprite = CreateRoundedRectSprite();
        entry.KeyHintBorderImage.color = HINT_BORDER;
        entry.KeyHintBorderImage.raycastTarget = false;

        var borderRT = borderObj.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-1.5f, -1.5f);
        borderRT.offsetMax = new Vector2(1.5f, 1.5f);

        // Background (rounded rect)
        var bgObj = new GameObject("HintBg", typeof(RectTransform));
        bgObj.transform.SetParent(badgeObj.transform, false);

        entry.KeyHintBgImage = bgObj.AddComponent<Image>();
        entry.KeyHintBgImage.sprite = CreateRoundedRectSprite();
        entry.KeyHintBgImage.color = HINT_BG;
        entry.KeyHintBgImage.raycastTarget = false;

        var bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Number text
        var textObj = new GameObject("HintText", typeof(RectTransform));
        textObj.transform.SetParent(badgeObj.transform, false);

        entry.KeyHintText = textObj.AddComponent<Text>();
        entry.KeyHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        entry.KeyHintText.fontSize = (int)KEY_HINT_FONT_SIZE;
        entry.KeyHintText.fontStyle = FontStyle.Bold;
        entry.KeyHintText.color = HINT_TEXT_IDLE;
        entry.KeyHintText.alignment = TextAnchor.MiddleCenter;
        entry.KeyHintText.text = (index + 1).ToString();
        entry.KeyHintText.raycastTarget = false;
        entry.KeyHintText.horizontalOverflow = HorizontalWrapMode.Overflow;
        entry.KeyHintText.verticalOverflow = VerticalWrapMode.Overflow;

        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.5f);
        outline.effectDistance = new Vector2(0.8f, -0.8f);

        var textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }

    // ──────────────────────────────────────────────
    // Target Selection Panel
    // ──────────────────────────────────────────────

    private void CreateTargetSelectionPanel()
    {
        _targetPanel = new GameObject("UltimateTargetPanel", typeof(RectTransform));
        _targetPanel.transform.SetParent(transform, false);

        var panelRT = _targetPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, CONTAINER_Y + OUTER_AURA_SIZE + 12f);
        panelRT.sizeDelta = new Vector2(340f, 50f);

        _targetPanelBg = _targetPanel.AddComponent<Image>();
        _targetPanelBg.sprite = CreateRoundedRectSprite();
        _targetPanelBg.color = TARGET_PANEL_BG;
        _targetPanelBg.raycastTarget = false;

        // Gold accent line at top
        var borderObj = new GameObject("GoldAccent", typeof(RectTransform));
        borderObj.transform.SetParent(_targetPanel.transform, false);

        _targetGoldBorder = borderObj.AddComponent<Image>();
        _targetGoldBorder.color = TARGET_GOLD_ACCENT;
        _targetGoldBorder.raycastTarget = false;

        var borderRT = borderObj.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0.05f, 1f);
        borderRT.anchorMax = new Vector2(0.95f, 1f);
        borderRT.pivot = new Vector2(0.5f, 1f);
        borderRT.anchoredPosition = Vector2.zero;
        borderRT.sizeDelta = new Vector2(0f, 2f);

        // Target name text
        var nameObj = new GameObject("TargetName", typeof(RectTransform));
        nameObj.transform.SetParent(_targetPanel.transform, false);

        _targetNameText = nameObj.AddComponent<Text>();
        _targetNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _targetNameText.fontSize = 15;
        _targetNameText.fontStyle = FontStyle.Bold;
        _targetNameText.color = TARGET_TEXT_COLOR;
        _targetNameText.alignment = TextAnchor.MiddleLeft;
        _targetNameText.raycastTarget = false;
        _targetNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _targetNameText.verticalOverflow = VerticalWrapMode.Overflow;

        var nameOutline = nameObj.AddComponent<Outline>();
        nameOutline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        nameOutline.effectDistance = new Vector2(1f, -1f);

        var nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.5f);
        nameRT.anchorMax = new Vector2(0.60f, 0.5f);
        nameRT.pivot = new Vector2(0f, 0.5f);
        nameRT.anchoredPosition = new Vector2(14f, 0f);
        nameRT.sizeDelta = new Vector2(0f, 26f);

        // Navigation hint
        var hintObj = new GameObject("TargetHint", typeof(RectTransform));
        hintObj.transform.SetParent(_targetPanel.transform, false);

        _targetHintText = hintObj.AddComponent<Text>();
        _targetHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _targetHintText.fontSize = 11;
        _targetHintText.color = TEXT_DIM;
        _targetHintText.alignment = TextAnchor.MiddleRight;
        _targetHintText.raycastTarget = false;
        _targetHintText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _targetHintText.verticalOverflow = VerticalWrapMode.Overflow;
        _targetHintText.text = "A/D:\u9078\u629e Enter:\u6c7a\u5b9a Esc:\u623b\u308b";

        var hintOutline = hintObj.AddComponent<Outline>();
        hintOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
        hintOutline.effectDistance = new Vector2(0.8f, -0.8f);

        var hintRT = hintObj.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0.60f, 0.5f);
        hintRT.anchorMax = new Vector2(1f, 0.5f);
        hintRT.pivot = new Vector2(1f, 0.5f);
        hintRT.anchoredPosition = new Vector2(-10f, 0f);
        hintRT.sizeDelta = new Vector2(0f, 20f);

        _targetPanel.SetActive(false);
    }

    private void ShowTargetSelectionPanel()
    {
        if (_targetPanel == null) return;
        _targetPanel.SetActive(true);
        UpdateTargetSelectionPanel();
    }

    private void UpdateTargetSelectionPanel()
    {
        if (_targetNameText == null || _targetCandidates.Count == 0) return;

        var target = _targetCandidates[_targetIndex];
        string charName = _pendingUltimateEntry != null
            ? _pendingUltimateEntry.Character.DisplayName
            : "???";
        _targetNameText.text = $"{charName} \u5fc5\u6bba\u6280 \u2192 {target.DisplayName}";
    }

    // ══════════════════════════════════════════════
    // UI HELPERS
    // ══════════════════════════════════════════════

    private Image CreateImageElement(Transform parent, string objName, Sprite sprite, Color color,
        float width, float height)
    {
        var obj = new GameObject(objName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        var image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        var rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        return image;
    }

    private void CenterChild(RectTransform rt, float size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);
    }

    // ══════════════════════════════════════════════
    // PROCEDURAL SPRITES (higher resolution, cleaner)
    // ══════════════════════════════════════════════

    // ──────────────────────────────────────────────
    // Circle (solid, anti-aliased, 128x128)
    // ──────────────────────────────────────────────

    private static Sprite CreateCircleSprite()
    {
        if (_cachedCircleSprite != null) return _cachedCircleSprite;

        int res = CIRCLE_RESOLUTION;
        var texture = new Texture2D(res, res, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = res * 0.5f;
        float radius = center - 1.5f;

        var pixels = new Color32[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 1.5px anti-alias band for smoother edges
                float alpha;
                if (dist <= radius - 0.75f)
                    alpha = 1f;
                else if (dist <= radius + 0.75f)
                    alpha = 1f - (dist - (radius - 0.75f)) / 1.5f;
                else
                    alpha = 0f;

                byte a = (byte)(Mathf.Clamp01(alpha) * 255);
                pixels[y * res + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        _cachedCircleSprite = Sprite.Create(
            texture,
            new Rect(0, 0, res, res),
            new Vector2(0.5f, 0.5f),
            100f);

        return _cachedCircleSprite;
    }

    // ──────────────────────────────────────────────
    // Soft Circle (smooth radial gradient, 96x96)
    // ──────────────────────────────────────────────

    private static Sprite CreateSoftCircleSprite()
    {
        if (_cachedSoftCircleSprite != null) return _cachedSoftCircleSprite;

        int res = SOFT_CIRCLE_RESOLUTION;
        var texture = new Texture2D(res, res, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = res * 0.5f;
        float radius = center - 1f;
        float featherStart = radius * 0.35f;

        var pixels = new Color32[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha;
                if (dist > radius)
                {
                    alpha = 0f;
                }
                else if (dist <= featherStart)
                {
                    alpha = 1f;
                }
                else
                {
                    // Cubic falloff for natural-looking glow
                    float t = (dist - featherStart) / (radius - featherStart);
                    alpha = 1f - t * t * t;
                    alpha = Mathf.Max(0f, alpha);
                }

                byte a = (byte)(alpha * 255);
                pixels[y * res + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        _cachedSoftCircleSprite = Sprite.Create(
            texture,
            new Rect(0, 0, res, res),
            new Vector2(0.5f, 0.5f),
            100f);

        return _cachedSoftCircleSprite;
    }

    // ──────────────────────────────────────────────
    // Ring (clean donut with anti-aliased edges, 128x128)
    // ──────────────────────────────────────────────

    private static Sprite CreateRingSprite()
    {
        if (_cachedRingSprite != null) return _cachedRingSprite;

        int res = RING_RESOLUTION;
        var texture = new Texture2D(res, res, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = res * 0.5f;
        float outerRadius = center - 1.5f;
        float ringThickness = (EP_RING_OUTER - EP_RING_INNER) * 0.5f / EP_RING_OUTER * res;
        float innerRadius = outerRadius - ringThickness;

        var pixels = new Color32[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 1px anti-alias on both edges
                float outerAlpha;
                if (dist > outerRadius + 0.75f) outerAlpha = 0f;
                else if (dist > outerRadius - 0.75f) outerAlpha = 1f - (dist - (outerRadius - 0.75f)) / 1.5f;
                else outerAlpha = 1f;

                float innerAlpha;
                if (dist < innerRadius - 0.75f) innerAlpha = 0f;
                else if (dist < innerRadius + 0.75f) innerAlpha = (dist - (innerRadius - 0.75f)) / 1.5f;
                else innerAlpha = 1f;

                float alpha = Mathf.Clamp01(outerAlpha * innerAlpha);
                byte a = (byte)(alpha * 255);
                pixels[y * res + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        _cachedRingSprite = Sprite.Create(
            texture,
            new Rect(0, 0, res, res),
            new Vector2(0.5f, 0.5f),
            100f);

        return _cachedRingSprite;
    }

    // ──────────────────────────────────────────────
    // Thin Ring (decorative border / shockwave, 128x128)
    // ──────────────────────────────────────────────

    private static Sprite CreateThinRingSprite()
    {
        if (_cachedThinRingSprite != null) return _cachedThinRingSprite;

        int res = RING_RESOLUTION;
        var texture = new Texture2D(res, res, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = res * 0.5f;
        float outerRadius = center - 1.5f;
        float innerRadius = outerRadius - 2.0f; // Very thin ring (2px)

        var pixels = new Color32[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float outerAlpha;
                if (dist > outerRadius + 0.5f) outerAlpha = 0f;
                else if (dist > outerRadius - 0.5f) outerAlpha = 1f - (dist - (outerRadius - 0.5f));
                else outerAlpha = 1f;

                float innerAlpha;
                if (dist < innerRadius - 0.5f) innerAlpha = 0f;
                else if (dist < innerRadius + 0.5f) innerAlpha = dist - (innerRadius - 0.5f);
                else innerAlpha = 1f;

                float alpha = Mathf.Clamp01(outerAlpha * innerAlpha);
                byte a = (byte)(alpha * 255);
                pixels[y * res + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        _cachedThinRingSprite = Sprite.Create(
            texture,
            new Rect(0, 0, res, res),
            new Vector2(0.5f, 0.5f),
            100f);

        return _cachedThinRingSprite;
    }

    // ──────────────────────────────────────────────
    // Diamond (refined sparkle, 24x24)
    // ──────────────────────────────────────────────

    private static Sprite CreateDiamondSprite()
    {
        if (_cachedDiamondSprite != null) return _cachedDiamondSprite;

        int res = DIAMOND_RESOLUTION;
        var texture = new Texture2D(res, res, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = res * 0.5f;
        float halfSize = center - 1.5f;

        var pixels = new Color32[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - center);
                float dy = Mathf.Abs(y + 0.5f - center);
                float dist = dx + dy;

                float alpha;
                if (dist <= halfSize - 1f)
                {
                    // Inner glow: radial brightness from center
                    float brightness = 1f - (dist / halfSize) * 0.5f;
                    alpha = brightness;
                }
                else if (dist <= halfSize + 0.5f)
                {
                    alpha = 1f - (dist - (halfSize - 1f)) / 1.5f;
                    alpha = Mathf.Max(0f, alpha);
                }
                else
                {
                    alpha = 0f;
                }

                byte a = (byte)(Mathf.Clamp01(alpha) * 255);
                pixels[y * res + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        _cachedDiamondSprite = Sprite.Create(
            texture,
            new Rect(0, 0, res, res),
            new Vector2(0.5f, 0.5f),
            100f);

        return _cachedDiamondSprite;
    }

    // ──────────────────────────────────────────────
    // Rounded Rectangle (for key hint badges, 32x32)
    // ──────────────────────────────────────────────

    private static Sprite CreateRoundedRectSprite()
    {
        if (_cachedRoundedRectSprite != null) return _cachedRoundedRectSprite;

        int res = ROUNDED_RECT_RESOLUTION;
        var texture = new Texture2D(res, res, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float cornerRadius = res * 0.22f; // ~22% of size for smooth corners
        float halfRes = res * 0.5f;

        var pixels = new Color32[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                // Distance from the nearest edge, accounting for rounded corners
                float dist = RoundedRectSDF(px - halfRes, py - halfRes, halfRes - 1f, halfRes - 1f, cornerRadius);

                float alpha;
                if (dist <= -0.75f)
                    alpha = 1f;
                else if (dist <= 0.75f)
                    alpha = 1f - (dist + 0.75f) / 1.5f;
                else
                    alpha = 0f;

                byte a = (byte)(Mathf.Clamp01(alpha) * 255);
                pixels[y * res + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        // 9-slice borders for proper scaling
        int border = (int)(cornerRadius + 2);
        _cachedRoundedRectSprite = Sprite.Create(
            texture,
            new Rect(0, 0, res, res),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));

        return _cachedRoundedRectSprite;
    }

    /// <summary>Signed distance function for a rounded rectangle.</summary>
    private static float RoundedRectSDF(float px, float py, float halfW, float halfH, float radius)
    {
        float qx = Mathf.Abs(px) - halfW + radius;
        float qy = Mathf.Abs(py) - halfH + radius;
        float outsideDist = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
        float insideDist = Mathf.Min(Mathf.Max(qx, qy), 0f);
        return outsideDist + insideDist - radius;
    }

    // ──────────────────────────────────────────────
    // Cleanup
    // ──────────────────────────────────────────────

    private void Cleanup()
    {
        CancelTargetSelection();

        foreach (var entry in _entries)
        {
            if (entry.Root != null) Destroy(entry.Root);
        }
        _entries.Clear();

        if (_container != null) Destroy(_container);
        if (_targetPanel != null) Destroy(_targetPanel);
    }
}
