using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// マネジメントシーンのフェーズ進行ボタンを制御する UI コントローラー。
/// Evening → Night（準備完了）、Night → Midnight（営業終了）の遷移を担当する。
/// </summary>
public sealed class PhaseControlUI : MonoBehaviour
{
    [Header("ボタン参照")]
    [SerializeField] private Button _readyButton;
    [SerializeField] private Button _closeButton;

    [Header("ボタンラベル（任意）")]
    [SerializeField] private Text _readyButtonLabel;
    [SerializeField] private Text _closeButtonLabel;

    private const string READY_LABEL_TEXT = "準備完了";
    private const string CLOSE_LABEL_TEXT = "営業終了";

    private void Awake()
    {
        // ラベルが設定されていれば初期テキストを適用
        if (_readyButtonLabel != null) _readyButtonLabel.text = READY_LABEL_TEXT;
        if (_closeButtonLabel != null) _closeButtonLabel.text = CLOSE_LABEL_TEXT;
    }

    private void OnEnable()
    {
        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        _readyButton.onClick.AddListener(OnReadyClicked);
        _closeButton.onClick.AddListener(OnCloseClicked);

        RefreshButtons();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }

        _readyButton.onClick.RemoveListener(OnReadyClicked);
        _closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    private void HandlePhaseChanged(GameManager.GamePhase newPhase)
    {
        RefreshButtons();
    }

    /// <summary>
    /// 現在のフェーズに応じてボタンの表示/非表示を切り替える。
    /// </summary>
    private void RefreshButtons()
    {
        var phase = GameManager.Instance.CurrentPhase;

        _readyButton.gameObject.SetActive(phase == GameManager.GamePhase.Evening);
        _closeButton.gameObject.SetActive(phase == GameManager.GamePhase.Night);
    }

    /// <summary>
    /// 「準備完了」ボタン押下 — Evening → Night へ進行。
    /// </summary>
    private void OnReadyClicked()
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Evening) return;

        GameManager.Instance.AdvancePhase();
    }

    /// <summary>
    /// 「営業終了」ボタン押下 — Night → Midnight へ進行。
    /// </summary>
    private void OnCloseClicked()
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Night) return;

        GameManager.Instance.AdvancePhase();
    }
}
