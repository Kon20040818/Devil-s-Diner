// ============================================================
// EnemyStatusUI.cs
// Honkai Star Rail inspired world-space enemy status billboard.
//
// Layout (top to bottom, no solid background panel):
//   1. Enemy name (bold, white, shadow + outline)
//   2. Thin element-colored underline
//   3. HP bar (red gradient, rounded, lag trail, bright frontier edge)
//      with HP percentage text to the right
//   4. Toughness bar (thin white bar, shatter-break effect)
//   5. Weakness element icons (small circles in a row, flash on hit)
//
// All sprites are procedurally generated and cached across instances.
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space billboard UI displayed above each enemy.
/// Renders name (with element-colored underline), HP bar (with trailing
/// lag bar, bright frontier edge, and HP percentage), toughness bar (with
/// shatter break effect), and circular weakness element icons (with flash
/// on hit). Attach to enemy GameObject; call <see cref="Initialize"/> with
/// the target <see cref="CharacterBattleController"/>.
/// </summary>
public sealed class EnemyStatusUI : MonoBehaviour
{
    // ----------------------------------------------------------------
    // Layout constants
    // ----------------------------------------------------------------

    private const float CANVAS_SCALE        = 0.004f;
    private const float CANVAS_WIDTH        = 260f;
    private const float CANVAS_HEIGHT       = 140f;

    private const float HP_BAR_HEIGHT       = 22f;
    private const float TOUGHNESS_BAR_HEIGHT = 8f;
    private const float WEAKNESS_ICON_SIZE  = 28f;
    private const float WEAKNESS_DOT_SIZE   = 20f;
    private const float WEAKNESS_ICON_SPACING = 5f;
    private const float ELEMENT_SPACING     = 4f;
    private const float NAME_UNDERLINE_HEIGHT = 2f;

    private const int NAME_FONT_SIZE        = 22;
    private const int HP_FONT_SIZE          = 16;
    private const int HP_PERCENT_FONT_SIZE  = 14;

    private const int HP_BAR_CORNER_RADIUS  = 4;
    private const float BRIGHT_EDGE_WIDTH   = 0.018f;

    private static readonly Vector3 BILLBOARD_OFFSET = new Vector3(0f, 2.2f, 0f);

    // ----------------------------------------------------------------
    // Color palette
    // ----------------------------------------------------------------

    private static readonly Color HP_BAR_BG          = new Color(0.08f, 0.08f, 0.13f, 0.92f);
    private static readonly Color HP_FILL_LEFT       = new Color(0.85f, 0.20f, 0.15f, 1.00f);
    private static readonly Color HP_FILL_RIGHT      = new Color(1.00f, 0.30f, 0.20f, 1.00f);
    private static readonly Color HP_LAG_COLOR       = new Color(1.00f, 1.00f, 1.00f, 0.70f);
    private static readonly Color HP_BRIGHT_EDGE     = new Color(1.00f, 0.90f, 0.75f, 0.95f);
    private static readonly Color TOUGHNESS_BG       = new Color(0.10f, 0.10f, 0.15f, 0.70f);
    private static readonly Color TOUGHNESS_FILL     = new Color(0.95f, 0.95f, 1.00f, 1.00f);
    private static readonly Color TOUGHNESS_BROKEN   = new Color(0.50f, 0.15f, 0.15f, 0.80f);
    private static readonly Color WEAKNESS_ICON_BG   = new Color(0.05f, 0.05f, 0.15f, 0.75f);
    private static readonly Color WEAKNESS_FLASH     = new Color(1.00f, 1.00f, 1.00f, 0.95f);
    private static readonly Color HP_PERCENT_COLOR   = new Color(1.00f, 1.00f, 1.00f, 0.75f);

    // ----------------------------------------------------------------
    // Animation tuning
    // ----------------------------------------------------------------

    private const float LAG_SPEED               = 1.2f;
    private const float BREAK_FLASH_DURATION    = 0.6f;
    private const int   BREAK_FLASH_COUNT       = 5;
    private const int   SHATTER_FRAGMENT_COUNT  = 12;
    private const float SHATTER_DURATION        = 0.7f;
    private const float SHATTER_SPREAD          = 60f;
    private const float WEAKNESS_FLASH_DURATION = 0.4f;
    private const float DEATH_FADE_DURATION     = 0.5f;

    // ----------------------------------------------------------------
    // Shared procedural sprite cache (static -- survives across instances)
    // ----------------------------------------------------------------

    private static Sprite _spriteWhite;
    private static Sprite _spriteRounded;
    private static Sprite _spriteGradient;
    private static Sprite _spriteGradientRounded;
    private static Sprite _spriteCircle;
    private static Sprite _spriteSmallSquare;

    // ----------------------------------------------------------------
    // Runtime references
    // ----------------------------------------------------------------

    private CharacterBattleController _enemy;
    private Canvas _canvas;
    private Transform _canvasTransform;
    private Camera _mainCamera;

    // UI elements
    private Text  _nameText;
    private Image _nameUnderline;
    private Image _hpBarBgImage;
    private Image _hpLagFill;
    private Image _hpFill;
    private Image _hpBrightEdge;
    private Text  _hpText;
    private Text  _hpPercentText;

    private GameObject _toughnessRoot;
    private Image _toughnessBgImage;
    private Image _toughnessFillImage;
    private Transform _toughnessTransform;

    private readonly Dictionary<CharacterStats.ElementType, Image> _weaknessIconBgs =
        new Dictionary<CharacterStats.ElementType, Image>();
    private readonly Dictionary<CharacterStats.ElementType, Image> _weaknessDotImages =
        new Dictionary<CharacterStats.ElementType, Image>();

    // HP lag state
    private float _lagHP;
    private float _targetHPRatio;

    // Active coroutine handles for safe cancellation
    private Coroutine _breakFlashCoroutine;

    // ================================================================
    // Public API
    // ================================================================

    /// <summary>
    /// Initialize with the target enemy. Builds the entire UI hierarchy
    /// and subscribes to <see cref="CharacterBattleController"/> events.
    /// </summary>
    public void Initialize(CharacterBattleController enemy)
    {
        if (enemy == null)
        {
            Debug.LogWarning("[EnemyStatusUI] Initialize called with null enemy.");
            return;
        }

        _enemy = enemy;
        _mainCamera = Camera.main;

        float maxHP = _enemy.MaxHP;
        _targetHPRatio = maxHP > 0 ? (float)_enemy.CurrentHP / maxHP : 1f;
        _lagHP = _targetHPRatio;

        EnsureProceduralSprites();
        BuildCanvas();
        SubscribeEvents();

        // Apply initial values
        RefreshHP(_enemy.CurrentHP, _enemy.MaxHP);

        if (_enemy.HasToughness)
        {
            RefreshToughness(_enemy.CurrentToughness, _enemy.MaxToughness);
        }
    }

    /// <summary>
    /// Triggers a brightness flash on the specified weakness icon.
    /// Call this when the enemy is hit by that element type.
    /// </summary>
    public void TriggerWeaknessFlash(CharacterStats.ElementType element)
    {
        if (_weaknessIconBgs.TryGetValue(element, out Image bgImage)
            && _weaknessDotImages.TryGetValue(element, out Image dotImage))
        {
            StartCoroutine(AnimateWeaknessFlash(bgImage, dotImage, element));
        }
    }

    // ================================================================
    // MonoBehaviour
    // ================================================================

    private void Update()
    {
        UpdateLagBar();
        UpdateBrightEdge();
    }

    private void LateUpdate()
    {
        FaceCameraBillboard();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        CancelBreakFlash();
    }

    // ================================================================
    // Per-frame helpers
    // ================================================================

    /// <summary>
    /// Smoothly catches the white lag bar up to the actual HP fill.
    /// </summary>
    private void UpdateLagBar()
    {
        if (_hpLagFill == null) return;

        if (_lagHP > _targetHPRatio)
        {
            _lagHP = Mathf.MoveTowards(_lagHP, _targetHPRatio, LAG_SPEED * Time.deltaTime);
        }
        else
        {
            _lagHP = _targetHPRatio;
        }

        _hpLagFill.fillAmount = _lagHP;
    }

    /// <summary>
    /// Positions a thin bright sliver at the frontier of the HP fill.
    /// Hidden when the bar is essentially full or empty.
    /// </summary>
    private void UpdateBrightEdge()
    {
        if (_hpBrightEdge == null) return;

        bool visible = _targetHPRatio > 0.01f && _targetHPRatio < 0.99f;
        _hpBrightEdge.enabled = visible;

        if (!visible) return;

        RectTransform edgeRT = _hpBrightEdge.rectTransform;
        float left  = Mathf.Clamp01(_targetHPRatio - BRIGHT_EDGE_WIDTH);
        float right = Mathf.Clamp01(_targetHPRatio);

        edgeRT.anchorMin = new Vector2(left, 0f);
        edgeRT.anchorMax = new Vector2(right, 1f);
        edgeRT.offsetMin = Vector2.zero;
        edgeRT.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Keeps the canvas facing the main camera (billboard behaviour).
    /// Caches Camera.main to avoid per-frame property lookup overhead.
    /// </summary>
    private void FaceCameraBillboard()
    {
        if (_canvasTransform == null) return;

        // Re-acquire if the cached reference became stale
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera != null)
        {
            _canvasTransform.forward = _mainCamera.transform.forward;
        }
    }

    // ================================================================
    // Procedural sprite generation (static, lazily cached)
    // ================================================================

    private static void EnsureProceduralSprites()
    {
        if (_spriteWhite == null)           _spriteWhite           = CreateWhiteSprite();
        if (_spriteRounded == null)         _spriteRounded         = CreateRoundedRectSprite(64, 16, HP_BAR_CORNER_RADIUS, Color.white);
        if (_spriteGradient == null)        _spriteGradient        = CreateHorizontalGradientSprite(64, 4);
        if (_spriteGradientRounded == null) _spriteGradientRounded = CreateGradientRoundedSprite(64, 16, HP_BAR_CORNER_RADIUS);
        if (_spriteCircle == null)          _spriteCircle          = CreateCircleSprite(32);
        if (_spriteSmallSquare == null)     _spriteSmallSquare     = CreateSmallSquareSprite(8);
    }

    /// <summary>4x4 solid white sprite for flat colour fills.</summary>
    private static Sprite CreateWhiteSprite()
    {
        const int size = 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Rounded rectangle with anti-aliased edges and 9-slice borders so
    /// the sprite stretches cleanly at any size.
    /// </summary>
    private static Sprite CreateRoundedRectSprite(int width, int height, int radius, Color color)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Max(Mathf.Max(radius - x, x - (width - 1 - radius)), 0f);
                float dy = Mathf.Max(Mathf.Max(radius - y, y - (height - 1 - radius)), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = (dist <= radius) ? Mathf.Clamp01((radius - dist) * 1.5f) : 0f;
                pixels[y * width + x] = new Color(color.r, color.g, color.b, color.a * alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);

        var border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    /// <summary>Horizontal red gradient sprite for the HP bar fill.</summary>
    private static Sprite CreateHorizontalGradientSprite(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                pixels[y * width + x] = Color.Lerp(HP_FILL_LEFT, HP_FILL_RIGHT, t);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>Horizontal gradient with rounded corners for the HP fill.</summary>
    private static Sprite CreateGradientRoundedSprite(int width, int height, int radius)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                Color gradColor = Color.Lerp(HP_FILL_LEFT, HP_FILL_RIGHT, t);

                float dx = Mathf.Max(Mathf.Max(radius - x, x - (width - 1 - radius)), 0f);
                float dy = Mathf.Max(Mathf.Max(radius - y, y - (height - 1 - radius)), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = (dist <= radius) ? Mathf.Clamp01((radius - dist) * 1.5f) : 0f;
                pixels[y * width + x] = new Color(gradColor.r, gradColor.g, gradColor.b, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);

        var border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    /// <summary>Anti-aliased circle sprite for weakness icon backgrounds and dots.</summary>
    private static Sprite CreateCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = (dist <= radius) ? Mathf.Clamp01((radius - dist) * 1.5f) : 0f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>Small slightly-rounded square for shatter fragments.</summary>
    private static Sprite CreateSmallSquareSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Max(1 - x, x - (size - 2)), 0f);
                float dy = Mathf.Max(Mathf.Max(1 - y, y - (size - 2)), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1.5f - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ================================================================
    // Canvas & UI hierarchy construction
    // ================================================================

    private void BuildCanvas()
    {
        // -- World-space Canvas --
        var canvasGO = new GameObject("EnemyStatusCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = BILLBOARD_OFFSET;
        canvasGO.transform.localScale    = Vector3.one * CANVAS_SCALE;

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.WorldSpace;
        _canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        canvasGO.AddComponent<GraphicRaycaster>();

        _canvasTransform = canvasGO.transform;

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(CANVAS_WIDTH, CANVAS_HEIGHT);
        canvasRT.pivot     = new Vector2(0.5f, 0f);

        // -- Layout root (no background) --
        var layoutRoot = CreateChild<RectTransform>("LayoutRoot", canvasGO.transform);
        StretchFill(layoutRoot.GetComponent<RectTransform>());

        var vlg = layoutRoot.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.spacing              = ELEMENT_SPACING;
        vlg.padding              = new RectOffset(4, 4, 2, 2);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight   = true;
        vlg.childControlWidth    = true;

        // -- Build each section --
        BuildNameSection(layoutRoot.transform);
        BuildHPBarSection(layoutRoot.transform);
        BuildToughnessSection(layoutRoot.transform);
        BuildWeaknessIcons(layoutRoot.transform);
    }

    // ----------------------------------------------------------------
    // Name + underline
    // ----------------------------------------------------------------

    private void BuildNameSection(Transform parent)
    {
        var section = CreateChild<RectTransform>("NameSection", parent);

        var sectionVLG = section.AddComponent<VerticalLayoutGroup>();
        sectionVLG.childAlignment       = TextAnchor.UpperCenter;
        sectionVLG.spacing              = 2f;
        sectionVLG.childForceExpandWidth  = false;
        sectionVLG.childForceExpandHeight = false;
        sectionVLG.childControlHeight   = true;
        sectionVLG.childControlWidth    = true;

        float sectionHeight = NAME_FONT_SIZE + 10f + NAME_UNDERLINE_HEIGHT;
        SetLayoutElement(section, preferredHeight: sectionHeight, minHeight: sectionHeight - 2f);

        // -- Name text --
        var nameGO = CreateChild<RectTransform>("NameText", section.transform);

        _nameText = nameGO.AddComponent<Text>();
        _nameText.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _nameText.fontSize           = NAME_FONT_SIZE;
        _nameText.fontStyle          = FontStyle.Bold;
        _nameText.color              = Color.white;
        _nameText.alignment          = TextAnchor.MiddleCenter;
        _nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _nameText.verticalOverflow   = VerticalWrapMode.Overflow;
        _nameText.raycastTarget      = false;
        _nameText.text               = _enemy != null ? _enemy.DisplayName : string.Empty;

        AddTextShadow(nameGO, new Color(0f, 0f, 0f, 0.85f), new Vector2(1.2f, -1.2f));
        AddTextOutline(nameGO, new Color(0f, 0f, 0f, 0.60f), new Vector2(1.0f, -1.0f));

        SetLayoutElement(nameGO, preferredHeight: NAME_FONT_SIZE + 6f, minHeight: NAME_FONT_SIZE + 4f);

        // -- Element-colored underline --
        var underlineGO = CreateChild<RectTransform>("NameUnderline", section.transform);

        _nameUnderline = underlineGO.AddComponent<Image>();
        _nameUnderline.sprite        = _spriteWhite;
        _nameUnderline.raycastTarget = false;

        CharacterStats.ElementType elem = (_enemy != null && _enemy.Stats != null)
            ? _enemy.Stats.Element
            : CharacterStats.ElementType.Physical;

        Color underlineColor = GetElementColor(elem);
        underlineColor.a = 0.7f;
        _nameUnderline.color = underlineColor;

        SetLayoutElement(underlineGO, preferredHeight: NAME_UNDERLINE_HEIGHT, minHeight: NAME_UNDERLINE_HEIGHT);
    }

    // ----------------------------------------------------------------
    // HP bar (background, lag fill, main fill, bright edge, text, %)
    // ----------------------------------------------------------------

    private void BuildHPBarSection(Transform parent)
    {
        // Row: [ HP bar (flexible) | % text (fixed) ]
        var hpRow = CreateChild<RectTransform>("HPRow", parent);

        var rowHLG = hpRow.AddComponent<HorizontalLayoutGroup>();
        rowHLG.childAlignment       = TextAnchor.MiddleCenter;
        rowHLG.spacing              = 6f;
        rowHLG.childForceExpandWidth  = false;
        rowHLG.childForceExpandHeight = false;
        rowHLG.childControlHeight   = true;
        rowHLG.childControlWidth    = true;

        SetLayoutElement(hpRow, preferredHeight: HP_BAR_HEIGHT, minHeight: HP_BAR_HEIGHT);

        // -- Bar container --
        var barContainer = CreateChild<RectTransform>("HPBarContainer", hpRow.transform);
        SetLayoutElement(barContainer, preferredHeight: HP_BAR_HEIGHT, minHeight: HP_BAR_HEIGHT, flexibleWidth: 1f);

        _hpBarBgImage = barContainer.AddComponent<Image>();
        _hpBarBgImage.sprite = _spriteRounded;
        _hpBarBgImage.type   = Image.Type.Sliced;
        _hpBarBgImage.color  = HP_BAR_BG;

        // Lag fill (white trailing bar)
        _hpLagFill = CreateFilledImage("HPLagFill", barContainer.transform,
            _spriteRounded, HP_LAG_COLOR, 1f);

        // Main HP fill (gradient rounded)
        _hpFill = CreateFilledImage("HPFill", barContainer.transform,
            _spriteGradientRounded, Color.white, 1f);

        // Bright edge at the fill frontier
        _hpBrightEdge = CreateAnchoredImage("BrightEdge", barContainer.transform,
            _spriteWhite, HP_BRIGHT_EDGE);
        _hpBrightEdge.enabled = false;

        RectTransform edgeRT = _hpBrightEdge.rectTransform;
        edgeRT.anchorMin = new Vector2(0.98f, 0f);
        edgeRT.anchorMax = new Vector2(1.00f, 1f);
        edgeRT.offsetMin = Vector2.zero;
        edgeRT.offsetMax = Vector2.zero;

        // HP text overlay (centered on bar)
        _hpText = CreateTextOverlay("HPText", barContainer.transform,
            HP_FONT_SIZE, FontStyle.Normal, new Color(1f, 1f, 1f, 0.92f),
            TextAnchor.MiddleCenter);

        AddTextOutline(_hpText.gameObject, new Color(0f, 0f, 0f, 0.70f), new Vector2(1f, -1f));

        // HP percentage text (right of bar)
        var percentGO = CreateChild<RectTransform>("HPPercent", hpRow.transform);

        _hpPercentText = percentGO.AddComponent<Text>();
        _hpPercentText.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _hpPercentText.fontSize           = HP_PERCENT_FONT_SIZE;
        _hpPercentText.fontStyle          = FontStyle.Bold;
        _hpPercentText.color              = HP_PERCENT_COLOR;
        _hpPercentText.alignment          = TextAnchor.MiddleLeft;
        _hpPercentText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _hpPercentText.verticalOverflow   = VerticalWrapMode.Overflow;
        _hpPercentText.raycastTarget      = false;
        _hpPercentText.text               = "100%";

        AddTextOutline(percentGO, new Color(0f, 0f, 0f, 0.50f), new Vector2(0.8f, -0.8f));
        SetLayoutElement(percentGO, preferredWidth: 50f, minWidth: 40f, preferredHeight: HP_BAR_HEIGHT);
    }

    // ----------------------------------------------------------------
    // Toughness bar
    // ----------------------------------------------------------------

    private void BuildToughnessSection(Transform parent)
    {
        var container = CreateChild<RectTransform>("ToughnessContainer", parent);
        _toughnessRoot = container;

        SetLayoutElement(container, preferredHeight: TOUGHNESS_BAR_HEIGHT, minHeight: TOUGHNESS_BAR_HEIGHT);

        _toughnessBgImage = container.AddComponent<Image>();
        _toughnessBgImage.sprite = _spriteRounded;
        _toughnessBgImage.type   = Image.Type.Sliced;
        _toughnessBgImage.color  = TOUGHNESS_BG;

        _toughnessTransform = container.transform;

        _toughnessFillImage = CreateFilledImage("ToughnessFill", container.transform,
            _spriteRounded, TOUGHNESS_FILL, 1f);

        bool showToughness = _enemy != null && _enemy.HasToughness;
        _toughnessRoot.SetActive(showToughness);
    }

    // ----------------------------------------------------------------
    // Weakness element icons
    // ----------------------------------------------------------------

    private void BuildWeaknessIcons(Transform parent)
    {
        if (_enemy == null || _enemy.Stats == null) return;

        CharacterStats.ElementType[] weakElements = _enemy.Stats.WeakElements;
        if (weakElements == null || weakElements.Length == 0) return;

        var row = CreateChild<RectTransform>("WeaknessRow", parent);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment       = TextAnchor.MiddleCenter;
        hlg.spacing              = WEAKNESS_ICON_SPACING;
        hlg.padding              = new RectOffset(2, 2, 2, 2);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlHeight   = false;
        hlg.childControlWidth    = false;

        SetLayoutElement(row, preferredHeight: WEAKNESS_ICON_SIZE + 4f);

        foreach (CharacterStats.ElementType element in weakElements)
        {
            BuildSingleWeaknessIcon(row.transform, element);
        }
    }

    private void BuildSingleWeaknessIcon(Transform parent, CharacterStats.ElementType element)
    {
        var iconGO = CreateChild<RectTransform>($"Weak_{element}", parent);
        iconGO.GetComponent<RectTransform>().sizeDelta = new Vector2(WEAKNESS_ICON_SIZE, WEAKNESS_ICON_SIZE);

        // Dark circle background
        Image bgImage = CreateCenteredCircle("CircleBg", iconGO.transform,
            WEAKNESS_ICON_SIZE, WEAKNESS_ICON_BG);

        // Colored element dot (smaller)
        Image dotImage = CreateCenteredCircle("ElementDot", iconGO.transform,
            WEAKNESS_DOT_SIZE, GetElementColor(element));

        _weaknessIconBgs[element]   = bgImage;
        _weaknessDotImages[element] = dotImage;
    }

    // ================================================================
    // Event subscription / unsubscription
    // ================================================================

    private void SubscribeEvents()
    {
        if (_enemy == null) return;

        _enemy.OnHPChanged        += RefreshHP;
        _enemy.OnToughnessChanged += RefreshToughness;
        _enemy.OnToughnessBreak   += HandleToughnessBreak;
        _enemy.OnDeath            += HandleDeath;
    }

    private void UnsubscribeEvents()
    {
        if (_enemy == null) return;

        _enemy.OnHPChanged        -= RefreshHP;
        _enemy.OnToughnessChanged -= RefreshToughness;
        _enemy.OnToughnessBreak   -= HandleToughnessBreak;
        _enemy.OnDeath            -= HandleDeath;
    }

    // ================================================================
    // Event handlers
    // ================================================================

    private void RefreshHP(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        _targetHPRatio = ratio;

        if (_hpFill != null)
        {
            _hpFill.fillAmount = ratio;
        }

        if (_hpText != null)
        {
            _hpText.text = $"{current}/{max}";
        }

        if (_hpPercentText != null)
        {
            int percent = max > 0 ? Mathf.RoundToInt(ratio * 100f) : 0;
            _hpPercentText.text = $"{percent}%";
        }
    }

    private void RefreshToughness(int current, int max)
    {
        if (_toughnessFillImage == null) return;

        _toughnessFillImage.fillAmount = max > 0 ? (float)current / max : 0f;

        if (current > 0)
        {
            _toughnessFillImage.color = TOUGHNESS_FILL;
        }
    }

    private void HandleToughnessBreak(CharacterBattleController _)
    {
        if (_toughnessFillImage != null)
        {
            _toughnessFillImage.fillAmount = 0f;
            _toughnessFillImage.color      = TOUGHNESS_BROKEN;
        }

        CancelBreakFlash();
        _breakFlashCoroutine = StartCoroutine(AnimateBreakFlashAndShatter());
    }

    private void HandleDeath(CharacterBattleController _)
    {
        if (_canvas != null)
        {
            StartCoroutine(AnimateDeathFadeOut());
        }
    }

    // ================================================================
    // Animations (coroutines)
    // ================================================================

    /// <summary>
    /// Flashes the toughness bar background and spawns shatter fragments
    /// that fly outward from the bar, simulating a dramatic break.
    /// </summary>
    private IEnumerator AnimateBreakFlashAndShatter()
    {
        if (_toughnessBgImage == null) yield break;

        // Spawn shatter particles
        if (_toughnessTransform != null)
        {
            SpawnShatterFragments(_toughnessTransform);
        }

        Color brightFlash = new Color(1.00f, 0.80f, 0.90f, 0.95f);
        Color redFlash    = new Color(0.60f, 0.15f, 0.30f, 0.90f);
        Color normal      = TOUGHNESS_BG;

        float singleDuration = BREAK_FLASH_DURATION / BREAK_FLASH_COUNT;

        for (int i = 0; i < BREAK_FLASH_COUNT; i++)
        {
            _toughnessBgImage.color = (i == 0) ? brightFlash : redFlash;
            yield return new WaitForSeconds(singleDuration * 0.4f);

            _toughnessBgImage.color = normal;
            yield return new WaitForSeconds(singleDuration * 0.6f);
        }

        _toughnessBgImage.color = normal;
        _breakFlashCoroutine = null;
    }

    /// <summary>
    /// Spawns small fragment particles that fly outward from the toughness bar.
    /// </summary>
    private void SpawnShatterFragments(Transform barTransform)
    {
        Transform fragmentParent = barTransform.parent;

        for (int i = 0; i < SHATTER_FRAGMENT_COUNT; i++)
        {
            var fragGO = CreateChild<RectTransform>("ShatterFragment", fragmentParent);

            Image fragImage = fragGO.AddComponent<Image>();
            fragImage.sprite        = _spriteSmallSquare;
            fragImage.raycastTarget = false;

            float fragSize = UnityEngine.Random.Range(3f, 7f);

            RectTransform fragRT = fragGO.GetComponent<RectTransform>();
            fragRT.sizeDelta        = new Vector2(fragSize, fragSize);
            fragRT.anchorMin        = new Vector2(0.5f, 0.5f);
            fragRT.anchorMax        = new Vector2(0.5f, 0.5f);
            fragRT.pivot            = new Vector2(0.5f, 0.5f);
            fragRT.anchoredPosition = Vector2.zero;

            // Random outward direction
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // Pale blue-white tint
            float brightness = UnityEngine.Random.Range(0.8f, 1.0f);
            fragImage.color = new Color(brightness, brightness, 1f, 1f);

            StartCoroutine(AnimateSingleFragment(fragGO, fragRT, fragImage, direction));
        }
    }

    /// <summary>
    /// Drives a single shatter fragment: outward motion with deceleration,
    /// rotation, fade-out, shrink, then self-destruction.
    /// </summary>
    private IEnumerator AnimateSingleFragment(
        GameObject fragGO, RectTransform fragRT, Image fragImage, Vector2 direction)
    {
        float elapsed  = 0f;
        float speed    = UnityEngine.Random.Range(SHATTER_SPREAD * 0.5f, SHATTER_SPREAD);
        float rotSpeed = UnityEngine.Random.Range(-720f, 720f);
        Vector2 origin = fragRT.anchoredPosition;

        while (elapsed < SHATTER_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / SHATTER_DURATION;

            // Ease-out deceleration: 1 - (1-t)^2
            float moveT = 1f - (1f - t) * (1f - t);
            fragRT.anchoredPosition = origin + direction * (speed * moveT);

            // Rotate
            fragRT.localEulerAngles = new Vector3(0f, 0f, rotSpeed * t);

            // Fade out (quadratic)
            Color c = fragImage.color;
            c.a = Mathf.Lerp(1f, 0f, t * t);
            fragImage.color = c;

            // Shrink
            float scale = Mathf.Lerp(1f, 0.3f, t);
            fragRT.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        if (fragGO != null)
        {
            Destroy(fragGO);
        }
    }

    /// <summary>
    /// Flashes a weakness icon brighter when that element hits the enemy.
    /// Quick ramp up, then smooth fade back to original colour and scale.
    /// </summary>
    private IEnumerator AnimateWeaknessFlash(
        Image bgImage, Image dotImage, CharacterStats.ElementType element)
    {
        Color originalBg  = WEAKNESS_ICON_BG;
        Color originalDot = GetElementColor(element);
        float elapsed     = 0f;

        while (elapsed < WEAKNESS_FLASH_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / WEAKNESS_FLASH_DURATION;

            // Asymmetric envelope: fast attack (20%), slow release (80%)
            float intensity = (t < 0.2f)
                ? t / 0.2f
                : 1f - (t - 0.2f) / 0.8f;

            bgImage.color  = Color.Lerp(originalBg, WEAKNESS_FLASH, intensity);
            dotImage.color = Color.Lerp(originalDot, Color.white, intensity * 0.6f);

            // Scale pulse
            float scale = 1f + intensity * 0.2f;
            Vector3 scaleVec = new Vector3(scale, scale, 1f);
            bgImage.rectTransform.localScale  = scaleVec;
            dotImage.rectTransform.localScale = scaleVec;

            yield return null;
        }

        // Ensure clean reset
        bgImage.color  = originalBg;
        dotImage.color = originalDot;
        bgImage.rectTransform.localScale  = Vector3.one;
        dotImage.rectTransform.localScale = Vector3.one;
    }

    /// <summary>
    /// Fades the entire billboard canvas to zero alpha on death.
    /// </summary>
    private IEnumerator AnimateDeathFadeOut()
    {
        CanvasGroup cg = _canvas.gameObject.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = _canvas.gameObject.AddComponent<CanvasGroup>();
        }

        cg.alpha = 1f;
        float elapsed = 0f;

        while (elapsed < DEATH_FADE_DURATION)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(elapsed / DEATH_FADE_DURATION);
            yield return null;
        }

        cg.alpha = 0f;
        _canvas.gameObject.SetActive(false);
    }

    // ================================================================
    // Element colour mapping
    // ================================================================

    private static Color GetElementColor(CharacterStats.ElementType element)
    {
        switch (element)
        {
            case CharacterStats.ElementType.Physical:  return new Color(0.70f, 0.70f, 0.70f, 1f);
            case CharacterStats.ElementType.Fire:      return new Color(1.00f, 0.45f, 0.15f, 1f);
            case CharacterStats.ElementType.Ice:       return new Color(0.30f, 0.75f, 1.00f, 1f);
            case CharacterStats.ElementType.Lightning: return new Color(0.80f, 0.40f, 1.00f, 1f);
            case CharacterStats.ElementType.Wind:      return new Color(0.30f, 0.95f, 0.50f, 1f);
            case CharacterStats.ElementType.Dark:      return new Color(0.50f, 0.20f, 0.80f, 1f);
            default:                                   return Color.white;
        }
    }

    // ================================================================
    // UI construction utilities (reduce boilerplate)
    // ================================================================

    /// <summary>Creates a child GameObject with the specified component attached.</summary>
    private static GameObject CreateChild<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(T));
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>Stretches a RectTransform to fill its parent.</summary>
    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Adds a <see cref="LayoutElement"/> with the specified constraints.
    /// Omitted (default 0 or -1) parameters are left at Unity defaults.
    /// </summary>
    private static void SetLayoutElement(GameObject go,
        float preferredHeight = -1f, float minHeight = -1f,
        float preferredWidth = -1f, float minWidth = -1f,
        float flexibleWidth = -1f)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();

        if (preferredHeight >= 0f) le.preferredHeight = preferredHeight;
        if (minHeight >= 0f)       le.minHeight       = minHeight;
        if (preferredWidth >= 0f)  le.preferredWidth  = preferredWidth;
        if (minWidth >= 0f)        le.minWidth        = minWidth;
        if (flexibleWidth >= 0f)   le.flexibleWidth   = flexibleWidth;
    }

    /// <summary>
    /// Creates a horizontally filled Image that stretches to fill its parent.
    /// Used for HP fill, lag fill, and toughness fill.
    /// </summary>
    private static Image CreateFilledImage(string name, Transform parent,
        Sprite sprite, Color color, float initialFill)
    {
        var go = CreateChild<RectTransform>(name, parent);

        Image img = go.AddComponent<Image>();
        img.sprite        = sprite;
        img.type          = Image.Type.Filled;
        img.fillMethod    = Image.FillMethod.Horizontal;
        img.fillOrigin    = (int)Image.OriginHorizontal.Left;
        img.fillAmount    = initialFill;
        img.color         = color;
        img.raycastTarget = false;

        StretchFill(go.GetComponent<RectTransform>());
        return img;
    }

    /// <summary>
    /// Creates a non-interactive Image anchored inside its parent
    /// (anchors/offsets must be set by the caller).
    /// </summary>
    private static Image CreateAnchoredImage(string name, Transform parent,
        Sprite sprite, Color color)
    {
        var go = CreateChild<RectTransform>(name, parent);

        Image img = go.AddComponent<Image>();
        img.sprite        = sprite;
        img.color         = color;
        img.raycastTarget = false;

        return img;
    }

    /// <summary>Creates a Text overlay that stretches to fill its parent.</summary>
    private static Text CreateTextOverlay(string name, Transform parent,
        int fontSize, FontStyle style, Color color, TextAnchor alignment)
    {
        var go = CreateChild<RectTransform>(name, parent);

        Text txt = go.AddComponent<Text>();
        txt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize           = fontSize;
        txt.fontStyle          = style;
        txt.color              = color;
        txt.alignment          = alignment;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
        txt.raycastTarget      = false;
        txt.text               = string.Empty;

        StretchFill(go.GetComponent<RectTransform>());
        return txt;
    }

    /// <summary>Creates a circle Image centred inside its parent at the given size.</summary>
    private static Image CreateCenteredCircle(string name, Transform parent,
        float size, Color color)
    {
        var go = CreateChild<RectTransform>(name, parent);

        Image img = go.AddComponent<Image>();
        img.sprite        = _spriteCircle;
        img.color         = color;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;

        return img;
    }

    private static void AddTextShadow(GameObject go, Color color, Vector2 distance)
    {
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor    = color;
        shadow.effectDistance = distance;
    }

    private static void AddTextOutline(GameObject go, Color color, Vector2 distance)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor    = color;
        outline.effectDistance = distance;
    }

    /// <summary>Safely stops the break flash coroutine if one is running.</summary>
    private void CancelBreakFlash()
    {
        if (_breakFlashCoroutine != null)
        {
            StopCoroutine(_breakFlashCoroutine);
            _breakFlashCoroutine = null;
        }
    }
}
