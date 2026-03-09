// ============================================================
// ItemData.cs
// 全アイテム共通の基底 ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>
/// アイテムの基底データ。素材・料理・武器など全ての
/// アイテム系 ScriptableObject はこのクラスを継承する。
/// </summary>
[CreateAssetMenu(fileName = "ITEM_New", menuName = "DevilsDiner/Item/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private string _itemID;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea(2, 4)] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private int _sellPrice;

    /// <summary>一意のアイテム識別子。</summary>
    public string ItemID => _itemID;

    /// <summary>UI 表示名。</summary>
    public string DisplayName => _displayName;

    /// <summary>アイテム説明文。</summary>
    public string Description => _description;

    /// <summary>アイコン画像。</summary>
    public Sprite Icon => _icon;

    /// <summary>売却価格。</summary>
    public int SellPrice => _sellPrice;
}
