// ============================================================
// InventoryManager.cs
// GameManager と同一 GameObject にアタッチされ、DontDestroyOnLoad で永続化。
// 素材・料理・武器・家具の在庫を一元管理する。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーのインベントリ（素材・料理・武器・家具）を管理する。
/// GameManager.Inventory でアクセスする。
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

    /// <summary>素材が追加されたとき。引数は (MaterialData, 変化後の個数)。</summary>
    public event Action<MaterialData, int> OnMaterialAdded;

    /// <summary>素材が消費されたとき。引数は (MaterialData, 変化後の個数)。</summary>
    public event Action<MaterialData, int> OnMaterialConsumed;

    /// <summary>料理（調理済み）が追加されたとき。</summary>
    public event Action<CookedDishData> OnDishAdded;

    /// <summary>料理が提供（消費）されたとき。</summary>
    public event Action<CookedDishData> OnDishServed;

    // ──────────────────────────────────────────────
    // データ構造
    // ──────────────────────────────────────────────

    /// <summary>素材 ID → 所持数。</summary>
    private readonly Dictionary<string, int> _materials = new Dictionary<string, int>();

    /// <summary>素材 ID → MaterialData 参照（逆引き用）。</summary>
    private readonly Dictionary<string, MaterialData> _materialDataMap
        = new Dictionary<string, MaterialData>();

    /// <summary>調理済み料理のストック。キューで先入れ先出し。</summary>
    private readonly Queue<CookedDishData> _cookedDishes = new Queue<CookedDishData>();

    /// <summary>所持武器リスト。</summary>
    private readonly List<WeaponData> _weapons = new List<WeaponData>();

    /// <summary>所持家具リスト。</summary>
    private readonly List<FurnitureData> _furniture = new List<FurnitureData>();

    // ──────────────────────────────────────────────
    // 公開 API — 素材
    // ──────────────────────────────────────────────

    /// <summary>素材を追加する。</summary>
    public void AddMaterial(MaterialData data, int amount = 1)
    {
        if (data == null || amount <= 0) return;

        string id = data.Id;

        if (!_materialDataMap.ContainsKey(id))
        {
            _materialDataMap[id] = data;
        }

        if (!_materials.ContainsKey(id))
        {
            _materials[id] = 0;
        }

        _materials[id] = Mathf.Min(_materials[id] + amount, MAX_STACK_SIZE);
        OnMaterialAdded?.Invoke(data, _materials[id]);
    }

    /// <summary>素材を消費する。不足時は false を返し何もしない。</summary>
    public bool TryConsumeMaterial(MaterialData data, int amount = 1)
    {
        if (data == null || amount <= 0) return false;

        string id = data.Id;

        if (!_materials.TryGetValue(id, out int current) || current < amount)
        {
            return false;
        }

        _materials[id] = current - amount;

        if (_materials[id] <= 0)
        {
            _materials.Remove(id);
            _materialDataMap.Remove(id);
        }

        OnMaterialConsumed?.Invoke(data, _materials.GetValueOrDefault(id, 0));
        return true;
    }

    /// <summary>レシピの必要素材をすべて持っているか判定する。</summary>
    public bool HasMaterialsForRecipe(RecipeData recipe)
    {
        if (recipe == null) return false;

        foreach (RecipeData.RequiredMaterial req in recipe.RequiredMaterials)
        {
            if (!_materials.TryGetValue(req.Material.Id, out int owned) || owned < req.Amount)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>レシピの必要素材をまとめて消費する。不足なら false（何も消費しない）。</summary>
    public bool TryConsumeMaterialsForRecipe(RecipeData recipe)
    {
        if (!HasMaterialsForRecipe(recipe)) return false;

        foreach (RecipeData.RequiredMaterial req in recipe.RequiredMaterials)
        {
            TryConsumeMaterial(req.Material, req.Amount);
        }
        return true;
    }

    /// <summary>指定素材の所持数を返す。</summary>
    public int GetMaterialCount(MaterialData data)
    {
        if (data == null) return 0;
        return _materials.GetValueOrDefault(data.Id, 0);
    }

    /// <summary>所持中の全素材を返す（読み取り専用）。</summary>
    public IReadOnlyDictionary<string, int> GetAllMaterials() => _materials;

    // ──────────────────────────────────────────────
    // 公開 API — 調理済み料理
    // ──────────────────────────────────────────────

    /// <summary>調理済み料理をストックに追加する。</summary>
    public void AddCookedDish(CookedDishData dish)
    {
        _cookedDishes.Enqueue(dish);
        OnDishAdded?.Invoke(dish);
    }

    /// <summary>ストックから料理を1つ取り出す（FIFO）。空なら null。</summary>
    public CookedDishData ServeDish()
    {
        if (_cookedDishes.Count == 0) return null;
        CookedDishData dish = _cookedDishes.Dequeue();
        OnDishServed?.Invoke(dish);
        return dish;
    }

    /// <summary>指定レシピの調理済み料理があるか確認する。</summary>
    public bool HasCookedDish(RecipeData recipe)
    {
        foreach (CookedDishData dish in _cookedDishes)
        {
            if (dish.OriginalRecipe == recipe) return true;
        }
        return false;
    }

    /// <summary>調理済み料理のストック数。</summary>
    public int CookedDishCount => _cookedDishes.Count;

    // ──────────────────────────────────────────────
    // 公開 API — 武器
    // ──────────────────────────────────────────────

    /// <summary>武器を追加する。</summary>
    public void AddWeapon(WeaponData weapon)
    {
        if (weapon != null) _weapons.Add(weapon);
    }

    /// <summary>所持武器リスト（読み取り専用）。</summary>
    public IReadOnlyList<WeaponData> Weapons => _weapons;

    // ──────────────────────────────────────────────
    // 公開 API — 家具
    // ──────────────────────────────────────────────

    /// <summary>家具を追加する。</summary>
    public void AddFurniture(FurnitureData item)
    {
        if (item != null) _furniture.Add(item);
    }

    /// <summary>所持家具リスト（読み取り専用）。</summary>
    public IReadOnlyList<FurnitureData> Furniture => _furniture;

    // ──────────────────────────────────────────────
    // 公開 API — 全クリア
    // ──────────────────────────────────────────────

    /// <summary>全インベントリを空にする。</summary>
    public void ClearAll()
    {
        _materials.Clear();
        _materialDataMap.Clear();
        _cookedDishes.Clear();
        _weapons.Clear();
        _furniture.Clear();
    }
}
