// ============================================================
// AttackPhase.cs
// 攻撃モーションのフェーズ列挙型。
// ============================================================

/// <summary>攻撃モーションの各フェーズを表す列挙型。</summary>
public enum AttackPhase
{
    None,
    PreCast,
    Active,
    HitStop,
    Recovery
}
