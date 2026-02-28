// ============================================================
// TitleMenuUI.cs
// BootScene で表示されるタイトルメニュー。
// New Game / Continue / Options の3ボタンを管理する。
// ============================================================
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面の UI コントローラー。
/// New Game: データリセット → Morning フェーズ開始。
/// Continue: セーブデータロード → 保存フェーズ再開。
/// Options: モック（将来拡張用）。
/// </summary>
public sealed class TitleMenuUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const string GAME_TITLE = "Devil's Diner";
    private const string SUBTITLE = "～魔界の荒野とガンブレード～";

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("UI 要素")]
    [SerializeField] private Text _titleLabel;
    [SerializeField] private Text _subtitleLabel;
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _optionsButton;

    [Header("Options パネル（モック）")]
    [SerializeField] private GameObject _optionsPanel;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        // タイトルテキスト設定
        if (_titleLabel != null) _titleLabel.text = GAME_TITLE;
        if (_subtitleLabel != null) _subtitleLabel.text = SUBTITLE;

        // Options パネルは初期非表示
        if (_optionsPanel != null) _optionsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        // Continue ボタンの活性化判定
        UpdateContinueButton();

        // ボタン購読
        if (_newGameButton != null)
        {
            _newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (_optionsButton != null)
        {
            _optionsButton.onClick.AddListener(OnOptionsClicked);
        }
    }

    private void OnDisable()
    {
        if (_newGameButton != null)
        {
            _newGameButton.onClick.RemoveListener(OnNewGameClicked);
        }

        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        if (_optionsButton != null)
        {
            _optionsButton.onClick.RemoveListener(OnOptionsClicked);
        }
    }

    // ──────────────────────────────────────────────
    // ボタンハンドラ
    // ──────────────────────────────────────────────

    private void OnNewGameClicked()
    {
        Debug.Log("[TitleMenuUI] New Game を選択しました。");

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[TitleMenuUI] GameManager.Instance が null です。");
            return;
        }

        // 進行データリセット
        gm.ResetProgress();

        // セーブデータも削除
        if (gm.SaveData != null)
        {
            gm.SaveData.DeleteSaveData();
        }

        // SE
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("menu_decide");
        }

        // Morning フェーズ → ManagementScene へ遷移
        gm.StartMorningPhase();
    }

    private void OnContinueClicked()
    {
        Debug.Log("[TitleMenuUI] Continue を選択しました。");

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[TitleMenuUI] GameManager.Instance が null です。");
            return;
        }

        // セーブデータをロード
        if (gm.SaveData != null && gm.SaveData.HasSaveData())
        {
            gm.SaveData.Load();
        }

        // SE
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("menu_decide");
        }

        // ロード後のフェーズに応じてシーン遷移
        gm.StartMorningPhase();
    }

    private void OnOptionsClicked()
    {
        Debug.Log("[TitleMenuUI] Options を選択しました（モック）。");

        // SE
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("menu_select");
        }

        // Options パネルのトグル表示
        if (_optionsPanel != null)
        {
            _optionsPanel.SetActive(!_optionsPanel.activeSelf);
        }
    }

    // ──────────────────────────────────────────────
    // 内部メソッド
    // ──────────────────────────────────────────────

    /// <summary>セーブデータの有無に応じて Continue ボタンを活性/非活性にする。</summary>
    private void UpdateContinueButton()
    {
        if (_continueButton == null) return;

        bool hasSave = false;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.SaveData != null)
        {
            hasSave = gm.SaveData.HasSaveData();
        }
        else
        {
            // GameManager 未初期化時のフォールバック
            SaveDataManager saveManager = FindFirstObjectByType<SaveDataManager>();
            if (saveManager != null)
            {
                hasSave = saveManager.HasSaveData();
            }
        }

        _continueButton.interactable = hasSave;

        // グレーアウト表示
        ColorBlock colors = _continueButton.colors;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        _continueButton.colors = colors;
    }
}
