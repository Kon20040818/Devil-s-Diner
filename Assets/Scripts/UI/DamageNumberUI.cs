// ============================================================
// DamageNumberUI.cs
// Honkai: Star Rail inspired dramatic damage number display.
// Large pop-in with EaseOutBack, element-colored text,
// weakness prefix with shake, break suffix, screen-space overlay.
// All procedural -- no asset files required.
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space overlay manager for floating damage numbers.
/// Spawns stylized, element-colored text that pops in with an overshoot bounce,
/// floats upward with a subtle scale-down for depth, and fades out
/// with an ease-in alpha curve.  Weakness hits get a gold tint, "弱点!" prefix,
/// and a short dramatic shake.  Break hits append a "BREAK!" label below.
/// </summary>
public sealed class DamageNumberUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Constants - Layout
    // ──────────────────────────────────────────────

    private const int CANVAS_SORT_ORDER = 200;

    // ──────────────────────────────────────────────
    // Constants - Normal Damage
    // ──────────────────────────────────────────────

    private const int NORMAL_FONT_SIZE = 60;
    private const float NORMAL_POP_SCALE = 2.0f;
    private const float NORMAL_FINAL_SCALE = 1.0f;
    private const float NORMAL_SHRINK_SCALE = 0.7f;
    private const float NORMAL_POP_DURATION = 0.14f;
    private const float NORMAL_FLOAT_PIXELS = 150f;
    private const float NORMAL_LIFETIME = 1.0f;
    private const float NORMAL_FADE_START = 0.45f;
    private const float NORMAL_RANDOM_X = 55f;

    // ──────────────────────────────────────────────
    // Constants - Weakness Damage
    // ──────────────────────────────────────────────

    private const int WEAKNESS_FONT_SIZE = 76;
    private const float WEAKNESS_POP_SCALE = 2.5f;
    private const float WEAKNESS_FINAL_SCALE = 1.05f;
    private const float WEAKNESS_SHRINK_SCALE = 0.75f;
    private const float WEAKNESS_POP_DURATION = 0.16f;
    private const float WEAKNESS_FLOAT_PIXELS = 180f;
    private const float WEAKNESS_LIFETIME = 1.3f;
    private const float WEAKNESS_FADE_START = 0.65f;
    private const float WEAKNESS_SHAKE_AMPLITUDE = 10f;
    private const float WEAKNESS_SHAKE_DURATION = 0.08f;
    private const float WEAKNESS_SHAKE_FREQUENCY = 55f;
    private const string WEAKNESS_PREFIX = "\u5f31\u70b9! ";

    // ──────────────────────────────────────────────
    // Constants - Break Label
    // ──────────────────────────────────────────────

    private const int BREAK_FONT_SIZE = 52;
    private const float BREAK_Y_OFFSET = -48f;

    // ──────────────────────────────────────────────
    // Constants - Outline / Shadow
    // ──────────────────────────────────────────────

    private const float OUTLINE_DISTANCE = 2.0f;
    private const float OUTER_OUTLINE_DISTANCE = 3.5f;
    private static readonly Color OUTLINE_COLOR = new Color(0f, 0f, 0f, 1f);
    private static readonly Color OUTER_OUTLINE_COLOR = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color DROP_SHADOW_COLOR = new Color(0f, 0f, 0f, 0.45f);

    // ──────────────────────────────────────────────
    // Constants - Easing
    // ──────────────────────────────────────────────

    private const float EASE_OUT_BACK_OVERSHOOT = 2.2f;

    // ──────────────────────────────────────────────
    // Constants - Element Colors (high saturation)
    // ──────────────────────────────────────────────

    private static readonly Color COLOR_PHYSICAL  = new Color(0.92f, 0.92f, 1.00f, 1f);
    private static readonly Color COLOR_FIRE      = new Color(1.00f, 0.45f, 0.08f, 1f);
    private static readonly Color COLOR_ICE       = new Color(0.30f, 0.82f, 1.00f, 1f);
    private static readonly Color COLOR_LIGHTNING  = new Color(0.80f, 0.40f, 1.00f, 1f);
    private static readonly Color COLOR_WIND      = new Color(0.30f, 1.00f, 0.50f, 1f);
    private static readonly Color COLOR_DARK      = new Color(0.55f, 0.18f, 0.90f, 1f);
    private static readonly Color COLOR_WEAKNESS  = new Color(1.00f, 0.88f, 0.15f, 1f);

    // ──────────────────────────────────────────────
    // Runtime references
    // ──────────────────────────────────────────────

    private Camera _mainCamera;
    private Canvas _overlayCanvas;
    private RectTransform _canvasRT;

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Initialize with the main camera. Builds the overlay canvas procedurally.
    /// Must be called before <see cref="SpawnDamageNumber"/>.
    /// </summary>
    public void Initialize(Camera mainCamera)
    {
        _mainCamera = mainCamera;
        BuildOverlayCanvas();
    }

    /// <summary>
    /// Spawn a floating damage number based on the given <see cref="CharacterBattleController.DamageResult"/>.
    /// The number pops in above the target, floats upward, then fades out.
    /// </summary>
    public void SpawnDamageNumber(CharacterBattleController.DamageResult result)
    {
        if (_overlayCanvas == null || _mainCamera == null) return;
        if (result.Target == null) return;

        StartCoroutine(AnimateDamageNumber(result));
    }

    // ──────────────────────────────────────────────
    // Canvas Construction
    // ──────────────────────────────────────────────

    private void BuildOverlayCanvas()
    {
        var canvasObj = new GameObject("DamageNumberCanvas");
        canvasObj.transform.SetParent(transform, false);

        _overlayCanvas = canvasObj.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.sortingOrder = CANVAS_SORT_ORDER;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // No GraphicRaycaster -- damage numbers must not intercept clicks.
        _canvasRT = canvasObj.GetComponent<RectTransform>();
    }

    // ──────────────────────────────────────────────
    // Damage Text Creation
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates the damage number hierarchy: a root RectTransform containing
    /// the main text and an optional "BREAK!" label underneath.
    /// Applies thick multi-shadow outlines for readability on any background.
    /// </summary>
    private GameObject CreateDamageText(
        CharacterBattleController.DamageResult result,
        out Text mainText,
        out Text breakText)
    {
        // -- Root container --
        var rootObj = new GameObject("DamageNumber", typeof(RectTransform));
        rootObj.transform.SetParent(_overlayCanvas.transform, false);

        var rootRT = rootObj.GetComponent<RectTransform>();
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);

        // -- Main damage text --
        mainText = CreateLabel(rootObj.transform, "MainText", Vector2.zero, new Vector2(500f, 100f));

        if (result.IsWeakness)
        {
            mainText.text = $"{WEAKNESS_PREFIX}{result.FinalDamage:N0}";
            mainText.color = COLOR_WEAKNESS;
            mainText.fontSize = WEAKNESS_FONT_SIZE;
        }
        else
        {
            mainText.text = result.FinalDamage.ToString("N0");
            mainText.color = GetElementColor(result.Element);
            mainText.fontSize = NORMAL_FONT_SIZE;
        }

        ApplyOutlines(mainText.gameObject);

        // -- Break text (shown below the main number when break occurs) --
        breakText = null;
        if (result.CausedBreak)
        {
            breakText = CreateLabel(
                rootObj.transform,
                "BreakText",
                new Vector2(0f, BREAK_Y_OFFSET),
                new Vector2(400f, 60f));

            breakText.text = "BREAK!";
            breakText.fontSize = BREAK_FONT_SIZE;
            breakText.color = COLOR_WEAKNESS;
            ApplyOutlines(breakText.gameObject);
        }

        return rootObj;
    }

    /// <summary>
    /// Helper: creates a centered, bold, overflow-enabled Text label
    /// using the built-in legacy font.
    /// </summary>
    private static Text CreateLabel(
        Transform parent,
        string name,
        Vector2 anchoredPos,
        Vector2 sizeDelta)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        var text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.fontStyle = FontStyle.Bold;

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        return text;
    }

    /// <summary>
    /// Applies a single thick Outline plus a drop Shadow for readable text.
    /// Kept minimal to avoid exceeding uGUI's 65000 vertex limit when
    /// multiple damage numbers are displayed simultaneously.
    /// </summary>
    private static void ApplyOutlines(GameObject textObj)
    {
        // Single thick outline — covers all directions uniformly.
        AddOutline(textObj, new Vector2(OUTLINE_DISTANCE, OUTLINE_DISTANCE), OUTLINE_COLOR);
        AddOutline(textObj, new Vector2(-OUTLINE_DISTANCE, -OUTLINE_DISTANCE), OUTLINE_COLOR);

        // Drop shadow for depth.
        var shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = DROP_SHADOW_COLOR;
        shadow.effectDistance = new Vector2(3f, -4f);
    }

    private static void AddOutline(GameObject obj, Vector2 distance, Color color)
    {
        var outline = obj.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    // ──────────────────────────────────────────────
    // Animation
    // ──────────────────────────────────────────────

    /// <summary>
    /// Main animation coroutine.
    /// Phases: pop-in scale (EaseOutBack) -> optional weakness shake ->
    /// float upward with gradual scale-down -> ease-in alpha fade -> destroy.
    /// </summary>
    private IEnumerator AnimateDamageNumber(CharacterBattleController.DamageResult result)
    {
        var rootObj = CreateDamageText(result, out Text mainText, out Text breakText);
        var rootRT = rootObj.GetComponent<RectTransform>();

        bool isWeakness = result.IsWeakness;

        // Select parameter set.
        float popScale = isWeakness ? WEAKNESS_POP_SCALE : NORMAL_POP_SCALE;
        float finalScale = isWeakness ? WEAKNESS_FINAL_SCALE : NORMAL_FINAL_SCALE;
        float shrinkScale = isWeakness ? WEAKNESS_SHRINK_SCALE : NORMAL_SHRINK_SCALE;
        float popDuration = isWeakness ? WEAKNESS_POP_DURATION : NORMAL_POP_DURATION;
        float floatPixels = isWeakness ? WEAKNESS_FLOAT_PIXELS : NORMAL_FLOAT_PIXELS;
        float lifetime = isWeakness ? WEAKNESS_LIFETIME : NORMAL_LIFETIME;
        float fadeStart = isWeakness ? WEAKNESS_FADE_START : NORMAL_FADE_START;
        float randomXRange = isWeakness ? NORMAL_RANDOM_X * 0.4f : NORMAL_RANDOM_X;

        // Cache original alpha values for outline/shadow fading.
        float[] mainOutlineAlphas = CacheEffectAlphas(mainText);
        float[] breakOutlineAlphas = breakText != null ? CacheEffectAlphas(breakText) : null;

        // World position: above target's head.
        Transform targetTransform = result.Target.transform;
        Vector3 worldBase = targetTransform.position + new Vector3(0f, 2.0f, 0f);

        // Random horizontal offset to prevent stacking.
        float randomX = Random.Range(-randomXRange, randomXRange);

        // Convert world -> screen -> canvas-local.
        Vector3 baseScreenPos = _mainCamera.WorldToScreenPoint(worldBase);
        if (baseScreenPos.z < 0f)
        {
            Destroy(rootObj);
            yield break;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, baseScreenPos, null, out Vector2 baseLocalPoint);
        baseLocalPoint.x += randomX;

        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            // ── Position: ease-out float upward ──
            // Use a sqrt curve so movement decelerates over time.
            float floatT = Mathf.Sqrt(t);
            float yOffset = floatPixels * floatT;
            Vector2 currentPos = new Vector2(baseLocalPoint.x, baseLocalPoint.y + yOffset);

            // ── Weakness shake: dramatic but short ──
            if (isWeakness && elapsed < WEAKNESS_SHAKE_DURATION)
            {
                float shakeT = elapsed / WEAKNESS_SHAKE_DURATION;
                // Rapid decay envelope.
                float envelope = (1f - shakeT) * (1f - shakeT);
                float shakeX = Mathf.Sin(elapsed * WEAKNESS_SHAKE_FREQUENCY) * WEAKNESS_SHAKE_AMPLITUDE * envelope;
                float shakeY = Mathf.Cos(elapsed * WEAKNESS_SHAKE_FREQUENCY * 1.3f) * WEAKNESS_SHAKE_AMPLITUDE * 0.5f * envelope;
                currentPos.x += shakeX;
                currentPos.y += shakeY;
            }

            rootRT.anchoredPosition = currentPos;

            // ── Scale: pop-in with EaseOutBack, then subtle shrink for depth ──
            float currentScale;
            if (elapsed < popDuration)
            {
                float popT = elapsed / popDuration;
                float easedT = EaseOutBack(popT);
                currentScale = Mathf.LerpUnclamped(popScale, finalScale, easedT);
            }
            else
            {
                // Gradual scale-down from finalScale toward shrinkScale.
                float postPopT = (elapsed - popDuration) / (lifetime - popDuration);
                currentScale = Mathf.Lerp(finalScale, shrinkScale, postPopT * postPopT);
            }
            rootRT.localScale = Vector3.one * currentScale;

            // ── Fade: ease-in alpha (slow start, accelerating to zero) ──
            if (elapsed > fadeStart)
            {
                float fadeT = (elapsed - fadeStart) / (lifetime - fadeStart);
                // Quadratic ease-in: fade accelerates toward the end.
                float alpha = 1f - fadeT * fadeT;

                SetTextAlpha(mainText, alpha, mainOutlineAlphas);
                if (breakText != null)
                {
                    SetTextAlpha(breakText, alpha, breakOutlineAlphas);
                }
            }

            yield return null;
        }

        Destroy(rootObj);
    }

    // ──────────────────────────────────────────────
    // Easing Functions
    // ──────────────────────────────────────────────

    /// <summary>
    /// EaseOutBack: overshoots past the target then settles back.
    /// Uses a configurable overshoot amount for a punchier pop-in.
    /// <paramref name="t"/> should be in [0, 1].
    /// </summary>
    private static float EaseOutBack(float t)
    {
        float c1 = EASE_OUT_BACK_OVERSHOOT;
        float c3 = c1 + 1f;
        float tm1 = t - 1f;
        return 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1;
    }

    // ──────────────────────────────────────────────
    // Utility
    // ──────────────────────────────────────────────

    /// <summary>
    /// Caches the original alpha values of all Shadow/Outline effects on a Text component
    /// so that fading can scale them proportionally without exceeding their design values.
    /// Returns array ordered: [text alpha, effect0 alpha, effect1 alpha, ...].
    /// </summary>
    private static float[] CacheEffectAlphas(Text text)
    {
        var shadows = text.GetComponents<Shadow>();
        float[] alphas = new float[shadows.Length + 1];
        alphas[0] = text.color.a;
        for (int i = 0; i < shadows.Length; i++)
        {
            alphas[i + 1] = shadows[i].effectColor.a;
        }
        return alphas;
    }

    /// <summary>
    /// Sets the alpha of a Text component and all its Shadow/Outline effects,
    /// scaling each proportionally to its cached original alpha.
    /// </summary>
    private static void SetTextAlpha(Text text, float alpha, float[] originalAlphas)
    {
        if (text == null) return;

        Color c = text.color;
        c.a = originalAlphas[0] * alpha;
        text.color = c;

        var shadows = text.GetComponents<Shadow>();
        for (int i = 0; i < shadows.Length; i++)
        {
            Color sc = shadows[i].effectColor;
            sc.a = originalAlphas[i + 1] * alpha;
            shadows[i].effectColor = sc;
        }
    }

    // ──────────────────────────────────────────────
    // Element Color Mapping
    // ──────────────────────────────────────────────

    /// <summary>Returns the display color for a given element type.</summary>
    private static Color GetElementColor(CharacterStats.ElementType element)
    {
        switch (element)
        {
            case CharacterStats.ElementType.Physical:  return COLOR_PHYSICAL;
            case CharacterStats.ElementType.Fire:      return COLOR_FIRE;
            case CharacterStats.ElementType.Ice:       return COLOR_ICE;
            case CharacterStats.ElementType.Lightning: return COLOR_LIGHTNING;
            case CharacterStats.ElementType.Wind:      return COLOR_WIND;
            case CharacterStats.ElementType.Dark:      return COLOR_DARK;
            default:                                   return COLOR_PHYSICAL;
        }
    }
}
