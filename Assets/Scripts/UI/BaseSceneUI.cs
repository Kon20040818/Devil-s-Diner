// ============================================================
// BaseSceneUI.cs
// 拠点シーン（車の運転席）の UI Toolkit メニュー。
// 出撃・料理・装備・スカウト・セーブの各ボタンを表示する。
// ============================================================
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 拠点シーンの UI メニューを構築・管理するコンポーネント。
/// UIDocument にインラインで VisualElement を構築する。
/// </summary>
public sealed class BaseSceneUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [SerializeField] private UIDocument _uiDocument;

    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private static readonly Color BG_COLOR = new Color(0.02f, 0.02f, 0.06f, 0.75f);
    private static readonly Color BTN_COLOR = new Color(0.08f, 0.08f, 0.15f, 0.9f);
    private static readonly Color BTN_HOVER_COLOR = new Color(0.15f, 0.15f, 0.25f, 0.95f);
    private static readonly Color BTN_TEXT_COLOR = new Color(0.9f, 0.9f, 0.95f);
    private static readonly Color TITLE_COLOR = new Color(1f, 0.85f, 0.3f);
    private static readonly Color DISABLED_TEXT_COLOR = new Color(0.4f, 0.4f, 0.45f);
    private static readonly Color INFO_COLOR = new Color(0.6f, 0.7f, 0.8f);

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private VisualElement _root;
    private Label _infoLabel;
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

        // ── フルスクリーンコンテナ ──
        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.top = 0;
        overlay.style.bottom = 0;
        overlay.style.alignItems = Align.FlexEnd;
        overlay.style.justifyContent = Justify.Center;
        overlay.style.paddingRight = 80;
        _root.Add(overlay);

        // ── メニューパネル（右寄せ） ──
        var menuPanel = new VisualElement();
        menuPanel.style.backgroundColor = BG_COLOR;
        menuPanel.style.paddingTop = 24;
        menuPanel.style.paddingBottom = 24;
        menuPanel.style.paddingLeft = 32;
        menuPanel.style.paddingRight = 32;
        menuPanel.style.borderTopLeftRadius = 12;
        menuPanel.style.borderTopRightRadius = 12;
        menuPanel.style.borderBottomLeftRadius = 12;
        menuPanel.style.borderBottomRightRadius = 12;
        menuPanel.style.width = 320;
        overlay.Add(menuPanel);

        // ── タイトル ──
        var title = new Label("DEVIL'S DINER");
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

        _infoLabel = new Label($"{dayText}  |  {goldText}");
        _infoLabel.style.fontSize = 16;
        _infoLabel.style.color = INFO_COLOR;
        _infoLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        _infoLabel.style.marginBottom = 24;
        menuPanel.Add(_infoLabel);

        // ── メニューボタン生成 ──
        var menuItems = new MenuItem[]
        {
            new MenuItem
            {
                Label = "出撃",
                Description = "フィールドへ出発する",
                Enabled = true,
                Callback = OnSortie
            },
            new MenuItem
            {
                Label = "料理",
                Description = "素材から料理を作る",
                Enabled = false,
                Callback = null
            },
            new MenuItem
            {
                Label = "装備",
                Description = "武器やスキルを変更する",
                Enabled = false,
                Callback = null
            },
            new MenuItem
            {
                Label = "スカウト管理",
                Description = "雇用した悪魔を管理する",
                Enabled = false,
                Callback = null
            },
            new MenuItem
            {
                Label = "セーブ",
                Description = "ゲームデータを保存する",
                Enabled = false,
                Callback = null
            },
        };

        foreach (var item in menuItems)
        {
            var btn = CreateMenuButton(item);
            menuPanel.Add(btn);
        }

        _isBuilt = true;
    }

    // ──────────────────────────────────────────────
    // ボタン生成
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
        button.style.borderTopColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
        button.style.borderBottomColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
        button.style.borderLeftColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
        button.style.borderRightColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);

        // ラベル
        var label = new Label(item.Label);
        label.style.fontSize = 20;
        label.style.color = item.Enabled ? BTN_TEXT_COLOR : DISABLED_TEXT_COLOR;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginBottom = 2;
        button.Add(label);

        // 説明
        var desc = new Label(item.Description);
        desc.style.fontSize = 12;
        desc.style.color = item.Enabled ? INFO_COLOR : DISABLED_TEXT_COLOR;
        button.Add(desc);

        if (item.Enabled && item.Callback != null)
        {
            button.clicked += item.Callback;

            // ホバーエフェクト
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
    // メニューコールバック
    // ──────────────────────────────────────────────

    private void OnSortie()
    {
        Debug.Log("[BaseSceneUI] 出撃！FieldScene へ遷移します。");

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
