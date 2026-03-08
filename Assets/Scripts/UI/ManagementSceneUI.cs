// ============================================================
// ManagementSceneUI.cs
// 経営シーンの UI Toolkit メニュー（枠組みのみ）。
// 経営パートの各機能ボタンと「1日を終える」ボタンを表示する。
// ============================================================
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 経営シーンの UI メニューを構築・管理するコンポーネント。
/// ゲーム性は未定のため、placeholder ボタン＋フェーズ遷移のみ実装。
/// </summary>
public sealed class ManagementSceneUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [SerializeField] private UIDocument _uiDocument;

    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private static readonly Color BG_COLOR = new Color(0.03f, 0.02f, 0.05f, 0.8f);
    private static readonly Color BTN_COLOR = new Color(0.1f, 0.06f, 0.12f, 0.9f);
    private static readonly Color BTN_HOVER_COLOR = new Color(0.18f, 0.12f, 0.22f, 0.95f);
    private static readonly Color BTN_TEXT_COLOR = new Color(0.95f, 0.9f, 0.85f);
    private static readonly Color TITLE_COLOR = new Color(0.95f, 0.6f, 0.2f);
    private static readonly Color DISABLED_TEXT_COLOR = new Color(0.4f, 0.4f, 0.45f);
    private static readonly Color INFO_COLOR = new Color(0.7f, 0.6f, 0.8f);
    private static readonly Color END_BTN_COLOR = new Color(0.6f, 0.25f, 0.1f, 0.9f);
    private static readonly Color END_BTN_HOVER_COLOR = new Color(0.75f, 0.35f, 0.15f, 0.95f);

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private VisualElement _root;
    private bool _isBuilt;

    // ──────────────────────────────────────────────
    // メニュー項目定義
    // ──────────────────────────────────────────────

    private struct MenuItem
    {
        public string Label;
        public string Description;
        public bool Enabled;
        public System.Action Callback;
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Start()
    {
        BuildUI();
    }

    // ──────────────────────────────────────────────
    // UI 構築
    // ──────────────────────────────────────────────

    private void BuildUI()
    {
        if (_isBuilt) return;

        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();

        if (_uiDocument == null)
        {
            _uiDocument = gameObject.AddComponent<UIDocument>();
            _uiDocument.panelSettings = FindPanelSettings();
        }

        _root = _uiDocument.rootVisualElement;
        if (_root == null) return;

        // ── フルスクリーンオーバーレイ ──
        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.top = 0;
        overlay.style.bottom = 0;
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        _root.Add(overlay);

        // ── メニューパネル（中央） ──
        var menuPanel = new VisualElement();
        menuPanel.style.backgroundColor = BG_COLOR;
        menuPanel.style.paddingTop = 32;
        menuPanel.style.paddingBottom = 32;
        menuPanel.style.paddingLeft = 40;
        menuPanel.style.paddingRight = 40;
        menuPanel.style.borderTopLeftRadius = 12;
        menuPanel.style.borderTopRightRadius = 12;
        menuPanel.style.borderBottomLeftRadius = 12;
        menuPanel.style.borderBottomRightRadius = 12;
        menuPanel.style.width = 400;
        overlay.Add(menuPanel);

        // ── タイトル ──
        var title = new Label("DINER MANAGEMENT");
        title.style.fontSize = 28;
        title.style.color = TITLE_COLOR;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.marginBottom = 8;
        menuPanel.Add(title);

        // ── 日付・所持金 ──
        string dayText = GameManager.Instance != null
            ? $"Day {GameManager.Instance.CurrentDay}"
            : "Day 1";
        string goldText = GameManager.Instance != null
            ? $"{GameManager.Instance.Gold} G"
            : "500 G";

        var infoLabel = new Label($"{dayText}  |  {goldText}");
        infoLabel.style.fontSize = 16;
        infoLabel.style.color = INFO_COLOR;
        infoLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        infoLabel.style.marginBottom = 24;
        menuPanel.Add(infoLabel);

        // ── 経営メニューボタン（placeholder） ──
        var menuItems = new MenuItem[]
        {
            new MenuItem
            {
                Label = "店舗運営",
                Description = "お客さんに料理を提供する",
                Enabled = false,
                Callback = null
            },
            new MenuItem
            {
                Label = "メニュー編成",
                Description = "提供する料理を選ぶ",
                Enabled = false,
                Callback = null
            },
            new MenuItem
            {
                Label = "店舗改装",
                Description = "店の設備を拡張する",
                Enabled = false,
                Callback = null
            },
            new MenuItem
            {
                Label = "従業員配置",
                Description = "スカウトした悪魔を配置する",
                Enabled = false,
                Callback = null
            },
        };

        foreach (var item in menuItems)
        {
            var btn = CreateMenuButton(item);
            menuPanel.Add(btn);
        }

        // ── スペーサー ──
        var spacer = new VisualElement();
        spacer.style.height = 16;
        menuPanel.Add(spacer);

        // ── 1日を終えるボタン（有効） ──
        var endDayBtn = CreateEndDayButton();
        menuPanel.Add(endDayBtn);

        _isBuilt = true;
    }

    // ──────────────────────────────────────────────
    // 通常ボタン生成
    // ──────────────────────────────────────────────

    private VisualElement CreateMenuButton(MenuItem item)
    {
        var button = new Button();
        button.style.backgroundColor = BTN_COLOR;
        button.style.borderTopLeftRadius = 6;
        button.style.borderTopRightRadius = 6;
        button.style.borderBottomLeftRadius = 6;
        button.style.borderBottomRightRadius = 6;
        button.style.marginBottom = 8;
        button.style.paddingTop = 12;
        button.style.paddingBottom = 12;
        button.style.paddingLeft = 16;
        button.style.paddingRight = 16;
        button.style.borderTopWidth = 1;
        button.style.borderBottomWidth = 1;
        button.style.borderLeftWidth = 1;
        button.style.borderRightWidth = 1;
        button.style.borderTopColor = new Color(0.25f, 0.2f, 0.3f, 0.5f);
        button.style.borderBottomColor = new Color(0.25f, 0.2f, 0.3f, 0.5f);
        button.style.borderLeftColor = new Color(0.25f, 0.2f, 0.3f, 0.5f);
        button.style.borderRightColor = new Color(0.25f, 0.2f, 0.3f, 0.5f);

        var label = new Label(item.Label);
        label.style.fontSize = 20;
        label.style.color = item.Enabled ? BTN_TEXT_COLOR : DISABLED_TEXT_COLOR;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginBottom = 2;
        button.Add(label);

        var desc = new Label(item.Description);
        desc.style.fontSize = 12;
        desc.style.color = item.Enabled ? INFO_COLOR : DISABLED_TEXT_COLOR;
        button.Add(desc);

        if (item.Enabled && item.Callback != null)
        {
            button.clicked += item.Callback;
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

    // ──────────────────────────────────────────────
    // 「1日を終える」ボタン
    // ──────────────────────────────────────────────

    private Button CreateEndDayButton()
    {
        var button = new Button();
        button.style.backgroundColor = END_BTN_COLOR;
        button.style.borderTopLeftRadius = 8;
        button.style.borderTopRightRadius = 8;
        button.style.borderBottomLeftRadius = 8;
        button.style.borderBottomRightRadius = 8;
        button.style.paddingTop = 14;
        button.style.paddingBottom = 14;
        button.style.paddingLeft = 16;
        button.style.paddingRight = 16;
        button.style.borderTopWidth = 0;
        button.style.borderBottomWidth = 0;
        button.style.borderLeftWidth = 0;
        button.style.borderRightWidth = 0;

        var label = new Label("1日を終える");
        label.style.fontSize = 22;
        label.style.color = BTN_TEXT_COLOR;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        button.Add(label);

        button.clicked += OnEndDay;
        button.RegisterCallback<MouseEnterEvent>(evt =>
            button.style.backgroundColor = END_BTN_HOVER_COLOR);
        button.RegisterCallback<MouseLeaveEvent>(evt =>
            button.style.backgroundColor = END_BTN_COLOR);

        return button;
    }

    // ──────────────────────────────────────────────
    // コールバック
    // ──────────────────────────────────────────────

    private void OnEndDay()
    {
        Debug.Log("[ManagementSceneUI] 1日を終える → BaseScene へ遷移します。");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvancePhase();
        }
    }

    // ──────────────────────────────────────────────
    // ヘルパー
    // ──────────────────────────────────────────────

    private PanelSettings FindPanelSettings()
    {
        var existing = FindFirstObjectByType<UIDocument>();
        if (existing != null && existing != _uiDocument && existing.panelSettings != null)
            return existing.panelSettings;

        return null;
    }
}
