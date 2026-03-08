// ============================================================
// TitleScreenUI.cs
// タイトル画面の UI Toolkit メニュー。
// BootScene に配置し、BootLoader からタイトル表示 → ゲーム開始を制御する。
// ============================================================
using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// タイトル画面の UI。「NEW GAME」「CONTINUE」「EXIT」の3ボタン。
/// BootLoader が Show() で表示し、OnStartGame / OnContinueGame イベントで開始通知を受ける。
/// </summary>
public sealed class TitleScreenUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private static readonly Color BG_COLOR = new Color(0.01f, 0.01f, 0.04f, 0.95f);
    private static readonly Color TITLE_COLOR = new Color(1f, 0.75f, 0.15f);
    private static readonly Color SUBTITLE_COLOR = new Color(0.7f, 0.5f, 0.2f);
    private static readonly Color BTN_COLOR = new Color(0.08f, 0.06f, 0.12f, 0.9f);
    private static readonly Color BTN_HOVER_COLOR = new Color(0.15f, 0.12f, 0.22f, 0.95f);
    private static readonly Color BTN_TEXT_COLOR = new Color(0.9f, 0.88f, 0.8f);
    private static readonly Color DISABLED_COLOR = new Color(0.4f, 0.4f, 0.45f);
    private static readonly Color BORDER_COLOR = new Color(0.3f, 0.25f, 0.15f, 0.5f);
    private static readonly Color VERSION_COLOR = new Color(0.4f, 0.4f, 0.5f);

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>NEW GAME が選択されたとき。</summary>
    public event Action OnStartGame;

    /// <summary>CONTINUE が選択されたとき。</summary>
    public event Action OnContinueGame;

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private UIDocument _uiDocument;
    private VisualElement _root;
    private bool _hasSaveData;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>タイトル画面を構築して表示する。</summary>
    public void Show(bool hasSaveData)
    {
        _hasSaveData = hasSaveData;
        EnsureUI();
        BuildTitleScreen();
    }

    // ──────────────────────────────────────────────
    // UI 構築
    // ──────────────────────────────────────────────

    private void EnsureUI()
    {
        if (_uiDocument != null) return;

        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            _uiDocument = gameObject.AddComponent<UIDocument>();

            // 既存の PanelSettings を流用
            var existing = FindFirstObjectByType<UIDocument>();
            if (existing != null && existing != _uiDocument && existing.panelSettings != null)
            {
                _uiDocument.panelSettings = existing.panelSettings;
            }
        }

        _root = _uiDocument.rootVisualElement;
    }

    private void BuildTitleScreen()
    {
        _root.Clear();

        // 全画面背景
        var bg = new VisualElement();
        bg.style.position = Position.Absolute;
        bg.style.left = 0;
        bg.style.right = 0;
        bg.style.top = 0;
        bg.style.bottom = 0;
        bg.style.backgroundColor = BG_COLOR;
        bg.style.alignItems = Align.Center;
        bg.style.justifyContent = Justify.Center;
        _root.Add(bg);

        // タイトルコンテナ
        var container = new VisualElement();
        container.style.alignItems = Align.Center;
        container.style.width = 400;
        bg.Add(container);

        // タイトル
        var title = new Label("DEVIL'S DINER");
        title.style.fontSize = 48;
        title.style.color = TITLE_COLOR;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.marginBottom = 8;
        title.style.letterSpacing = 4;
        container.Add(title);

        // サブタイトル
        var subtitle = new Label("- Demon Chef Simulator -");
        subtitle.style.fontSize = 16;
        subtitle.style.color = SUBTITLE_COLOR;
        subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        subtitle.style.marginBottom = 60;
        container.Add(subtitle);

        // NEW GAME ボタン
        container.Add(CreateTitleButton("NEW GAME", true, () =>
        {
            _root.Clear();
            OnStartGame?.Invoke();
        }));

        // CONTINUE ボタン
        container.Add(CreateTitleButton("CONTINUE", _hasSaveData, () =>
        {
            _root.Clear();
            OnContinueGame?.Invoke();
        }));

        // EXIT ボタン
        container.Add(CreateTitleButton("EXIT", true, () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }));

        // バージョン
        var version = new Label("v0.1.0");
        version.style.fontSize = 12;
        version.style.color = VERSION_COLOR;
        version.style.unityTextAlign = TextAnchor.MiddleCenter;
        version.style.marginTop = 40;
        container.Add(version);
    }

    private VisualElement CreateTitleButton(string text, bool enabled, Action callback)
    {
        var button = new Button();
        button.style.backgroundColor = BTN_COLOR;
        button.style.borderTopLeftRadius = 8;
        button.style.borderTopRightRadius = 8;
        button.style.borderBottomLeftRadius = 8;
        button.style.borderBottomRightRadius = 8;
        button.style.marginBottom = 12;
        button.style.paddingTop = 14;
        button.style.paddingBottom = 14;
        button.style.paddingLeft = 40;
        button.style.paddingRight = 40;
        button.style.width = 280;
        button.style.borderTopWidth = 1;
        button.style.borderBottomWidth = 1;
        button.style.borderLeftWidth = 1;
        button.style.borderRightWidth = 1;
        button.style.borderTopColor = BORDER_COLOR;
        button.style.borderBottomColor = BORDER_COLOR;
        button.style.borderLeftColor = BORDER_COLOR;
        button.style.borderRightColor = BORDER_COLOR;

        var label = new Label(text);
        label.style.fontSize = 22;
        label.style.color = enabled ? BTN_TEXT_COLOR : DISABLED_COLOR;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.letterSpacing = 2;
        button.Add(label);

        if (enabled && callback != null)
        {
            button.clicked += callback;
            button.RegisterCallback<MouseEnterEvent>(evt =>
                button.style.backgroundColor = BTN_HOVER_COLOR);
            button.RegisterCallback<MouseLeaveEvent>(evt =>
                button.style.backgroundColor = BTN_COLOR);
        }
        else
        {
            button.SetEnabled(false);
        }

        return button;
    }
}
