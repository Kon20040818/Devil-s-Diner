// ============================================================
// ManagementSceneUI.cs
// 経営シーンの UI Toolkit メニュー。
// 4つのサブパネル（店舗運営/メニュー編成/店舗改装/従業員配置）と
// メインメニューのパネル切替を管理する。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 経営シーンの UI メニューを構築・管理するコンポーネント。
/// ManagementSceneBootstrap から Initialize() で CookingManager/DinerService を受け取る。
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
    private static readonly Color PANEL_BG_COLOR = new Color(0.05f, 0.03f, 0.08f, 0.9f);
    private static readonly Color ACCENT_COLOR = new Color(0.4f, 0.8f, 0.4f);
    private static readonly Color SELECTED_COLOR = new Color(0.2f, 0.15f, 0.3f, 0.95f);
    private static readonly Color CARD_BG_COLOR = new Color(0.08f, 0.05f, 0.1f, 0.9f);
    private static readonly Color BORDER_COLOR = new Color(0.25f, 0.2f, 0.3f, 0.5f);

    // ──────────────────────────────────────────────
    // ランタイム参照
    // ──────────────────────────────────────────────

    private VisualElement _root;
    private bool _isBuilt;

    private CookingManager _cookingMgr;
    private DinerService _dinerService;

    // パネル参照
    private VisualElement _mainMenuPanel;
    private VisualElement _dinerOperationsPanel;
    private VisualElement _menuCompositionPanel;
    private VisualElement _shopExpansionPanel;
    private VisualElement _staffPlacementPanel;
    private VisualElement _resultPanel;

    // 店舗運営用
    private readonly List<DishInstance> _selectedDishes = new List<DishInstance>();
    private VisualElement _dinerDishListContainer;

    // メニュー編成用
    private VisualElement _cookingRecipeListContainer;
    private Label _cookingInfoLabel;

    // 従業員配置用
    private VisualElement _staffListContainer;

    // 結果パネル用
    private Label _resultTitleLabel;
    private Label _resultBodyLabel;

    // メインメニュー情報
    private Label _infoLabel;

    // ──────────────────────────────────────────────
    // 初期化
    // ──────────────────────────────────────────────

    /// <summary>ManagementSceneBootstrap から呼ばれる初期化。</summary>
    public void Initialize(CookingManager cookingMgr, DinerService dinerService)
    {
        _cookingMgr = cookingMgr;
        _dinerService = dinerService;
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
        BuildDinerOperationsPanel();
        BuildMenuCompositionPanel();
        BuildShopExpansionPanel();
        BuildStaffPlacementPanel();
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
        _mainMenuPanel.style.alignItems = Align.Center;
        _mainMenuPanel.style.justifyContent = Justify.Center;
        _root.Add(_mainMenuPanel);

        var menuPanel = CreatePanelContainer(400);
        _mainMenuPanel.Add(menuPanel);

        // タイトル
        menuPanel.Add(CreateTitle("DINER MANAGEMENT"));

        // 日付・所持金
        string dayText = GameManager.Instance != null ? $"Day {GameManager.Instance.CurrentDay}" : "Day 1";
        string goldText = GameManager.Instance != null ? $"{GameManager.Instance.Gold} G" : "500 G";
        _infoLabel = CreateInfoLabel($"{dayText}  |  {goldText}");
        _infoLabel.style.marginBottom = 24;
        menuPanel.Add(_infoLabel);

        // ボタン
        menuPanel.Add(CreateMenuButton("店舗運営", "お客さんに料理を提供する", true, () => ShowPanel(_dinerOperationsPanel)));
        menuPanel.Add(CreateMenuButton("メニュー編成", "レシピから料理を調理する", true, () => ShowPanel(_menuCompositionPanel)));
        menuPanel.Add(CreateMenuButton("店舗改装", "店の設備を拡張する", false, null));
        menuPanel.Add(CreateMenuButton("従業員配置", "スカウトした悪魔を配置する", true, () => ShowPanel(_staffPlacementPanel)));

        // スペーサー
        var spacer = new VisualElement();
        spacer.style.height = 16;
        menuPanel.Add(spacer);

        // 1日を終える
        menuPanel.Add(CreateEndDayButton());
    }

    // ──────────────────────────────────────────────
    // 店舗運営パネル
    // ──────────────────────────────────────────────

    private void BuildDinerOperationsPanel()
    {
        _dinerOperationsPanel = CreateFullscreenPanel();
        _root.Add(_dinerOperationsPanel);

        var container = CreatePanelContainer(500);
        _dinerOperationsPanel.Add(container);

        container.Add(CreateTitle("店舗運営"));
        container.Add(CreateInfoLabel("提供する料理を選んで営業を開始しましょう"));

        // 料理リスト
        _dinerDishListContainer = new VisualElement();
        _dinerDishListContainer.style.maxHeight = 300;
        _dinerDishListContainer.style.overflow = Overflow.Hidden;
        _dinerDishListContainer.style.marginBottom = 16;
        container.Add(_dinerDishListContainer);

        // 営業開始ボタン
        container.Add(CreateActionButton("営業開始", ACCENT_COLOR, () => ExecuteDinerService()));
        container.Add(CreateBackButton());
    }

    // ──────────────────────────────────────────────
    // メニュー編成（調理）パネル
    // ──────────────────────────────────────────────

    private void BuildMenuCompositionPanel()
    {
        _menuCompositionPanel = CreateFullscreenPanel();
        _root.Add(_menuCompositionPanel);

        var container = CreatePanelContainer(500);
        _menuCompositionPanel.Add(container);

        container.Add(CreateTitle("メニュー編成"));

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
    // 店舗改装パネル
    // ──────────────────────────────────────────────

    private void BuildShopExpansionPanel()
    {
        _shopExpansionPanel = CreateFullscreenPanel();
        _root.Add(_shopExpansionPanel);

        var container = CreatePanelContainer(400);
        _shopExpansionPanel.Add(container);

        container.Add(CreateTitle("店舗改装"));

        var comingSoon = CreateInfoLabel("Coming Soon...");
        comingSoon.style.fontSize = 24;
        comingSoon.style.marginTop = 40;
        comingSoon.style.marginBottom = 40;
        container.Add(comingSoon);

        container.Add(CreateBackButton());
    }

    // ──────────────────────────────────────────────
    // 従業員配置パネル
    // ──────────────────────────────────────────────

    private void BuildStaffPlacementPanel()
    {
        _staffPlacementPanel = CreateFullscreenPanel();
        _root.Add(_staffPlacementPanel);

        var container = CreatePanelContainer(520);
        _staffPlacementPanel.Add(container);

        container.Add(CreateTitle("従業員配置"));

        _staffListContainer = new VisualElement();
        _staffListContainer.style.maxHeight = 400;
        _staffListContainer.style.overflow = Overflow.Hidden;
        _staffListContainer.style.marginBottom = 16;
        container.Add(_staffListContainer);

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

        container.Add(CreateActionButton("OK", END_BTN_COLOR, ShowMainMenu));
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

        if (panel == _dinerOperationsPanel) RefreshDinerPanel();
        else if (panel == _menuCompositionPanel) RefreshCookingPanel();
        else if (panel == _staffPlacementPanel) RefreshStaffPanel();
    }

    private void HideAllPanels()
    {
        _mainMenuPanel.style.display = DisplayStyle.None;
        _dinerOperationsPanel.style.display = DisplayStyle.None;
        _menuCompositionPanel.style.display = DisplayStyle.None;
        _shopExpansionPanel.style.display = DisplayStyle.None;
        _staffPlacementPanel.style.display = DisplayStyle.None;
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
    // 店舗運営ロジック
    // ──────────────────────────────────────────────

    private void RefreshDinerPanel()
    {
        _selectedDishes.Clear();
        _dinerDishListContainer.Clear();

        if (GameManager.Instance == null) return;

        var dishes = GameManager.Instance.Inventory.GetAllDishes();
        if (dishes.Count == 0)
        {
            _dinerDishListContainer.Add(CreateInfoLabel("提供可能な料理がありません。\n先にメニュー編成で調理しましょう。"));
            return;
        }

        foreach (var kvp in dishes)
        {
            DishInstance dish = kvp.Key;
            int count = kvp.Value;
            bool isSelected = false;

            var row = CreateCardRow();

            var nameLabel = new Label($"{dish} x{count}");
            nameLabel.style.fontSize = 16;
            nameLabel.style.color = BTN_TEXT_COLOR;
            nameLabel.style.flexGrow = 1;
            row.Add(nameLabel);

            var priceLabel = new Label($"{dish.ShopPrice}G");
            priceLabel.style.fontSize = 14;
            priceLabel.style.color = INFO_COLOR;
            priceLabel.style.width = 60;
            priceLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(priceLabel);

            DishInstance capturedDish = dish;
            row.RegisterCallback<ClickEvent>(evt =>
            {
                isSelected = !isSelected;
                row.style.backgroundColor = isSelected ? SELECTED_COLOR : CARD_BG_COLOR;
                if (isSelected)
                    _selectedDishes.Add(capturedDish);
                else
                    _selectedDishes.Remove(capturedDish);
            });

            _dinerDishListContainer.Add(row);
        }
    }

    private void ExecuteDinerService()
    {
        if (_dinerService == null || _selectedDishes.Count == 0)
        {
            ShowResult("営業失敗", "提供する料理を選択してください。");
            return;
        }

        CalendarEventData calendarEvent = FindActiveCalendarEvent();
        DinerResult result = _dinerService.RunService(_selectedDishes.ToArray(), calendarEvent);

        ShowResult("営業完了！",
            $"売上: {result.TotalRevenue} G\n" +
            $"チップ: {result.TotalTips} G\n" +
            $"総収入: {result.TotalEarnings} G\n" +
            $"客数: {result.CustomersServed}\n" +
            $"平均満足度: {result.AverageSatisfaction:F1}\n" +
            $"評判変動: {(result.ReputationChange >= 0 ? "+" : "")}{result.ReputationChange}");
    }

    // ──────────────────────────────────────────────
    // メニュー編成（調理）ロジック
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

        CalendarEventData calendarEvent = FindActiveCalendarEvent();
        var result = _cookingMgr.Cook(recipe, freshnessBuff, calendarEvent);

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
    // 従業員配置ロジック
    // ──────────────────────────────────────────────

    private void RefreshStaffPanel()
    {
        _staffListContainer.Clear();

        if (GameManager.Instance == null || GameManager.Instance.Staff == null)
        {
            _staffListContainer.Add(CreateInfoLabel("StaffManager が未初期化です。"));
            return;
        }

        var staffMgr = GameManager.Instance.Staff;

        // 常勤セクション
        _staffListContainer.Add(CreateSectionHeader($"常勤スタッフ ({staffMgr.PermanentStaff.Count}/3)"));
        if (staffMgr.PermanentStaff.Count == 0)
        {
            _staffListContainer.Add(CreateInfoLabel("  （なし）"));
        }
        foreach (var staff in staffMgr.PermanentStaff)
        {
            _staffListContainer.Add(CreateStaffCard(staff, staffMgr, false));
        }

        // 臨時セクション
        _staffListContainer.Add(CreateSectionHeader($"臨時スタッフ ({staffMgr.TemporaryStaff.Count}/2)"));
        if (staffMgr.TemporaryStaff.Count == 0)
        {
            _staffListContainer.Add(CreateInfoLabel("  （なし）"));
        }
        foreach (var staff in staffMgr.TemporaryStaff)
        {
            _staffListContainer.Add(CreateStaffCard(staff, staffMgr, true));
        }

        // 日給合計
        var salaryLabel = CreateInfoLabel($"日給合計: {staffMgr.GetTotalDailySalary()} G");
        salaryLabel.style.marginTop = 12;
        _staffListContainer.Add(salaryLabel);
    }

    private VisualElement CreateStaffCard(StaffInstance staff, StaffManager staffMgr, bool showPromote)
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

        var salaryLabel = new Label(staff.SlotType == StaffSlotType.Permanent
            ? $"日給: {staff.CalculateSalary()}G"
            : "無給（臨時）");
        salaryLabel.style.fontSize = 12;
        salaryLabel.style.color = INFO_COLOR;
        nameRow.Add(salaryLabel);

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
            buffLabel.style.marginBottom = 2;
            card.Add(buffLabel);
        }

        // 不満度
        if (staff.MoralePenalty > 0)
        {
            var moraleLabel = new Label($"不満: {staff.MoralePenalty}/3");
            moraleLabel.style.fontSize = 12;
            moraleLabel.style.color = new Color(1f, 0.4f, 0.3f);
            card.Add(moraleLabel);
        }

        // ボタン行
        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        btnRow.style.justifyContent = Justify.FlexEnd;
        btnRow.style.marginTop = 4;
        card.Add(btnRow);

        // 昇格ボタン（臨時のみ）
        if (showPromote && staffMgr.PermanentSlotsAvailable > 0)
        {
            StaffInstance capturedStaff = staff;
            btnRow.Add(CreateSmallButton("昇格", ACCENT_COLOR, () =>
            {
                staffMgr.TryPromote(capturedStaff);
                RefreshStaffPanel();
            }));
        }

        // 解雇ボタン
        {
            StaffInstance capturedStaff = staff;
            btnRow.Add(CreateSmallButton("解雇", new Color(0.8f, 0.3f, 0.2f), () =>
            {
                staffMgr.Fire(capturedStaff);
                RefreshStaffPanel();
            }));
        }

        return card;
    }

    // ──────────────────────────────────────────────
    // カレンダーイベント検索
    // ──────────────────────────────────────────────

    private CalendarEventData FindActiveCalendarEvent()
    {
        if (GameManager.Instance == null) return null;

        int day = GameManager.Instance.CurrentDay;
        CalendarEventData[] allEvents = Resources.LoadAll<CalendarEventData>("");
        foreach (var evt in allEvents)
        {
            if (evt != null && evt.IsActiveOnDay(day))
                return evt;
        }
        return null;
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
        label.style.color = new Color(0.05f, 0.05f, 0.05f);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        button.Add(label);

        if (callback != null)
            button.clicked += callback;

        return button;
    }

    private Button CreateSmallButton(string text, Color bgColor, System.Action callback)
    {
        var button = new Button();
        button.style.backgroundColor = bgColor;
        button.style.borderTopLeftRadius = 4;
        button.style.borderTopRightRadius = 4;
        button.style.borderBottomLeftRadius = 4;
        button.style.borderBottomRightRadius = 4;
        button.style.paddingTop = 4;
        button.style.paddingBottom = 4;
        button.style.paddingLeft = 10;
        button.style.paddingRight = 10;
        button.style.marginLeft = 6;
        button.style.borderTopWidth = 0;
        button.style.borderBottomWidth = 0;
        button.style.borderLeftWidth = 0;
        button.style.borderRightWidth = 0;

        var label = new Label(text);
        label.style.fontSize = 12;
        label.style.color = new Color(0.05f, 0.05f, 0.05f);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
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
