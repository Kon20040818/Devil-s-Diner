// ============================================================
// ActionTimelineUI.cs
// 画面左上に行動順を縦型リストで表示するUI（Honkai: Star Rail準拠）。
// アクティブキャラ（先頭）はゴールド枠＋グロー強調、
// 後続キャラはポートレート風アイコンで陣営カラー枠。
// 上→下の順で行動順を可視化。すべてプロシージャル生成。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Honkai: Star Rail準拠の行動順タイムラインUI。
/// 画面左上に縦型リストとしてキャラアイコンを表示する。
/// リスト先頭（最上部）が次に行動するキャラ。
/// アクティブキャラ（index 0）はゴールド枠 + パルスグロー。
/// 後続キャラは陣営カラー枠（プレイヤー=シアン / 敵=クリムゾン）。
/// </summary>
public sealed class ActionTimelineUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // カラー定数（Star Rail パレット）
    // ──────────────────────────────────────────────

    private static readonly Color PANEL_BG         = new Color(0.02f, 0.03f, 0.08f, 0.55f);
    private static readonly Color PANEL_BORDER     = new Color(0.12f, 0.20f, 0.45f, 0.25f);
    private static readonly Color ACTIVE_GOLD      = new Color(1.0f, 0.85f, 0.20f, 1.0f);
    private static readonly Color ACTIVE_GLOW      = new Color(1.0f, 0.80f, 0.15f, 0.45f);
    private static readonly Color PLAYER_CYAN      = new Color(0.0f, 0.88f, 1.0f, 0.90f);
    private static readonly Color ENEMY_RED        = new Color(0.95f, 0.22f, 0.22f, 0.90f);
    private static readonly Color TEXT_WHITE       = new Color(1f, 1f, 1f, 0.92f);
    private static readonly Color TEXT_SHADOW      = new Color(0f, 0f, 0f, 0.60f);
    private static readonly Color ICON_BG_PLAYER   = new Color(0.04f, 0.10f, 0.20f, 0.92f);
    private static readonly Color ICON_BG_ENEMY    = new Color(0.20f, 0.04f, 0.04f, 0.92f);
    private static readonly Color ACTIVE_ICON_BG   = new Color(0.14f, 0.11f, 0.02f, 0.95f);
    private static readonly Color ARROW_COLOR      = new Color(1.0f, 0.85f, 0.25f, 0.75f);

    // ──────────────────────────────────────────────
    // レイアウト定数（左側・縦型リスト）
    // ──────────────────────────────────────────────

    private const int MAX_VISIBLE       = 10;

    // パネル位置（左上）
    private const float PANEL_X         = 20f;
    private const float PANEL_Y         = -80f;
    private const float PANEL_WIDTH     = 110f;
    private const float PANEL_PADDING   = 10f;
    private const float PANEL_CORNER_R  = 12f;

    // アイコンサイズ
    private const float ACTIVE_SIZE     = 88f;
    private const float QUEUE_SIZE      = 70f;
    private const float ICON_SPACING    = 8f;
    private const float GLOW_EXPAND     = 8f;

    // テキスト
    private const float INITIAL_FONT    = 16f;

    // アクティブ強調（右側の三角マーカー）
    private const float ACTIVE_ARROW_SIZE = 8f;

    // ── アニメーション ──
    private const float ANIM_SPEED       = 12f;
    private const float GLOW_PULSE_SPEED = 2.5f;
    private const float GLOW_PULSE_MIN   = 0.30f;
    private const float GLOW_PULSE_MAX   = 0.80f;

    // ──────────────────────────────────────────────
    // 参照
    // ──────────────────────────────────────────────

    private BattleManager _battleManager;
    private Canvas _canvas;
    private RectTransform _panelRect;
    private Image _panelBgImage;

    // ──────────────────────────────────────────────
    // エントリ管理
    // ──────────────────────────────────────────────

    private readonly List<TimelineEntry> _entries = new List<TimelineEntry>();
    private readonly List<CharacterBattleController> _prevOrder = new List<CharacterBattleController>();

    // ──────────────────────────────────────────────
    // テクスチャキャッシュ
    // ──────────────────────────────────────────────

    private Sprite _circleSprite;
    private Sprite _circleFillSprite;
    private Sprite _roundedRectSprite;
    private Sprite _glowCircleSprite;

    // ──────────────────────────────────────────────
    // 内部クラス
    // ──────────────────────────────────────────────

    private sealed class TimelineEntry
    {
        public RectTransform Root;
        public CanvasGroup Group;
        public Image FrameImage;
        public Image GlowImage;
        public Image IconBgImage;
        public Text InitialLabel;
        public Image ArrowImage;
        public CharacterBattleController Character;
        public bool IsActive;
        // アニメーション用（縦方向）
        public float TargetY;
        public float CurrentY;
        public float TargetAlpha;
        public float CurrentAlpha;
        public float TargetScale;
        public float CurrentScale;
    }

    // ════════════════════════════════════════════════
    // 公開 API
    // ════════════════════════════════════════════════

    public void Initialize(BattleManager battleManager)
    {
        _battleManager = battleManager;
        GenerateSprites();
        BuildUI();

        if (_battleManager != null && _battleManager.Queue != null)
            _battleManager.Queue.OnQueueUpdated += OnQueueUpdated;

        Refresh();
    }

    public void Refresh()
    {
        if (_battleManager == null || _battleManager.Queue == null) return;
        var order = _battleManager.Queue.GetOrderPreview(MAX_VISIBLE);
        UpdateEntries(order);
    }

    // ──────────────────────────────────────────────
    // MonoBehaviour
    // ──────────────────────────────────────────────

    private void Update()
    {
        AnimateEntries();
        PulseActiveGlow();
    }

    private void OnDestroy()
    {
        if (_battleManager != null && _battleManager.Queue != null)
            _battleManager.Queue.OnQueueUpdated -= OnQueueUpdated;
        DestroySprites();
    }

    // ════════════════════════════════════════════════
    // UI 構築
    // ════════════════════════════════════════════════

    private void BuildUI()
    {
        // ── Canvas（オーバーライドCanvas） ──
        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 100;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (!gameObject.TryGetComponent<GraphicRaycaster>(out _))
            gameObject.AddComponent<GraphicRaycaster>();

        // ── パネル背景（画面左上、縦長） ──
        _panelRect = MakeRect("TimelinePanel", transform);
        _panelRect.anchorMin = new Vector2(0f, 1f);
        _panelRect.anchorMax = new Vector2(0f, 1f);
        _panelRect.pivot = new Vector2(0f, 1f);
        _panelRect.anchoredPosition = new Vector2(PANEL_X, PANEL_Y);
        // 高さは動的にエントリ数から決定、初期値
        _panelRect.sizeDelta = new Vector2(PANEL_WIDTH, 400f);

        _panelBgImage = _panelRect.gameObject.AddComponent<Image>();
        _panelBgImage.sprite = _roundedRectSprite;
        _panelBgImage.type = Image.Type.Sliced;
        _panelBgImage.color = PANEL_BG;
        _panelBgImage.raycastTarget = false;

        // ── ボーダー ──
        var borderRect = MakeRect("PanelBorder", _panelRect);
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-1f, -1f);
        borderRect.offsetMax = new Vector2(1f, 1f);
        var borderImg = borderRect.gameObject.AddComponent<Image>();
        borderImg.sprite = _roundedRectSprite;
        borderImg.type = Image.Type.Sliced;
        borderImg.color = PANEL_BORDER;
        borderImg.raycastTarget = false;
        borderRect.SetAsFirstSibling();
    }

    // ════════════════════════════════════════════════
    // エントリ更新
    // ════════════════════════════════════════════════

    private void UpdateEntries(List<CharacterBattleController> order)
    {
        int count = Mathf.Min(order.Count, MAX_VISIBLE);

        // ── エントリ数を合わせる ──
        while (_entries.Count < count)
            _entries.Add(CreateEntry(_entries.Count));
        while (_entries.Count > count)
        {
            int last = _entries.Count - 1;
            DestroyEntry(_entries[last]);
            _entries.RemoveAt(last);
        }

        // ── パネル高さを計算（縦型） ──
        float totalHeight = PANEL_PADDING;
        for (int i = 0; i < count; i++)
        {
            float sz = (i == 0) ? ACTIVE_SIZE : QUEUE_SIZE;
            totalHeight += sz;
            if (i < count - 1) totalHeight += ICON_SPACING;
        }
        totalHeight += PANEL_PADDING;
        _panelRect.sizeDelta = new Vector2(PANEL_WIDTH, totalHeight);

        // ── 各エントリを配置（上から下へ） ──
        float yCursor = -PANEL_PADDING;
        float panelCenterX = PANEL_WIDTH * 0.5f;

        for (int i = 0; i < count; i++)
        {
            bool isActive = (i == 0);
            float sz = isActive ? ACTIVE_SIZE : QUEUE_SIZE;
            float centerY = yCursor - sz * 0.5f;

            var entry = _entries[i];
            var character = order[i];
            bool changed = entry.Character != character;

            entry.Character = character;
            entry.IsActive = isActive;

            // ── サイズ ──
            entry.Root.sizeDelta = new Vector2(sz, sz);

            // ── フレーム ──
            entry.FrameImage.sprite = _circleSprite;
            entry.FrameImage.rectTransform.sizeDelta = new Vector2(sz, sz);
            Color factionColor = GetFactionColor(character);
            entry.FrameImage.color = isActive ? ACTIVE_GOLD : factionColor;

            // ── グロー ──
            float glowSz = sz + GLOW_EXPAND * 2f;
            entry.GlowImage.rectTransform.sizeDelta = new Vector2(glowSz, glowSz);
            entry.GlowImage.gameObject.SetActive(isActive);
            entry.GlowImage.color = ACTIVE_GLOW;

            // ── アイコン背景 ──
            Color iconBg = isActive ? ACTIVE_ICON_BG
                : (character.CharacterFaction == CharacterBattleController.Faction.Player
                    ? ICON_BG_PLAYER : ICON_BG_ENEMY);
            entry.IconBgImage.color = iconBg;
            float bgSz = sz - 4f;
            entry.IconBgImage.rectTransform.sizeDelta = new Vector2(bgSz, bgSz);

            // ── イニシャル ──
            string displayName = character.DisplayName;
            string initial = !string.IsNullOrEmpty(displayName)
                ? displayName.Substring(0, 1) : "?";
            entry.InitialLabel.text = initial;
            entry.InitialLabel.fontSize = isActive ? (int)(INITIAL_FONT * 1.1f) : (int)INITIAL_FONT;
            entry.InitialLabel.color = isActive ? ACTIVE_GOLD : TEXT_WHITE;
            entry.InitialLabel.rectTransform.sizeDelta = new Vector2(sz, sz);

            // ── アクティブ矢印（► 右に表示） ──
            if (entry.ArrowImage != null)
            {
                entry.ArrowImage.gameObject.SetActive(isActive);
            }

            // ── ポジション（縦方向） ──
            float targetY = centerY;
            entry.TargetY = targetY;
            entry.TargetAlpha = 1f;
            entry.TargetScale = 1f;

            if (changed && _prevOrder.Count > 0)
            {
                int prevIdx = _prevOrder.IndexOf(character);
                if (prevIdx >= 0)
                {
                    float prevY = CalcYForIndex(prevIdx, _prevOrder.Count);
                    entry.CurrentY = prevY;
                }
                else
                {
                    entry.CurrentY = targetY;
                    entry.CurrentAlpha = 0f;
                    entry.CurrentScale = 0.5f;
                }
            }
            else if (_prevOrder.Count == 0)
            {
                entry.CurrentY = targetY;
                entry.CurrentAlpha = 1f;
                entry.CurrentScale = 1f;
            }

            entry.Root.anchoredPosition = new Vector2(panelCenterX, entry.CurrentY);

            yCursor -= sz + ICON_SPACING;
        }

        _prevOrder.Clear();
        _prevOrder.AddRange(order);
        ApplyPositions();
    }

    private float CalcYForIndex(int index, int total)
    {
        float y = -PANEL_PADDING;
        for (int i = 0; i <= index && i < total && i < MAX_VISIBLE; i++)
        {
            float sz = (i == 0) ? ACTIVE_SIZE : QUEUE_SIZE;
            if (i == index) return y - sz * 0.5f;
            y -= sz + ICON_SPACING;
        }
        return 0f;
    }

    // ════════════════════════════════════════════════
    // アニメーション
    // ════════════════════════════════════════════════

    private void AnimateEntries()
    {
        float dt = Time.unscaledDeltaTime;

        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];

            e.CurrentY = Mathf.Lerp(e.CurrentY, e.TargetY, dt * ANIM_SPEED);
            if (Mathf.Abs(e.CurrentY - e.TargetY) < 0.3f) e.CurrentY = e.TargetY;

            e.CurrentAlpha = Mathf.Lerp(e.CurrentAlpha, e.TargetAlpha, dt * ANIM_SPEED * 0.8f);
            if (Mathf.Abs(e.CurrentAlpha - e.TargetAlpha) < 0.01f) e.CurrentAlpha = e.TargetAlpha;

            e.CurrentScale = Mathf.Lerp(e.CurrentScale, e.TargetScale, dt * ANIM_SPEED * 0.8f);
            if (Mathf.Abs(e.CurrentScale - e.TargetScale) < 0.01f) e.CurrentScale = e.TargetScale;
        }

        ApplyPositions();
    }

    private void ApplyPositions()
    {
        float panelCenterX = PANEL_WIDTH * 0.5f;

        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            e.Root.anchoredPosition = new Vector2(panelCenterX, e.CurrentY);
            e.Group.alpha = e.CurrentAlpha;
            e.Root.localScale = Vector3.one * e.CurrentScale;
        }
    }

    private void PulseActiveGlow()
    {
        if (_entries.Count == 0) return;
        var a = _entries[0];
        if (!a.IsActive || a.GlowImage == null) return;

        float t = (Mathf.Sin(Time.unscaledTime * GLOW_PULSE_SPEED) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(GLOW_PULSE_MIN, GLOW_PULSE_MAX, t);
        Color c = ACTIVE_GLOW;
        c.a = alpha;
        a.GlowImage.color = c;

        float brightness = Mathf.Lerp(0.88f, 1.0f, t);
        a.FrameImage.color = new Color(
            ACTIVE_GOLD.r * brightness,
            ACTIVE_GOLD.g * brightness,
            ACTIVE_GOLD.b * brightness,
            ACTIVE_GOLD.a);
    }

    // ════════════════════════════════════════════════
    // エントリ生成/破棄
    // ════════════════════════════════════════════════

    private TimelineEntry CreateEntry(int index)
    {
        var entry = new TimelineEntry();

        // ── Root（パネル左上原点、ピボット中央） ──
        entry.Root = MakeRect($"E{index}", _panelRect);
        entry.Root.anchorMin = new Vector2(0f, 1f);
        entry.Root.anchorMax = new Vector2(0f, 1f);
        entry.Root.pivot = new Vector2(0.5f, 0.5f);
        entry.Root.sizeDelta = new Vector2(QUEUE_SIZE, QUEUE_SIZE);

        entry.Group = entry.Root.gameObject.AddComponent<CanvasGroup>();
        entry.Group.interactable = false;
        entry.Group.blocksRaycasts = false;

        // ── Glow ──
        var glowR = MakeRect("Glow", entry.Root);
        CenterAnchor(glowR, Vector2.zero);
        glowR.sizeDelta = new Vector2(QUEUE_SIZE + GLOW_EXPAND * 2f, QUEUE_SIZE + GLOW_EXPAND * 2f);
        entry.GlowImage = glowR.gameObject.AddComponent<Image>();
        entry.GlowImage.sprite = _glowCircleSprite;
        entry.GlowImage.color = ACTIVE_GLOW;
        entry.GlowImage.raycastTarget = false;
        entry.GlowImage.gameObject.SetActive(false);

        // ── Frame ──
        var frameR = MakeRect("Frame", entry.Root);
        CenterAnchor(frameR, Vector2.zero);
        frameR.sizeDelta = new Vector2(QUEUE_SIZE, QUEUE_SIZE);
        entry.FrameImage = frameR.gameObject.AddComponent<Image>();
        entry.FrameImage.sprite = _circleSprite;
        entry.FrameImage.color = PLAYER_CYAN;
        entry.FrameImage.raycastTarget = false;

        // ── Icon BG ──
        var bgR = MakeRect("IconBg", entry.Root);
        CenterAnchor(bgR, Vector2.zero);
        bgR.sizeDelta = new Vector2(QUEUE_SIZE - 4f, QUEUE_SIZE - 4f);
        entry.IconBgImage = bgR.gameObject.AddComponent<Image>();
        entry.IconBgImage.sprite = _circleFillSprite;
        entry.IconBgImage.color = ICON_BG_PLAYER;
        entry.IconBgImage.raycastTarget = false;

        // ── Initial ──
        var initR = MakeRect("Init", entry.Root);
        CenterAnchor(initR, Vector2.zero);
        initR.sizeDelta = new Vector2(QUEUE_SIZE, QUEUE_SIZE);
        entry.InitialLabel = initR.gameObject.AddComponent<Text>();
        entry.InitialLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        entry.InitialLabel.fontSize = (int)INITIAL_FONT;
        entry.InitialLabel.fontStyle = FontStyle.Bold;
        entry.InitialLabel.alignment = TextAnchor.MiddleCenter;
        entry.InitialLabel.color = TEXT_WHITE;
        entry.InitialLabel.raycastTarget = false;
        entry.InitialLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        entry.InitialLabel.verticalOverflow = VerticalWrapMode.Overflow;

        // テキストシャドウ
        var initShadow = initR.gameObject.AddComponent<Shadow>();
        initShadow.effectColor = TEXT_SHADOW;
        initShadow.effectDistance = new Vector2(1f, -1f);

        // ── アクティブ矢印（アイコン右に ► 表示） ──
        var arrowR = MakeRect("Arrow", entry.Root);
        arrowR.anchorMin = new Vector2(1f, 0.5f);
        arrowR.anchorMax = new Vector2(1f, 0.5f);
        arrowR.pivot = new Vector2(0f, 0.5f);
        arrowR.anchoredPosition = new Vector2(2f, 0f);
        arrowR.sizeDelta = new Vector2(ACTIVE_ARROW_SIZE, ACTIVE_ARROW_SIZE);
        entry.ArrowImage = arrowR.gameObject.AddComponent<Image>();
        entry.ArrowImage.sprite = _circleFillSprite;
        entry.ArrowImage.color = ARROW_COLOR;
        entry.ArrowImage.raycastTarget = false;
        entry.ArrowImage.gameObject.SetActive(false);

        // ── 初期状態 ──
        entry.CurrentAlpha = 0f;
        entry.TargetAlpha = 1f;
        entry.CurrentScale = 0.5f;
        entry.TargetScale = 1f;

        return entry;
    }

    private void DestroyEntry(TimelineEntry entry)
    {
        if (entry.Root != null) Destroy(entry.Root.gameObject);
    }

    // ════════════════════════════════════════════════
    // プロシージャルスプライト
    // ════════════════════════════════════════════════

    private void GenerateSprites()
    {
        _circleSprite = MakeCircleSprite(64, Color.white, 2.5f);
        _circleFillSprite = MakeCircleSprite(64, Color.white, 0f);
        _roundedRectSprite = MakeRoundedRectSprite(64, 64, (int)PANEL_CORNER_R);
        _glowCircleSprite = MakeGlowCircleSprite(80, Color.white, 14f);
    }

    private void DestroySprites()
    {
        DestroySpr(_circleSprite);
        DestroySpr(_circleFillSprite);
        DestroySpr(_roundedRectSprite);
        DestroySpr(_glowCircleSprite);
    }

    private void DestroySpr(Sprite s)
    {
        if (s == null) return;
        if (s.texture != null) Destroy(s.texture);
        Destroy(s);
    }

    private Sprite MakeCircleSprite(int size, Color col, float border)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float c = size * 0.5f;
        float oR = c - 1f;
        float iR = oR - border;
        var px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c + 0.5f;
                float dy = y - c + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a;
                if (border > 0f)
                {
                    float outerEdge = 1f - Mathf.Clamp01(d - oR);
                    float innerEdge = Mathf.Clamp01(d - iR);
                    a = Mathf.Min(outerEdge, innerEdge);
                }
                else
                {
                    a = 1f - Mathf.Clamp01(d - oR);
                }
                px[y * size + x] = new Color(col.r, col.g, col.b, col.a * a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite MakeRoundedRectSprite(int w, int h, int r)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var px = new Color[w * h];
        float rf = r;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(rf - x, x - (w - 1 - rf)));
                float dy = Mathf.Max(0f, Mathf.Max(rf - y, y - (h - 1 - rf)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(rf - d + 0.5f);
                px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        int b = r + 2;
        return Sprite.Create(tex, new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    private Sprite MakeGlowCircleSprite(int size, Color col, float glowR)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float c = size * 0.5f;
        float core = c - glowR;
        var px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c + 0.5f;
                float dy = y - c + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a;
                if (d <= core) a = 0.7f;
                else if (d <= c)
                {
                    float t = (d - core) / glowR;
                    a = 0.7f * (1f - t * t);
                }
                else a = 0f;

                px[y * size + x] = new Color(col.r, col.g, col.b, col.a * a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ════════════════════════════════════════════════
    // ユーティリティ
    // ════════════════════════════════════════════════

    private Color GetFactionColor(CharacterBattleController character)
    {
        if (character == null) return TEXT_WHITE;
        return character.CharacterFaction == CharacterBattleController.Faction.Player
            ? PLAYER_CYAN : ENEMY_RED;
    }

    private RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void CenterAnchor(RectTransform rt, Vector2 offset)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
    }

    private void OnQueueUpdated()
    {
        Refresh();
    }
}
