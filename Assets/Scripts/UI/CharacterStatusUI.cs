// ============================================================
// CharacterStatusUI.cs
// Star Rail-inspired party HP status panel.
// Bottom-center horizontal row of compact dark-glass cards with
// thin gradient HP bars, trailing lag effect, shimmer sweep,
// active character border glow, low-HP pulse, and element dots.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a compact, dark-glass row of party member HP cards
/// at the bottom-center of the screen. Procedurally generates
/// all sprites so no external assets are required.
/// </summary>
public sealed class CharacterStatusUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Constants -- Palette
    // ──────────────────────────────────────────────

    // Card backgrounds
    private static readonly Color CARD_BG            = new Color(0.04f, 0.04f, 0.08f, 0.88f);
    private static readonly Color CARD_BORDER_IDLE   = new Color(0.18f, 0.22f, 0.32f, 0.25f);
    private static readonly Color CARD_BORDER_ACTIVE = new Color(0.40f, 0.70f, 1.00f, 0.85f);
    private static readonly Color CARD_ACTIVE_INNER  = new Color(0.20f, 0.40f, 0.80f, 0.06f);

    // HP bar
    private static readonly Color HP_BAR_BG          = new Color(0.08f, 0.08f, 0.12f, 0.90f);
    private static readonly Color HP_FILL_LEFT       = new Color(0.20f, 0.78f, 0.35f, 1.0f);
    private static readonly Color HP_FILL_RIGHT      = new Color(0.45f, 0.95f, 0.55f, 1.0f);
    private static readonly Color HP_LOW_LEFT        = new Color(0.95f, 0.55f, 0.10f, 1.0f);
    private static readonly Color HP_LOW_RIGHT       = new Color(0.95f, 0.20f, 0.12f, 1.0f);
    private static readonly Color HP_LAG_COLOR       = new Color(1.0f, 1.0f, 1.0f, 0.35f);

    // Text
    private static readonly Color TEXT_PRIMARY       = new Color(0.92f, 0.94f, 0.96f, 1.0f);
    private static readonly Color TEXT_SECONDARY     = new Color(0.60f, 0.65f, 0.72f, 1.0f);
    private static readonly Color TEXT_SHADOW        = new Color(0f, 0f, 0f, 0.55f);

    // Effects
    private static readonly Color SHIMMER_COLOR      = new Color(1.0f, 1.0f, 1.0f, 0.18f);

    // ──────────────────────────────────────────────
    // Constants -- Layout
    // ──────────────────────────────────────────────

    private const float CARD_WIDTH       = 175f;
    private const float CARD_HEIGHT      = 80f;
    private const float CARD_SPACING     = 12f;
    private const float CARD_RADIUS      = 8f;
    private const float CARD_BORDER_W    = 1.5f;

    private const float INNER_PAD_H      = 10f;
    private const float INNER_PAD_V      = 6f;

    private const int   NAME_FONT_SIZE   = 13;
    private const int   HP_FONT_SIZE     = 11;
    private const float HP_BAR_HEIGHT    = 7f;
    private const float HP_BAR_RADIUS    = 3.5f;
    private const float ELEMENT_DOT_SIZE = 10f;
    private const float ELEMENT_DOT_GAP  = 3f;

    private const float LOW_HP_THRESHOLD = 0.30f;

    // ──────────────────────────────────────────────
    // Constants -- Animation
    // ──────────────────────────────────────────────

    private const float LAG_SPEED             = 1.2f;
    private const float SHIMMER_PERIOD        = 5.5f;
    private const float SHIMMER_SWEEP_SPEED   = 0.35f;
    private const float SHIMMER_BAND_WIDTH    = 0.10f;
    private const float ACTIVE_PULSE_SPEED    = 2.0f;
    private const float ACTIVE_ALPHA_MIN      = 0.50f;
    private const float ACTIVE_ALPHA_MAX      = 0.90f;
    private const float LOW_HP_PULSE_SPEED    = 2.5f;
    private const float LOW_HP_BRIGHT_MIN     = 0.80f;
    private const float LOW_HP_BRIGHT_MAX     = 1.10f;

    // ──────────────────────────────────────────────
    // Procedural sprite cache (shared across instances)
    // ──────────────────────────────────────────────

    private static Sprite _sCardSprite;
    private static Sprite _sBarBgSprite;
    private static Sprite _sBarFillNormal;
    private static Sprite _sBarFillLow;
    private static Sprite _sCircle;
    private static Sprite _sShimmer;
    private static Sprite _sGlowSprite;

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("UI References")]
    [Tooltip("Parent transform (HorizontalLayoutGroup) for status cards.")]
    [SerializeField] private Transform _statusContainer;

    [Tooltip("Optional prefab for status cards; if null, cards are built procedurally.")]
    [SerializeField] private GameObject _statusPanelPrefab;

    // ──────────────────────────────────────────────
    // Internal types
    // ──────────────────────────────────────────────

    private class StatusEntry
    {
        public CharacterBattleController Character;
        public GameObject Panel;

        // Visual references
        public Image CardBg;
        public Image BorderImage;
        public Image ActiveGlowImage;
        public Image ElementDot;
        public Text  NameLabel;
        public Image HPBarBg;
        public Image HPFill;
        public Image HPLagFill;
        public Image ShimmerImage;
        public Text  HPLabel;

        // State
        public float TargetRatio;
        public float LagRatio;
    }

    private readonly List<StatusEntry> _entries = new List<StatusEntry>();
    private BattleManager _battleManager;
    private float _shimmerTimer;

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Populate the status panel from the given BattleManager.
    /// </summary>
    public void Initialize(BattleManager battleManager)
    {
        foreach (var e in _entries)
        {
            if (e.Panel != null) Destroy(e.Panel);
        }
        _entries.Clear();

        _battleManager = battleManager;
        if (battleManager.PlayerParty == null) return;

        EnsureSprites();

        foreach (var character in battleManager.PlayerParty)
        {
            if (character == null) continue;
            var entry = BuildCard(character);
            _entries.Add(entry);

            character.OnHPChanged += (hp, max) => RefreshHP(entry, hp, max);
            RefreshHP(entry, character.CurrentHP, character.MaxHP);
        }

        _shimmerTimer = 0f;
    }

    // ──────────────────────────────────────────────
    // Update -- lag, shimmer, active glow, low-HP pulse
    // ──────────────────────────────────────────────

    private void Update()
    {
        if (_entries.Count == 0) return;

        _shimmerTimer += Time.deltaTime;

        // Shimmer cycle: idle wait then a single sweep
        float cycle     = Mathf.Repeat(_shimmerTimer, SHIMMER_PERIOD);
        float sweepLen  = 1.0f / SHIMMER_SWEEP_SPEED;
        float shimmerT  = (cycle < sweepLen) ? (cycle / sweepLen) : -1f;

        CharacterBattleController activeChar =
            _battleManager != null ? _battleManager.ActiveCharacter : null;

        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            bool isActive = activeChar != null && e.Character == activeChar;

            // ── HP lag ──
            if (e.HPLagFill != null)
            {
                if (e.LagRatio > e.TargetRatio)
                {
                    e.LagRatio = Mathf.MoveTowards(
                        e.LagRatio, e.TargetRatio, LAG_SPEED * Time.deltaTime);
                }
                else
                {
                    e.LagRatio = e.TargetRatio;
                }
                e.HPLagFill.fillAmount = e.LagRatio;
            }

            // ── Shimmer sweep across fill ──
            if (e.ShimmerImage != null)
            {
                if (shimmerT >= 0f && e.TargetRatio > 0.01f)
                {
                    e.ShimmerImage.enabled = true;
                    var rt = e.ShimmerImage.rectTransform;
                    float half = SHIMMER_BAND_WIDTH * 0.5f;
                    rt.anchorMin = new Vector2(Mathf.Clamp01(shimmerT - half), 0f);
                    rt.anchorMax = new Vector2(Mathf.Clamp01(shimmerT + half), 1f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    float edgeFade = Mathf.Clamp01(Mathf.Min(shimmerT, 1f - shimmerT) * 6f);
                    float fillFade = shimmerT <= e.TargetRatio ? 1f : 0f;
                    var c = SHIMMER_COLOR;
                    c.a = SHIMMER_COLOR.a * edgeFade * fillFade;
                    e.ShimmerImage.color = c;
                }
                else
                {
                    e.ShimmerImage.enabled = false;
                }
            }

            // ── Active character border glow ──
            if (e.BorderImage != null)
            {
                if (isActive)
                {
                    float sin = (Mathf.Sin(Time.time * ACTIVE_PULSE_SPEED) + 1f) * 0.5f;
                    float a = Mathf.Lerp(ACTIVE_ALPHA_MIN, ACTIVE_ALPHA_MAX, sin);
                    var gc = CARD_BORDER_ACTIVE;
                    gc.a = a;
                    e.BorderImage.color = gc;
                }
                else
                {
                    e.BorderImage.color = CARD_BORDER_IDLE;
                }
            }

            // ── Active glow overlay (soft inner radiance) ──
            if (e.ActiveGlowImage != null)
            {
                if (isActive)
                {
                    e.ActiveGlowImage.enabled = true;
                    float sin = (Mathf.Sin(Time.time * ACTIVE_PULSE_SPEED * 0.8f) + 1f) * 0.5f;
                    var gc = CARD_ACTIVE_INNER;
                    gc.a = Mathf.Lerp(0.03f, 0.10f, sin);
                    e.ActiveGlowImage.color = gc;
                }
                else
                {
                    e.ActiveGlowImage.enabled = false;
                }
            }

            // ── Low-HP pulse (orange to red + brightness oscillation) ──
            if (e.HPFill != null)
            {
                if (e.TargetRatio <= LOW_HP_THRESHOLD && e.TargetRatio > 0f)
                {
                    float pulse = (Mathf.Sin(Time.time * LOW_HP_PULSE_SPEED) + 1f) * 0.5f;
                    float b = Mathf.Lerp(LOW_HP_BRIGHT_MIN, LOW_HP_BRIGHT_MAX, pulse);
                    e.HPFill.color = new Color(b, b, b, 1f);
                    if (e.HPFill.sprite != _sBarFillLow)
                        e.HPFill.sprite = _sBarFillLow;
                }
                else
                {
                    if (e.HPFill.sprite != _sBarFillNormal)
                        e.HPFill.sprite = _sBarFillNormal;
                    e.HPFill.color = Color.white;
                }
            }
        }
    }

    // ──────────────────────────────────────────────
    // Sprite generation
    // ──────────────────────────────────────────────

    private static void EnsureSprites()
    {
        if (_sCardSprite == null)
            _sCardSprite = MakeRoundedRect(64, 64, 10);

        if (_sBarBgSprite == null)
            _sBarBgSprite = MakeRoundedRect(32, 12, 6);

        if (_sBarFillNormal == null)
            _sBarFillNormal = MakeGradientBar(128, 12, 6, HP_FILL_LEFT, HP_FILL_RIGHT);

        if (_sBarFillLow == null)
            _sBarFillLow = MakeGradientBar(128, 12, 6, HP_LOW_LEFT, HP_LOW_RIGHT);

        if (_sCircle == null)
            _sCircle = MakeCircle(24);

        if (_sShimmer == null)
            _sShimmer = MakeShimmer(16, 12);

        if (_sGlowSprite == null)
            _sGlowSprite = MakeGlow(64, 64, 12);
    }

    /// <summary>
    /// Rounded rectangle with smooth anti-aliased edges, set up as a 9-sliced sprite.
    /// </summary>
    private static Sprite MakeRoundedRect(int w, int h, int r)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        var px = new Color[w * h];
        float rf = r;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(rf - x, x - (w - 1 - rf)));
                float dy = Mathf.Max(0f, Mathf.Max(rf - y, y - (h - 1 - rf)));
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float a  = Mathf.Clamp01(rf - d + 0.5f);
                px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        var border = new Vector4(r + 2, r + 2, r + 2, r + 2);
        return Sprite.Create(tex, new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
    }

    /// <summary>
    /// Horizontal gradient bar with rounded ends for the HP fill.
    /// </summary>
    private static Sprite MakeGradientBar(int w, int h, int r,
        Color left, Color right)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        var px = new Color[w * h];
        float rf = r;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(rf - x, x - (w - 1 - rf)));
                float dy = Mathf.Max(0f, Mathf.Max(rf - y, y - (h - 1 - rf)));
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float a  = Mathf.Clamp01(rf - d + 0.5f);

                float t = (float)x / (w - 1);
                Color c = Color.Lerp(left, right, t);

                // Subtle top highlight for a glossy 3D feel
                float yNorm = (float)y / (h - 1);
                float highlight = Mathf.Pow(Mathf.Clamp01(yNorm - 0.55f) * 3f, 2f) * 0.15f;
                c.r = Mathf.Clamp01(c.r + highlight);
                c.g = Mathf.Clamp01(c.g + highlight);
                c.b = Mathf.Clamp01(c.b + highlight);

                c.a = a;
                px[y * w + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        var border = new Vector4(r + 2, r + 2, r + 2, r + 2);
        return Sprite.Create(tex, new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
    }

    /// <summary>Anti-aliased filled circle.</summary>
    private static Sprite MakeCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear
        };

        var px    = new Color[size * size];
        float ctr = (size - 1) * 0.5f;
        float rad = ctr;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - ctr;
                float dy = y - ctr;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float a  = Mathf.Clamp01((rad - d) * 1.8f);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Vertical bell-curve shimmer band -- bright at center, transparent at edges.
    /// </summary>
    private static Sprite MakeShimmer(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = (float)x / (w - 1);
                float intensity = Mathf.Exp(-Mathf.Pow((t - 0.5f) * 4.5f, 2f));
                px[y * w + x] = new Color(1f, 1f, 1f, intensity);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Soft inner glow sprite for active card highlight. Bright edge fading inward.
    /// </summary>
    private static Sprite MakeGlow(int w, int h, int r)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        var px = new Color[w * h];
        float rf = r;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Distance from nearest edge
                float edgeX = Mathf.Min(x, w - 1 - x);
                float edgeY = Mathf.Min(y, h - 1 - y);
                float edge  = Mathf.Min(edgeX, edgeY);

                // Rounded corners
                float dx = Mathf.Max(0f, Mathf.Max(rf - x, x - (w - 1 - rf)));
                float dy = Mathf.Max(0f, Mathf.Max(rf - y, y - (h - 1 - rf)));
                float cornerDist = Mathf.Sqrt(dx * dx + dy * dy);
                float cornerAlpha = Mathf.Clamp01(rf - cornerDist + 0.5f);

                // Glow fades from edges inward
                float glowFalloff = 8f;
                float glowAlpha = Mathf.Exp(-edge / glowFalloff) * 0.6f;

                float a = glowAlpha * cornerAlpha;
                px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        var border = new Vector4(r + 2, r + 2, r + 2, r + 2);
        return Sprite.Create(tex, new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
    }

    // ──────────────────────────────────────────────
    // Card construction
    // ──────────────────────────────────────────────

    private StatusEntry BuildCard(CharacterBattleController character)
    {
        var entry = new StatusEntry
        {
            Character   = character,
            TargetRatio = 1f,
            LagRatio    = 1f
        };

        // ── Prefab path ──
        if (_statusPanelPrefab != null)
        {
            entry.Panel = Instantiate(_statusPanelPrefab, _statusContainer);
            entry.NameLabel   = entry.Panel.transform.Find("NameText")?.GetComponent<Text>();
            entry.HPLabel     = entry.Panel.transform.Find("HPText")?.GetComponent<Text>();
            entry.HPFill      = entry.Panel.transform.Find("HPBar/Fill")?.GetComponent<Image>();
            entry.HPLagFill   = entry.Panel.transform.Find("HPBar/LagFill")?.GetComponent<Image>();
            if (entry.NameLabel != null) entry.NameLabel.text = character.DisplayName;
            return entry;
        }

        // ── Procedural build ──

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ============================
        // Root: border shell
        // ============================
        var root = new GameObject($"Status_{character.DisplayName}", typeof(RectTransform));
        root.transform.SetParent(_statusContainer, false);

        var borderImg = root.AddComponent<Image>();
        borderImg.sprite = _sCardSprite;
        borderImg.type   = Image.Type.Sliced;
        borderImg.color  = CARD_BORDER_IDLE;
        borderImg.pixelsPerUnitMultiplier = 1f;

        var rootLE = root.AddComponent<LayoutElement>();
        rootLE.preferredWidth  = CARD_WIDTH;
        rootLE.preferredHeight = CARD_HEIGHT;
        rootLE.flexibleWidth   = 0;
        rootLE.flexibleHeight  = 0;

        entry.Panel       = root;
        entry.BorderImage = borderImg;

        // ============================
        // Inner background (inset by border width)
        // ============================
        var inner = CreateChild("Inner", root.transform);
        StretchFill(inner, CARD_BORDER_W);

        var innerImg = inner.AddComponent<Image>();
        innerImg.sprite = _sCardSprite;
        innerImg.type   = Image.Type.Sliced;
        innerImg.color  = CARD_BG;
        innerImg.pixelsPerUnitMultiplier = 1f;
        entry.CardBg = innerImg;

        // ============================
        // Active glow overlay (edge glow when selected)
        // ============================
        var glowObj = CreateChild("ActiveGlow", inner.transform);
        StretchFill(glowObj, 0f);

        var glowImg = glowObj.AddComponent<Image>();
        glowImg.sprite = _sGlowSprite;
        glowImg.type   = Image.Type.Sliced;
        glowImg.color  = Color.clear;
        glowImg.raycastTarget = false;
        glowImg.enabled = false;
        glowImg.pixelsPerUnitMultiplier = 1f;
        entry.ActiveGlowImage = glowImg;

        // ============================
        // Content area (padded)
        // ============================
        var content = CreateChild("Content", inner.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(INNER_PAD_H, INNER_PAD_V);
        contentRT.offsetMax = new Vector2(-INNER_PAD_H, -INNER_PAD_V);

        // We manually position children inside content rather than using
        // layout groups, for tighter control over the compact card layout.

        // ============================
        // Top row: element dot + name (left) + HP text (right)
        // ============================

        // -- Element dot --
        var dotObj = CreateChild("ElementDot", content.transform);
        var dotRT  = dotObj.GetComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(0f, 1f);
        dotRT.anchorMax = new Vector2(0f, 1f);
        dotRT.pivot     = new Vector2(0f, 1f);
        dotRT.sizeDelta = new Vector2(ELEMENT_DOT_SIZE, ELEMENT_DOT_SIZE);
        // Vertically center the dot with the name text line
        float nameLineH = NAME_FONT_SIZE + 2f;
        dotRT.anchoredPosition = new Vector2(0f, -(nameLineH - ELEMENT_DOT_SIZE) * 0.5f);

        var dotImg = dotObj.AddComponent<Image>();
        dotImg.sprite = _sCircle;
        dotImg.raycastTarget = false;
        Color elemColor = GetElementColor(
            character.Stats != null ? character.Stats.Element : CharacterStats.ElementType.Physical);
        dotImg.color = elemColor;
        entry.ElementDot = dotImg;

        // -- Name label --
        var nameObj = CreateChild("Name", content.transform);
        var nameRT  = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(0.62f, 1f);
        nameRT.pivot     = new Vector2(0f, 1f);
        nameRT.anchoredPosition = new Vector2(ELEMENT_DOT_SIZE + ELEMENT_DOT_GAP, 0f);
        nameRT.sizeDelta = new Vector2(0f, nameLineH);

        var nameText = nameObj.AddComponent<Text>();
        nameText.font       = font;
        nameText.fontSize   = NAME_FONT_SIZE;
        nameText.fontStyle  = FontStyle.Bold;
        nameText.color      = TEXT_PRIMARY;
        nameText.alignment  = TextAnchor.MiddleLeft;
        nameText.text       = character.DisplayName;
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameText.verticalOverflow   = VerticalWrapMode.Overflow;
        nameText.raycastTarget      = false;

        var nameShadow = nameObj.AddComponent<Shadow>();
        nameShadow.effectColor    = TEXT_SHADOW;
        nameShadow.effectDistance = new Vector2(0.6f, -0.6f);

        entry.NameLabel = nameText;

        // -- HP text (right-aligned on top row) --
        var hpLabelObj = CreateChild("HPLabel", content.transform);
        var hpLabelRT  = hpLabelObj.GetComponent<RectTransform>();
        hpLabelRT.anchorMin = new Vector2(0.55f, 1f);
        hpLabelRT.anchorMax = new Vector2(1f, 1f);
        hpLabelRT.pivot     = new Vector2(1f, 1f);
        hpLabelRT.anchoredPosition = Vector2.zero;
        hpLabelRT.sizeDelta = new Vector2(0f, nameLineH);

        var hpLabel = hpLabelObj.AddComponent<Text>();
        hpLabel.font       = font;
        hpLabel.fontSize   = HP_FONT_SIZE;
        hpLabel.color      = TEXT_SECONDARY;
        hpLabel.alignment  = TextAnchor.MiddleRight;
        hpLabel.text       = "";
        hpLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        hpLabel.verticalOverflow   = VerticalWrapMode.Overflow;
        hpLabel.raycastTarget      = false;

        var hpShadow = hpLabelObj.AddComponent<Shadow>();
        hpShadow.effectColor    = TEXT_SHADOW;
        hpShadow.effectDistance = new Vector2(0.5f, -0.5f);

        entry.HPLabel = hpLabel;

        // ============================
        // Bottom row: HP bar
        // ============================

        float barY = -(nameLineH + 5f);

        // -- Bar background --
        var barBgObj = CreateChild("HPBarBg", content.transform);
        var barBgRT  = barBgObj.GetComponent<RectTransform>();
        barBgRT.anchorMin = new Vector2(0f, 1f);
        barBgRT.anchorMax = new Vector2(1f, 1f);
        barBgRT.pivot     = new Vector2(0f, 1f);
        barBgRT.anchoredPosition = new Vector2(0f, barY);
        barBgRT.sizeDelta = new Vector2(0f, HP_BAR_HEIGHT);

        var barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.sprite = _sBarBgSprite;
        barBgImg.type   = Image.Type.Sliced;
        barBgImg.color  = HP_BAR_BG;
        barBgImg.pixelsPerUnitMultiplier = 1f;
        entry.HPBarBg = barBgImg;

        // -- Lag fill (white trailing bar behind main fill) --
        var lagObj = CreateChild("LagFill", barBgObj.transform);
        StretchFill(lagObj, 0f);

        var lagImg = lagObj.AddComponent<Image>();
        lagImg.sprite     = _sBarBgSprite;
        lagImg.type       = Image.Type.Filled;
        lagImg.fillMethod = Image.FillMethod.Horizontal;
        lagImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        lagImg.fillAmount = 1f;
        lagImg.color      = HP_LAG_COLOR;
        lagImg.raycastTarget = false;
        entry.HPLagFill = lagImg;

        // -- Main HP fill --
        var fillObj = CreateChild("Fill", barBgObj.transform);
        StretchFill(fillObj, 0f);

        var fillImg = fillObj.AddComponent<Image>();
        fillImg.sprite     = _sBarFillNormal;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;
        fillImg.color      = Color.white;
        fillImg.raycastTarget = false;
        entry.HPFill = fillImg;

        // -- Shimmer overlay --
        var shimObj = CreateChild("Shimmer", barBgObj.transform);
        var shimRT  = shimObj.GetComponent<RectTransform>();
        shimRT.anchorMin = new Vector2(0f, 0f);
        shimRT.anchorMax = new Vector2(0.1f, 1f);
        shimRT.offsetMin = Vector2.zero;
        shimRT.offsetMax = Vector2.zero;

        var shimImg = shimObj.AddComponent<Image>();
        shimImg.sprite = _sShimmer;
        shimImg.color  = SHIMMER_COLOR;
        shimImg.raycastTarget = false;
        shimImg.enabled = false;
        entry.ShimmerImage = shimImg;

        return entry;
    }

    // ──────────────────────────────────────────────
    // HP refresh
    // ──────────────────────────────────────────────

    private void RefreshHP(StatusEntry entry, int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        entry.TargetRatio = ratio;

        if (entry.HPFill != null)
            entry.HPFill.fillAmount = ratio;

        if (entry.HPLabel != null)
            entry.HPLabel.text = $"{current}/{max}";
    }

    // ──────────────────────────────────────────────
    // Element color mapping
    // ──────────────────────────────────────────────

    private static Color GetElementColor(CharacterStats.ElementType element)
    {
        switch (element)
        {
            case CharacterStats.ElementType.Physical:  return new Color(0.72f, 0.72f, 0.72f, 1f);
            case CharacterStats.ElementType.Fire:      return new Color(1.00f, 0.42f, 0.12f, 1f);
            case CharacterStats.ElementType.Ice:       return new Color(0.35f, 0.78f, 1.00f, 1f);
            case CharacterStats.ElementType.Lightning: return new Color(0.78f, 0.40f, 1.00f, 1f);
            case CharacterStats.ElementType.Wind:      return new Color(0.35f, 0.92f, 0.52f, 1f);
            case CharacterStats.ElementType.Dark:      return new Color(0.52f, 0.22f, 0.82f, 1f);
            default:                                   return Color.white;
        }
    }

    // ──────────────────────────────────────────────
    // Utility helpers
    // ──────────────────────────────────────────────

    /// <summary>Create a child GameObject with a RectTransform.</summary>
    private static GameObject CreateChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>
    /// Stretch a RectTransform to fill its parent with the given inset on all sides.
    /// </summary>
    private static void StretchFill(GameObject go, float inset)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
