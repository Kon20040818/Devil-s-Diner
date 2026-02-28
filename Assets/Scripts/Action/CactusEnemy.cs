// ============================================================
// CactusEnemy.cs
// サボテン魔人の敵AI。素早い接近と連続攻撃を行う。
// ============================================================
using UnityEngine;

/// <summary>
/// サボテン魔人（雑魚）のAIコントローラー。
/// 基本の EnemyController を継承し、高速な追跡と短いクールダウンの連続攻撃を行う。
/// </summary>
public class CactusEnemy : EnemyController
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float CACTUS_CHASE_SPEED = 6f;
    private const float CACTUS_ATTACK_COOLDOWN = 1.0f;
    private const float CACTUS_ATTACK_RANGE = 2.5f;
    private const float CACTUS_DETECTION_RANGE = 20f;
    private const float CACTUS_COOLDOWN_REDUCTION = 0.7f;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private float _defaultAgentSpeed;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        // サボテン魔人用パラメータ上書き
        _attackCooldown = CACTUS_ATTACK_COOLDOWN;
        _attackRange = CACTUS_ATTACK_RANGE;
        _detectionRange = CACTUS_DETECTION_RANGE;

        // デフォルト速度を記録
        _defaultAgentSpeed = _agent.speed;
    }

    // ──────────────────────────────────────────────
    // ステート更新（オーバーライド）
    // ──────────────────────────────────────────────

    protected override void UpdateChase()
    {
        // 追跡時は速度を引き上げる
        _agent.speed = CACTUS_CHASE_SPEED;

        base.UpdateChase();
    }

    protected override void UpdateIdle()
    {
        // Idle 時はデフォルト速度に戻す
        _agent.speed = _defaultAgentSpeed;

        base.UpdateIdle();
    }

    // ──────────────────────────────────────────────
    // 攻撃実行（オーバーライド）
    // ──────────────────────────────────────────────

    protected override void ExecuteAttack()
    {
        // 基底の攻撃処理を実行
        base.ExecuteAttack();

        // クールダウンを短縮して連続攻撃を可能にする
        _attackCooldownTimer *= CACTUS_COOLDOWN_REDUCTION;
    }
}
