// ============================================================
// WeaponColliderHandler.cs
// 武器のヒット判定を制御する。トリガーコライダーで敵との接触を検知し、
// JustInputAction にヒット情報を通知する。
// ============================================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器オブジェクトにアタッチし、トリガーコライダーによるヒット判定を行う。
/// 同一攻撃モーション中の重複ヒットを HashSet で防止する。
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class WeaponColliderHandler : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private JustInputAction _justInputAction;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private Collider _collider;
    private readonly HashSet<int> _hitTargets = new HashSet<int>();

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        _collider.enabled = false;
    }

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>ヒットボックスを有効化し、重複ヒット記録をクリアする。</summary>
    public void EnableHitbox()
    {
        _hitTargets.Clear();
        _collider.enabled = true;
    }

    /// <summary>ヒットボックスを無効化する。</summary>
    public void DisableHitbox()
    {
        _collider.enabled = false;
    }

    // ──────────────────────────────────────────────
    // トリガー判定
    // ──────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        // タグで敵のハートボックスか判定
        if (!other.CompareTag("EnemyHurtbox")) return;

        // 同一モーション中の重複ヒット防止
        int targetId = other.gameObject.GetInstanceID();
        if (_hitTargets.Contains(targetId)) return;
        _hitTargets.Add(targetId);

        // IDamageable を取得（直接 or 親オブジェクト）
        IDamageable damageable;
        if (!other.TryGetComponent(out damageable))
        {
            damageable = other.GetComponentInParent<IDamageable>();
        }

        if (damageable == null) return;

        // 武器データからベースダメージを取得（スキル攻撃力補正を適用）
        WeaponData weapon = _playerController != null ? _playerController.EquippedWeapon : null;
        int rawDamage = weapon != null ? weapon.BaseDamage : 10;
        int baseDamage = Mathf.RoundToInt(rawDamage * SkillEffectApplier.AttackMultiplier);
        int basePartBreak = weapon != null ? weapon.BasePartBreakValue : 0;

        // ヒットポイントを計算
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = (hitPoint - transform.position).normalized;

        // DamageInfo を生成
        DamageInfo damageInfo = new DamageInfo(
            baseDamage,
            basePartBreak,
            hitPoint,
            hitNormal,
            _playerController != null ? _playerController.gameObject : gameObject
        );

        // JustInputAction に通知
        if (_justInputAction != null)
        {
            _justInputAction.NotifyWeaponHit(damageable, damageInfo);
        }
        else
        {
            // JustInputAction が未設定の場合は通常ダメージを直接適用
            HitResult result = new HitResult(
                baseDamage,
                baseDamage,
                basePartBreak,
                hitPoint,
                hitNormal,
                false,
                1f,
                damageInfo.Attacker
            );
            damageable.TakeDamage(result);
        }
    }
}
