// ============================================================
// FurnitureData.cs
// 家具データの ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>家具データ。店舗改装で購入し、営業ボーナスを得る。</summary>
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
    [SerializeField, TextArea] private string _description;
    [SerializeField] private FurnitureType _type;
    [SerializeField, Min(0)] private int _price = 100;
    [SerializeField, Tooltip("居心地ボーナス（レガシー）")]
    private float _comfortBonus;
    [SerializeField, Tooltip("接客満足度ボーナス倍率")]
    private float _satisfactionBonus;
    [SerializeField, Tooltip("来客数ボーナス")]
    private int _customerBonus;
    [SerializeField] private GameObject _prefab;

    public string        Id                => _id;
    public string        FurnitureName     => _furnitureName;
    public string        Description       => _description;
    public FurnitureType Type              => _type;
    public int           Price             => _price;
    public float         ComfortBonus      => _comfortBonus;
    public float         SatisfactionBonus => _satisfactionBonus;
    public int           CustomerBonus     => _customerBonus;
    public GameObject    Prefab            => _prefab;
}
