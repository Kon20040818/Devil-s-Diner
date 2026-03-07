// ============================================================
// InventoryManager.cs
// GameManager と同一 GameObject にアタッチされ、DontDestroyOnLoad で永続化。
// アイテム（素材・料理・武器など）の在庫を一元管理する。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーのインベントリを管理する。
/// GameManager.Inventory でアクセスする。
/// 全アイテムを Dictionary&lt;ItemData, int&gt; の単一ストアで管理する。
/// </summary>
public sealed class InventoryManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const int MAX_STACK_SIZE = 999;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>インベントリの内容が変化したとき。</summary>
    public event Action OnInventoryChanged;

    // ──────────────────────────────────────────────
    // データ構造 — 単一ストア
    // ──────────────────────────────────────────────

    /// <summary>ItemData → 所持数。全アイテム共通。</summary>
    private readonly Dictionary<ItemData, int> _items = new Dictionary<ItemData, int>();

    // ──────────────────────────────────────────────
    // 公開 API — 汎用 CRUD
    // ──────────────────────────────────────────────

    /// <summary>アイテムを追加する。</summary>
    public void Add(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return;

        if (_items.TryGetValue(item, out int current))
            _items[item] = Mathf.Min(current + amount, MAX_STACK_SIZE);
        else
            _items[item] = Mathf.Min(amount, MAX_STACK_SIZE);

        OnInventoryChanged?.Invoke();
    }

    /// <summary>アイテムを除去する。不足時は false を返し何もしない。</summary>
    public bool Remove(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (!_items.TryGetValue(item, out int current) || current < amount)
            return false;

        int remaining = current - amount;
        if (remaining <= 0)
            _items.Remove(item);
        else
            _items[item] = remaining;

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>指定アイテムの所持数を返す。</summary>
    public int GetCount(ItemData item)
    {
        if (item == null) return 0;
        return _items.TryGetValue(item, out int count) ? count : 0;
    }

    /// <summary>指定アイテムを所持しているか。</summary>
    public bool Has(ItemData item, int requiredAmount = 1)
    {
        return GetCount(item) >= requiredAmount;
    }

    /// <summary>所持中の全アイテムを返す（読み取り専用）。</summary>
    public IReadOnlyDictionary<ItemData, int> GetAllItems() => _items;

    /// <summary>指定型のアイテムだけを列挙する。</summary>
    public List<KeyValuePair<T, int>> GetItemsOfType<T>() where T : ItemData
    {
        var result = new List<KeyValuePair<T, int>>();
        foreach (var kvp in _items)
        {
            if (kvp.Key is T typed)
                result.Add(new KeyValuePair<T, int>(typed, kvp.Value));
        }
        return result;
    }

    // ──────────────────────────────────────────────
    // 公開 API — 全クリア
    // ──────────────────────────────────────────────

    /// <summary>全インベントリを空にする。</summary>
    public void ClearAll()
    {
        _items.Clear();
#pragma warning disable CS0612, CS0618
        _legacyMaterials.Clear();
#pragma warning restore CS0612, CS0618
        OnInventoryChanged?.Invoke();
    }

    // ──────────────────────────────────────────────
    // 旧API互換ラッパー（廃止予定）
    // ──────────────────────────────────────────────

#pragma warning disable CS0612, CS0618 // Obsolete 警告を抑制

    /// <summary>[Obsolete] Add(ItemData, int) を使用してください。</summary>
    [Obsolete("AddMaterial は廃止予定です。Add(ItemData, int) を使用してください。")]
    public void AddMaterial(MaterialData data, int amount = 1)
    {
        if (data == null || amount <= 0) return;
        if (!_legacyMaterials.TryGetValue(data.Id, out var e))
            e = new LegacyMaterialEntry { Data = data, Count = 0 };
        e.Count = Mathf.Min(e.Count + amount, MAX_STACK_SIZE);
        _legacyMaterials[data.Id] = e;
        OnInventoryChanged?.Invoke();
    }

    /// <summary>[Obsolete] Remove(ItemData, int) を使用してください。</summary>
    [Obsolete("TryConsumeMaterial は廃止予定です。Remove(ItemData, int) を使用してください。")]
    public bool TryConsumeMaterial(MaterialData data, int amount = 1)
    {
        if (data == null || amount <= 0) return false;
        if (!_legacyMaterials.TryGetValue(data.Id, out var e) || e.Count < amount)
            return false;
        e.Count -= amount;
        if (e.Count <= 0)
            _legacyMaterials.Remove(data.Id);
        else
            _legacyMaterials[data.Id] = e;
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>[Obsolete] GetCount(ItemData) を使用してください。</summary>
    [Obsolete("GetMaterialCount は廃止予定です。GetCount(ItemData) を使用してください。")]
    public int GetMaterialCount(MaterialData data)
    {
        if (data == null) return 0;
        return _legacyMaterials.TryGetValue(data.Id, out var e) ? e.Count : 0;
    }

    /// <summary>[Obsolete] GetAllItems() を使用してください。</summary>
    [Obsolete("GetAllMaterials は廃止予定です。GetAllItems() を使用してください。")]
    public IReadOnlyDictionary<string, int> GetAllMaterials()
    {
        var result = new Dictionary<string, int>();
        foreach (var kvp in _legacyMaterials)
            result[kvp.Key] = kvp.Value.Count;
        return result;
    }

    /// <summary>[Obsolete] Add(WeaponData) を使用してください。</summary>
    [Obsolete("AddWeapon は廃止予定です。Add(ItemData, int) を使用してください。")]
    public void AddWeapon(WeaponData weapon)
    {
        if (weapon != null) Add(weapon, 1);
    }

    /// <summary>[Obsolete] GetItemsOfType&lt;WeaponData&gt;() を使用してください。</summary>
    [Obsolete("Weapons は廃止予定です。GetItemsOfType<WeaponData>() を使用してください。")]
    public IReadOnlyList<WeaponData> Weapons
    {
        get
        {
            var list = new List<WeaponData>();
            foreach (var kvp in _items)
            {
                if (kvp.Key is WeaponData w)
                    list.Add(w);
            }
            return list;
        }
    }

    // 旧 MaterialData 用の内部ストレージ（MaterialData は ItemData 非継承のため）
    private struct LegacyMaterialEntry
    {
        public MaterialData Data;
        public int Count;
    }
    private readonly Dictionary<string, LegacyMaterialEntry> _legacyMaterials
        = new Dictionary<string, LegacyMaterialEntry>();

#pragma warning restore CS0612, CS0618
}
