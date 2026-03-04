// ============================================================
// InventoryManager.cs
// GameManager と同一 GameObject にアタッチされ、DontDestroyOnLoad で永続化。
// 素材・武器の在庫を一元管理する。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーのインベントリ（素材・武器）を管理する。
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

    // ──────────────────────────────────────────────
    // データ構造
    // ──────────────────────────────────────────────

    /// <summary>素材 ID → 所持数。</summary>
    private readonly Dictionary<string, int> _materials = new Dictionary<string, int>();

    /// <summary>素材 ID → MaterialData 参照（逆引き用）。</summary>
    private readonly Dictionary<string, MaterialData> _materialDataMap
        = new Dictionary<string, MaterialData>();

    /// <summary>所持武器リスト。</summary>
    private readonly List<WeaponData> _weapons = new List<WeaponData>();

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

    /// <summary>指定素材の所持数を返す。</summary>
    public int GetMaterialCount(MaterialData data)
    {
        if (data == null) return 0;
        return _materials.GetValueOrDefault(data.Id, 0);
    }

    /// <summary>所持中の全素材を返す（読み取り専用）。</summary>
    public IReadOnlyDictionary<string, int> GetAllMaterials() => _materials;

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
    // 公開 API — 全クリア
    // ──────────────────────────────────────────────

    /// <summary>全インベントリを空にする。</summary>
    public void ClearAll()
    {
        _materials.Clear();
        _materialDataMap.Clear();
        _weapons.Clear();
    }
}
