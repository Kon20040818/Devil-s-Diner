// ============================================================
// CustomerAI.cs
// 客NPCのAIステートマシン。NavMeshAgentによる移動と
// Spawn → FindSeat → Order → Eating → Payment → Leave の遷移を制御。
// ============================================================
using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 客NPCのAI。ステートマシンで行動を制御する。
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public sealed class CustomerAI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float WAIT_FOR_SEAT_TIMEOUT = 10f;
    private const float EATING_TIME = 10f;
    private const float ARRIVAL_THRESHOLD = 0.5f;

    // ──────────────────────────────────────────────
    // ステート定義
    // ──────────────────────────────────────────────
    public enum CustomerState
    {
        Spawned,
        FindingSeat,
        WaitingForSeat,
        Ordering,
        WaitingForFood,
        Eating,
        Paying,
        Leaving
    }

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>客が注文したとき。引数は注文したレシピ。</summary>
    public event Action<RecipeData> OnCustomerOrdered;

    /// <summary>支払い完了時。引数は支払い金額。</summary>
    public event Action<int> OnPaymentMade;

    // ──────────────────────────────────────────────
    // シリアライズフィールド
    // ──────────────────────────────────────────────

    [SerializeField] private Transform _exitPoint;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private NavMeshAgent _agent;
    private CustomerState _currentState = CustomerState.Spawned;
    private SeatNode _assignedSeat;
    private SeatManager _seatManager;
    private CookedDishData _servedDish;
    private float _stateTimer;

    /// <summary>現在のステート（読み取り専用）。</summary>
    public CustomerState CurrentState => _currentState;

    /// <summary>提供された料理（読み取り専用）。</summary>
    public CookedDishData ServedDish => _servedDish;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        switch (_currentState)
        {
            case CustomerState.Spawned:
                HandleSpawned();
                break;
            case CustomerState.FindingSeat:
                HandleFindingSeat();
                break;
            case CustomerState.WaitingForSeat:
                HandleWaitingForSeat();
                break;
            case CustomerState.Ordering:
                HandleOrdering();
                break;
            case CustomerState.WaitingForFood:
                HandleWaitingForFood();
                break;
            case CustomerState.Eating:
                HandleEating();
                break;
            case CustomerState.Paying:
                HandlePaying();
                break;
            case CustomerState.Leaving:
                HandleLeaving();
                break;
        }
    }

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>初期化。CustomerSpawner から呼び出される。</summary>
    public void Initialize(SeatManager seatManager, Transform exitPoint)
    {
        _seatManager = seatManager;
        _exitPoint = exitPoint;
        TransitionTo(CustomerState.FindingSeat);
    }

    /// <summary>料理を提供する。WaitingForFood ステート中に呼び出す。</summary>
    public void ServeDish(CookedDishData dish)
    {
        if (_currentState != CustomerState.WaitingForFood) return;
        _servedDish = dish;
        TransitionTo(CustomerState.Eating);
    }

    // ──────────────────────────────────────────────
    // ステート処理
    // ──────────────────────────────────────────────

    private void HandleSpawned()
    {
        // Initialize() が呼ばれるまで待機
    }

    private void HandleFindingSeat()
    {
        if (_seatManager == null)
        {
            TransitionTo(CustomerState.Leaving);
            return;
        }

        _assignedSeat = _seatManager.TryReserveSeat(this);

        if (_assignedSeat != null)
        {
            _agent.SetDestination(_assignedSeat.SitPosition);
            TransitionTo(CustomerState.Ordering);
        }
        else
        {
            // 満席 → 待機タイマー開始
            _stateTimer = 0f;
            TransitionTo(CustomerState.WaitingForSeat);
        }
    }

    private void HandleWaitingForSeat()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= WAIT_FOR_SEAT_TIMEOUT)
        {
            // タイムアウト → 退店
            TransitionTo(CustomerState.Leaving);
        }
        else
        {
            // 定期的に空席をリトライ
            _assignedSeat = _seatManager.TryReserveSeat(this);
            if (_assignedSeat != null)
            {
                _agent.SetDestination(_assignedSeat.SitPosition);
                TransitionTo(CustomerState.Ordering);
            }
        }
    }

    private void HandleOrdering()
    {
        // 席への移動完了を待つ
        if (!HasArrived()) return;

        // 到着 → 注文
        // 提供可能なメニューからランダムで1つ注文（仮: イベント発火のみ）
        OnCustomerOrdered?.Invoke(null); // 実際のレシピ選択は OrderQueue で処理
        TransitionTo(CustomerState.WaitingForFood);
    }

    private void HandleWaitingForFood()
    {
        // ServeDish() が呼ばれるまで待機
    }

    private void HandleEating()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= EATING_TIME)
        {
            TransitionTo(CustomerState.Paying);
        }
    }

    private void HandlePaying()
    {
        int payment = _servedDish != null ? _servedDish.FinalPrice : 0;

        // チップ計算（将来: 居心地度ベースのボーナス）
        OnPaymentMade?.Invoke(payment);
        TransitionTo(CustomerState.Leaving);
    }

    private void HandleLeaving()
    {
        if (_exitPoint != null)
        {
            if (!_agent.hasPath || _agent.remainingDistance > ARRIVAL_THRESHOLD)
            {
                _agent.SetDestination(_exitPoint.position);
                return;
            }
        }

        // ドア外到達 → 席解放 & 破棄
        if (_assignedSeat != null && _seatManager != null)
        {
            _seatManager.ReleaseSeat(_assignedSeat);
        }

        Destroy(gameObject);
    }

    // ──────────────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────────────

    private void TransitionTo(CustomerState newState)
    {
        _currentState = newState;
        _stateTimer = 0f;
    }

    private bool HasArrived()
    {
        if (_agent.pathPending) return false;
        return _agent.remainingDistance <= ARRIVAL_THRESHOLD;
    }
}
