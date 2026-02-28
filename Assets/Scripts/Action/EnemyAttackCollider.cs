using System;
using UnityEngine;

/// <summary>
/// 敵の攻撃判定用トリガーコライダー。
/// EnemyControllerのAttackステート中のみ有効化され、
/// プレイヤーに接触するとダメージを与える。
/// </summary>
public sealed class EnemyAttackCollider : MonoBehaviour
{
    // ───────────────────────── Inspector Fields ─────────────────────────
    [SerializeField] private EnemyController _enemyController;
    [SerializeField] private float _attackDamage = 10f;

    // ───────────────────────── Internal State ─────────────────────────
    private Collider _collider;
    private bool _hasHitThisSwing;

    // ───────────────────────── Lifecycle ─────────────────────────
    private void Awake()
    {
        // トリガーコライダーをキャッシュして無効化
        if (TryGetComponent(out _collider))
        {
            _collider.enabled = false;
        }

        // EnemyControllerが未設定の場合、親から取得を試みる
        if (_enemyController == null)
        {
            _enemyController = GetComponentInParent<EnemyController>();
        }
    }

    private void Update()
    {
        if (_enemyController == null || _collider == null) return;

        // Attackステート中のみコライダーを有効化
        if (_enemyController.CurrentState == EnemyController.EnemyState.Attack)
        {
            _collider.enabled = true;
        }
        else
        {
            _collider.enabled = false;
            _hasHitThisSwing = false;
        }
    }

    // ───────────────────────── Trigger ─────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        // 1スイングにつき1ヒットのみ
        if (_hasHitThisSwing) return;

        // プレイヤータグ判定
        if (!other.CompareTag("Player")) return;

        // PlayerHealthの取得を試みる（直接 → 親）
        if (!other.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth = other.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null) return;

        // ダメージ計算
        int damage = Mathf.RoundToInt(_attackDamage);
        Vector3 hitDirection = (other.transform.position - transform.position).normalized;
        GameObject attacker = _enemyController != null ? _enemyController.gameObject : gameObject;

        HitResult hitResult = new HitResult(
            baseDamage: damage,
            finalDamage: damage,
            partBreakValue: 0,
            hitPosition: other.transform.position,
            hitNormal: hitDirection,
            isJustInput: false,
            damageMultiplier: 1f,
            attacker: attacker
        );

        // ダメージ適用
        playerHealth.TakeDamage(hitResult);
        _hasHitThisSwing = true;

        Debug.Log($"[EnemyAttackCollider] プレイヤーにヒット！ ダメージ: {damage}");
    }
}
