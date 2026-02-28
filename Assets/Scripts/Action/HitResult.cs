// ============================================================
// HitResult.cs
// ダメージ計算後の最終結果を運ぶ構造体。
// JustInputAction → IDamageable.TakeDamage() へ渡される。
// ============================================================
using UnityEngine;

/// <summary>ダメージ計算後の最終ヒット結果。</summary>
public struct HitResult
{
    public int BaseDamage;
    public int FinalDamage;
    public int PartBreakValue;
    public Vector3 HitPosition;
    public Vector3 HitNormal;
    public bool IsJustInput;
    public float DamageMultiplier;
    public GameObject Attacker;

    public HitResult(
        int baseDamage,
        int finalDamage,
        int partBreakValue,
        Vector3 hitPosition,
        Vector3 hitNormal,
        bool isJustInput,
        float damageMultiplier,
        GameObject attacker)
    {
        BaseDamage = baseDamage;
        FinalDamage = finalDamage;
        PartBreakValue = partBreakValue;
        HitPosition = hitPosition;
        HitNormal = hitNormal;
        IsJustInput = isJustInput;
        DamageMultiplier = damageMultiplier;
        Attacker = attacker;
    }
}
