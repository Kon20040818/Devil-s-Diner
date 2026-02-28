// ============================================================
// RecipeData.cs
// レシピデータの ScriptableObject。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>レシピデータ。必要素材と売値を定義する。</summary>
[CreateAssetMenu(fileName = "RCP_New", menuName = "DevilsDiner/RecipeData")]
public sealed class RecipeData : ScriptableObject
{
    [Serializable]
    public struct RequiredMaterial
    {
        public MaterialData Material;
        public int Amount;
    }

    [SerializeField] private string _id;
    [SerializeField] private string _recipeName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private List<RequiredMaterial> _requiredMaterials;
    [SerializeField] private int _basePrice = 100;
    [SerializeField] private float _perfectMultiplier = 1.5f;
    [SerializeField] private GameObject _modelPrefab;

    public string                      Id                 => _id;
    public string                      RecipeName         => _recipeName;
    public Sprite                      Icon               => _icon;
    public IReadOnlyList<RequiredMaterial> RequiredMaterials => _requiredMaterials;
    public int                         BasePrice          => _basePrice;
    public float                       PerfectMultiplier  => _perfectMultiplier;
    public GameObject                  ModelPrefab        => _modelPrefab;
}
