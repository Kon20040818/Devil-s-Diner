// ============================================================
// ProgressionUI.cs
// スキルツリーと店舗拡張の2パネルを管理するUIコントローラー。
// Morning / Evening フェーズ中に表示される。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スキルツリーパネルと店舗拡張パネルをタブ切替で管理するUI。
/// Morning または Evening フェーズ中に表示され、
/// プレイヤーがスキル解放や店舗アップグレードを行えるようにする。
/// </summary>
public sealed class ProgressionUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const float BUTTON_WIDTH = 380f;
    private const float BUTTON_HEIGHT = 60f;
    private const int BUTTON_FONT_SIZE = 16;
    private const string GOLD_DISPLAY_FORMAT = "所持金: {0}G";
    private const string SKILL_BUTTON_FORMAT = "{0} ({1}G)";
    private const string SKILL_BUTTON_UNLOCKED_SUFFIX = " [UNLOCKED]";
    private const string SHOP_LEVEL_FORMAT = "Lv.{0} - {1}";
    private const string UPGRADE_COST_FORMAT = "次のレベル: {0}G";
    private const string UPGRADE_MAX_TEXT = "最大レベル到達！";

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("スキルツリーパネル")]
    [SerializeField] private GameObject _skillPanel;
    [SerializeField] private Transform _skillButtonContainer;
    [SerializeField] private Text _skillTitleLabel;

    [Header("店舗拡張パネル")]
    [SerializeField] private GameObject _expansionPanel;
    [SerializeField] private Text _shopLevelLabel;
    [SerializeField] private Text _upgradeCostLabel;
    [SerializeField] private Button _upgradeButton;

    [Header("共通")]
    [SerializeField] private Button _skillTabButton;
    [SerializeField] private Button _expansionTabButton;
    [SerializeField] private Text _goldLabel;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    private SkillManager _skillManager;
    private ShopExpansionManager _shopExpansionManager;
    private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _skillManager = FindFirstObjectByType<SkillManager>();
        _shopExpansionManager = FindFirstObjectByType<ShopExpansionManager>();
    }

    private void OnEnable()
    {
        // GameManager イベント購読
        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        GameManager.Instance.OnGoldChanged += HandleGoldChanged;

        // SkillManager イベント購読
        if (_skillManager != null)
        {
            _skillManager.OnSkillUnlocked += HandleSkillUnlocked;
        }

        // ShopExpansionManager イベント購読
        if (_shopExpansionManager != null)
        {
            _shopExpansionManager.OnShopExpanded += HandleShopExpanded;
        }

        // タブボタン購読
        if (_skillTabButton != null)
        {
            _skillTabButton.onClick.AddListener(ShowSkillPanel);
        }

        if (_expansionTabButton != null)
        {
            _expansionTabButton.onClick.AddListener(ShowExpansionPanel);
        }

        // アップグレードボタン購読
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        // 現在のフェーズに合わせて初期表示を設定
        HandlePhaseChanged(GameManager.Instance.CurrentPhase);
    }

    private void OnDisable()
    {
        // GameManager イベント解除
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            GameManager.Instance.OnGoldChanged -= HandleGoldChanged;
        }

        // SkillManager イベント解除
        if (_skillManager != null)
        {
            _skillManager.OnSkillUnlocked -= HandleSkillUnlocked;
        }

        // ShopExpansionManager イベント解除
        if (_shopExpansionManager != null)
        {
            _shopExpansionManager.OnShopExpanded -= HandleShopExpanded;
        }

        // タブボタン解除
        if (_skillTabButton != null)
        {
            _skillTabButton.onClick.RemoveListener(ShowSkillPanel);
        }

        if (_expansionTabButton != null)
        {
            _expansionTabButton.onClick.RemoveListener(ShowExpansionPanel);
        }

        // アップグレードボタン解除
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        }
    }

    // ──────────────────────────────────────────────
    // イベントハンドラ
    // ──────────────────────────────────────────────

    /// <summary>
    /// フェーズ変化時のコールバック。
    /// Morning / Evening ならパネルを表示し、それ以外なら非表示にする。
    /// </summary>
    private void HandlePhaseChanged(GameManager.GamePhase newPhase)
    {
        if (newPhase == GameManager.GamePhase.Morning || newPhase == GameManager.GamePhase.Evening)
        {
            ShowUI();
        }
        else
        {
            HideUI();
        }
    }

    /// <summary>所持金変化時のコールバック。</summary>
    private void HandleGoldChanged(int newGold)
    {
        UpdateGoldLabel();
        RefreshSkillList();
        RefreshExpansionPanel();
    }

    /// <summary>スキル解放時のコールバック。</summary>
    private void HandleSkillUnlocked(SkillData skill)
    {
        RefreshSkillList();
        UpdateGoldLabel();
    }

    /// <summary>店舗拡張時のコールバック。</summary>
    private void HandleShopExpanded(int newLevel)
    {
        RefreshExpansionPanel();
        UpdateGoldLabel();
    }

    // ──────────────────────────────────────────────
    // UI 表示 / 非表示
    // ──────────────────────────────────────────────

    /// <summary>UI全体を表示し、スキルパネルをデフォルトで開く。</summary>
    private void ShowUI()
    {
        gameObject.SetActive(true);
        ShowSkillPanel();
        UpdateGoldLabel();
    }

    /// <summary>UI全体を非表示にする。</summary>
    private void HideUI()
    {
        if (_skillPanel != null) _skillPanel.SetActive(false);
        if (_expansionPanel != null) _expansionPanel.SetActive(false);
    }

    // ──────────────────────────────────────────────
    // タブ切替
    // ──────────────────────────────────────────────

    /// <summary>スキルツリーパネルを表示し、拡張パネルを非表示にする。</summary>
    private void ShowSkillPanel()
    {
        if (_skillPanel != null) _skillPanel.SetActive(true);
        if (_expansionPanel != null) _expansionPanel.SetActive(false);

        if (_skillTitleLabel != null)
        {
            _skillTitleLabel.text = "スキルツリー";
        }

        RefreshSkillList();
    }

    /// <summary>店舗拡張パネルを表示し、スキルパネルを非表示にする。</summary>
    private void ShowExpansionPanel()
    {
        if (_skillPanel != null) _skillPanel.SetActive(false);
        if (_expansionPanel != null) _expansionPanel.SetActive(true);

        RefreshExpansionPanel();
    }

    // ──────────────────────────────────────────────
    // スキルパネル更新
    // ──────────────────────────────────────────────

    /// <summary>
    /// スキルボタンコンテナ内の既存ボタンをすべて破棄し、
    /// 利用可能なスキルごとにボタンを再生成する。
    /// </summary>
    private void RefreshSkillList()
    {
        ClearButtons();

        if (_skillButtonContainer == null) return;

        EnsureVerticalLayoutGroup(_skillButtonContainer);

        // スキル一覧の取得: SkillManager.AvailableSkills → フォールバックとして Resources
        IReadOnlyList<SkillData> skills = null;

        if (_skillManager != null)
        {
            skills = _skillManager.AvailableSkills;
        }

        if (skills == null || skills.Count == 0)
        {
            SkillData[] loaded = Resources.LoadAll<SkillData>("");
            skills = loaded;
        }

        if (skills == null || skills.Count == 0) return;

        for (int i = 0; i < skills.Count; i++)
        {
            SkillData skill = skills[i];
            if (skill == null) continue;

            bool isUnlocked = _skillManager != null && _skillManager.IsSkillUnlocked(skill);
            CreateSkillButton(skill, isUnlocked);
        }
    }

    /// <summary>
    /// 1スキル分のボタンを動的生成してコンテナに追加する。
    /// </summary>
    private void CreateSkillButton(SkillData skill, bool isUnlocked)
    {
        // ルートオブジェクト
        GameObject buttonObj = new GameObject("SkillButton_" + skill.Id, typeof(RectTransform));
        buttonObj.transform.SetParent(_skillButtonContainer, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT);

        // Image（Button が必要とする Graphic）
        Image image = buttonObj.AddComponent<Image>();
        image.color = isUnlocked
            ? new Color(0.15f, 0.35f, 0.15f, 0.9f)   // 解放済み: ダークグリーン
            : new Color(0.20f, 0.15f, 0.30f, 0.9f);   // 未解放: ダークパープル

        // Button コンポーネント
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        // テキスト（子オブジェクト）
        GameObject textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-12f, 0f);

        Text text = textObj.AddComponent<Text>();
        text.font = LoadFont();
        text.fontSize = BUTTON_FONT_SIZE;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        // ボタンテキスト構築
        string label = string.Format(SKILL_BUTTON_FORMAT, skill.SkillName, skill.Cost);
        if (isUnlocked)
        {
            label += SKILL_BUTTON_UNLOCKED_SUFFIX;
        }
        text.text = label;

        // 解放済みならボタンを無効化、未解放ならクリックでスキル解放を試行
        if (isUnlocked)
        {
            button.interactable = false;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            button.colors = colors;
        }
        else
        {
            // ローカルキャプチャ
            SkillData targetSkill = skill;
            button.onClick.AddListener(() => OnSkillButtonClicked(targetSkill));
        }

        _spawnedButtons.Add(buttonObj);
    }

    /// <summary>
    /// スキルボタンが押されたときの処理。
    /// SkillManager 経由でスキル解放を試みる。
    /// </summary>
    private void OnSkillButtonClicked(SkillData skill)
    {
        if (_skillManager == null)
        {
            Debug.LogWarning("[ProgressionUI] SkillManager が見つかりません。");
            return;
        }

        _skillManager.TryUnlockSkill(skill);
    }

    // ──────────────────────────────────────────────
    // 店舗拡張パネル更新
    // ──────────────────────────────────────────────

    /// <summary>
    /// 店舗拡張パネルのラベルとボタン状態を現在の値で更新する。
    /// </summary>
    private void RefreshExpansionPanel()
    {
        if (_shopExpansionManager == null)
        {
            // ShopExpansionManager が存在しない場合はフォールバック表示
            if (_shopLevelLabel != null)
            {
                _shopLevelLabel.text = string.Format(SHOP_LEVEL_FORMAT, GameManager.Instance.ShopLevel, "不明");
            }

            if (_upgradeCostLabel != null)
            {
                _upgradeCostLabel.text = "";
            }

            if (_upgradeButton != null)
            {
                _upgradeButton.interactable = false;
            }

            return;
        }

        // 現在のレベル名を表示
        if (_shopLevelLabel != null)
        {
            _shopLevelLabel.text = string.Format(
                SHOP_LEVEL_FORMAT,
                _shopExpansionManager.CurrentLevel,
                _shopExpansionManager.CurrentLevelName);
        }

        // アップグレード可否の判定
        if (_shopExpansionManager.IsMaxLevel)
        {
            if (_upgradeCostLabel != null)
            {
                _upgradeCostLabel.text = UPGRADE_MAX_TEXT;
            }

            if (_upgradeButton != null)
            {
                _upgradeButton.interactable = false;
            }
        }
        else
        {
            int upgradeCost = _shopExpansionManager.NextUpgradeCost;
            bool canAfford = GameManager.Instance.CanAfford(upgradeCost);

            if (_upgradeCostLabel != null)
            {
                _upgradeCostLabel.text = string.Format(UPGRADE_COST_FORMAT, upgradeCost);
            }

            if (_upgradeButton != null)
            {
                _upgradeButton.interactable = canAfford;
            }
        }
    }

    /// <summary>
    /// アップグレードボタンが押されたときの処理。
    /// ShopExpansionManager 経由で店舗拡張を試みる。
    /// </summary>
    private void OnUpgradeClicked()
    {
        if (_shopExpansionManager == null)
        {
            Debug.LogWarning("[ProgressionUI] ShopExpansionManager が見つかりません。");
            return;
        }

        _shopExpansionManager.TryUpgrade();
    }

    // ──────────────────────────────────────────────
    // ゴールドラベル更新
    // ──────────────────────────────────────────────

    /// <summary>所持金ラベルを現在の値で更新する。</summary>
    private void UpdateGoldLabel()
    {
        if (_goldLabel != null)
        {
            _goldLabel.text = string.Format(GOLD_DISPLAY_FORMAT, GameManager.Instance.Gold);
        }
    }

    // ──────────────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────────────

    /// <summary>生成済みのスキルボタンをすべて破棄する。</summary>
    private void ClearButtons()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            if (_spawnedButtons[i] != null)
            {
                Destroy(_spawnedButtons[i]);
            }
        }

        _spawnedButtons.Clear();
    }

    /// <summary>
    /// 指定コンテナに VerticalLayoutGroup が無ければ追加する。
    /// </summary>
    private void EnsureVerticalLayoutGroup(Transform container)
    {
        if (container == null) return;

        if (!container.TryGetComponent(out VerticalLayoutGroup _))
        {
            VerticalLayoutGroup layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
    }

    /// <summary>
    /// UIフォントをロードする。LegacyRuntime.ttf を優先し、失敗時は Arial.ttf にフォールバック。
    /// </summary>
    private static Font LoadFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return font;
    }
}
