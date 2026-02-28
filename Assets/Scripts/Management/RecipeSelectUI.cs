// ============================================================
// RecipeSelectUI.cs
// 夕方フェーズのレシピ選択 UI。
// 利用可能なレシピをボタン一覧で表示し、選択すると調理ミニゲームを開始する。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 夕方フェーズのレシピ選択パネル。
/// 所持素材に応じてレシピボタンの有効/無効を切り替え、
/// 選択されたレシピで <see cref="CookingMinigame"/> を開始する。
/// </summary>
public sealed class RecipeSelectUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float BUTTON_WIDTH  = 400f;
    private const float BUTTON_HEIGHT = 60f;
    private const int   BUTTON_FONT_SIZE = 18;
    private const int   SUB_TEXT_FONT_SIZE = 14;
    private const string BUTTON_TEXT_FORMAT = "{0}  ({1}G)";

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("レシピデータ")]
    [SerializeField] private RecipeData[] _availableRecipes;

    [Header("参照")]
    [SerializeField] private CookingMinigame _cookingMinigame;

    [Header("UI 要素")]
    [SerializeField] private GameObject _recipeListPanel;
    [SerializeField] private Transform _buttonContainer;
    [SerializeField] private GameObject _recipeButtonPrefab;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    /// <summary>生成済みボタンのキャッシュ。RefreshRecipeList でクリアされる。</summary>
    private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void OnEnable()
    {
        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;

        if (_cookingMinigame != null)
        {
            _cookingMinigame.OnCookingCompleted += HandleCookingCompleted;
        }

        // 現在のフェーズに合わせて初期状態を設定
        if (GameManager.Instance.CurrentPhase == GameManager.GamePhase.Evening)
        {
            ShowPanel();
        }
        else
        {
            HidePanel();
        }
    }

    private void OnDisable()
    {
        // シングルトン破棄順を考慮した null チェック
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }

        if (_cookingMinigame != null)
        {
            _cookingMinigame.OnCookingCompleted -= HandleCookingCompleted;
        }
    }

    // ──────────────────────────────────────────────
    // イベントハンドラ
    // ──────────────────────────────────────────────

    /// <summary>
    /// フェーズ変化時のコールバック。
    /// Evening ならパネルを表示し、それ以外なら非表示にする。
    /// </summary>
    private void HandlePhaseChanged(GameManager.GamePhase newPhase)
    {
        if (newPhase == GameManager.GamePhase.Evening)
        {
            ShowPanel();
        }
        else
        {
            HidePanel();
        }
    }

    /// <summary>
    /// 調理完了時のコールバック。
    /// まだ Evening フェーズなら再度パネルを表示して複数回調理を可能にする。
    /// </summary>
    private void HandleCookingCompleted(CookedDishData dish)
    {
        if (GameManager.Instance.CurrentPhase == GameManager.GamePhase.Evening)
        {
            ShowPanel();
        }
    }

    // ──────────────────────────────────────────────
    // パネル表示制御
    // ──────────────────────────────────────────────

    private void ShowPanel()
    {
        if (_recipeListPanel != null)
        {
            _recipeListPanel.SetActive(true);
        }

        RefreshRecipeList();
    }

    private void HidePanel()
    {
        if (_recipeListPanel != null)
        {
            _recipeListPanel.SetActive(false);
        }
    }

    // ──────────────────────────────────────────────
    // レシピリスト更新
    // ──────────────────────────────────────────────

    /// <summary>
    /// ボタンコンテナ内の既存ボタンをすべて破棄し、
    /// 利用可能なレシピごとにボタンを再生成する。
    /// 素材が足りないレシピのボタンは無効化（グレーアウト）される。
    /// </summary>
    public void RefreshRecipeList()
    {
        ClearButtons();
        EnsureVerticalLayoutGroup();

        if (_availableRecipes == null || _availableRecipes.Length == 0) return;

        InventoryManager inventory = GameManager.Instance.Inventory;

        for (int i = 0; i < _availableRecipes.Length; i++)
        {
            RecipeData recipe = _availableRecipes[i];
            if (recipe == null) continue;

            bool canCraft = inventory.HasMaterialsForRecipe(recipe);
            CreateRecipeButton(recipe, canCraft);
        }
    }

    // ──────────────────────────────────────────────
    // ボタン生成
    // ──────────────────────────────────────────────

    /// <summary>
    /// 1 レシピ分のボタンを生成してコンテナに追加する。
    /// プレハブが設定されていればインスタンスを生成し、
    /// 未設定の場合は動的に Button + Text を組み立てる。
    /// </summary>
    private void CreateRecipeButton(RecipeData recipe, bool canCraft)
    {
        GameObject buttonObj;
        Button button;
        Text buttonText;

        if (_recipeButtonPrefab != null)
        {
            // --- プレハブベース ---
            buttonObj = Instantiate(_recipeButtonPrefab, _buttonContainer);

            if (!buttonObj.TryGetComponent(out button))
            {
                button = buttonObj.AddComponent<Button>();
            }

            buttonText = buttonObj.GetComponentInChildren<Text>();
        }
        else
        {
            // --- 動的生成 ---
            buttonObj = CreateButtonFromScratch(out button, out buttonText);
        }

        // ボタンテキスト設定
        if (buttonText != null)
        {
            buttonText.text = string.Format(BUTTON_TEXT_FORMAT, recipe.RecipeName, recipe.BasePrice);
        }

        // 素材要件サブテキスト（動的生成時のみ追加）
        if (_recipeButtonPrefab == null)
        {
            CreateMaterialSubText(buttonObj, recipe);
        }

        // 有効/無効切り替え
        button.interactable = canCraft;

        // グレーアウト
        ColorBlock colors = button.colors;
        if (!canCraft)
        {
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;
        }

        // クリックハンドラ — ローカル変数キャプチャで対象レシピを保持
        RecipeData targetRecipe = recipe;
        button.onClick.AddListener(() => OnRecipeButtonClicked(targetRecipe));

        _spawnedButtons.Add(buttonObj);
    }

    /// <summary>
    /// プレハブなしの場合にボタンを動的生成する。
    /// </summary>
    private GameObject CreateButtonFromScratch(out Button button, out Text text)
    {
        // ルートオブジェクト
        GameObject buttonObj = new GameObject("RecipeButton", typeof(RectTransform));
        buttonObj.transform.SetParent(_buttonContainer, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT);

        // Image（Button が必要とする Graphic）
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // Button コンポーネント
        button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        // テキスト（子オブジェクト）
        GameObject textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);

        text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = BUTTON_FONT_SIZE;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        return buttonObj;
    }

    /// <summary>
    /// ボタンの下に素材要件を表示するサブテキストを追加する。
    /// 例: "肉x2, 野菜x1"
    /// </summary>
    private void CreateMaterialSubText(GameObject buttonObj, RecipeData recipe)
    {
        IReadOnlyList<RecipeData.RequiredMaterial> materials = recipe.RequiredMaterials;
        if (materials == null || materials.Count == 0) return;

        // サブテキスト文字列を組み立て
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < materials.Count; i++)
        {
            RecipeData.RequiredMaterial req = materials[i];
            if (req.Material == null) continue;

            if (sb.Length > 0) sb.Append(", ");
            sb.Append(req.Material.MaterialName);
            sb.Append("x");
            sb.Append(req.Amount);
        }

        if (sb.Length == 0) return;

        // サブテキスト用 GameObject
        GameObject subTextObj = new GameObject("MaterialsLabel", typeof(RectTransform));
        subTextObj.transform.SetParent(buttonObj.transform, false);

        RectTransform subRect = subTextObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.4f);
        subRect.offsetMin = new Vector2(8f, 0f);
        subRect.offsetMax = new Vector2(-8f, 0f);

        Text subText = subTextObj.AddComponent<Text>();
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = SUB_TEXT_FONT_SIZE;
        subText.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        subText.alignment = TextAnchor.LowerLeft;
        subText.horizontalOverflow = HorizontalWrapMode.Overflow;
        subText.text = sb.ToString();

        // メインラベルの anchor を上半分に寄せる
        Transform labelTransform = buttonObj.transform.Find("Label");
        if (labelTransform != null)
        {
            RectTransform labelRect = labelTransform.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.35f);
            labelRect.anchorMax = Vector2.one;
        }
    }

    // ──────────────────────────────────────────────
    // ボタンクリック
    // ──────────────────────────────────────────────

    /// <summary>
    /// レシピボタンが押されたときの処理。
    /// 調理ミニゲームを開始し、成功したらパネルを閉じる。
    /// </summary>
    private void OnRecipeButtonClicked(RecipeData recipe)
    {
        if (_cookingMinigame == null)
        {
            Debug.LogWarning("[RecipeSelectUI] CookingMinigame の参照が設定されていません。");
            return;
        }

        bool started = _cookingMinigame.StartCooking(recipe);

        if (started)
        {
            // 調理ミニゲームに制御を移すためパネルを非表示
            HidePanel();
        }
        else
        {
            Debug.LogWarning(
                $"[RecipeSelectUI] レシピ '{recipe.RecipeName}' の調理を開始できませんでした。");
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
