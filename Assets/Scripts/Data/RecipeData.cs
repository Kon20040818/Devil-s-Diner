// ============================================================
// RecipeData.cs
// レシピマスターの ScriptableObject。
// 必要素材リスト + 完成品（DishData）+ 解放条件を定義する。
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// レシピ定義。必要素材と完成品（DishData）を結びつける。
/// <see cref="CookingManager"/> が参照し、調理の可否判定・品質決定を行う。
/// </summary>
[CreateAssetMenu(fileName = "RCP_New", menuName = "DevilsDiner/RecipeData")]
public sealed class RecipeData : ScriptableObject
{
    // ──────────────────────────────────────────────
    // 素材スロット定義
    // ──────────────────────────────────────────────

    /// <summary>レシピに必要な素材1種の情報。</summary>
    [Serializable]
    public struct IngredientSlot
    {
        [Tooltip("必要素材")]
        public IngredientData Ingredient;

        [Tooltip("必要数量"), Min(1)]
        public int Amount;
    }

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("基本情報")]
    [SerializeField] private string _recipeID;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea(2, 3)] private string _description;

    [Header("完成品")]
    [SerializeField, Tooltip("調理で生成される料理データ")]
    private DishData _outputDish;

    [Header("必要素材")]
    [SerializeField] private IngredientSlot[] _ingredients;

    [Header("解放条件")]
    [SerializeField, Tooltip("この値以上のシェフレベルで解放"), Min(1)]
    private int _requiredChefLevel = 1;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>一意のレシピ識別子。</summary>
    public string RecipeID => _recipeID;

    /// <summary>UI 表示名。</summary>
    public string DisplayName => _displayName;

    /// <summary>レシピの説明文。</summary>
    public string Description => _description;

    /// <summary>完成品の DishData。</summary>
    public DishData OutputDish => _outputDish;

    /// <summary>必要素材の配列。</summary>
    public IngredientSlot[] Ingredients => _ingredients;

    /// <summary>解放に必要なシェフレベル。</summary>
    public int RequiredChefLevel => _requiredChefLevel;

    /// <summary>料理カテゴリ（OutputDish から取得）。</summary>
    public DishCategory Category => _outputDish != null ? _outputDish.Category : DishCategory.Meat;

    // ──────────────────────────────────────────────
    // ヘルパー
    // ──────────────────────────────────────────────

    /// <summary>全素材の平均レア度を返す。品質計算に使用。</summary>
    public float AverageIngredientRarity()
    {
        if (_ingredients == null || _ingredients.Length == 0) return 1f;

        float total = 0f;
        int count = 0;
        foreach (var slot in _ingredients)
        {
            if (slot.Ingredient != null)
            {
                total += slot.Ingredient.Rarity * slot.Amount;
                count += slot.Amount;
            }
        }
        return count > 0 ? total / count : 1f;
    }
}
