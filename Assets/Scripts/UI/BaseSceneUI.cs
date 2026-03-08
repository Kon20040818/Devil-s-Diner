// ============================================================
// BaseSceneUI.cs
// 拠点シーン（車の運転席）の UI Toolkit メニュー。
// 出撃・料理・装備・スカウト管理・セーブの各ボタンとサブパネルを管理する。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 拠点シーンの UI メニューを構築・管理するコンポーネント。
/// BaseSceneBootstrap から Initialize() で CookingManager を受け取る。
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
    private static readonly Color PANEL_BG_COLOR = new Color(0.04f, 0.03f, 0.08f, 0.9f);
    private static readonly Color ACCENT_COLOR = new Color(0.4f, 0.8f, 0.4f);
    private static readonly Color CARD_BG_COLOR = new Color(0.08f, 0.05f, 0.1f, 0.9f);
    private static readonly Color BORDER_COLOR = new Color(0.2f, 0.2f, 0.3f, 0.5f);
    private static readonly Color SAVE_BTN_COLOR = new Color(0.2f, 0.5f, 0.7f, 0.9f);
    private static readonly Color SAVE_BTN_HOVER_COLOR = new Color(0.3f, 0.6f, 0.8f, 0.95f);

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private VisualElement _root;
    private Label _infoLabel;
    private bool _isBuilt;

    private CookingManager _cookingMgr;

    // パネル参照
    private VisualElement _mainMenuPanel;
    private VisualElement _cookingPanel;
    private VisualElement _equipmentPanel;
    private VisualElement _scoutPanel;
    private VisualElement _resultPanel;

    // 調理用
    private VisualElement _cookingRecipeListContainer;
    private Label _cookingInfoLabel;

    // スカウト管理用
    private VisualElement _scoutListContainer;

    // 結果パネル用
    private Label _resultTitleLabel;
    private Label _resultBodyLabel;

    // セーブフィードバック用
    private Label _saveFeedbackLabel;

    // ──────────────────────────────────────────────
    // 初期化
    // ──────────────────────────────────────────────

    /// <summary>BaseSceneBootstrap から呼ばれる初期化。</summary>
    public void Initialize(CookingManager cookingMgr)
    {
        _cookingMgr = cookingMgr;
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

        BuildMainMenu();
        BuildCookingPanel();
        BuildEquipmentPanel();
        BuildScoutPanel();
        BuildResultPanel();

        ShowMainMenu();
        _isBuilt = true;
    }

    // ──────────────────────────────────────────────
    // メインメニュー
    // ──────────────────────────────────────────────

    private void BuildMainMenu()
    {
        _mainMenuPanel = new VisualElement();
        _mainMenuPanel.style.position = Position.Absolute;
        _mainMenuPanel.style.left = 0;
        _mainMenuPanel.style.right = 0;
        _mainMenuPanel.style.top = 0;
        _mainMenuPanel.style.bottom = 0;
        _mainMenuPanel.style.alignItems = Align.FlexEnd;
        _mainMenuPanel.style.justifyContent = Justify.Center;
        _mainMenuPanel.style.paddingRight = 80;
        _root.Add(_mainMenuPanel);

        var menuPanel = CreatePanelContainer(320);
        _mainMenuPanel.Add(menuPanel);

        // タイトル
        menuPanel.Add(CreateTitle("DEVIL'S DINER"));

        // 日付・所持金
        string dayText = GameManager.Instance != null ? $"Day {GameManager.Instance.CurrentDay}" : "Day 1";
        string goldText = GameManager.Instance != null ? $"{GameManager.Instance.Gold} G" : "500 G";
        _infoLabel = CreateInfoLabel($"{dayText}  |  {goldText}");
        _infoLabel.style.marginBottom = 24;
        menuPanel.Add(_infoLabel);

        // ボタン
        menuPanel.Add(CreateMenuButton("出撃", "フィールドへ出発する", true, OnSortie));
        menuPanel.Add(CreateMenuButton("料理", "素材から料理を作る", true, () => ShowPanel(_cookingPanel)));
        menuPanel.Add(CreateMenuButton("装備", "武器やスキルを変更する", false, null));
        menuPanel.Add(CreateMenuButton("スカウト管理", "雇用した悪魔を管理する", true, () => ShowPanel(_scoutPanel)));
        menuPanel.Add(CreateSaveButton());

        // セーブフィードバック
        _saveFeedbackLabel = CreateInfoLabel("");
        _saveFeedbackLabel.style.color = ACCENT_COLOR;
        _saveFeedbackLabel.style.display = DisplayStyle.None;
        menuPanel.Add(_saveFeedbackLabel);
    }

    // ──────────────────────────────────────────────
    // 料理パネル
    // ──────────────────────────────────────────────

    private void BuildCookingPanel()
    {
        _cookingPanel = CreateFullscreenPanel();
        _root.Add(_cookingPanel);

        var container = CreatePanelContainer(500);
        _cookingPanel.Add(container);

        container.Add(CreateTitle("料理"));

        _cookingInfoLabel = CreateInfoLabel("レシピを選んで調理しましょう");
        container.Add(_cookingInfoLabel);

        _cookingRecipeListContainer = new VisualElement();
        _cookingRecipeListContainer.style.maxHeight = 350;
        _cookingRecipeListContainer.style.overflow = Overflow.Hidden;
        _cookingRecipeListContainer.style.marginBottom = 16;
        container.Add(_cookingRecipeListContainer);

        container.Add(CreateBackButton());
    }

    // ──────────────────────────────────────────────
    // 装備パネル
    // ──────────────────────────────────────────────

    private void BuildEquipmentPanel()
    {
        _equipmentPanel = CreateFullscreenPanel();
        _root.Add(_equipmentPanel);

        var container = CreatePanelContainer(400);
        _equipmentPanel.Add(container);

        container.Add(CreateTitle("装備"));

        var comingSoon = CreateInfoLabel("Coming Soon...");
        comingSoon.style.fontSize = 24;
        comingSoon.style.marginTop = 40;
        comingSoon.style.marginBottom = 40;
        container.Add(comingSoon);

        container.Add(CreateBackButton());
    }

    // ──────────────────────────────────────────────
    // スカウト管理パネル（読み取り専用）
    // ──────────────────────────────────────────────

    private void BuildScoutPanel()
    {
        _scoutPanel = CreateFullscreenPanel();
        _root.Add(_scoutPanel);

        var container = CreatePanelContainer(500);
        _scoutPanel.Add(container);

        container.Add(CreateTitle("スカウト管理"));

        _scoutListContainer = new VisualElement();
        _scoutListContainer.style.maxHeight = 400;
        _scoutListContainer.style.overflow = Overflow.Hidden;
        _scoutListContainer.style.marginBottom = 16;
        container.Add(_scoutListContainer);

        container.Add(CreateBackButton());
    }

    // ──────────────────────────────────────────────
    // 結果パネル
    // ──────────────────────────────────────────────

    private void BuildResultPanel()
    {
        _resultPanel = CreateFullscreenPanel();
        _root.Add(_resultPanel);

        var container = CreatePanelContainer(400);
        _resultPanel.Add(container);

        _resultTitleLabel = CreateTitle("");
        container.Add(_resultTitleLabel);

        _resultBodyLabel = CreateInfoLabel("");
        _resultBodyLabel.style.whiteSpace = WhiteSpace.Normal;
        _resultBodyLabel.style.marginTop = 16;
        _resultBodyLabel.style.marginBottom = 24;
        container.Add(_resultBodyLabel);

        container.Add(CreateActionButton("OK", new Color(0.6f, 0.25f, 0.1f, 0.9f), ShowMainMenu));
    }

    // ──────────────────────────────────────────────
    // パネル切替
    // ──────────────────────────────────────────────

    private void ShowMainMenu()
    {
        HideAllPanels();
        _mainMenuPanel.style.display = DisplayStyle.Flex;
        RefreshMainMenuInfo();
    }

    private void ShowPanel(VisualElement panel)
    {
        HideAllPanels();
        panel.style.display = DisplayStyle.Flex;

        if (panel == _cookingPanel) RefreshCookingPanel();
        else if (panel == _scoutPanel) RefreshScoutPanel();
    }

    private void HideAllPanels()
    {
        _mainMenuPanel.style.display = DisplayStyle.None;
        _cookingPanel.style.display = DisplayStyle.None;
        _equipmentPanel.style.display = DisplayStyle.None;
        _scoutPanel.style.display = DisplayStyle.None;
        _resultPanel.style.display = DisplayStyle.None;
    }

    private void ShowResult(string title, string body)
    {
        HideAllPanels();
        _resultTitleLabel.text = title;
        _resultBodyLabel.text = body;
        _resultPanel.style.display = DisplayStyle.Flex;
    }

    // ──────────────────────────────────────────────
    // メインメニュー更新
    // ──────────────────────────────────────────────

    private void RefreshMainMenuInfo()
    {
        if (_infoLabel == null || GameManager.Instance == null) return;
        _infoLabel.text = $"Day {GameManager.Instance.CurrentDay}  |  {GameManager.Instance.Gold} G";
    }

    // ──────────────────────────────────────────────
    // 料理ロジック
    // ──────────────────────────────────────────────

    private void RefreshCookingPanel()
    {
        _cookingRecipeListContainer.Clear();

        if (_cookingMgr == null)
        {
            _cookingInfoLabel.text = "CookingManager が未初期化です。";
            return;
        }

        var recipes = _cookingMgr.GetAvailableRecipes();
        _cookingInfoLabel.text = $"シェフLv.{_cookingMgr.ChefLevel}  |  利用可能レシピ: {recipes.Count}件";

        if (recipes.Count == 0)
        {
            _cookingRecipeListContainer.Add(CreateInfoLabel("利用可能なレシピがありません。"));
            return;
        }

        foreach (var recipe in recipes)
        {
            var row = CreateCardRow();

            var nameLabel = new Label(recipe.DisplayName);
            nameLabel.style.fontSize = 16;
            nameLabel.style.color = BTN_TEXT_COLOR;
            nameLabel.style.flexGrow = 1;
            row.Add(nameLabel);

            // 素材一覧
            string ingredients = "";
            foreach (var slot in recipe.Ingredients)
            {
                if (slot.Ingredient == null) continue;
                int have = GameManager.Instance != null
                    ? GameManager.Instance.Inventory.GetCount(slot.Ingredient)
                    : 0;
                ingredients += $"{slot.Ingredient.DisplayName}({have}/{slot.Amount}) ";
            }
            var ingredientLabel = new Label(ingredients.Trim());
            ingredientLabel.style.fontSize = 11;
            ingredientLabel.style.color = INFO_COLOR;
            ingredientLabel.style.width = 200;
            row.Add(ingredientLabel);

            // 調理ボタン
            bool canCook = _cookingMgr.CanCook(recipe);
            var cookBtn = new Button();
            cookBtn.style.backgroundColor = canCook ? ACCENT_COLOR : DISABLED_TEXT_COLOR;
            cookBtn.style.borderTopLeftRadius = 4;
            cookBtn.style.borderTopRightRadius = 4;
            cookBtn.style.borderBottomLeftRadius = 4;
            cookBtn.style.borderBottomRightRadius = 4;
            cookBtn.style.paddingTop = 4;
            cookBtn.style.paddingBottom = 4;
            cookBtn.style.paddingLeft = 12;
            cookBtn.style.paddingRight = 12;
            cookBtn.style.borderTopWidth = 0;
            cookBtn.style.borderBottomWidth = 0;
            cookBtn.style.borderLeftWidth = 0;
            cookBtn.style.borderRightWidth = 0;

            var cookLabel = new Label("調理");
            cookLabel.style.fontSize = 14;
            cookLabel.style.color = canCook ? new Color(0.05f, 0.05f, 0.05f) : BTN_TEXT_COLOR;
            cookBtn.Add(cookLabel);

            if (canCook)
            {
                RecipeData capturedRecipe = recipe;
                cookBtn.clicked += () => ExecuteCook(capturedRecipe);
            }
            else
            {
                cookBtn.SetEnabled(false);
            }

            row.Add(cookBtn);
            _cookingRecipeListContainer.Add(row);
        }
    }

    private void ExecuteCook(RecipeData recipe)
    {
        if (_cookingMgr == null) return;

        float freshnessBuff = GameManager.Instance != null
            ? GameManager.Instance.DailyFreshnessBuff
            : 1f;

        // 拠点での調理はカレンダーイベントなし
        var result = _cookingMgr.Cook(recipe, freshnessBuff);

        if (result.Success)
        {
            ShowResult("調理完了！",
                $"{result.Dish}\n" +
                $"品質スコア: {result.QualityScore:F2}\n" +
                $"販売価格: {result.Dish.ShopPrice} G");
        }
        else
        {
            ShowResult("調理失敗", $"{recipe.DisplayName} の調理条件を満たしていません。");
        }
    }

    // ──────────────────────────────────────────────
    // スカウト管理ロジック（読み取り専用）
    // ──────────────────────────────────────────────

    private void RefreshScoutPanel()
    {
        _scoutListContainer.Clear();

        if (GameManager.Instance == null || GameManager.Instance.Staff == null)
        {
            _scoutListContainer.Add(CreateInfoLabel("StaffManager が未初期化です。"));
            return;
        }

        var staffMgr = GameManager.Instance.Staff;

        // 常勤セクション
        _scoutListContainer.Add(CreateSectionHeader($"常勤スタッフ ({staffMgr.PermanentStaff.Count}/3)"));
        if (staffMgr.PermanentStaff.Count == 0)
        {
            _scoutListContainer.Add(CreateInfoLabel("  （なし）"));
        }
        foreach (var staff in staffMgr.PermanentStaff)
        {
            _scoutListContainer.Add(CreateStaffCardReadOnly(staff));
        }

        // 臨時セクション
        _scoutListContainer.Add(CreateSectionHeader($"臨時スタッフ ({staffMgr.TemporaryStaff.Count}/2)"));
        if (staffMgr.TemporaryStaff.Count == 0)
        {
            _scoutListContainer.Add(CreateInfoLabel("  （なし）"));
        }
        foreach (var staff in staffMgr.TemporaryStaff)
        {
            _scoutListContainer.Add(CreateStaffCardReadOnly(staff));
        }

        // 日給合計
        var salaryLabel = CreateInfoLabel($"日給合計: {staffMgr.GetTotalDailySalary()} G");
        salaryLabel.style.marginTop = 12;
        _scoutListContainer.Add(salaryLabel);
    }

    private VisualElement CreateStaffCardReadOnly(StaffInstance staff)
    {
        var card = CreateCardRow();
        card.style.flexDirection = FlexDirection.Column;
        card.style.alignItems = Align.Stretch;
        card.style.paddingTop = 8;
        card.style.paddingBottom = 8;

        // 名前行
        var nameRow = new VisualElement();
        nameRow.style.flexDirection = FlexDirection.Row;
        nameRow.style.justifyContent = Justify.SpaceBetween;
        nameRow.style.marginBottom = 4;
        card.Add(nameRow);

        var nameLabel = new Label(staff.DisplayName);
        nameLabel.style.fontSize = 16;
        nameLabel.style.color = BTN_TEXT_COLOR;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameRow.Add(nameLabel);

        var slotLabel = new Label(staff.SlotType == StaffSlotType.Permanent
            ? $"常勤 | 日給: {staff.CalculateSalary()}G"
            : "臨時（無給）");
        slotLabel.style.fontSize = 12;
        slotLabel.style.color = INFO_COLOR;
        nameRow.Add(slotLabel);

        // 種族効果
        if (staff.Race != null)
        {
            var raceLabel = new Label($"種族効果: {staff.Race.FixedEffect} +{staff.Race.FixedEffectValue:P0}");
            raceLabel.style.fontSize = 12;
            raceLabel.style.color = new Color(0.6f, 0.8f, 1f);
            raceLabel.style.marginBottom = 2;
            card.Add(raceLabel);
        }

        // バフ一覧
        if (staff.RandomBuffs.Length > 0)
        {
            string buffText = "";
            foreach (var buff in staff.RandomBuffs)
            {
                if (buff == null) continue;
                buffText += $"[{buff.DisplayName} R{buff.Rarity}] ";
            }
            var buffLabel = new Label(buffText.Trim());
            buffLabel.style.fontSize = 11;
            buffLabel.style.color = ACCENT_COLOR;
            card.Add(buffLabel);
        }

        return card;
    }

    // ──────────────────────────────────────────────
    // セーブ処理
    // ──────────────────────────────────────────────

    private void ExecuteSave()
    {
        if (GameManager.Instance == null || GameManager.Instance.SaveData == null)
        {
            Debug.LogWarning("[BaseSceneUI] SaveDataManager が未初期化です。");
            return;
        }

        GameManager.Instance.SaveData.Save();

        // UIフィードバック
        _saveFeedbackLabel.text = "セーブ完了！";
        _saveFeedbackLabel.style.display = DisplayStyle.Flex;

        // 2秒後に非表示
        _saveFeedbackLabel.schedule.Execute(() =>
        {
            _saveFeedbackLabel.style.display = DisplayStyle.None;
        }).ExecuteLater(2000);
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
    // UI ヘルパー — パネル
    // ──────────────────────────────────────────────

    private VisualElement CreateFullscreenPanel()
    {
        var panel = new VisualElement();
        panel.style.position = Position.Absolute;
        panel.style.left = 0;
        panel.style.right = 0;
        panel.style.top = 0;
        panel.style.bottom = 0;
        panel.style.alignItems = Align.Center;
        panel.style.justifyContent = Justify.Center;
        panel.style.display = DisplayStyle.None;
        return panel;
    }

    private VisualElement CreatePanelContainer(int width)
    {
        var container = new VisualElement();
        container.style.backgroundColor = PANEL_BG_COLOR;
        container.style.paddingTop = 24;
        container.style.paddingBottom = 24;
        container.style.paddingLeft = 32;
        container.style.paddingRight = 32;
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        container.style.width = width;
        return container;
    }

    // ──────────────────────────────────────────────
    // UI ヘルパー — テキスト
    // ──────────────────────────────────────────────

    private Label CreateTitle(string text)
    {
        var label = new Label(text);
        label.style.fontSize = 28;
        label.style.color = TITLE_COLOR;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.marginBottom = 8;
        return label;
    }

    private Label CreateInfoLabel(string text)
    {
        var label = new Label(text);
        label.style.fontSize = 14;
        label.style.color = INFO_COLOR;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.marginBottom = 8;
        return label;
    }

    private Label CreateSectionHeader(string text)
    {
        var label = new Label(text);
        label.style.fontSize = 18;
        label.style.color = TITLE_COLOR;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginTop = 12;
        label.style.marginBottom = 8;
        return label;
    }

    // ──────────────────────────────────────────────
    // UI ヘルパー — ボタン
    // ──────────────────────────────────────────────

    private VisualElement CreateMenuButton(string label, string description, bool enabled, System.Action callback)
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
        button.style.borderTopColor = BORDER_COLOR;
        button.style.borderBottomColor = BORDER_COLOR;
        button.style.borderLeftColor = BORDER_COLOR;
        button.style.borderRightColor = BORDER_COLOR;

        var nameLabel = new Label(label);
        nameLabel.style.fontSize = 20;
        nameLabel.style.color = enabled ? BTN_TEXT_COLOR : DISABLED_TEXT_COLOR;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.marginBottom = 2;
        button.Add(nameLabel);

        var descLabel = new Label(description);
        descLabel.style.fontSize = 12;
        descLabel.style.color = enabled ? INFO_COLOR : DISABLED_TEXT_COLOR;
        button.Add(descLabel);

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

    private VisualElement CreateSaveButton()
    {
        var button = new Button();
        button.style.backgroundColor = SAVE_BTN_COLOR;
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
        button.style.borderTopColor = BORDER_COLOR;
        button.style.borderBottomColor = BORDER_COLOR;
        button.style.borderLeftColor = BORDER_COLOR;
        button.style.borderRightColor = BORDER_COLOR;

        var nameLabel = new Label("セーブ");
        nameLabel.style.fontSize = 20;
        nameLabel.style.color = BTN_TEXT_COLOR;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.marginBottom = 2;
        button.Add(nameLabel);

        var descLabel = new Label("ゲームデータを保存する");
        descLabel.style.fontSize = 12;
        descLabel.style.color = INFO_COLOR;
        button.Add(descLabel);

        button.clicked += ExecuteSave;
        button.RegisterCallback<MouseEnterEvent>(evt =>
            button.style.backgroundColor = SAVE_BTN_HOVER_COLOR);
        button.RegisterCallback<MouseLeaveEvent>(evt =>
            button.style.backgroundColor = SAVE_BTN_COLOR);

        return button;
    }

    private Button CreateActionButton(string text, Color bgColor, System.Action callback)
    {
        var button = new Button();
        button.style.backgroundColor = bgColor;
        button.style.borderTopLeftRadius = 6;
        button.style.borderTopRightRadius = 6;
        button.style.borderBottomLeftRadius = 6;
        button.style.borderBottomRightRadius = 6;
        button.style.paddingTop = 10;
        button.style.paddingBottom = 10;
        button.style.paddingLeft = 24;
        button.style.paddingRight = 24;
        button.style.marginBottom = 8;
        button.style.borderTopWidth = 0;
        button.style.borderBottomWidth = 0;
        button.style.borderLeftWidth = 0;
        button.style.borderRightWidth = 0;

        var label = new Label(text);
        label.style.fontSize = 18;
        label.style.color = BTN_TEXT_COLOR;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        button.Add(label);

        if (callback != null)
            button.clicked += callback;

        return button;
    }

    private Button CreateBackButton()
    {
        var button = new Button();
        button.style.backgroundColor = BTN_COLOR;
        button.style.borderTopLeftRadius = 6;
        button.style.borderTopRightRadius = 6;
        button.style.borderBottomLeftRadius = 6;
        button.style.borderBottomRightRadius = 6;
        button.style.paddingTop = 10;
        button.style.paddingBottom = 10;
        button.style.paddingLeft = 24;
        button.style.paddingRight = 24;
        button.style.marginTop = 8;
        button.style.borderTopWidth = 1;
        button.style.borderBottomWidth = 1;
        button.style.borderLeftWidth = 1;
        button.style.borderRightWidth = 1;
        button.style.borderTopColor = BORDER_COLOR;
        button.style.borderBottomColor = BORDER_COLOR;
        button.style.borderLeftColor = BORDER_COLOR;
        button.style.borderRightColor = BORDER_COLOR;

        var label = new Label("戻る");
        label.style.fontSize = 16;
        label.style.color = BTN_TEXT_COLOR;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        button.Add(label);

        button.clicked += ShowMainMenu;
        button.RegisterCallback<MouseEnterEvent>(evt =>
            button.style.backgroundColor = BTN_HOVER_COLOR);
        button.RegisterCallback<MouseLeaveEvent>(evt =>
            button.style.backgroundColor = BTN_COLOR);

        return button;
    }

    // ──────────────────────────────────────────────
    // UI ヘルパー — カード
    // ──────────────────────────────────────────────

    private VisualElement CreateCardRow()
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.backgroundColor = CARD_BG_COLOR;
        row.style.borderTopLeftRadius = 4;
        row.style.borderTopRightRadius = 4;
        row.style.borderBottomLeftRadius = 4;
        row.style.borderBottomRightRadius = 4;
        row.style.paddingTop = 6;
        row.style.paddingBottom = 6;
        row.style.paddingLeft = 12;
        row.style.paddingRight = 12;
        row.style.marginBottom = 4;
        row.style.borderTopWidth = 1;
        row.style.borderBottomWidth = 1;
        row.style.borderLeftWidth = 1;
        row.style.borderRightWidth = 1;
        row.style.borderTopColor = BORDER_COLOR;
        row.style.borderBottomColor = BORDER_COLOR;
        row.style.borderLeftColor = BORDER_COLOR;
        row.style.borderRightColor = BORDER_COLOR;
        return row;
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
