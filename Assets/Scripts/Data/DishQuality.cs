// ============================================================
// DishQuality.cs
// 料理の品質ランク。QualityScaleTable と組み合わせて
// 回復量・バフ倍率・販売価格などのスケーリングを行う。
// ============================================================

/// <summary>
/// 料理の品質ランク。同一レシピでも品質によって効果が変動する。
/// </summary>
public enum DishQuality
{
    /// <summary>失敗品 — 全パラメータ低下。</summary>
    Poor,

    /// <summary>普通 — 基本値そのまま。</summary>
    Normal,

    /// <summary>上出来 — やや強化。</summary>
    Fine,

    /// <summary>極上 — 大幅強化。</summary>
    Exquisite,
}
