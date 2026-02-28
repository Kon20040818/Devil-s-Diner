// ============================================================
// EnemyController.cs
// 敵のAIコントローラー。NavMeshAgent を使用した追跡・攻撃ステートマシンと、
// IDamageable を実装した HP 管理・死亡時ドロップ処理を行う。
// ============================================================
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敵のAIコントローラー。
/// ステートマシン（Idle / Chase / Attack / Dead）で行動を管理し、
/// IDamageable を実装してダメージ受付・死亡・ドロップを処理する。
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IDamageable
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float CHASE_UPDATE_INTERVAL = 0.25f;
    private const float LOST_INTEREST_MULTIPLIER = 1.5f;
    private const float DEATH_DESTROY_DELAY = 0.1f;

    // ──────────────────────────────────────────────
    // ステート定義
    // ──────────────────────────────────────────────

    /// <summary>敵AIの状態。</summary>
    public enum EnemyState { Idle, Chase, Attack, Dead }

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────
    [SerializeField] protected EnemyData _enemyData;

    [Header("AI設定")]
    [SerializeField] protected float _detectionRange = 15f;
    [SerializeField] protected float _attackRange = 2f;
    [SerializeField] protected float _attackCooldown = 2f;
    [SerializeField] protected float _attackDamage = 10f;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    protected int _currentHP;
    protected EnemyState _currentState = EnemyState.Idle;
    protected NavMeshAgent _agent;
    protected Animator _animator;
    protected Transform _playerTransform;
    protected float _chaseUpdateTimer;
    protected float _attackCooldownTimer;
    protected float _resolvedAttackDamage;

    // ──────────────────────────────────────────────
    // IDamageable 実装
    // ──────────────────────────────────────────────

    /// <inheritdoc/>
    public int CurrentHP => _currentHP;

    /// <inheritdoc/>
    public bool IsAlive => _currentHP > 0;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>現在のAIステート。</summary>
    public EnemyState CurrentState => _currentState;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    protected virtual void Awake()
    {
        // NavMeshAgent 取得
        _agent = GetComponent<NavMeshAgent>();

        // Animator 取得（任意）
        TryGetComponent(out _animator);

        // プレイヤー検索
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"[EnemyController] 'Player' タグのオブジェクトが見つかりません: {gameObject.name}");
        }

        // HP 初期化
        if (_enemyData != null)
        {
            _currentHP = _enemyData.MaxHP;
        }
        else
        {
            Debug.LogError($"[EnemyController] EnemyData が未設定です: {gameObject.name}");
        }

        // 攻撃力の解決: _attackDamage が 0 以下なら EnemyData.BaseAttack を使用
        if (_attackDamage <= 0f && _enemyData != null)
        {
            _resolvedAttackDamage = _enemyData.BaseAttack;
        }
        else
        {
            _resolvedAttackDamage = _attackDamage;
        }

        // 初期ステート
        _currentState = EnemyState.Idle;
    }

    protected virtual void Update()
    {
        switch (_currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Dead:
                // 何もしない
                break;
        }
    }

    // ──────────────────────────────────────────────
    // ステート更新
    // ──────────────────────────────────────────────

    protected virtual void UpdateIdle()
    {
        if (_playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= _detectionRange)
        {
            TransitionTo(EnemyState.Chase);
        }
    }

    protected virtual void UpdateChase()
    {
        if (_playerTransform == null)
        {
            TransitionTo(EnemyState.Idle);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        // 見失い判定
        if (distanceToPlayer > _detectionRange * LOST_INTEREST_MULTIPLIER)
        {
            TransitionTo(EnemyState.Idle);
            return;
        }

        // 攻撃範囲内
        if (distanceToPlayer <= _attackRange)
        {
            TransitionTo(EnemyState.Attack);
            return;
        }

        // 追跡先を定期的に更新（パフォーマンス対策）
        _chaseUpdateTimer -= Time.deltaTime;
        if (_chaseUpdateTimer <= 0f)
        {
            _agent.SetDestination(_playerTransform.position);
            _chaseUpdateTimer = CHASE_UPDATE_INTERVAL;
        }
    }

    protected virtual void UpdateAttack()
    {
        if (_playerTransform == null)
        {
            TransitionTo(EnemyState.Idle);
            return;
        }

        // プレイヤー方向へ滑らかに回転
        RotateTowardPlayer();

        // クールダウンタイマー
        _attackCooldownTimer -= Time.deltaTime;

        if (_attackCooldownTimer <= 0f)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (distanceToPlayer <= _attackRange)
            {
                ExecuteAttack();
                _attackCooldownTimer = _attackCooldown;
            }
            else
            {
                // 攻撃範囲外 → 追跡に戻る
                TransitionTo(EnemyState.Chase);
            }
        }
    }

    // ──────────────────────────────────────────────
    // ステート遷移
    // ──────────────────────────────────────────────

    protected virtual void TransitionTo(EnemyState newState)
    {
        if (_currentState == EnemyState.Dead) return;

        _currentState = newState;

        switch (newState)
        {
            case EnemyState.Idle:
                _agent.isStopped = true;
                _agent.ResetPath();
                break;

            case EnemyState.Chase:
                _agent.isStopped = false;
                _chaseUpdateTimer = 0f; // 即座に目的地を設定させる
                break;

            case EnemyState.Attack:
                _agent.isStopped = true;
                _attackCooldownTimer = 0f; // 遷移時に即座に攻撃
                break;

            case EnemyState.Dead:
                _agent.isStopped = true;
                _agent.ResetPath();
                break;
        }
    }

    // ──────────────────────────────────────────────
    // 攻撃実行
    // ──────────────────────────────────────────────

    protected virtual void ExecuteAttack()
    {
        if (_playerTransform == null) return;

        if (_playerTransform.TryGetComponent(out IDamageable damageable))
        {
            int attackDamageInt = Mathf.RoundToInt(_resolvedAttackDamage);

            var hitResult = new HitResult(
                baseDamage: attackDamageInt,
                finalDamage: attackDamageInt,
                partBreakValue: 0,
                hitPosition: _playerTransform.position,
                hitNormal: (_playerTransform.position - transform.position).normalized,
                isJustInput: false,
                damageMultiplier: 1f,
                attacker: gameObject
            );

            damageable.TakeDamage(hitResult);

            Debug.Log(
                $"[EnemyController] {(_enemyData != null ? _enemyData.EnemyName : gameObject.name)} " +
                $"がプレイヤーに攻撃！ ダメージ: {attackDamageInt}");
        }
    }

    // ──────────────────────────────────────────────
    // プレイヤー方向への回転
    // ──────────────────────────────────────────────

    protected virtual void RotateTowardPlayer()
    {
        Vector3 direction = _playerTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * 10f
        );
    }

    // ──────────────────────────────────────────────
    // IDamageable.TakeDamage
    // ──────────────────────────────────────────────

    /// <inheritdoc/>
    public void TakeDamage(HitResult hitResult)
    {
        if (_currentState == EnemyState.Dead) return;
        if (!IsAlive) return;

        _currentHP -= hitResult.FinalDamage;
        _currentHP = Mathf.Max(0, _currentHP);

        Debug.Log(
            $"[EnemyController] {(_enemyData != null ? _enemyData.EnemyName : gameObject.name)} " +
            $"にダメージ {hitResult.FinalDamage} (残HP: {_currentHP}) " +
            $"Just={hitResult.IsJustInput}");

        if (!IsAlive)
        {
            OnDeath(hitResult.IsJustInput);
        }
    }

    // ──────────────────────────────────────────────
    // 死亡処理
    // ──────────────────────────────────────────────

    protected virtual void OnDeath(bool wasJustInput)
    {
        Debug.Log($"[EnemyController] {(_enemyData != null ? _enemyData.EnemyName : gameObject.name)} 撃破!");

        // Dead ステートへ遷移
        _currentState = EnemyState.Dead;

        // エージェント停止
        _agent.isStopped = true;
        _agent.ResetPath();

        // コライダー無効化
        if (TryGetComponent(out Collider col))
        {
            col.enabled = false;
        }

        // ドロップ判定
        if (_enemyData != null)
        {
            DropResolver.ResolveDrop(_enemyData, wasJustInput, transform.position);
        }

        // オブジェクト破棄（死亡アニメーション用に若干の遅延）
        Destroy(gameObject, DEATH_DESTROY_DELAY);
    }
}
