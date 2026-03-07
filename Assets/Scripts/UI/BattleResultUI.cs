// ============================================================
// BattleResultUI.cs
// バトルリザルト画面の UI Toolkit 表示。
// 勝利時はゴールド報酬とドロップアイテムを表示する。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// バトルリザルトを UI Toolkit で表示するコンポーネント。
/// UIDocument がアタッチされた GameObject に配置するか、
/// BattleSceneBootstrap で動的生成して使用する。
/// </summary>
public sealed class BattleResultUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private StyleSheet _styleSheet;

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private VisualElement _root;
    private VisualElement _container;
    private bool _isInitialized;

    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private static readonly Color VICTORY_COLOR = new Color(1f, 0.85f, 0.15f);
    private static readonly Color DEFEAT_COLOR = new Color(0.9f, 0.15f, 0.15f);
    private static readonly Color GOLD_TEXT_COLOR = new Color(1f, 0.92f, 0.5f);
    private static readonly Color ITEM_TEXT_COLOR = new Color(0.85f, 0.95f, 1f);

    // ──────────────────────────────────────────────
    // 初期化
    // ──────────────────────────────────────────────

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        if (_uiDocument == null)
        {
            _uiDocument = gameObject.AddComponent<UIDocument>();
            _uiDocument.panelSettings = FindPanelSettings();
        }

        _root = _uiDocument.rootVisualElement;
        if (_root == null) return;

        if (_styleSheet != null)
            _root.styleSheets.Add(_styleSheet);

        // コンテナ（最初は非表示）
        _container = new VisualElement();
        _container.name = "result-container";
        _container.style.position = Position.Absolute;
        _container.style.left = 0;
        _container.style.right = 0;
        _container.style.top = 0;
        _container.style.bottom = 0;
        _container.style.alignItems = Align.Center;
        _container.style.justifyContent = Justify.Center;
        _container.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        _container.style.display = DisplayStyle.None;
        _root.Add(_container);

        _isInitialized = true;
    }

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>勝利リザルトを表示する。</summary>
    public void ShowVictory(int gold, List<DropResolver.DropResult> drops)
    {
        EnsureInitialized();
        if (_container == null) return;

        _container.Clear();

        // タイトル
        var title = CreateLabel("VICTORY", 48, VICTORY_COLOR, true);
        title.style.marginBottom = 24;
        _container.Add(title);

        // ゴールド
        if (gold > 0)
        {
            var goldLabel = CreateLabel($"+{gold} G", 32, GOLD_TEXT_COLOR, true);
            goldLabel.style.marginBottom = 16;
            _container.Add(goldLabel);
        }

        // ドロップアイテム
        int dropCount = 0;
        foreach (var drop in drops)
        {
            if (!drop.Success || drop.DroppedItem == null) continue;
            dropCount++;

            var itemLabel = CreateLabel($"+ {drop.DroppedItem.DisplayName}", 22, ITEM_TEXT_COLOR, false);
            itemLabel.style.marginBottom = 4;
            _container.Add(itemLabel);
        }

        if (dropCount == 0 && gold == 0)
        {
            var noReward = CreateLabel("報酬なし", 22, new Color(0.6f, 0.6f, 0.6f), false);
            _container.Add(noReward);
        }

        _container.style.display = DisplayStyle.Flex;
        _container.style.opacity = 0f;
        _container.schedule.Execute(() => _container.style.opacity = 1f).ExecuteLater(50);
    }

    /// <summary>敗北リザルトを表示する。</summary>
    public void ShowDefeat()
    {
        EnsureInitialized();
        if (_container == null) return;

        _container.Clear();

        var title = CreateLabel("DEFEAT", 48, DEFEAT_COLOR, true);
        _container.Add(title);

        _container.style.display = DisplayStyle.Flex;
        _container.style.opacity = 0f;
        _container.schedule.Execute(() => _container.style.opacity = 1f).ExecuteLater(50);
    }

    /// <summary>リザルト画面を非表示にする。</summary>
    public void Hide()
    {
        if (_container != null)
            _container.style.display = DisplayStyle.None;
    }

    // ──────────────────────────────────────────────
    // ヘルパー
    // ──────────────────────────────────────────────

    private Label CreateLabel(string text, int fontSize, Color color, bool bold)
    {
        var label = new Label(text);
        label.style.fontSize = fontSize;
        label.style.color = color;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        if (bold)
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
        return label;
    }

    private PanelSettings FindPanelSettings()
    {
        // 既存の UIDocument から PanelSettings を借用
        var existing = FindFirstObjectByType<UIDocument>();
        if (existing != null && existing != _uiDocument && existing.panelSettings != null)
            return existing.panelSettings;

        return null;
    }
}
