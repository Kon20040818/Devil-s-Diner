// ============================================================
// DamageInfo.cs
// 攻撃ヒット時の基本ダメージ情報を運ぶ構造体。
// WeaponColliderHandler → JustInputAction へ渡される。
// ============================================================
using UnityEngine;

/// <summary>攻撃ヒット時の基本ダメージ情報。</summary>
public struct DamageInfo
{
    public int BaseDamage;
    public int BasePartBreakValue;
    public Vector3 HitPoint;
    public Vector3 HitNormal;
    public GameObject Attacker;

    public DamageInfo(int baseDamage, int basePartBreakValue, Vector3 hitPoint, Vector3 hitNormal, GameObject attacker)
    {
        BaseDamage = baseDamage;
        BasePartBreakValue = basePartBreakValue;
        HitPoint = hitPoint;
        HitNormal = hitNormal;
        Attacker = attacker;
    }
}
