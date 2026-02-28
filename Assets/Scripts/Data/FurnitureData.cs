// ============================================================
// FurnitureData.cs
// 家具データの ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>家具データ。居心地度ボーナスを定義する。</summary>
[CreateAssetMenu(fileName = "FRN_New", menuName = "DevilsDiner/FurnitureData")]
public sealed class FurnitureData : ScriptableObject
{
    public enum FurnitureType
    {
        Table,
        Chair,
        Decoration,
        Lighting,
        Kitchen
    }

    [SerializeField] private string _id;
    [SerializeField] private string _furnitureName;
    [SerializeField] private FurnitureType _type;
    [SerializeField] private int _price = 100;
    [SerializeField] private float _comfortBonus;
    [SerializeField] private GameObject _prefab;

    public string        Id            => _id;
    public string        FurnitureName => _furnitureName;
    public FurnitureType Type          => _type;
    public int           Price         => _price;
    public float         ComfortBonus  => _comfortBonus;
    public GameObject    Prefab        => _prefab;
}
