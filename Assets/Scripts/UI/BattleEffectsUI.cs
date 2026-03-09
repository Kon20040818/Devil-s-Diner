// ============================================================
// BattleEffectsUI.cs
// バトル中のシネマティック演出エフェクト。
// 崩壊スターレイル風: バトル開始ワイプ、勝利/敗北オーバーレイ、
// 必殺技フラッシュ+レターボックス、靭性破壊フラッシュ、ターン開始パルス。
// カメラ連携演出: 必殺技カットイン、スキル名表示。
// Screen Space - Overlay Canvas を自前で生成し、sort order 150 で描画。
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// バトル用フルスクリーン演出UI。
/// バトル開始ワイプ、勝利/敗北エフェクト、各種フラッシュ、
/// カメラ連携カットインを提供する。全要素をコードのみで手続き的に生成。
/// </summary>
public sealed class BattleEffectsUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数 -- キャンバス
    // ──────────────────────────────────────────────

    private const int CANVAS_SORT_ORDER = 150;
    private const float REFERENCE_WIDTH = 1920f;
    private const float REFERENCE_HEIGHT = 1080f;

    // ──────────────────────────────────────────────
    // 定数 -- カラーパレット
    // ──────────────────────────────────────────────

    // バトル開始
    private static readonly Color BATTLE_START_BG        = Color.black;
    private static readonly Color TEXT_BATTLE_START       = Color.white;
    private static readonly Color OUTLINE_GOLD           = new Color(1.0f, 0.84f, 0.0f, 1.0f);
    private static readonly Color OUTLINE_GOLD_OUTER     = new Color(0.55f, 0.4f, 0.0f, 0.8f);
    private static readonly Color OUTLINE_GOLD_SHADOW    = new Color(0.25f, 0.15f, 0.0f, 0.5f);

    // 勝利
    private static readonly Color VICTORY_OVERLAY        = new Color(1.0f, 0.85f, 0.15f, 0.35f);
    private static readonly Color TEXT_VICTORY            = new Color(1.0f, 0.88f, 0.15f, 1.0f);
    private static readonly Color VICTORY_OUTLINE_INNER  = new Color(0.7f, 0.5f, 0.0f, 1.0f);
    private static readonly Color VICTORY_OUTLINE_OUTER  = new Color(0.3f, 0.15f, 0.0f, 0.7f);
    private static readonly Color VICTORY_OUTLINE_SHADOW = new Color(0.1f, 0.05f, 0.0f, 0.5f);

    // 敗北
    private static readonly Color DEFEAT_OVERLAY         = new Color(0.25f, 0.02f, 0.02f, 0.8f);
    private static readonly Color TEXT_DEFEAT             = new Color(0.85f, 0.15f, 0.15f, 1.0f);
    private static readonly Color DEFEAT_OUTLINE_INNER   = new Color(0.15f, 0.0f, 0.0f, 0.8f);
    private static readonly Color DEFEAT_OUTLINE_OUTER   = new Color(0.05f, 0.0f, 0.0f, 0.5f);

    // フラッシュ
    private static readonly Color ULTIMATE_FLASH_COLOR   = new Color(1.0f, 0.92f, 0.5f, 1.0f);
    private const float ULTIMATE_FLASH_PEAK_ALPHA        = 0.45f;
    private static readonly Color BREAK_FLASH_COLOR      = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    private const float BREAK_FLASH_PEAK_ALPHA           = 0.5f;
    private static readonly Color TURN_FLASH_COLOR       = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    private const float TURN_FLASH_PEAK_ALPHA            = 0.06f;

    // レターボックス
    private static readonly Color LETTERBOX_COLOR        = Color.black;

    // カットイン
    private static readonly Color CUTIN_GOLD             = new Color(1.0f, 0.85f, 0.2f, 1.0f);
    private static readonly Color CUTIN_SLASH_COLOR      = new Color(1.0f, 0.88f, 0.3f, 0.95f);
    private static readonly Color CUTIN_BG_COLOR         = new Color(0.0f, 0.0f, 0.0f, 0.6f);

    // スキル名
    private static readonly Color SKILL_NAME_COLOR       = new Color(1.0f, 1.0f, 1.0f, 0.9f);

    // ビネット
    private static readonly Color VIGNETTE_COLOR         = new Color(0f, 0f, 0f, 0.5f);

    // 紙吹雪カラー
    private static readonly Color[] CONFETTI_COLORS =
    {
        new Color(1.0f, 0.85f, 0.2f, 1.0f),
        new Color(1.0f, 0.35f, 0.35f, 1.0f),
        new Color(0.35f, 0.75f, 1.0f, 1.0f),
        new Color(0.35f, 1.0f, 0.5f, 1.0f),
        new Color(1.0f, 0.55f, 0.85f, 1.0f),
        new Color(1.0f, 1.0f, 1.0f, 0.9f),
    };

    // ──────────────────────────────────────────────
    // 定数 -- タイミング
    // ──────────────────────────────────────────────

    // Battle Start (~3.2s total)
    private const float WIPE_HOLD_DURATION        = 0.25f;
    private const float WIPE_DURATION             = 0.7f;
    private const float TEXT_SLIDE_DURATION        = 0.45f;
    private const float TEXT_HOLD_DURATION         = 1.3f;
    private const float TEXT_FADE_OUT_DURATION     = 0.5f;
    private const float TEXT_SLIDE_START_X         = -800f;
    private const int   SPEED_LINE_COUNT          = 18;

    // Victory (~3.5s total)
    private const float VICTORY_OVERLAY_FADE_IN   = 0.7f;
    private const float VICTORY_TEXT_SCALE_TIME   = 0.9f;
    private const float VICTORY_HOLD_DURATION     = 1.8f;
    private const float VICTORY_SPARKLE_BURST     = 0.3f;
    private const int   CONFETTI_COUNT            = 50;

    // Defeat (~3.2s total)
    private const float DEFEAT_OVERLAY_FADE_IN    = 1.2f;
    private const float DEFEAT_TEXT_FADE_IN        = 0.6f;
    private const float DEFEAT_HOLD_DURATION       = 1.4f;
    private const float DEFEAT_SHAKE_AMOUNT        = 4f;
    private const int   CRACK_LINE_COUNT          = 16;

    // Ultimate Flash (~1.1s total)
    private const float ULTIMATE_FLASH_IN          = 0.08f;
    private const float ULTIMATE_FLASH_HOLD        = 0.06f;
    private const float ULTIMATE_FLASH_OUT         = 0.2f;
    private const float LETTERBOX_SLIDE_IN         = 0.2f;
    private const float LETTERBOX_HOLD             = 0.6f;
    private const float LETTERBOX_SLIDE_OUT        = 0.25f;
    private const float LETTERBOX_HEIGHT           = 90f;

    // Ultimate Cut-In (~1.4s total)
    private const float CUTIN_BG_FADE_IN           = 0.08f;
    private const float CUTIN_LETTERBOX_IN         = 0.15f;
    private const float CUTIN_SLASH_DURATION       = 0.12f;
    private const float CUTIN_NAME_SLIDE_IN        = 0.25f;
    private const float CUTIN_FLASH_DURATION       = 0.12f;
    private const float CUTIN_HOLD                 = 0.35f;
    private const float CUTIN_FADE_OUT             = 0.25f;

    // Skill Name Display (~1.2s total)
    private const float SKILL_NAME_SLIDE_IN        = 0.2f;
    private const float SKILL_NAME_HOLD            = 0.5f;
    private const float SKILL_NAME_FADE_OUT        = 0.5f;

    // Break Flash (~0.25s total)
    private const float BREAK_FLASH_IN             = 0.04f;
    private const float BREAK_FLASH_OUT            = 0.21f;

    // Turn Start Flash (~0.15s total)
    private const float TURN_FLASH_IN              = 0.03f;
    private const float TURN_FLASH_OUT             = 0.12f;

    // Break Explosion (~0.6s total)
    private const float BREAK_EXPLOSION_DURATION  = 0.6f;
    private const int   RADIAL_LINE_COUNT         = 12;

    // ──────────────────────────────────────────────
    // ランタイム参照
    // ──────────────────────────────────────────────

    private Canvas _canvas;
    private RectTransform _canvasRect;

    // メインフェードオーバーレイ
    private Image _fadeOverlay;

    // 対角ワイプ用オーバーレイ
    private Image _wipeOverlay;
    private RectTransform _wipeRect;

    // テキスト表示
    private Text _phaseText;
    private RectTransform _phaseTextRect;
    private Outline _phaseTextOutline;
    private Outline _phaseTextOutlineOuter;
    private Shadow _phaseTextShadow;

    // フラッシュオーバーレイ（radial pattern）
    private Image _flashOverlay;
    private Image _flashOverlayCenter;

    // レターボックスバー（上下の黒帯）
    private Image _letterboxTop;
    private Image _letterboxBottom;
    private RectTransform _letterboxTopRect;
    private RectTransform _letterboxBottomRect;

    // スクリーンビネット
    private Image _vignetteOverlay;

    // スピードライン
    private readonly List<Image> _speedLines = new List<Image>();
    private readonly List<RectTransform> _speedLineRects = new List<RectTransform>();

    // 紙吹雪
    private readonly List<Image> _confettiPieces = new List<Image>();
    private readonly List<RectTransform> _confettiRects = new List<RectTransform>();

    // ひび割れライン
    private readonly List<Image> _crackLines = new List<Image>();
    private readonly List<RectTransform> _crackLineRects = new List<RectTransform>();

    // カットイン用
    private Image _cutInBg;
    private Image _cutInSlashLine;
    private Image _cutInSlashLine2;
    private RectTransform _cutInSlashRect;
    private RectTransform _cutInSlashRect2;
    private Text _cutInNameText;
    private RectTransform _cutInNameRect;
    private Outline _cutInNameOutline;
    private Outline _cutInNameOutlineOuter;

    // スキル名表示用
    private Text _skillNameText;
    private RectTransform _skillNameRect;
    private Outline _skillNameOutline;
    private Image _skillNameLine;
    private RectTransform _skillNameLineRect;

    // 靭性破壊爆発用
    private readonly List<Image> _radialLines = new List<Image>();
    private readonly List<RectTransform> _radialLineRects = new List<RectTransform>();
    private Image _breakVignette;

    // 手続き的テクスチャキャッシュ
    private Texture2D _whitePixelTex;
    private Sprite _whitePixelSprite;
    private Texture2D _radialGradientTex;
    private Sprite _radialGradientSprite;
    private Texture2D _verticalGradientTex;
    private Sprite _verticalGradientSprite;
    private Texture2D _vignetteTex;
    private Sprite _vignetteSprite;

    // フォントキャッシュ
    private Font _font;

    // コルーチン管理
    private Coroutine _flashCoroutine;
    private Coroutine _letterboxCoroutine;
    private Coroutine _cutInCoroutine;
    private Coroutine _skillNameCoroutine;
    private Coroutine _breakExplosionCoroutine;

    // 紙吹雪の物理情報
    private float[] _confettiFallSpeed;
    private float[] _confettiSwayPhase;
    private float[] _confettiSwayAmplitude;
    private float[] _confettiRotSpeed;

    // ひび割れの方向・長さ情報
    private float[] _crackAngles;
    private float[] _crackLengths;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Canvas および子要素を生成して初期化する。
    /// バトルシーン開始時に一度だけ呼ぶこと。
    /// </summary>
    public void Initialize()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildProceduralSprites();
        BuildCanvas();
        BuildFadeOverlay();
        BuildWipeOverlay();
        BuildPhaseText();
        BuildFlashOverlay();
        BuildLetterbox();
        BuildVignette();
        BuildSpeedLines();
        BuildConfetti();
        BuildCrackLines();
        BuildCutInElements();
        BuildSkillNameDisplay();
        BuildRadialLines();
        BuildBreakVignette();
    }

    /// <summary>
    /// バトル開始演出。黒画面ホールド → 対角ワイプ → "バトル開始!" スライド（スピードライン付き）
    /// → フェードアウト。全体約3.2秒。
    /// </summary>
    public IEnumerator PlayBattleStartEffect()
    {
        // --- 1. フル黒オーバーレイ ---
        SetOverlayColor(_fadeOverlay, BATTLE_START_BG, 1f);
        _fadeOverlay.gameObject.SetActive(true);

        _wipeOverlay.color = BATTLE_START_BG;
        _wipeOverlay.gameObject.SetActive(true);
        _wipeRect.anchoredPosition = Vector2.zero;

        _phaseText.gameObject.SetActive(false);
        HideSpeedLines();

        // 一瞬の黒ホールドで溜めを作る
        yield return new WaitForSeconds(WIPE_HOLD_DURATION);

        // --- 2. 対角ワイプ ---
        float elapsed = 0f;
        Vector2 wipeStart = Vector2.zero;
        Vector2 wipeEnd = new Vector2(2200f, -2200f);

        while (elapsed < WIPE_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / WIPE_DURATION);
            float eased = EaseInOutQuart(t);

            SetOverlayColor(_fadeOverlay, BATTLE_START_BG, 1f - eased);
            _wipeRect.anchoredPosition = Vector2.Lerp(wipeStart, wipeEnd, eased);

            yield return null;
        }

        SetOverlayColor(_fadeOverlay, BATTLE_START_BG, 0f);
        _wipeOverlay.gameObject.SetActive(false);

        // --- 3. テキストスライドイン + スピードライン ---
        _phaseText.text = "バトル開始!";
        _phaseText.fontSize = 110;
        _phaseText.fontStyle = FontStyle.Bold;
        _phaseText.color = TEXT_BATTLE_START;
        _phaseTextOutline.effectColor = OUTLINE_GOLD;
        _phaseTextOutline.effectDistance = new Vector2(4f, -4f);
        _phaseTextOutline.enabled = true;
        _phaseTextOutlineOuter.effectColor = OUTLINE_GOLD_OUTER;
        _phaseTextOutlineOuter.effectDistance = new Vector2(7f, -7f);
        _phaseTextOutlineOuter.enabled = true;
        _phaseTextShadow.effectColor = OUTLINE_GOLD_SHADOW;
        _phaseTextShadow.effectDistance = new Vector2(10f, -10f);

        _phaseTextRect.anchoredPosition = new Vector2(TEXT_SLIDE_START_X, 0f);
        _phaseTextRect.localScale = new Vector3(1.15f, 1.15f, 1f);
        _phaseText.gameObject.SetActive(true);

        Color textColor = _phaseText.color;
        textColor.a = 1f;
        _phaseText.color = textColor;

        ShowSpeedLines();

        elapsed = 0f;
        while (elapsed < TEXT_SLIDE_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / TEXT_SLIDE_DURATION);
            float easedPos = EaseOutBack(t, 1.2f);
            float easedScale = EaseOutCubic(t);

            float x = Mathf.LerpUnclamped(TEXT_SLIDE_START_X, 0f, easedPos);
            _phaseTextRect.anchoredPosition = new Vector2(x, 0f);

            float scale = Mathf.Lerp(1.15f, 1.0f, easedScale);
            _phaseTextRect.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }
        _phaseTextRect.anchoredPosition = Vector2.zero;
        _phaseTextRect.localScale = Vector3.one;

        // --- 4. ホールド + スピードラインアニメーション ---
        elapsed = 0f;
        while (elapsed < TEXT_HOLD_DURATION)
        {
            elapsed += Time.deltaTime;
            AnimateSpeedLines(elapsed);

            // テキストに微かな呼吸アニメーション
            float breathe = 1.0f + 0.015f * Mathf.Sin(elapsed * 5f);
            _phaseTextRect.localScale = new Vector3(breathe, breathe, 1f);

            yield return null;
        }

        // --- 5. フェードアウト ---
        elapsed = 0f;
        while (elapsed < TEXT_FADE_OUT_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / TEXT_FADE_OUT_DURATION);
            float eased = EaseInCubic(t);
            float alpha = 1f - eased;

            textColor = _phaseText.color;
            textColor.a = alpha;
            _phaseText.color = textColor;

            Color outlineColor = _phaseTextOutline.effectColor;
            outlineColor.a = alpha;
            _phaseTextOutline.effectColor = outlineColor;

            Color outerOC = _phaseTextOutlineOuter.effectColor;
            outerOC.a = alpha * OUTLINE_GOLD_OUTER.a;
            _phaseTextOutlineOuter.effectColor = outerOC;

            Color shadowC = _phaseTextShadow.effectColor;
            shadowC.a = alpha * OUTLINE_GOLD_SHADOW.a;
            _phaseTextShadow.effectColor = shadowC;

            // テキストを少し上にスライドさせながらフェード
            _phaseTextRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 30f, eased));

            FadeSpeedLines(alpha);

            yield return null;
        }

        HideSpeedLines();
        _phaseText.gameObject.SetActive(false);
        _phaseTextRect.localScale = Vector3.one;
        _fadeOverlay.gameObject.SetActive(false);
    }

    /// <summary>
    /// 勝利演出。ゴールドオーバーレイ + "勝利!" スケールバウンス + グローパルス + 紙吹雪。
    /// 全体約3.5秒。
    /// </summary>
    public IEnumerator PlayVictoryEffect()
    {
        // --- 1. ゴールドオーバーレイ フェードイン ---
        _fadeOverlay.gameObject.SetActive(true);
        SetOverlayColor(_fadeOverlay, VICTORY_OVERLAY, 0f);

        float elapsed = 0f;
        while (elapsed < VICTORY_OVERLAY_FADE_IN)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / VICTORY_OVERLAY_FADE_IN);
            float eased = EaseOutQuad(t);
            SetOverlayColor(_fadeOverlay, VICTORY_OVERLAY, Mathf.Lerp(0f, VICTORY_OVERLAY.a, eased));
            yield return null;
        }
        SetOverlayColor(_fadeOverlay, VICTORY_OVERLAY, VICTORY_OVERLAY.a);

        // --- 2. テキスト登場: 大きくスケールインして弾む ---
        _phaseText.text = "勝利!";
        _phaseText.fontSize = 120;
        _phaseText.fontStyle = FontStyle.Bold;
        _phaseText.color = TEXT_VICTORY;
        _phaseTextOutline.effectColor = VICTORY_OUTLINE_INNER;
        _phaseTextOutline.effectDistance = new Vector2(3f, -3f);
        _phaseTextOutline.enabled = true;
        _phaseTextOutlineOuter.effectColor = VICTORY_OUTLINE_OUTER;
        _phaseTextOutlineOuter.effectDistance = new Vector2(6f, -6f);
        _phaseTextOutlineOuter.enabled = true;
        _phaseTextShadow.effectColor = VICTORY_OUTLINE_SHADOW;
        _phaseTextShadow.effectDistance = new Vector2(9f, -9f);

        _phaseTextRect.anchoredPosition = Vector2.zero;
        _phaseTextRect.localScale = new Vector3(0.01f, 0.01f, 1f);
        _phaseText.gameObject.SetActive(true);

        // 紙吹雪開始
        InitializeConfetti();

        // スケールアニメーション: 0.01 -> overshoot 1.2 -> bounce -> 1.0
        elapsed = 0f;
        while (elapsed < VICTORY_TEXT_SCALE_TIME)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / VICTORY_TEXT_SCALE_TIME);
            float easedT = EaseOutElastic(t);
            float scale = Mathf.LerpUnclamped(0.01f, 1.0f, easedT);
            _phaseTextRect.localScale = new Vector3(scale, scale, 1f);

            AnimateConfetti(Time.deltaTime);

            yield return null;
        }
        _phaseTextRect.localScale = Vector3.one;

        // 着地の瞬間に小さなフラッシュ
        StartFlash(
            new Color(1f, 0.95f, 0.7f, 1f),
            0.15f,
            VICTORY_SPARKLE_BURST);

        // --- 3. ホールド + グローパルス + 紙吹雪 ---
        elapsed = 0f;
        while (elapsed < VICTORY_HOLD_DURATION)
        {
            elapsed += Time.deltaTime;

            // テキストのグローパルス
            float pulse = 0.85f + 0.15f * Mathf.Sin(elapsed * 3.5f);
            Color tc = TEXT_VICTORY;
            tc.a = pulse;
            _phaseText.color = tc;

            // 微妙なスケールパルス
            float scalePulse = 1.0f + 0.02f * Mathf.Sin(elapsed * 3.5f + 0.5f);
            _phaseTextRect.localScale = new Vector3(scalePulse, scalePulse, 1f);

            AnimateConfetti(Time.deltaTime);

            yield return null;
        }

        _phaseText.color = TEXT_VICTORY;
        _phaseTextRect.localScale = Vector3.one;
        HideConfetti();
    }

    /// <summary>
    /// 敗北演出。暗赤オーバーレイ + "敗北..." テキスト＋シェイク + ひび割れ。
    /// 全体約3.2秒。
    /// </summary>
    public IEnumerator PlayDefeatEffect()
    {
        // --- 1. 暗赤オーバーレイ フェードイン ---
        _fadeOverlay.gameObject.SetActive(true);
        SetOverlayColor(_fadeOverlay, DEFEAT_OVERLAY, 0f);

        float elapsed = 0f;
        while (elapsed < DEFEAT_OVERLAY_FADE_IN)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / DEFEAT_OVERLAY_FADE_IN);
            // 重苦しさを表現するEaseInQuadで徐々に暗く
            float eased = EaseInQuad(t);
            SetOverlayColor(_fadeOverlay, DEFEAT_OVERLAY, Mathf.Lerp(0f, DEFEAT_OVERLAY.a, eased));
            yield return null;
        }
        SetOverlayColor(_fadeOverlay, DEFEAT_OVERLAY, DEFEAT_OVERLAY.a);

        // --- 2. テキスト フェードイン + シェイク + ひび割れ ---
        _phaseText.text = "敗北...";
        _phaseText.fontSize = 100;
        _phaseText.fontStyle = FontStyle.Normal;
        _phaseText.color = new Color(TEXT_DEFEAT.r, TEXT_DEFEAT.g, TEXT_DEFEAT.b, 0f);
        _phaseTextOutline.effectColor = new Color(DEFEAT_OUTLINE_INNER.r, DEFEAT_OUTLINE_INNER.g, DEFEAT_OUTLINE_INNER.b, 0f);
        _phaseTextOutline.effectDistance = new Vector2(3f, -3f);
        _phaseTextOutline.enabled = true;
        _phaseTextOutlineOuter.effectColor = new Color(DEFEAT_OUTLINE_OUTER.r, DEFEAT_OUTLINE_OUTER.g, DEFEAT_OUTLINE_OUTER.b, 0f);
        _phaseTextOutlineOuter.effectDistance = new Vector2(5f, -5f);
        _phaseTextOutlineOuter.enabled = true;
        _phaseTextShadow.effectColor = new Color(0f, 0f, 0f, 0f);
        _phaseTextShadow.effectDistance = new Vector2(6f, -6f);

        _phaseTextRect.anchoredPosition = Vector2.zero;
        _phaseTextRect.localScale = new Vector3(1.05f, 1.05f, 1f);
        _phaseText.gameObject.SetActive(true);

        elapsed = 0f;
        while (elapsed < DEFEAT_TEXT_FADE_IN)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / DEFEAT_TEXT_FADE_IN);
            float eased = EaseOutQuad(t);

            Color tc = TEXT_DEFEAT;
            tc.a = eased;
            _phaseText.color = tc;

            Color olInner = DEFEAT_OUTLINE_INNER;
            olInner.a = eased * DEFEAT_OUTLINE_INNER.a;
            _phaseTextOutline.effectColor = olInner;

            Color olOuter = DEFEAT_OUTLINE_OUTER;
            olOuter.a = eased * DEFEAT_OUTLINE_OUTER.a;
            _phaseTextOutlineOuter.effectColor = olOuter;

            Color shadow = Color.black;
            shadow.a = eased * 0.4f;
            _phaseTextShadow.effectColor = shadow;

            // スケールが1.05 -> 1.0に収束
            float scale = Mathf.Lerp(1.05f, 1.0f, eased);
            _phaseTextRect.localScale = new Vector3(scale, scale, 1f);

            // シェイク -- 不安定さを表現（指数減衰ノイズ）
            float shakeIntensity = DEFEAT_SHAKE_AMOUNT * (1f - eased * 0.3f);
            float shakeX = Mathf.PerlinNoise(Time.time * 25f, 0f) * 2f - 1f;
            float shakeY = Mathf.PerlinNoise(0f, Time.time * 25f) * 2f - 1f;
            _phaseTextRect.anchoredPosition = new Vector2(
                shakeX * shakeIntensity,
                shakeY * shakeIntensity);

            AnimateCrackLines(eased);

            yield return null;
        }

        _phaseText.color = TEXT_DEFEAT;
        _phaseTextRect.localScale = Vector3.one;

        // --- 3. ホールド + 減衰シェイク ---
        elapsed = 0f;
        while (elapsed < DEFEAT_HOLD_DURATION)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - Mathf.Clamp01(elapsed / DEFEAT_HOLD_DURATION);
            float shakeIntensity = DEFEAT_SHAKE_AMOUNT * 0.4f * decay * decay;

            float shakeX = Mathf.PerlinNoise(Time.time * 20f, 100f) * 2f - 1f;
            float shakeY = Mathf.PerlinNoise(100f, Time.time * 20f) * 2f - 1f;
            _phaseTextRect.anchoredPosition = new Vector2(
                shakeX * shakeIntensity,
                shakeY * shakeIntensity);

            yield return null;
        }

        _phaseTextRect.anchoredPosition = Vector2.zero;
        HideCrackLines();
    }

    /// <summary>
    /// 必殺技発動フラッシュ + レターボックス演出。
    /// ゴールドホワイトフラッシュ + スムーズなレターボックスバー。
    /// </summary>
    public void PlayUltimateFlash()
    {
        StartFlash(ULTIMATE_FLASH_COLOR, ULTIMATE_FLASH_PEAK_ALPHA,
            ULTIMATE_FLASH_IN, ULTIMATE_FLASH_HOLD, ULTIMATE_FLASH_OUT);

        if (_letterboxCoroutine != null)
        {
            StopCoroutine(_letterboxCoroutine);
        }
        _letterboxCoroutine = StartCoroutine(LetterboxCoroutine());
    }

    /// <summary>
    /// 靭性破壊フラッシュ。白 alpha パルス (0.25s)。
    /// </summary>
    public void PlayBreakFlash()
    {
        StartFlash(BREAK_FLASH_COLOR, BREAK_FLASH_PEAK_ALPHA,
            BREAK_FLASH_IN, 0f, BREAK_FLASH_OUT);
    }

    /// <summary>
    /// ターン開始フラッシュ。非常に控えめな白パルス (0.15s)。
    /// </summary>
    public void PlayTurnStartFlash()
    {
        StartFlash(TURN_FLASH_COLOR, TURN_FLASH_PEAK_ALPHA,
            TURN_FLASH_IN, 0f, TURN_FLASH_OUT);
    }

    /// <summary>
    /// 必殺技カットイン演出（スターレイル風）。
    /// 暗転 → レターボックス → 対角ダブルスラッシュライン → キャラ名スウィープイン →
    /// ゴールドフラッシュ → フェードアウト。約1.4秒。
    /// </summary>
    public void PlayUltimateCutIn(string characterName)
    {
        if (_cutInCoroutine != null)
        {
            StopCoroutine(_cutInCoroutine);
        }
        _cutInCoroutine = StartCoroutine(UltimateCutInCoroutine(characterName));
    }

    /// <summary>
    /// スキル名表示。"キャラ名 - スキル名" テキストが右からスライドイン、
    /// 下線付きでホールド後フェードアウト。約1.2秒。
    /// </summary>
    public void PlaySkillNameDisplay(string skillName)
    {
        if (_skillNameCoroutine != null)
        {
            StopCoroutine(_skillNameCoroutine);
        }
        _skillNameCoroutine = StartCoroutine(SkillNameDisplayCoroutine(skillName));
    }

    /// <summary>
    /// 靭性破壊爆発演出。白フラッシュ + 放射ライン + ビネットパルス。約0.6秒。
    /// </summary>
    public void PlayBreakExplosion()
    {
        if (_breakExplosionCoroutine != null)
        {
            StopCoroutine(_breakExplosionCoroutine);
        }
        _breakExplosionCoroutine = StartCoroutine(BreakExplosionCoroutine());
    }

    /// <summary>
    /// スクリーンビネットの表示・非表示を切り替える。
    /// </summary>
    public void SetVignetteActive(bool active)
    {
        if (_vignetteOverlay != null)
        {
            _vignetteOverlay.gameObject.SetActive(active);
        }
    }

    // ──────────────────────────────────────────────
    // 内部 -- フラッシュ（3フェーズ: in / hold / out）
    // ──────────────────────────────────────────────

    private void StartFlash(Color baseColor, float peakAlpha, float duration)
    {
        float halfDur = duration * 0.5f;
        StartFlash(baseColor, peakAlpha, halfDur, 0f, halfDur);
    }

    private void StartFlash(Color baseColor, float peakAlpha, float fadeIn, float hold, float fadeOut)
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }
        _flashCoroutine = StartCoroutine(FlashCoroutine(baseColor, peakAlpha, fadeIn, hold, fadeOut));
    }

    private IEnumerator FlashCoroutine(Color baseColor, float peakAlpha, float fadeIn, float hold, float fadeOut)
    {
        _flashOverlay.gameObject.SetActive(true);
        _flashOverlayCenter.gameObject.SetActive(true);

        // フェードイン: 0 -> peakAlpha（EaseOutQuadで素早くピークへ）
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeIn);
            float alpha = Mathf.Lerp(0f, peakAlpha, EaseOutQuad(t));
            ApplyFlashAlpha(baseColor, alpha);
            yield return null;
        }
        ApplyFlashAlpha(baseColor, peakAlpha);

        // ホールド
        if (hold > 0f)
        {
            yield return new WaitForSeconds(hold);
        }

        // フェードアウト: peakAlpha -> 0（EaseInCubicでゆっくり消える）
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOut);
            float alpha = Mathf.Lerp(peakAlpha, 0f, EaseInCubic(t));
            ApplyFlashAlpha(baseColor, alpha);
            yield return null;
        }

        ApplyFlashAlpha(baseColor, 0f);
        _flashOverlay.gameObject.SetActive(false);
        _flashOverlayCenter.gameObject.SetActive(false);
        _flashCoroutine = null;
    }

    private void ApplyFlashAlpha(Color baseColor, float alpha)
    {
        Color edge = baseColor;
        edge.a = alpha * 0.4f;
        _flashOverlay.color = edge;

        Color center = baseColor;
        center.a = alpha;
        _flashOverlayCenter.color = center;
    }

    // ──────────────────────────────────────────────
    // 内部 -- レターボックス（スムーズイーズイン/アウト）
    // ──────────────────────────────────────────────

    private IEnumerator LetterboxCoroutine()
    {
        _letterboxTop.gameObject.SetActive(true);
        _letterboxBottom.gameObject.SetActive(true);

        // スライドイン（EaseOutCubicでスムーズに停止）
        float elapsed = 0f;
        while (elapsed < LETTERBOX_SLIDE_IN)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / LETTERBOX_SLIDE_IN);
            float height = Mathf.Lerp(0f, LETTERBOX_HEIGHT, EaseOutCubic(t));
            SetLetterboxHeight(height);
            yield return null;
        }
        SetLetterboxHeight(LETTERBOX_HEIGHT);

        yield return new WaitForSeconds(LETTERBOX_HOLD);

        // スライドアウト（EaseInOutQuadで優雅に）
        elapsed = 0f;
        while (elapsed < LETTERBOX_SLIDE_OUT)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / LETTERBOX_SLIDE_OUT);
            float height = Mathf.Lerp(LETTERBOX_HEIGHT, 0f, EaseInOutQuad(t));
            SetLetterboxHeight(height);
            yield return null;
        }

        SetLetterboxHeight(0f);
        _letterboxTop.gameObject.SetActive(false);
        _letterboxBottom.gameObject.SetActive(false);
        _letterboxCoroutine = null;
    }

    private void SetLetterboxHeight(float height)
    {
        _letterboxTopRect.sizeDelta = new Vector2(0f, height);
        _letterboxBottomRect.sizeDelta = new Vector2(0f, height);
    }

    // ──────────────────────────────────────────────
    // 内部 -- 必殺技カットイン（ドラマチック・スウィープ）
    // ──────────────────────────────────────────────

    private IEnumerator UltimateCutInCoroutine(string characterName)
    {
        // --- Phase 0: 暗転バックドロップ ---
        _cutInBg.gameObject.SetActive(true);
        _letterboxTop.gameObject.SetActive(true);
        _letterboxBottom.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < CUTIN_BG_FADE_IN)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / CUTIN_BG_FADE_IN);
            Color bgC = CUTIN_BG_COLOR;
            bgC.a = CUTIN_BG_COLOR.a * EaseOutQuad(t);
            _cutInBg.color = bgC;
            yield return null;
        }

        // --- Phase 1: レターボックス スライドイン ---
        elapsed = 0f;
        while (elapsed < CUTIN_LETTERBOX_IN)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / CUTIN_LETTERBOX_IN);
            SetLetterboxHeight(Mathf.Lerp(0f, LETTERBOX_HEIGHT, EaseOutCubic(t)));
            yield return null;
        }
        SetLetterboxHeight(LETTERBOX_HEIGHT);

        // --- Phase 2: ダブル対角スラッシュライン（ドラマチック・スウィープ） ---
        _cutInSlashLine.gameObject.SetActive(true);
        _cutInSlashLine2.gameObject.SetActive(true);

        _cutInSlashRect.localScale = new Vector3(0f, 1f, 1f);
        _cutInSlashRect2.localScale = new Vector3(0f, 1f, 1f);
        _cutInSlashLine.color = CUTIN_SLASH_COLOR;
        _cutInSlashLine2.color = new Color(CUTIN_SLASH_COLOR.r, CUTIN_SLASH_COLOR.g, CUTIN_SLASH_COLOR.b, CUTIN_SLASH_COLOR.a * 0.5f);

        elapsed = 0f;
        while (elapsed < CUTIN_SLASH_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / CUTIN_SLASH_DURATION);
            float eased = EaseOutExpo(t);
            _cutInSlashRect.localScale = new Vector3(eased, 1f, 1f);
            // 2本目は少し遅れてスケール
            float t2 = Mathf.Clamp01((elapsed - 0.02f) / CUTIN_SLASH_DURATION);
            float eased2 = EaseOutExpo(Mathf.Max(0f, t2));
            _cutInSlashRect2.localScale = new Vector3(eased2, 1f, 1f);
            yield return null;
        }
        _cutInSlashRect.localScale = Vector3.one;
        _cutInSlashRect2.localScale = Vector3.one;

        // --- Phase 3: キャラ名ドラマチック・スウィープイン ---
        _cutInNameText.text = characterName;
        _cutInNameText.gameObject.SetActive(true);

        Color nameColor = CUTIN_GOLD;
        nameColor.a = 0f;
        _cutInNameText.color = nameColor;
        _cutInNameRect.anchoredPosition = new Vector2(600f, -120f);
        _cutInNameRect.localScale = new Vector3(1.1f, 1.1f, 1f);

        elapsed = 0f;
        while (elapsed < CUTIN_NAME_SLIDE_IN)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / CUTIN_NAME_SLIDE_IN);
            float easedPos = EaseOutQuart(t);
            float easedAlpha = EaseOutCubic(t);

            float x = Mathf.Lerp(600f, 200f, easedPos);
            _cutInNameRect.anchoredPosition = new Vector2(x, -120f);

            float scale = Mathf.Lerp(1.1f, 1.0f, easedPos);
            _cutInNameRect.localScale = new Vector3(scale, scale, 1f);

            nameColor.a = easedAlpha;
            _cutInNameText.color = nameColor;

            yield return null;
        }
        nameColor.a = 1f;
        _cutInNameText.color = nameColor;
        _cutInNameRect.localScale = Vector3.one;

        // --- Phase 4: ゴールドフラッシュ ---
        StartFlash(
            new Color(CUTIN_GOLD.r, CUTIN_GOLD.g, CUTIN_GOLD.b, 1f),
            0.35f,
            0.04f, 0.02f, CUTIN_FLASH_DURATION);

        // --- Phase 5: ホールド ---
        yield return new WaitForSeconds(CUTIN_HOLD);

        // --- Phase 6: 全要素フェードアウト（同期的にスムーズに） ---
        elapsed = 0f;
        while (elapsed < CUTIN_FADE_OUT)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / CUTIN_FADE_OUT);
            float eased = EaseInOutQuad(t);
            float alpha = 1f - eased;

            Color slashC = CUTIN_SLASH_COLOR;
            slashC.a = alpha * CUTIN_SLASH_COLOR.a;
            _cutInSlashLine.color = slashC;

            Color slash2C = CUTIN_SLASH_COLOR;
            slash2C.a = alpha * CUTIN_SLASH_COLOR.a * 0.5f;
            _cutInSlashLine2.color = slash2C;

            nameColor = CUTIN_GOLD;
            nameColor.a = alpha;
            _cutInNameText.color = nameColor;

            // キャラ名が少しスライドアウト
            float nameX = Mathf.Lerp(200f, 150f, eased);
            _cutInNameRect.anchoredPosition = new Vector2(nameX, -120f);

            SetLetterboxHeight(Mathf.Lerp(LETTERBOX_HEIGHT, 0f, eased));

            Color bgC = CUTIN_BG_COLOR;
            bgC.a = CUTIN_BG_COLOR.a * alpha;
            _cutInBg.color = bgC;

            yield return null;
        }

        _cutInSlashLine.gameObject.SetActive(false);
        _cutInSlashLine2.gameObject.SetActive(false);
        _cutInNameText.gameObject.SetActive(false);
        _cutInBg.gameObject.SetActive(false);
        SetLetterboxHeight(0f);
        _letterboxTop.gameObject.SetActive(false);
        _letterboxBottom.gameObject.SetActive(false);
        _cutInCoroutine = null;
    }

    // ──────────────────────────────────────────────
    // 内部 -- スキル名表示（下線付き、3フェーズ）
    // ──────────────────────────────────────────────

    private IEnumerator SkillNameDisplayCoroutine(string skillName)
    {
        _skillNameText.text = skillName;
        _skillNameText.gameObject.SetActive(true);
        _skillNameLine.gameObject.SetActive(true);

        Color textColor = SKILL_NAME_COLOR;
        textColor.a = 0f;
        _skillNameText.color = textColor;

        _skillNameRect.anchoredPosition = new Vector2(400f, -200f);
        _skillNameLineRect.localScale = new Vector3(0f, 1f, 1f);

        Color lineColor = new Color(1f, 0.85f, 0.2f, 0f);
        _skillNameLine.color = lineColor;

        // Phase 1: スライドイン + フェードイン
        float elapsed = 0f;
        while (elapsed < SKILL_NAME_SLIDE_IN)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SKILL_NAME_SLIDE_IN);
            float easedPos = EaseOutCubic(t);
            float easedAlpha = EaseOutQuad(t);

            float x = Mathf.Lerp(400f, 280f, easedPos);
            _skillNameRect.anchoredPosition = new Vector2(x, -200f);

            textColor = SKILL_NAME_COLOR;
            textColor.a = SKILL_NAME_COLOR.a * easedAlpha;
            _skillNameText.color = textColor;

            Color olColor = _skillNameOutline.effectColor;
            olColor.a = easedAlpha * 0.5f;
            _skillNameOutline.effectColor = olColor;

            // 下線がスウィープイン
            _skillNameLineRect.localScale = new Vector3(easedPos, 1f, 1f);
            lineColor.a = easedAlpha * 0.6f;
            _skillNameLine.color = lineColor;

            yield return null;
        }

        // Phase 2: ホールド（微妙なスライド継続）
        elapsed = 0f;
        while (elapsed < SKILL_NAME_HOLD)
        {
            elapsed += Time.deltaTime;
            float drift = Mathf.Lerp(280f, 260f, elapsed / SKILL_NAME_HOLD);
            _skillNameRect.anchoredPosition = new Vector2(drift, -200f);
            yield return null;
        }

        // Phase 3: フェードアウト + 左へスライド
        elapsed = 0f;
        while (elapsed < SKILL_NAME_FADE_OUT)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SKILL_NAME_FADE_OUT);
            float eased = EaseInCubic(t);
            float alpha = 1f - eased;

            float x = Mathf.Lerp(260f, 200f, eased);
            _skillNameRect.anchoredPosition = new Vector2(x, -200f);

            textColor = SKILL_NAME_COLOR;
            textColor.a = SKILL_NAME_COLOR.a * alpha;
            _skillNameText.color = textColor;

            Color olColor = _skillNameOutline.effectColor;
            olColor.a = alpha * 0.5f;
            _skillNameOutline.effectColor = olColor;

            lineColor.a = alpha * 0.6f;
            _skillNameLine.color = lineColor;
            _skillNameLineRect.localScale = new Vector3(alpha, 1f, 1f);

            yield return null;
        }

        _skillNameText.gameObject.SetActive(false);
        _skillNameLine.gameObject.SetActive(false);
        _skillNameCoroutine = null;
    }

    // ──────────────────────────────────────────────
    // 内部 -- 靭性破壊爆発
    // ──────────────────────────────────────────────

    private IEnumerator BreakExplosionCoroutine()
    {
        _flashOverlay.gameObject.SetActive(true);
        _flashOverlayCenter.gameObject.SetActive(true);
        _breakVignette.gameObject.SetActive(true);
        ShowRadialLines();

        float peakPhase = BREAK_EXPLOSION_DURATION * 0.3f;
        float decayPhase = BREAK_EXPLOSION_DURATION - peakPhase;

        // フェードイン（素早く爆発）
        float elapsed = 0f;
        while (elapsed < peakPhase)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / peakPhase);
            float eased = EaseOutQuad(t);

            Color centerColor = Color.white;
            centerColor.a = Mathf.Lerp(0f, 0.7f, eased);
            _flashOverlayCenter.color = centerColor;

            Color edgeColor = Color.white;
            edgeColor.a = centerColor.a * 0.25f;
            _flashOverlay.color = edgeColor;

            AnimateRadialLinesExpand(eased);

            Color vigColor = VIGNETTE_COLOR;
            vigColor.a = Mathf.Lerp(0f, 0.45f, eased);
            _breakVignette.color = vigColor;

            yield return null;
        }

        // フェードアウト（余韻を残す）
        elapsed = 0f;
        while (elapsed < decayPhase)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / decayPhase);
            float eased = EaseInCubic(t);

            Color centerColor = Color.white;
            centerColor.a = Mathf.Lerp(0.7f, 0f, eased);
            _flashOverlayCenter.color = centerColor;

            Color edgeColor = Color.white;
            edgeColor.a = centerColor.a * 0.25f;
            _flashOverlay.color = edgeColor;

            AnimateRadialLinesFade(t);

            Color vigColor = VIGNETTE_COLOR;
            vigColor.a = Mathf.Lerp(0.45f, 0f, eased);
            _breakVignette.color = vigColor;

            yield return null;
        }

        HideRadialLines();
        _breakVignette.gameObject.SetActive(false);

        Color finalC = Color.white;
        finalC.a = 0f;
        _flashOverlay.color = finalC;
        _flashOverlayCenter.color = finalC;
        _flashOverlay.gameObject.SetActive(false);
        _flashOverlayCenter.gameObject.SetActive(false);

        _breakExplosionCoroutine = null;
    }

    // ──────────────────────────────────────────────
    // 内部 -- スピードライン
    // ──────────────────────────────────────────────

    private void ShowSpeedLines()
    {
        for (int i = 0; i < _speedLines.Count; i++)
        {
            _speedLines[i].gameObject.SetActive(true);
            float y = Random.Range(-450f, 450f);
            _speedLineRects[i].anchoredPosition = new Vector2(Random.Range(-960f, 960f), y);

            Color c = Color.white;
            c.a = Random.Range(0.06f, 0.18f);
            _speedLines[i].color = c;
        }
    }

    private void AnimateSpeedLines(float time)
    {
        for (int i = 0; i < _speedLines.Count; i++)
        {
            RectTransform rt = _speedLineRects[i];
            Vector2 pos = rt.anchoredPosition;
            float speed = 1800f + i * 180f;
            pos.x += speed * Time.deltaTime;

            if (pos.x > 1300f)
            {
                pos.x = -1300f;
                pos.y = Random.Range(-450f, 450f);
                Color c = Color.white;
                c.a = Random.Range(0.06f, 0.18f);
                _speedLines[i].color = c;

                // ランダムに太さを変える
                Vector2 sd = rt.sizeDelta;
                sd.y = Random.Range(1f, 4f);
                rt.sizeDelta = sd;
            }
            rt.anchoredPosition = pos;
        }
    }

    private void FadeSpeedLines(float alpha)
    {
        for (int i = 0; i < _speedLines.Count; i++)
        {
            Color c = _speedLines[i].color;
            c.a = Mathf.Min(c.a, alpha * 0.18f);
            _speedLines[i].color = c;
        }
    }

    private void HideSpeedLines()
    {
        for (int i = 0; i < _speedLines.Count; i++)
        {
            _speedLines[i].gameObject.SetActive(false);
        }
    }

    // ──────────────────────────────────────────────
    // 内部 -- 紙吹雪
    // ──────────────────────────────────────────────

    private void InitializeConfetti()
    {
        if (_confettiFallSpeed == null || _confettiFallSpeed.Length != CONFETTI_COUNT)
        {
            _confettiFallSpeed = new float[CONFETTI_COUNT];
            _confettiSwayPhase = new float[CONFETTI_COUNT];
            _confettiSwayAmplitude = new float[CONFETTI_COUNT];
            _confettiRotSpeed = new float[CONFETTI_COUNT];
        }

        for (int i = 0; i < _confettiPieces.Count; i++)
        {
            _confettiPieces[i].gameObject.SetActive(true);
            float x = Random.Range(-900f, 900f);
            float y = Random.Range(560f, 900f);
            _confettiRects[i].anchoredPosition = new Vector2(x, y);
            _confettiRects[i].localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Color c = CONFETTI_COLORS[i % CONFETTI_COLORS.Length];
            _confettiPieces[i].color = c;

            _confettiFallSpeed[i] = Random.Range(120f, 320f);
            _confettiSwayPhase[i] = Random.Range(0f, Mathf.PI * 2f);
            _confettiSwayAmplitude[i] = Random.Range(30f, 70f);
            _confettiRotSpeed[i] = Random.Range(60f, 300f);
        }
    }

    private void AnimateConfetti(float deltaTime)
    {
        for (int i = 0; i < _confettiPieces.Count; i++)
        {
            if (!_confettiPieces[i].gameObject.activeSelf) continue;

            RectTransform rt = _confettiRects[i];
            Vector2 pos = rt.anchoredPosition;

            pos.y -= _confettiFallSpeed[i] * deltaTime;
            pos.x += Mathf.Sin(Time.time * 2.5f + _confettiSwayPhase[i]) * _confettiSwayAmplitude[i] * deltaTime;

            rt.anchoredPosition = pos;
            rt.Rotate(0f, 0f, _confettiRotSpeed[i] * deltaTime);

            if (pos.y < -620f)
            {
                pos.x = Random.Range(-900f, 900f);
                pos.y = Random.Range(560f, 750f);
                rt.anchoredPosition = pos;
            }
        }
    }

    private void HideConfetti()
    {
        for (int i = 0; i < _confettiPieces.Count; i++)
        {
            _confettiPieces[i].gameObject.SetActive(false);
        }
    }

    // ──────────────────────────────────────────────
    // 内部 -- ひび割れライン
    // ──────────────────────────────────────────────

    private void AnimateCrackLines(float progress)
    {
        for (int i = 0; i < _crackLines.Count; i++)
        {
            if (!_crackLines[i].gameObject.activeSelf)
            {
                _crackLines[i].gameObject.SetActive(true);
            }

            // 各ラインが少しずつ遅延して伸びる
            float delay = i * 0.04f;
            float adjustedProgress = Mathf.Clamp01((progress - delay) * 1.8f);
            float eased = EaseOutCubic(adjustedProgress);

            float targetLength = _crackLengths[i];
            float currentLength = targetLength * eased;

            _crackLineRects[i].sizeDelta = new Vector2(currentLength, 2f);

            Color c = new Color(0.12f, 0.03f, 0.03f, eased * 0.85f);
            _crackLines[i].color = c;
        }
    }

    private void HideCrackLines()
    {
        for (int i = 0; i < _crackLines.Count; i++)
        {
            _crackLines[i].gameObject.SetActive(false);
        }
    }

    // ──────────────────────────────────────────────
    // 内部 -- 放射ライン（靭性破壊爆発用）
    // ──────────────────────────────────────────────

    private void ShowRadialLines()
    {
        for (int i = 0; i < _radialLines.Count; i++)
        {
            _radialLines[i].gameObject.SetActive(true);
            _radialLineRects[i].sizeDelta = new Vector2(0f, 3f);

            Color c = Color.white;
            c.a = 0.75f;
            _radialLines[i].color = c;
        }
    }

    private void AnimateRadialLinesExpand(float t)
    {
        for (int i = 0; i < _radialLines.Count; i++)
        {
            float targetLen = 500f + i * 60f;
            float currentLen = Mathf.Lerp(0f, targetLen, EaseOutExpo(t));
            float thickness = Mathf.Lerp(2f, 4f, t);
            _radialLineRects[i].sizeDelta = new Vector2(currentLen, thickness);
        }
    }

    private void AnimateRadialLinesFade(float t)
    {
        for (int i = 0; i < _radialLines.Count; i++)
        {
            Color c = Color.white;
            c.a = Mathf.Lerp(0.75f, 0f, EaseInQuad(t));
            _radialLines[i].color = c;

            Vector2 sd = _radialLineRects[i].sizeDelta;
            sd.x += 180f * Time.deltaTime;
            sd.y = Mathf.Lerp(4f, 1f, t);
            _radialLineRects[i].sizeDelta = sd;
        }
    }

    private void HideRadialLines()
    {
        for (int i = 0; i < _radialLines.Count; i++)
        {
            _radialLines[i].gameObject.SetActive(false);
        }
    }

    // ──────────────────────────────────────────────
    // イージング関数
    // ──────────────────────────────────────────────

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static float EaseInCubic(float t)
    {
        return t * t * t;
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseInQuad(float t)
    {
        return t * t;
    }

    private static float EaseInOutQuad(float t)
    {
        return t < 0.5f
            ? 2f * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
    }

    private static float EaseOutQuart(float t)
    {
        return 1f - Mathf.Pow(1f - t, 4f);
    }

    private static float EaseInOutQuart(float t)
    {
        return t < 0.5f
            ? 8f * t * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 4f) * 0.5f;
    }

    private static float EaseOutExpo(float t)
    {
        return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
    }

    private static float EaseOutBack(float t, float overshoot)
    {
        float c1 = overshoot;
        float c3 = c1 + 1f;
        float tm1 = t - 1f;
        return 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1;
    }

    private static float EaseOutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;

        const float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    private static float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1)
        {
            return n1 * t * t;
        }
        else if (t < 2f / d1)
        {
            t -= 1.5f / d1;
            return n1 * t * t + 0.75f;
        }
        else if (t < 2.5f / d1)
        {
            t -= 2.25f / d1;
            return n1 * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }

    // ──────────────────────────────────────────────
    // ヘルパー
    // ──────────────────────────────────────────────

    private static void SetOverlayColor(Image overlay, Color baseColor, float alpha)
    {
        Color c = baseColor;
        c.a = alpha;
        overlay.color = c;
    }

    // ──────────────────────────────────────────────
    // 手続き的テクスチャ生成
    // ──────────────────────────────────────────────

    private void BuildProceduralSprites()
    {
        // 1x1 白ピクセル
        _whitePixelTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _whitePixelTex.SetPixel(0, 0, Color.white);
        _whitePixelTex.Apply();
        _whitePixelSprite = Sprite.Create(
            _whitePixelTex,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f));

        // 放射グラデーション（中心=白、端=透明）-- スムーズな二次カーブ
        int radialSize = 128;
        _radialGradientTex = new Texture2D(radialSize, radialSize, TextureFormat.RGBA32, false);
        float halfSize = radialSize * 0.5f;
        for (int y = 0; y < radialSize; y++)
        {
            for (int x = 0; x < radialSize; x++)
            {
                float dx = (x - halfSize) / halfSize;
                float dy = (y - halfSize) / halfSize;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - dist);
                a = a * a * a; // 三次カーブでよりソフトに
                _radialGradientTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        _radialGradientTex.Apply();
        _radialGradientSprite = Sprite.Create(
            _radialGradientTex,
            new Rect(0, 0, radialSize, radialSize),
            new Vector2(0.5f, 0.5f));

        // 縦グラデーション（上端=不透明黒、下端=透明）
        int gradH = 64;
        _verticalGradientTex = new Texture2D(1, gradH, TextureFormat.RGBA32, false);
        for (int y = 0; y < gradH; y++)
        {
            float t = (float)y / (gradH - 1);
            // スムーズなグラデーション（三次補間）
            float a = t * t * (3f - 2f * t);
            _verticalGradientTex.SetPixel(0, y, new Color(0f, 0f, 0f, a));
        }
        _verticalGradientTex.Apply();
        _verticalGradientTex.wrapMode = TextureWrapMode.Clamp;
        _verticalGradientSprite = Sprite.Create(
            _verticalGradientTex,
            new Rect(0, 0, 1, gradH),
            new Vector2(0.5f, 0.5f));

        // ビネットテクスチャ（端=暗い、中心=透明）
        int vigSize = 128;
        _vignetteTex = new Texture2D(vigSize, vigSize, TextureFormat.RGBA32, false);
        float vigHalf = vigSize * 0.5f;
        for (int y = 0; y < vigSize; y++)
        {
            for (int x = 0; x < vigSize; x++)
            {
                float dx = (x - vigHalf) / vigHalf;
                float dy = (y - vigHalf) / vigHalf;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(dist - 0.35f) / 0.65f;
                a = a * a * a; // 三次カーブ
                _vignetteTex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
        }
        _vignetteTex.Apply();
        _vignetteSprite = Sprite.Create(
            _vignetteTex,
            new Rect(0, 0, vigSize, vigSize),
            new Vector2(0.5f, 0.5f));
    }

    // ──────────────────────────────────────────────
    // UI 構築
    // ──────────────────────────────────────────────

    private void BuildCanvas()
    {
        GameObject canvasObj = new GameObject("BattleEffectsCanvas");
        canvasObj.transform.SetParent(transform);

        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = CANVAS_SORT_ORDER;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(REFERENCE_WIDTH, REFERENCE_HEIGHT);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        _canvasRect = canvasObj.GetComponent<RectTransform>();
    }

    private void BuildFadeOverlay()
    {
        GameObject obj = new GameObject("FadeOverlay");
        obj.transform.SetParent(_canvas.transform, false);

        _fadeOverlay = obj.AddComponent<Image>();
        _fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        _fadeOverlay.raycastTarget = false;

        StretchFull(_fadeOverlay.rectTransform);
        obj.SetActive(false);
    }

    private void BuildWipeOverlay()
    {
        GameObject obj = new GameObject("WipeOverlay");
        obj.transform.SetParent(_canvas.transform, false);

        _wipeOverlay = obj.AddComponent<Image>();
        _wipeOverlay.color = BATTLE_START_BG;
        _wipeOverlay.raycastTarget = false;

        _wipeRect = obj.GetComponent<RectTransform>();
        _wipeRect.anchorMin = new Vector2(0.5f, 0.5f);
        _wipeRect.anchorMax = new Vector2(0.5f, 0.5f);
        _wipeRect.sizeDelta = new Vector2(3500f, 3500f);
        _wipeRect.anchoredPosition = Vector2.zero;
        _wipeRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        obj.SetActive(false);
    }

    private void BuildPhaseText()
    {
        GameObject obj = new GameObject("PhaseText");
        obj.transform.SetParent(_canvas.transform, false);

        _phaseText = obj.AddComponent<Text>();
        _phaseText.text = string.Empty;
        _phaseText.font = _font;
        _phaseText.fontSize = 110;
        _phaseText.fontStyle = FontStyle.Bold;
        _phaseText.alignment = TextAnchor.MiddleCenter;
        _phaseText.color = Color.white;
        _phaseText.raycastTarget = false;
        _phaseText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _phaseText.verticalOverflow = VerticalWrapMode.Overflow;

        // 内側アウトライン（ゴールド）
        _phaseTextOutline = obj.AddComponent<Outline>();
        _phaseTextOutline.effectColor = OUTLINE_GOLD;
        _phaseTextOutline.effectDistance = new Vector2(4f, -4f);
        _phaseTextOutline.enabled = false;

        // 外側アウトライン（濃いゴールド）
        _phaseTextOutlineOuter = obj.AddComponent<Outline>();
        _phaseTextOutlineOuter.effectColor = OUTLINE_GOLD_OUTER;
        _phaseTextOutlineOuter.effectDistance = new Vector2(7f, -7f);
        _phaseTextOutlineOuter.enabled = false;

        // 影（奥行き感）
        _phaseTextShadow = obj.AddComponent<Shadow>();
        _phaseTextShadow.effectColor = OUTLINE_GOLD_SHADOW;
        _phaseTextShadow.effectDistance = new Vector2(10f, -10f);

        _phaseTextRect = _phaseText.rectTransform;
        StretchFull(_phaseTextRect);

        obj.SetActive(false);
    }

    private void BuildFlashOverlay()
    {
        // 外周フラッシュ
        GameObject flashObj = new GameObject("FlashOverlay");
        flashObj.transform.SetParent(_canvas.transform, false);

        _flashOverlay = flashObj.AddComponent<Image>();
        _flashOverlay.color = new Color(1f, 1f, 1f, 0f);
        _flashOverlay.raycastTarget = false;

        StretchFull(_flashOverlay.rectTransform);
        flashObj.SetActive(false);

        // 中心フラッシュ（radial gradient）
        GameObject centerObj = new GameObject("FlashOverlayCenter");
        centerObj.transform.SetParent(_canvas.transform, false);

        _flashOverlayCenter = centerObj.AddComponent<Image>();
        _flashOverlayCenter.sprite = _radialGradientSprite;
        _flashOverlayCenter.type = Image.Type.Simple;
        _flashOverlayCenter.color = new Color(1f, 1f, 1f, 0f);
        _flashOverlayCenter.raycastTarget = false;

        StretchFull(_flashOverlayCenter.rectTransform);
        centerObj.SetActive(false);
    }

    private void BuildLetterbox()
    {
        // 上バー
        GameObject topObj = new GameObject("LetterboxTop");
        topObj.transform.SetParent(_canvas.transform, false);

        _letterboxTop = topObj.AddComponent<Image>();
        _letterboxTop.sprite = _verticalGradientSprite;
        _letterboxTop.type = Image.Type.Sliced;
        _letterboxTop.color = Color.white;
        _letterboxTop.raycastTarget = false;

        _letterboxTopRect = topObj.GetComponent<RectTransform>();
        _letterboxTopRect.anchorMin = new Vector2(0f, 1f);
        _letterboxTopRect.anchorMax = new Vector2(1f, 1f);
        _letterboxTopRect.pivot = new Vector2(0.5f, 1f);
        _letterboxTopRect.sizeDelta = new Vector2(0f, 0f);
        _letterboxTopRect.anchoredPosition = Vector2.zero;

        topObj.SetActive(false);

        // 下バー（180度回転で下端を不透明に）
        GameObject bottomObj = new GameObject("LetterboxBottom");
        bottomObj.transform.SetParent(_canvas.transform, false);

        _letterboxBottom = bottomObj.AddComponent<Image>();
        _letterboxBottom.sprite = _verticalGradientSprite;
        _letterboxBottom.type = Image.Type.Sliced;
        _letterboxBottom.color = Color.white;
        _letterboxBottom.raycastTarget = false;

        _letterboxBottomRect = bottomObj.GetComponent<RectTransform>();
        _letterboxBottomRect.anchorMin = new Vector2(0f, 0f);
        _letterboxBottomRect.anchorMax = new Vector2(1f, 0f);
        _letterboxBottomRect.pivot = new Vector2(0.5f, 0f);
        _letterboxBottomRect.sizeDelta = new Vector2(0f, 0f);
        _letterboxBottomRect.anchoredPosition = Vector2.zero;
        _letterboxBottomRect.localRotation = Quaternion.Euler(0f, 0f, 180f);

        bottomObj.SetActive(false);
    }

    private void BuildVignette()
    {
        GameObject obj = new GameObject("ScreenVignette");
        obj.transform.SetParent(_canvas.transform, false);

        _vignetteOverlay = obj.AddComponent<Image>();
        _vignetteOverlay.sprite = _vignetteSprite;
        _vignetteOverlay.type = Image.Type.Simple;
        _vignetteOverlay.color = VIGNETTE_COLOR;
        _vignetteOverlay.raycastTarget = false;
        _vignetteOverlay.preserveAspect = false;

        StretchFull(_vignetteOverlay.rectTransform);
        obj.SetActive(false);
    }

    private void BuildSpeedLines()
    {
        for (int i = 0; i < SPEED_LINE_COUNT; i++)
        {
            GameObject obj = new GameObject("SpeedLine_" + i);
            obj.transform.SetParent(_canvas.transform, false);

            Image img = obj.AddComponent<Image>();
            img.sprite = _whitePixelSprite;
            img.type = Image.Type.Simple;
            Color c = Color.white;
            c.a = 0.1f;
            img.color = c;
            img.raycastTarget = false;

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            float width = Random.Range(250f, 900f);
            float height = Random.Range(1f, 4f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;

            obj.SetActive(false);

            _speedLines.Add(img);
            _speedLineRects.Add(rt);
        }
    }

    private void BuildConfetti()
    {
        for (int i = 0; i < CONFETTI_COUNT; i++)
        {
            GameObject obj = new GameObject("Confetti_" + i);
            obj.transform.SetParent(_canvas.transform, false);

            Image img = obj.AddComponent<Image>();
            img.sprite = _whitePixelSprite;
            img.type = Image.Type.Simple;
            img.color = CONFETTI_COLORS[i % CONFETTI_COLORS.Length];
            img.raycastTarget = false;

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            float w = Random.Range(6f, 18f);
            float h = Random.Range(4f, 12f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;

            obj.SetActive(false);

            _confettiPieces.Add(img);
            _confettiRects.Add(rt);
        }
    }

    private void BuildCrackLines()
    {
        _crackAngles = new float[CRACK_LINE_COUNT];
        _crackLengths = new float[CRACK_LINE_COUNT];

        for (int i = 0; i < CRACK_LINE_COUNT; i++)
        {
            GameObject obj = new GameObject("CrackLine_" + i);
            obj.transform.SetParent(_canvas.transform, false);

            Image img = obj.AddComponent<Image>();
            img.sprite = _whitePixelSprite;
            img.type = Image.Type.Simple;
            img.color = new Color(0.12f, 0.03f, 0.03f, 0f);
            img.raycastTarget = false;

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 2f);

            float angle = Random.Range(0f, 360f);
            _crackAngles[i] = angle;
            _crackLengths[i] = Random.Range(180f, 650f);

            float offsetX = Random.Range(-120f, 120f);
            float offsetY = Random.Range(-100f, 100f);
            rt.anchoredPosition = new Vector2(offsetX, offsetY);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);

            obj.SetActive(false);

            _crackLines.Add(img);
            _crackLineRects.Add(rt);
        }
    }

    private void BuildCutInElements()
    {
        // 暗転バックドロップ
        GameObject bgObj = new GameObject("CutInBg");
        bgObj.transform.SetParent(_canvas.transform, false);

        _cutInBg = bgObj.AddComponent<Image>();
        _cutInBg.color = new Color(0f, 0f, 0f, 0f);
        _cutInBg.raycastTarget = false;

        StretchFull(_cutInBg.rectTransform);
        bgObj.SetActive(false);

        // メインスラッシュライン
        GameObject slashObj = new GameObject("CutInSlash");
        slashObj.transform.SetParent(_canvas.transform, false);

        _cutInSlashLine = slashObj.AddComponent<Image>();
        _cutInSlashLine.sprite = _whitePixelSprite;
        _cutInSlashLine.type = Image.Type.Simple;
        _cutInSlashLine.color = CUTIN_SLASH_COLOR;
        _cutInSlashLine.raycastTarget = false;

        _cutInSlashRect = slashObj.GetComponent<RectTransform>();
        _cutInSlashRect.anchorMin = new Vector2(0.5f, 0.5f);
        _cutInSlashRect.anchorMax = new Vector2(0.5f, 0.5f);
        _cutInSlashRect.sizeDelta = new Vector2(2400f, 5f);
        _cutInSlashRect.anchoredPosition = Vector2.zero;
        _cutInSlashRect.localRotation = Quaternion.Euler(0f, 0f, -25f);

        slashObj.SetActive(false);

        // セカンドスラッシュライン（パラレル、やや薄い）
        GameObject slash2Obj = new GameObject("CutInSlash2");
        slash2Obj.transform.SetParent(_canvas.transform, false);

        _cutInSlashLine2 = slash2Obj.AddComponent<Image>();
        _cutInSlashLine2.sprite = _whitePixelSprite;
        _cutInSlashLine2.type = Image.Type.Simple;
        _cutInSlashLine2.color = new Color(CUTIN_SLASH_COLOR.r, CUTIN_SLASH_COLOR.g, CUTIN_SLASH_COLOR.b, CUTIN_SLASH_COLOR.a * 0.5f);
        _cutInSlashLine2.raycastTarget = false;

        _cutInSlashRect2 = slash2Obj.GetComponent<RectTransform>();
        _cutInSlashRect2.anchorMin = new Vector2(0.5f, 0.5f);
        _cutInSlashRect2.anchorMax = new Vector2(0.5f, 0.5f);
        _cutInSlashRect2.sizeDelta = new Vector2(2400f, 3f);
        _cutInSlashRect2.anchoredPosition = new Vector2(0f, -40f);
        _cutInSlashRect2.localRotation = Quaternion.Euler(0f, 0f, -25f);

        slash2Obj.SetActive(false);

        // キャラ名テキスト
        GameObject nameObj = new GameObject("CutInName");
        nameObj.transform.SetParent(_canvas.transform, false);

        _cutInNameText = nameObj.AddComponent<Text>();
        _cutInNameText.text = string.Empty;
        _cutInNameText.font = _font;
        _cutInNameText.fontSize = 64;
        _cutInNameText.fontStyle = FontStyle.Bold;
        _cutInNameText.alignment = TextAnchor.MiddleRight;
        _cutInNameText.color = CUTIN_GOLD;
        _cutInNameText.raycastTarget = false;
        _cutInNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _cutInNameText.verticalOverflow = VerticalWrapMode.Overflow;

        _cutInNameOutline = nameObj.AddComponent<Outline>();
        _cutInNameOutline.effectColor = new Color(0.4f, 0.25f, 0f, 1f);
        _cutInNameOutline.effectDistance = new Vector2(3f, -3f);

        _cutInNameOutlineOuter = nameObj.AddComponent<Outline>();
        _cutInNameOutlineOuter.effectColor = new Color(0.2f, 0.1f, 0f, 0.7f);
        _cutInNameOutlineOuter.effectDistance = new Vector2(5f, -5f);

        _cutInNameRect = nameObj.GetComponent<RectTransform>();
        _cutInNameRect.anchorMin = new Vector2(0.5f, 0.5f);
        _cutInNameRect.anchorMax = new Vector2(0.5f, 0.5f);
        _cutInNameRect.sizeDelta = new Vector2(700f, 90f);
        _cutInNameRect.anchoredPosition = new Vector2(200f, -120f);

        nameObj.SetActive(false);
    }

    private void BuildSkillNameDisplay()
    {
        // スキル名テキスト
        GameObject obj = new GameObject("SkillNameText");
        obj.transform.SetParent(_canvas.transform, false);

        _skillNameText = obj.AddComponent<Text>();
        _skillNameText.text = string.Empty;
        _skillNameText.font = _font;
        _skillNameText.fontSize = 44;
        _skillNameText.fontStyle = FontStyle.Bold;
        _skillNameText.alignment = TextAnchor.MiddleRight;
        _skillNameText.color = SKILL_NAME_COLOR;
        _skillNameText.raycastTarget = false;
        _skillNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _skillNameText.verticalOverflow = VerticalWrapMode.Overflow;

        _skillNameOutline = obj.AddComponent<Outline>();
        _skillNameOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
        _skillNameOutline.effectDistance = new Vector2(2f, -2f);

        _skillNameRect = obj.GetComponent<RectTransform>();
        _skillNameRect.anchorMin = new Vector2(0.5f, 0.5f);
        _skillNameRect.anchorMax = new Vector2(0.5f, 0.5f);
        _skillNameRect.sizeDelta = new Vector2(600f, 70f);
        _skillNameRect.anchoredPosition = new Vector2(300f, -200f);

        obj.SetActive(false);

        // 下線（ゴールドアクセント）
        GameObject lineObj = new GameObject("SkillNameLine");
        lineObj.transform.SetParent(_canvas.transform, false);

        _skillNameLine = lineObj.AddComponent<Image>();
        _skillNameLine.sprite = _whitePixelSprite;
        _skillNameLine.type = Image.Type.Simple;
        _skillNameLine.color = new Color(1f, 0.85f, 0.2f, 0.6f);
        _skillNameLine.raycastTarget = false;

        _skillNameLineRect = lineObj.GetComponent<RectTransform>();
        _skillNameLineRect.anchorMin = new Vector2(0.5f, 0.5f);
        _skillNameLineRect.anchorMax = new Vector2(0.5f, 0.5f);
        _skillNameLineRect.pivot = new Vector2(1f, 0.5f);
        _skillNameLineRect.sizeDelta = new Vector2(350f, 2f);
        _skillNameLineRect.anchoredPosition = new Vector2(580f, -228f);

        lineObj.SetActive(false);
    }

    private void BuildRadialLines()
    {
        for (int i = 0; i < RADIAL_LINE_COUNT; i++)
        {
            GameObject obj = new GameObject("RadialLine_" + i);
            obj.transform.SetParent(_canvas.transform, false);

            Image img = obj.AddComponent<Image>();
            img.sprite = _whitePixelSprite;
            img.type = Image.Type.Simple;
            Color c = Color.white;
            c.a = 0.75f;
            img.color = c;
            img.raycastTarget = false;

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 3f);
            rt.anchoredPosition = Vector2.zero;

            float angle = (360f / RADIAL_LINE_COUNT) * i;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);

            obj.SetActive(false);

            _radialLines.Add(img);
            _radialLineRects.Add(rt);
        }
    }

    private void BuildBreakVignette()
    {
        GameObject obj = new GameObject("BreakVignette");
        obj.transform.SetParent(_canvas.transform, false);

        _breakVignette = obj.AddComponent<Image>();
        _breakVignette.sprite = _vignetteSprite;
        _breakVignette.type = Image.Type.Simple;
        _breakVignette.color = new Color(0f, 0f, 0f, 0f);
        _breakVignette.raycastTarget = false;
        _breakVignette.preserveAspect = false;

        StretchFull(_breakVignette.rectTransform);
        obj.SetActive(false);
    }

    /// <summary>
    /// RectTransform を親全体にストレッチさせる。
    /// </summary>
    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // ──────────────────────────────────────────────
    // 回復 / 偵察フラッシュ演出
    // ──────────────────────────────────────────────

    private static readonly Color HEAL_FLASH_COLOR = new Color(0.2f, 0.9f, 0.4f, 1f);
    private const float HEAL_FLASH_ALPHA = 0.3f;
    private const float HEAL_FLASH_IN = 0.1f;
    private const float HEAL_FLASH_OUT = 0.2f;

    private static readonly Color SCOUT_FLASH_COLOR = new Color(0.3f, 0.6f, 1f, 1f);
    private const float SCOUT_FLASH_ALPHA = 0.25f;
    private const float SCOUT_FLASH_IN = 0.08f;
    private const float SCOUT_FLASH_HOLD = 0.1f;
    private const float SCOUT_FLASH_OUT = 0.15f;

    /// <summary>回復時の緑フラッシュ演出（約0.3秒）。</summary>
    public IEnumerator PlayHealFlash()
    {
        StartFlash(HEAL_FLASH_COLOR, HEAL_FLASH_ALPHA, HEAL_FLASH_IN, 0f, HEAL_FLASH_OUT);
        yield return new WaitForSecondsRealtime(HEAL_FLASH_IN + HEAL_FLASH_OUT);
    }

    /// <summary>偵察時のスキャンライン風青フラッシュ演出（約0.33秒）。</summary>
    public IEnumerator PlayScoutFlash()
    {
        StartFlash(SCOUT_FLASH_COLOR, SCOUT_FLASH_ALPHA, SCOUT_FLASH_IN, SCOUT_FLASH_HOLD, SCOUT_FLASH_OUT);
        yield return new WaitForSecondsRealtime(SCOUT_FLASH_IN + SCOUT_FLASH_HOLD + SCOUT_FLASH_OUT);
    }
}
