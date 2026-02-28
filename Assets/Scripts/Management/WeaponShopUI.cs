// ============================================================
// WeaponShopUI.cs
// 武器ショップUI。所持金を使って武器を購入・装備する。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 武器ショップのUI。Morning / Evening フェーズ中にトグルボタンで開閉し、
/// 武器を購入して即座に装備する。
/// </summary>
public sealed class WeaponShopUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const float BUTTON_WIDTH = 400f;
    private const float BUTTON_HEIGHT = 70f;
    private const int BUTTON_FONT_SIZE = 18;
    private const int SUB_TEXT_FONT_SIZE = 14;
    private const string BUTTON_TEXT_FORMAT = "{0}  ({1}G)";
    private const string STAT_TEXT_FORMAT = "ATK {0}  JUST +{1}F";
    private const string GOLD_DISPLAY_FORMAT = "所持金: {0} G";
    private const string EQUIPPED_DISPLAY_FORMAT = "装備中: {0}";
    private const string EQUIP_BUTTON_TEXT = "装備";

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("ショップデータ")]
    [SerializeField] private WeaponData[] _shopWeapons;

    [Header("UI 要素")]
    [SerializeField] private GameObject _shopPanel;
    [SerializeField] private Transform _buttonContainer;
    [SerializeField] private Text _goldLabel;
    [SerializeField] private Text _equippedLabel;   // 装備中の武器名を表示
    [SerializeField] private Button _toggleButton;  // ショップ開閉ボタン

    // ──────────────────────────────────────────────
    // 静的フィールド
    // ──────────────────────────────────────────────

    /// <summary>最後に装備した武器 ID（シーン間で保持）。</summary>
    public static string LastEquippedWeaponId { get; set; }

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void OnEnable()
    {
        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        GameManager.Instance.OnGoldChanged += HandleGoldChanged;

        if (_toggleButton != null)
        {
            _toggleButton.onClick.AddListener(ToggleShop);
        }

        // 現在のフェーズに合わせて初期状態を設定
        GameManager.GamePhase phase = GameManager.Instance.CurrentPhase;
        if (phase == GameManager.GamePhase.Morning || phase == GameManager.GamePhase.Evening)
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
    /// Morning / Evening ならトグルボタンを表示し、それ以外ならすべて非表示にする。
    /// </summary>
    private void HandlePhaseChanged(GameManager.GamePhase newPhase)
    {
        if (newPhase == GameManager.GamePhase.Morning || newPhase == GameManager.GamePhase.Evening)
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
    /// </summary>
    private void HandleGoldChanged(int newGold)
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
    /// 既に所持済みの武器は「装備」ボタンとして表示する。
    /// </summary>
    private void RefreshShopList()
    {
        ClearButtons();
        EnsureVerticalLayoutGroup();

        // Inspector 未設定のフォールバック
        if (_shopWeapons == null || _shopWeapons.Length == 0)
        {
            _shopWeapons = Resources.LoadAll<WeaponData>("");
        }

        if (_shopWeapons == null || _shopWeapons.Length == 0) return;

        for (int i = 0; i < _shopWeapons.Length; i++)
        {
            WeaponData weapon = _shopWeapons[i];
            if (weapon == null) continue;

            bool alreadyOwned = IsWeaponOwned(weapon);
            bool canAfford = alreadyOwned || GameManager.Instance.CanAfford(weapon.Price);
            CreateShopItemButton(weapon, canAfford, alreadyOwned);
        }
    }

    // ──────────────────────────────────────────────
    // ボタン生成
    // ──────────────────────────────────────────────

    /// <summary>
    /// 1 アイテム分のボタンを動的生成してコンテナに追加する。
    /// メインテキスト（名前+価格 or 装備）とサブテキスト（攻撃力・ジャスト入力ボーナス）を持つ。
    /// </summary>
    private void CreateShopItemButton(WeaponData weapon, bool canAfford, bool alreadyOwned)
    {
        // ルートオブジェクト
        GameObject buttonObj = new GameObject("ShopButton_" + weapon.Id, typeof(RectTransform));
        buttonObj.transform.SetParent(_buttonContainer, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT);

        // Image（Button が必要とする Graphic）
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.35f, 0.1f, 0.1f, 0.9f);  // ショップ用ダークレッド / クリムゾン

        // Button コンポーネント
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        // メインテキスト（子オブジェクト）: "武器名  (500G)" or "装備"
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

        if (alreadyOwned)
        {
            text.text = string.Format("{0}  ({1})", weapon.WeaponName, EQUIP_BUTTON_TEXT);
        }
        else
        {
            text.text = string.Format(BUTTON_TEXT_FORMAT, weapon.WeaponName, weapon.Price);
        }

        // サブテキスト（子オブジェクト）: "ATK 100  JUST +3F"
        GameObject subTextObj = new GameObject("StatLabel", typeof(RectTransform));
        subTextObj.transform.SetParent(buttonObj.transform, false);

        RectTransform subRect = subTextObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.45f);
        subRect.offsetMin = new Vector2(12f, 0f);
        subRect.offsetMax = new Vector2(-12f, 0f);

        Text subText = subTextObj.AddComponent<Text>();
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = SUB_TEXT_FONT_SIZE;
        subText.color = new Color(1f, 0.6f, 0.6f, 0.8f);  // ライトレッド
        subText.alignment = TextAnchor.UpperLeft;
        subText.horizontalOverflow = HorizontalWrapMode.Overflow;
        subText.text = string.Format(STAT_TEXT_FORMAT, weapon.BaseDamage, weapon.JustInputFrameBonus);

        // 購入可否に応じた有効/無効切り替え（所持済みは常にクリック可能）
        button.interactable = canAfford;
        if (!canAfford)
        {
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            button.colors = colors;
        }

        // クリックハンドラ — ローカル変数キャプチャで対象アイテムを保持
        WeaponData targetWeapon = weapon;
        bool owned = alreadyOwned;
        button.onClick.AddListener(() => OnShopItemClicked(targetWeapon, owned));

        _spawnedButtons.Add(buttonObj);
    }

    // ──────────────────────────────────────────────
    // ボタンクリック
    // ──────────────────────────────────────────────

    /// <summary>
    /// ショップボタンが押されたときの処理。
    /// 未所持なら購入してインベントリに追加し、装備する。
    /// 所持済みなら装備のみ行う。
    /// </summary>
    private void OnShopItemClicked(WeaponData weapon, bool alreadyOwned)
    {
        if (!alreadyOwned)
        {
            // 購入処理
            if (!GameManager.Instance.TrySpendGold(weapon.Price))
            {
                Debug.LogWarning($"[WeaponShopUI] 所持金不足: {weapon.WeaponName} ({weapon.Price}G)");
                return;
            }

            // インベントリに追加
            GameManager.Instance.Inventory.AddWeapon(weapon);

            Debug.Log($"[WeaponShopUI] 購入成功: {weapon.WeaponName} ({weapon.Price}G)");
        }

        // 装備処理
        EquipWeaponInternal(weapon);

        // リスト更新（購入後の所持金変化で購入可否が変わるため）
        RefreshShopList();
        UpdateLabels();
    }

    // ──────────────────────────────────────────────
    // 装備処理
    // ──────────────────────────────────────────────

    /// <summary>
    /// 武器を装備する。PlayerController が存在すれば即座に反映し、
    /// LastEquippedWeaponId にシーン間保持用の ID を記録する。
    /// </summary>
    private void EquipWeaponInternal(WeaponData weapon)
    {
        // シーン間保持用 ID を記録
        LastEquippedWeaponId = weapon.Id;

        // PlayerController が存在すれば即座に装備（ManagementScene では null の場合あり）
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.EquipWeapon(weapon);
        }

        Debug.Log($"[WeaponShopUI] 装備: {weapon.WeaponName} (ID: {weapon.Id})");
    }

    // ──────────────────────────────────────────────
    // ラベル更新
    // ──────────────────────────────────────────────

    /// <summary>
    /// 所持金ラベルと装備中武器ラベルを現在の値で更新する。
    /// </summary>
    private void UpdateLabels()
    {
        if (_goldLabel != null)
        {
            _goldLabel.text = string.Format(GOLD_DISPLAY_FORMAT, GameManager.Instance.Gold);
        }

        if (_equippedLabel != null)
        {
            string equippedName = GetEquippedWeaponName();
            _equippedLabel.text = string.Format(EQUIPPED_DISPLAY_FORMAT, equippedName);
        }
    }

    /// <summary>
    /// 現在装備中の武器名を取得する。
    /// LastEquippedWeaponId からショップデータ内の武器を逆引きする。
    /// </summary>
    private string GetEquippedWeaponName()
    {
        if (string.IsNullOrEmpty(LastEquippedWeaponId)) return "---";

        if (_shopWeapons != null)
        {
            for (int i = 0; i < _shopWeapons.Length; i++)
            {
                if (_shopWeapons[i] != null && _shopWeapons[i].Id == LastEquippedWeaponId)
                {
                    return _shopWeapons[i].WeaponName;
                }
            }
        }

        // ショップデータに無い場合はインベントリから検索
        IReadOnlyList<WeaponData> ownedWeapons = GameManager.Instance.Inventory.Weapons;
        for (int i = 0; i < ownedWeapons.Count; i++)
        {
            if (ownedWeapons[i] != null && ownedWeapons[i].Id == LastEquippedWeaponId)
            {
                return ownedWeapons[i].WeaponName;
            }
        }

        return "---";
    }

    // ──────────────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────────────

    /// <summary>
    /// 指定武器がインベントリに既に存在するか判定する。
    /// </summary>
    private bool IsWeaponOwned(WeaponData weapon)
    {
        IReadOnlyList<WeaponData> ownedWeapons = GameManager.Instance.Inventory.Weapons;
        for (int i = 0; i < ownedWeapons.Count; i++)
        {
            if (ownedWeapons[i] != null && ownedWeapons[i].Id == weapon.Id)
            {
                return true;
            }
        }
        return false;
    }

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
