// ============================================================
// BossEnemy.cs
// 牛頭の保安官（中ボス）のAI。通常攻撃に加え、
// 予備動作の長い大振り攻撃（ジャスト入力の好機）を行う。
// ============================================================
using UnityEngine;

/// <summary>
/// 牛頭の保安官（中ボス）のAIコントローラー。
/// 通常攻撃のほかに、長い予備動作を持つ大振り攻撃を繰り出す。
/// 予備動作中はプレイヤーにジャスト入力の好機を与える。
/// </summary>
public class BossEnemy : EnemyController
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float HEAVY_ATTACK_WINDUP_DURATION = 2.0f;
    private const float HEAVY_ATTACK_DAMAGE_MULTIPLIER = 3.0f;
    private const float HEAVY_ATTACK_COOLDOWN = 8.0f;
    private const float HEAVY_ATTACK_CHANCE = 0.3f;
    private const float BOSS_DETECTION_RANGE = 25f;
    private const float BOSS_ATTACK_RANGE = 3f;
    private const float BOSS_ATTACK_COOLDOWN = 2.5f;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>大振り攻撃の予備動作が開始された（UI/音響演出用）。</summary>
    public event System.Action OnHeavyAttackWindupStart;

    /// <summary>大振り攻撃が実行された（UI/音響演出用）。</summary>
    public event System.Action OnHeavyAttackExecute;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>大振り攻撃の予備動作中かどうか。</summary>
    public bool IsHeavyAttackWindup => _isHeavyAttackWindup;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private bool _isHeavyAttackWindup;
    private bool _isHeavyAttacking;
    private float _heavyAttackWindupTimer;
    private float _heavyAttackTimer;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        // ボス用パラメータ上書き
        _detectionRange = BOSS_DETECTION_RANGE;
        _attackRange = BOSS_ATTACK_RANGE;
        _attackCooldown = BOSS_ATTACK_COOLDOWN;
    }

    protected override void Update()
    {
        base.Update();

        // 大振り攻撃クールダウンを独立してカウントダウン
        if (_heavyAttackTimer > 0f)
        {
            _heavyAttackTimer -= Time.deltaTime;
        }
    }

    // ──────────────────────────────────────────────
    // ステート更新（オーバーライド）
    // ──────────────────────────────────────────────

    protected override void UpdateAttack()
    {
        if (_playerTransform == null)
        {
            TransitionTo(EnemyState.Idle);
            return;
        }

        // ─── 大振り予備動作中 ───
        if (_isHeavyAttackWindup)
        {
            RotateTowardPlayer();

            _heavyAttackWindupTimer -= Time.deltaTime;
            if (_heavyAttackWindupTimer <= 0f)
            {
                ExecuteHeavyAttack();
                _isHeavyAttackWindup = false;
                _isHeavyAttacking = true;
            }
            return;
        }

        // ─── 大振り攻撃発動直後の硬直 ───
        if (_isHeavyAttacking)
        {
            // 硬直後、通常の攻撃ステートに戻る
            _isHeavyAttacking = false;
            _attackCooldownTimer = _attackCooldown;
            return;
        }

        // ─── 通常の攻撃処理 ───
        RotateTowardPlayer();

        _attackCooldownTimer -= Time.deltaTime;

        if (_attackCooldownTimer <= 0f)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (distanceToPlayer <= _attackRange)
            {
                // 大振り攻撃の抽選（クールダウンが終わっている場合のみ）
                if (_heavyAttackTimer <= 0f && Random.value < HEAVY_ATTACK_CHANCE)
                {
                    StartHeavyAttackWindup();
                }
                else
                {
                    // 通常攻撃
                    base.ExecuteAttack();
                    _attackCooldownTimer = _attackCooldown;
                }
            }
            else
            {
                // 攻撃範囲外 → 追跡に戻る
                TransitionTo(EnemyState.Chase);
            }
        }
    }

    // ──────────────────────────────────────────────
    // 大振り攻撃
    // ──────────────────────────────────────────────

    /// <summary>大振り攻撃の予備動作を開始する。</summary>
    private void StartHeavyAttackWindup()
    {
        _isHeavyAttackWindup = true;
        _heavyAttackWindupTimer = HEAVY_ATTACK_WINDUP_DURATION;

        // エージェント停止（構え中は移動しない）
        _agent.isStopped = true;

        OnHeavyAttackWindupStart?.Invoke();

        Debug.Log(
            $"[BossEnemy] {(_enemyData != null ? _enemyData.EnemyName : gameObject.name)} " +
            $"大振り攻撃の予備動作開始！");
    }

    /// <summary>大振り攻撃を実行する。通常の3倍ダメージ。</summary>
    private void ExecuteHeavyAttack()
    {
        if (_playerTransform == null) return;

        if (_playerTransform.TryGetComponent(out IDamageable damageable))
        {
            int baseDamageInt = Mathf.RoundToInt(_resolvedAttackDamage);
            int heavyDamage = Mathf.RoundToInt(_resolvedAttackDamage * HEAVY_ATTACK_DAMAGE_MULTIPLIER);

            var hitResult = new HitResult(
                baseDamage: baseDamageInt,
                finalDamage: heavyDamage,
                partBreakValue: 0,
                hitPosition: _playerTransform.position,
                hitNormal: (_playerTransform.position - transform.position).normalized,
                isJustInput: false,
                damageMultiplier: HEAVY_ATTACK_DAMAGE_MULTIPLIER,
                attacker: gameObject
            );

            damageable.TakeDamage(hitResult);

            Debug.Log(
                $"[BossEnemy] {(_enemyData != null ? _enemyData.EnemyName : gameObject.name)} " +
                $"大振り攻撃！ ダメージ: {heavyDamage}");
        }

        // 大振り攻撃クールダウンをセット
        _heavyAttackTimer = HEAVY_ATTACK_COOLDOWN;

        OnHeavyAttackExecute?.Invoke();
    }

    // ──────────────────────────────────────────────
    // 死亡処理（オーバーライド）
    // ──────────────────────────────────────────────

    protected override void OnDeath(bool wasJustInput)
    {
        // 大振り攻撃状態をクリア
        _isHeavyAttackWindup = false;
        _isHeavyAttacking = false;
        _heavyAttackWindupTimer = 0f;

        base.OnDeath(wasJustInput);
    }
}
