// ============================================================
// HousingShopUI.cs
// 家具ショップUI。所持金を使って家具を購入し、居心地度を上げる。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 家具ショップのUI。Evening / Night フェーズ中にトグルボタンで開閉し、
/// 家具を購入して <see cref="HousingManager"/> に配置する。
/// </summary>
public sealed class HousingShopUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const float BUTTON_WIDTH = 400f;
    private const float BUTTON_HEIGHT = 70f;
    private const int BUTTON_FONT_SIZE = 18;
    private const int SUB_TEXT_FONT_SIZE = 14;
    private const string BUTTON_TEXT_FORMAT = "{0}  ({1}G)";
    private const string COMFORT_TEXT_FORMAT = "居心地度 +{0:F1}";
    private const string GOLD_DISPLAY_FORMAT = "所持金: {0} G";
    private const string COMFORT_DISPLAY_FORMAT = "合計居心地度: {0:F1}";

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("ショップデータ")]
    [SerializeField] private FurnitureData[] _shopItems;

    [Header("UI 要素")]
    [SerializeField] private GameObject _shopPanel;
    [SerializeField] private Transform _buttonContainer;
    [SerializeField] private Text _goldLabel;
    [SerializeField] private Text _comfortLabel;
    [SerializeField] private Button _toggleButton;  // ショップ開閉ボタン

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    private readonly List<GameObject> _spawnedButtons = new List<GameObject>();
    private HousingManager _housingManager;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _housingManager = FindFirstObjectByType<HousingManager>();
    }

    private void OnEnable()
    {
        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        GameManager.Instance.OnGoldChanged += HandleGoldChanged;

        if (_housingManager != null)
        {
            _housingManager.OnComfortScoreChanged += HandleComfortScoreChanged;
        }

        if (_toggleButton != null)
        {
            _toggleButton.onClick.AddListener(ToggleShop);
        }

        // 現在のフェーズに合わせて初期状態を設定
        GameManager.GamePhase phase = GameManager.Instance.CurrentPhase;
        if (phase == GameManager.GamePhase.Evening || phase == GameManager.GamePhase.Night)
        {
            ShowToggleButton();
        }
        else
        {
            HideToggleButton();
            HideShop();
        }
    }

    private void OnDisable()
    {
        // シングルトン破棄順を考慮した null チェック
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            GameManager.Instance.OnGoldChanged -= HandleGoldChanged;
        }

        if (_housingManager != null)
        {
            _housingManager.OnComfortScoreChanged -= HandleComfortScoreChanged;
        }

        if (_toggleButton != null)
        {
            _toggleButton.onClick.RemoveListener(ToggleShop);
        }
    }

    // ──────────────────────────────────────────────
    // イベントハンドラ
    // ──────────────────────────────────────────────

    /// <summary>
    /// フェーズ変化時のコールバック。
    /// Evening / Night ならトグルボタンを表示し、それ以外ならすべて非表示にする。
    /// </summary>
    private void HandlePhaseChanged(GameManager.GamePhase newPhase)
    {
        if (newPhase == GameManager.GamePhase.Evening || newPhase == GameManager.GamePhase.Night)
        {
            ShowToggleButton();
        }
        else
        {
            HideToggleButton();
            HideShop();
        }
    }

    /// <summary>
    /// 所持金変化時のコールバック。ラベルのみ更新する。
    /// フルリフレッシュはショップ表示時と購入時に行うため、ここでは行わない。
    /// </summary>
    private void HandleGoldChanged(int newGold)
    {
        UpdateLabels();
    }

    /// <summary>
    /// 居心地度変化時のコールバック。ラベルのみ更新する。
    /// </summary>
    private void HandleComfortScoreChanged(float newScore)
    {
        UpdateLabels();
    }

    // ──────────────────────────────────────────────
    // ショップ開閉
    // ──────────────────────────────────────────────

    /// <summary>ショップパネルの表示/非表示をトグルする。</summary>
    private void ToggleShop()
    {
        if (_shopPanel != null && _shopPanel.activeSelf)
        {
            HideShop();
        }
        else
        {
            ShowShop();
        }
    }

    /// <summary>ショップパネルを表示し、リストとラベルを更新する。</summary>
    private void ShowShop()
    {
        if (_shopPanel != null)
        {
            _shopPanel.SetActive(true);
        }

        RefreshShopList();
        UpdateLabels();
    }

    /// <summary>ショップパネルを非表示にする。</summary>
    private void HideShop()
    {
        if (_shopPanel != null)
        {
            _shopPanel.SetActive(false);
        }
    }

    /// <summary>トグルボタンを表示する。</summary>
    private void ShowToggleButton()
    {
        if (_toggleButton != null)
        {
            _toggleButton.gameObject.SetActive(true);
        }
    }

    /// <summary>トグルボタンを非表示にする。</summary>
    private void HideToggleButton()
    {
        if (_toggleButton != null)
        {
            _toggleButton.gameObject.SetActive(false);
        }
    }

    // ──────────────────────────────────────────────
    // ショップリスト更新
    // ──────────────────────────────────────────────

    /// <summary>
    /// ボタンコンテナ内の既存ボタンをすべて破棄し、
    /// ショップアイテムごとにボタンを再生成する。
    /// 所持金が足りないアイテムのボタンは無効化（グレーアウト）される。
    /// </summary>
    private void RefreshShopList()
    {
        ClearButtons();
        EnsureVerticalLayoutGroup();

        // Inspector 未設定のフォールバック
        if (_shopItems == null || _shopItems.Length == 0)
        {
            _shopItems = Resources.LoadAll<FurnitureData>("");
        }

        if (_shopItems == null || _shopItems.Length == 0) return;

        for (int i = 0; i < _shopItems.Length; i++)
        {
            FurnitureData item = _shopItems[i];
            if (item == null) continue;

            bool canAfford = GameManager.Instance.CanAfford(item.Price);
            CreateShopItemButton(item, canAfford);
        }
    }

    // ──────────────────────────────────────────────
    // ボタン生成
    // ──────────────────────────────────────────────

    /// <summary>
    /// 1 アイテム分のボタンを動的生成してコンテナに追加する。
    /// メインテキスト（名前+価格）とサブテキスト（居心地度ボーナス）を持つ。
    /// </summary>
    private void CreateShopItemButton(FurnitureData item, bool canAfford)
    {
        // ルートオブジェクト
        GameObject buttonObj = new GameObject("ShopButton_" + item.Id, typeof(RectTransform));
        buttonObj.transform.SetParent(_buttonContainer, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT);

        // Image（Button が必要とする Graphic）
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.15f, 0.25f, 0.15f, 0.9f);  // ショップ用ダークグリーン

        // Button コンポーネント
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        // メインテキスト（子オブジェクト）: "家具名  (200G)"
        GameObject textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.4f);
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-12f, 0f);

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = BUTTON_FONT_SIZE;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.text = string.Format(BUTTON_TEXT_FORMAT, item.FurnitureName, item.Price);

        // サブテキスト（子オブジェクト）: "居心地度 +5.0"
        GameObject subTextObj = new GameObject("ComfortLabel", typeof(RectTransform));
        subTextObj.transform.SetParent(buttonObj.transform, false);

        RectTransform subRect = subTextObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.45f);
        subRect.offsetMin = new Vector2(12f, 0f);
        subRect.offsetMax = new Vector2(-12f, 0f);

        Text subText = subTextObj.AddComponent<Text>();
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = SUB_TEXT_FONT_SIZE;
        subText.color = new Color(0.6f, 1f, 0.6f, 0.8f);  // ライトグリーン
        subText.alignment = TextAnchor.UpperLeft;
        subText.horizontalOverflow = HorizontalWrapMode.Overflow;
        subText.text = string.Format(COMFORT_TEXT_FORMAT, item.ComfortBonus);

        // 購入可否に応じた有効/無効切り替え
        button.interactable = canAfford;
        if (!canAfford)
        {
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            button.colors = colors;
        }

        // クリックハンドラ — ローカル変数キャプチャで対象アイテムを保持
        FurnitureData targetItem = item;
        button.onClick.AddListener(() => OnShopItemClicked(targetItem));

        _spawnedButtons.Add(buttonObj);
    }

    // ──────────────────────────────────────────────
    // ボタンクリック
    // ──────────────────────────────────────────────

    /// <summary>
    /// ショップボタンが押されたときの処理。
    /// 所持金が足りれば購入し、インベントリと HousingManager に反映する。
    /// </summary>
    private void OnShopItemClicked(FurnitureData item)
    {
        if (!GameManager.Instance.TrySpendGold(item.Price))
        {
            Debug.LogWarning($"[HousingShopUI] 所持金不足: {item.FurnitureName} ({item.Price}G)");
            return;
        }

        // インベントリに追加
        GameManager.Instance.Inventory.AddFurniture(item);

        // HousingManager に通知して居心地度を即座に再計算
        if (_housingManager != null)
        {
            _housingManager.PlaceFurniture(item);
        }

        Debug.Log($"[HousingShopUI] 購入成功: {item.FurnitureName} ({item.Price}G), ComfortScore: {(_housingManager != null ? _housingManager.ComfortScore : 0f):F1}");

        // リスト更新（購入後の所持金変化で購入可否が変わるため）
        RefreshShopList();
        UpdateLabels();
    }

    // ──────────────────────────────────────────────
    // ラベル更新
    // ──────────────────────────────────────────────

    /// <summary>
    /// 所持金ラベルと合計居心地度ラベルを現在の値で更新する。
    /// </summary>
    private void UpdateLabels()
    {
        if (_goldLabel != null)
        {
            _goldLabel.text = string.Format(GOLD_DISPLAY_FORMAT, GameManager.Instance.Gold);
        }

        if (_comfortLabel != null)
        {
            _comfortLabel.text = string.Format(
                COMFORT_DISPLAY_FORMAT,
                _housingManager != null ? _housingManager.ComfortScore : 0f);
        }
    }

    // ──────────────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────────────

    /// <summary>
    /// 生成済みのボタンをすべて破棄する。
    /// </summary>
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
    /// ボタンコンテナに VerticalLayoutGroup が無ければ追加する。
    /// 動的生成時のオートレイアウト用。
    /// </summary>
    private void EnsureVerticalLayoutGroup()
    {
        if (_buttonContainer == null) return;

        if (!_buttonContainer.TryGetComponent(out VerticalLayoutGroup _))
        {
            VerticalLayoutGroup layout = _buttonContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
    }
}
