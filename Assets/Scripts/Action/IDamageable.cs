// ============================================================
// IDamageable.cs
// ダメージを受けるオブジェクトが実装するインターフェース。
// ============================================================

/// <summary>ダメージを受けるオブジェクトが実装するインターフェース。</summary>
public interface IDamageable
{
    /// <summary>ダメージを適用する。</summary>
    void TakeDamage(HitResult hitResult);

    /// <summary>現在のHP。</summary>
    int CurrentHP { get; }

    /// <summary>生存しているか。</summary>
    bool IsAlive { get; }
}
