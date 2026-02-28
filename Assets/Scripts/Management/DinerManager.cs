// ============================================================
// DinerManager.cs
// ManagementScene の統括マネージャー。
// 家具の居心地度集計、CustomerAI の支払い処理、営業開始制御を担う。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ManagementScene の統括マネージャー。
/// 配置済み家具の ComfortBonus 集計、CustomerAI の登録管理、
/// Night フェーズ開始時の営業開始処理を行う。
/// </summary>
public sealed class DinerManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // シリアライズフィールド
    // ──────────────────────────────────────────────

    [SerializeField] private List<FurnitureData> _placedFurniture = new List<FurnitureData>();
    [SerializeField] private HousingManager _housingManager;
    [SerializeField] private MoneyPopUp _moneyPopUp;
    [SerializeField] private MidnightResultUI _midnightResultUI;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>営業が開始されたとき。</summary>
    public event Action OnBusinessStarted;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private readonly List<CustomerAI> _customers = new List<CustomerAI>();

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>
    /// 配置済み家具の ComfortBonus 合計値。
    /// HousingManager が設定されている場合はそちらに委譲する。
    /// </summary>
    public float ComfortScore
    {
        get
        {
            if (_housingManager != null)
            {
                return _housingManager.ComfortScore;
            }

            // フォールバック: HousingManager 未設定時は従来の計算
            float total = 0f;
            for (int i = 0; i < _placedFurniture.Count; i++)
            {
                if (_placedFurniture[i] != null)
                {
                    total += _placedFurniture[i].ComfortBonus;
                }
            }
            return total;
        }
    }

    /// <summary>HousingManager への外部アクセス用プロパティ。</summary>
    public HousingManager Housing => _housingManager;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }

        // 登録済み CustomerAI のイベント購読を全解除
        for (int i = _customers.Count - 1; i >= 0; i--)
        {
            if (_customers[i] != null)
            {
                _customers[i].OnPaymentMade -= HandlePaymentMade;
            }
        }
        _customers.Clear();
    }

    // ──────────────────────────────────────────────
    // 公開 API — CustomerAI 登録 / 解除
    // ──────────────────────────────────────────────

    /// <summary>
    /// CustomerAI を登録し、支払いイベントをリッスンする。
    /// </summary>
    public void RegisterCustomer(CustomerAI customer)
    {
        if (customer == null) return;
        if (_customers.Contains(customer)) return;

        _customers.Add(customer);
        customer.OnPaymentMade += HandlePaymentMade;
    }

    /// <summary>
    /// CustomerAI の登録を解除し、支払いイベントのリッスンを停止する。
    /// </summary>
    public void UnregisterCustomer(CustomerAI customer)
    {
        if (customer == null) return;
        if (!_customers.Contains(customer)) return;

        customer.OnPaymentMade -= HandlePaymentMade;
        _customers.Remove(customer);
    }

    // ──────────────────────────────────────────────
    // 公開 API — 家具管理
    // ──────────────────────────────────────────────

    /// <summary>家具を配置リストに追加する。</summary>
    public void PlaceFurniture(FurnitureData furniture)
    {
        if (furniture == null) return;
        _placedFurniture.Add(furniture);
    }

    /// <summary>家具を配置リストから除去する。</summary>
    public void RemoveFurniture(FurnitureData furniture)
    {
        if (furniture == null) return;
        _placedFurniture.Remove(furniture);
    }

    /// <summary>配置済み家具リスト（読み取り専用）。</summary>
    public IReadOnlyList<FurnitureData> PlacedFurniture => _placedFurniture;

    // ──────────────────────────────────────────────
    // 内部メソッド
    // ──────────────────────────────────────────────

    /// <summary>
    /// フェーズ変更ハンドラ。Night フェーズ開始時に営業を開始する。
    /// </summary>
    private void HandlePhaseChanged(GameManager.GamePhase newPhase)
    {
        if (newPhase == GameManager.GamePhase.Night)
        {
            StartBusiness();
        }
    }

    /// <summary>
    /// 営業開始処理。Night フェーズに入ったときに呼ばれる。
    /// </summary>
    private void StartBusiness()
    {
        Debug.Log($"[DinerManager] 営業開始 — ComfortScore: {ComfortScore:F1}");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("business_start");
        }
        OnBusinessStarted?.Invoke();
    }

    /// <summary>
    /// CustomerAI の支払いイベントハンドラ。
    /// HousingManager のチップボーナスを適用して所持金に加算する。
    /// MoneyPopUp と MidnightResultUI に支払い情報を通知する。
    /// </summary>
    private void HandlePaymentMade(int amount)
    {
        int tip = 0;
        if (_housingManager != null)
        {
            tip = Mathf.RoundToInt(amount * _housingManager.TipBonusRate);
        }

        int totalPayment = amount + tip;
        GameManager.Instance.AddGold(totalPayment);

        // チャリンSE
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(tip > 0 ? "payment_with_tip" : "payment");
        }

        // MidnightResultUI に支払いを記録
        if (_midnightResultUI != null)
        {
            _midnightResultUI.RecordPayment(amount, tip);
        }

        // MoneyPopUp — 支払った客の頭上にポップアップ
        if (_moneyPopUp != null)
        {
            // 最後に支払いをした客の位置を取得
            CustomerAI payer = FindPayingCustomer();
            Vector3 popupPos = payer != null ? payer.transform.position : transform.position;
            _moneyPopUp.ShowPopUp(popupPos, totalPayment);
        }

        if (tip > 0)
        {
            Debug.Log($"[DinerManager] 支払い: {amount}G + チップ: {tip}G (ComfortScore: {ComfortScore:F1})");
        }
    }

    /// <summary>Paying ステートの客を検索する。</summary>
    private CustomerAI FindPayingCustomer()
    {
        for (int i = _customers.Count - 1; i >= 0; i--)
        {
            if (_customers[i] != null &&
                (_customers[i].CurrentState == CustomerAI.CustomerState.Paying ||
                 _customers[i].CurrentState == CustomerAI.CustomerState.Leaving))
            {
                return _customers[i];
            }
        }
        return null;
    }
}
