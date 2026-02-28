// ============================================================
// CookedDishData.cs
// 調理済み料理の情報。CookingMinigame の結果を格納する。
// ============================================================
using System;

/// <summary>調理済み料理データ。インベントリに格納される。</summary>
[Serializable]
public sealed class CookedDishData
{
    /// <summary>元のレシピ。</summary>
    public RecipeData OriginalRecipe { get; }

    /// <summary>調理ランク。</summary>
    public CookingRank Rank { get; }

    /// <summary>最終売値。</summary>
    public int FinalPrice { get; }

    public CookedDishData(RecipeData recipe, CookingRank rank, int finalPrice)
    {
        OriginalRecipe = recipe;
        Rank           = rank;
        FinalPrice     = finalPrice;
    }
}
