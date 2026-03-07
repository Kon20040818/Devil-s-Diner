// ============================================================
// DishInstance.cs
// DishData（レシピSO）+ DishQuality（品質）のタプル。
// InventoryManager の料理専用辞書キーとして使用する中間テーブル。
// ============================================================
using System;

/// <summary>
/// 料理インスタンス。同一レシピでも品質違いを区別する。
/// Dictionary キーとして使用するため IEquatable を実装。
/// </summary>
public readonly struct DishInstance : IEquatable<DishInstance>
{
    // ──────────────────────────────────────────────
    // フィールド
    // ──────────────────────────────────────────────

    /// <summary>レシピマスターデータ。</summary>
    public readonly DishData Data;

    /// <summary>品質ランク。</summary>
    public readonly DishQuality Quality;

    // ──────────────────────────────────────────────
    // コンストラクタ
    // ──────────────────────────────────────────────

    public DishInstance(DishData data, DishQuality quality)
    {
        Data    = data;
        Quality = quality;
    }

    // ──────────────────────────────────────────────
    // 便利プロパティ（品質適用済みの値）
    // ──────────────────────────────────────────────

    /// <summary>品質を加味した HP 回復量。</summary>
    public int HealAmount => Data != null ? Data.GetHealAmount(Quality) : 0;

    /// <summary>品質を加味したカテゴリバフ量。</summary>
    public float BuffAmount => Data != null ? Data.GetBuffAmount(Quality) : 0f;

    /// <summary>品質を加味したスカウトボーナス。</summary>
    public float ScoutBonus => Data != null ? Data.GetScoutBonus(Quality) : 0f;

    /// <summary>品質を加味した店舗販売価格。</summary>
    public int ShopPrice => Data != null ? Data.GetShopPrice(Quality) : 0;

    /// <summary>品質を加味した顧客満足度。</summary>
    public int Satisfaction => Data != null ? Data.GetSatisfaction(Quality) : 0;

    /// <summary>バフ持続ターン数（品質非依存）。</summary>
    public int BuffDurationTurns => Data != null ? Data.BuffDurationTurns : 0;

    /// <summary>料理カテゴリ（品質非依存）。</summary>
    public DishCategory Category => Data != null ? Data.Category : DishCategory.Meat;

    /// <summary>セーブ/ロード用一意ID。形式: "{ItemID}_{Quality}"</summary>
    public string UniqueID => Data != null ? $"{Data.ItemID}_{Quality}" : string.Empty;

    // ──────────────────────────────────────────────
    // IEquatable / Equals / GetHashCode
    // ──────────────────────────────────────────────

    public bool Equals(DishInstance other)
    {
        return ReferenceEquals(Data, other.Data) && Quality == other.Quality;
    }

    public override bool Equals(object obj)
    {
        return obj is DishInstance other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Data != null ? Data.GetHashCode() : 0;
            hash = (hash * 397) ^ (int)Quality;
            return hash;
        }
    }

    public static bool operator ==(DishInstance left, DishInstance right) => left.Equals(right);
    public static bool operator !=(DishInstance left, DishInstance right) => !left.Equals(right);

    // ──────────────────────────────────────────────
    // ToString
    // ──────────────────────────────────────────────

    public override string ToString()
    {
        string name = Data != null ? Data.DisplayName : "(null)";
        return $"{name} [{Quality}]";
    }
}
