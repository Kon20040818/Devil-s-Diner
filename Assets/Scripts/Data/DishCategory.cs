// ============================================================
// DishCategory.cs
// 料理のカテゴリ。カテゴリごとに異なるバフ効果を付与する。
// Meat=ATK↑, Fish=SPD↑, Salad=DEF↑, Dessert=リジェネ
// ============================================================

/// <summary>
/// 料理カテゴリ。食事コマンドで付与されるバフの種類を決定する。
/// </summary>
public enum DishCategory
{
    /// <summary>肉料理 — 攻撃力アップ。</summary>
    Meat,

    /// <summary>魚料理 — 速度アップ。</summary>
    Fish,

    /// <summary>サラダ — 防御力アップ。</summary>
    Salad,

    /// <summary>デザート — 毎ターンHP回復（リジェネ）。</summary>
    Dessert,
}
