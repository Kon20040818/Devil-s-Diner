// ============================================================
// EnemySymbol.cs
// フィールド上の敵シンボル。NavMeshAgent ベースの3ステートAI
// （Patrol / Chase / Returning）で徘徊・追跡を行う。
// プレイヤーとの接触でエンカウントイベントを発火する。
// ============================================================
using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// フィールドシーンの敵シンボルコンポーネント。
/// NavMeshAgent で徘徊し、プレイヤーを検知すると追跡する。
/// 接触時に OnEncounter イベントを発火する。
/// </summary>
public sealed class EnemySymbol : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // ステート
    // ──────────────────────────────────────────────

    public enum SymbolState { Patrol, Chase, Returning }

    // ──────────────────────────────────────────────
    // Inspector — データ
    // ──────────────────────────────────────────────

    [Header("データ")]
    [Tooltip("この敵シンボルの EnemyData")]
    [SerializeField] private EnemyData _enemyData;

    [Tooltip("この敵シンボルの CharacterStats")]
    [SerializeField] private CharacterStats _enemyStats;

    // ──────────────────────────────────────────────
    // Inspector — 徘徊設定
    // ──────────────────────────────────────────────

    [Header("徘徊設定")]
    [Tooltip("スポーン地点からの徘徊半径（m）")]
    [SerializeField] private float _patrolRadius = 8f;

    [Tooltip("徘徊時の移動速度（m/s）")]
    [SerializeField] private float _patrolSpeed = 2f;

    [Tooltip("各巡回地点での待機時間（秒）")]
    [SerializeField] private float _patrolWaitTime = 2f;

    // ──────────────────────────────────────────────
    // Inspector — 追跡設定
    // ──────────────────────────────────────────────

    [Header("追跡設定")]
    [Tooltip("プレイヤー検知距離（m）")]
    [SerializeField] private float _detectionRange = 10f;

    [Tooltip("前方検知角度（度）")]
    [SerializeField] private float _detectionAngle = 120f;

    [Tooltip("追跡時の移動速度（m/s）")]
    [SerializeField] private float _chaseSpeed = 5f;

    [Tooltip("追跡をやめる距離（m）")]
    [SerializeField] private float _chaseLostDistance = 15f;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>プレイヤーと接触した時に発火する。</summary>
    public event Action<EnemySymbol> OnEncounter;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    public EnemyData EnemyData => _enemyData;
    public CharacterStats EnemyStats => _enemyStats;
    public SymbolState CurrentState => _currentState;

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private NavMeshAgent _agent;
    private Transform _player;
    private Vector3 _spawnPosition;
    private SymbolState _currentState = SymbolState.Patrol;
    private float _patrolWaitTimer;
    private bool _hasPatrolTarget;

    // ──────────────────────────────────────────────
    // 外部設定
    // ──────────────────────────────────────────────

    /// <summary>プレイヤーの Transform を設定する（Bootstrap 用）。</summary>
    public void SetPlayer(Transform player) => _player = player;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            _agent = gameObject.AddComponent<NavMeshAgent>();
        }
        _spawnPosition = transform.position;
    }

    private void Start()
    {
        _agent.speed = _patrolSpeed;
        _agent.stoppingDistance = 0.5f;
    }

    private void Update()
    {
        switch (_currentState)
        {
            case SymbolState.Patrol:
                UpdatePatrol();
                break;
            case SymbolState.Chase:
                UpdateChase();
                break;
            case SymbolState.Returning:
                UpdateReturning();
                break;
        }
    }

    // ──────────────────────────────────────────────
    // エンカウント判定（Trigger）
    // ──────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (_player == null) return;

        // プレイヤーとの接触判定
        if (other.transform == _player || other.transform.IsChildOf(_player))
        {
            OnEncounter?.Invoke(this);
        }
    }

    // ──────────────────────────────────────────────
    // ステート: Patrol
    // ──────────────────────────────────────────────

    private void UpdatePatrol()
    {
        // プレイヤー検知チェック
        if (CanDetectPlayer())
        {
            TransitionTo(SymbolState.Chase);
            return;
        }

        // 目的地に到着したら待機
        if (!_hasPatrolTarget || HasReachedDestination())
        {
            _patrolWaitTimer += Time.deltaTime;
            if (_patrolWaitTimer >= _patrolWaitTime)
            {
                SetRandomPatrolTarget();
                _patrolWaitTimer = 0f;
            }
        }
    }

    // ──────────────────────────────────────────────
    // ステート: Chase
    // ──────────────────────────────────────────────

    private void UpdateChase()
    {
        if (_player == null)
        {
            TransitionTo(SymbolState.Returning);
            return;
        }

        float distance = Vector3.Distance(transform.position, _player.position);

        // 追跡距離を超えたら帰還
        if (distance > _chaseLostDistance)
        {
            TransitionTo(SymbolState.Returning);
            return;
        }

        _agent.SetDestination(_player.position);
    }

    // ──────────────────────────────────────────────
    // ステート: Returning
    // ──────────────────────────────────────────────

    private void UpdateReturning()
    {
        // プレイヤーが接近したら再追跡
        if (CanDetectPlayer())
        {
            TransitionTo(SymbolState.Chase);
            return;
        }

        // スポーン地点に到着したら Patrol へ
        if (HasReachedDestination())
        {
            TransitionTo(SymbolState.Patrol);
        }
    }

    // ──────────────────────────────────────────────
    // ステート遷移
    // ──────────────────────────────────────────────

    private void TransitionTo(SymbolState newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case SymbolState.Patrol:
                _agent.speed = _patrolSpeed;
                _hasPatrolTarget = false;
                _patrolWaitTimer = 0f;
                break;

            case SymbolState.Chase:
                _agent.speed = _chaseSpeed;
                if (_player != null) _agent.SetDestination(_player.position);
                break;

            case SymbolState.Returning:
                _agent.speed = _patrolSpeed;
                _agent.SetDestination(_spawnPosition);
                break;
        }
    }

    // ──────────────────────────────────────────────
    // ヘルパー
    // ──────────────────────────────────────────────

    private bool CanDetectPlayer()
    {
        if (_player == null) return false;

        float distance = Vector3.Distance(transform.position, _player.position);
        if (distance > _detectionRange) return false;

        // 視野角チェック
        Vector3 dirToPlayer = (_player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        return angle <= _detectionAngle * 0.5f;
    }

    private void SetRandomPatrolTarget()
    {
        Vector3 randomDir = UnityEngine.Random.insideUnitSphere * _patrolRadius;
        randomDir += _spawnPosition;
        randomDir.y = _spawnPosition.y;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
            _hasPatrolTarget = true;
        }
    }

    private bool HasReachedDestination()
    {
        if (_agent.pathPending) return false;
        return _agent.remainingDistance <= _agent.stoppingDistance + 0.1f;
    }
}
