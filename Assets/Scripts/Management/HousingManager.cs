// ============================================================
// HousingManager.cs
// 家具の購入・所有状態を管理する。
// GameManager.Awake() で AddComponent される。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 店舗の家具管理を担当するコンポーネント。
/// 購入済み家具のリストを保持し、営業ボーナスを算出する。
/// </summary>
public sealed class HousingManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 内部データ
    // ──────────────────────────────────────────────

    private readonly List<FurnitureData> _ownedFurniture = new List<FurnitureData>();

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>所有している家具一覧。</summary>
    public IReadOnlyList<FurnitureData> OwnedFurniture => _ownedFurniture;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>家具構成が変わったとき。</summary>
    public event Action OnFurnitureChanged;

    // ──────────────────────────────────────────────
    // 公開 API — 購入
    // ──────────────────────────────────────────────

    /// <summary>
    /// 家具を購入する。重複不可。所持金不足なら false。
    /// </summary>
    public bool TryBuyFurniture(FurnitureData furniture)
    {
        if (furniture == null) return false;
        if (_ownedFurniture.Contains(furniture)) return false;
        if (GameManager.Instance == null) return false;
        if (!GameManager.Instance.TrySpendGold(furniture.Price)) return false;

        _ownedFurniture.Add(furniture);
        OnFurnitureChanged?.Invoke();
        Debug.Log($"[HousingManager] 家具購入: {furniture.FurnitureName} ({furniture.Price}G)");
        return true;
    }

    /// <summary>指定の家具を所持しているか判定する。</summary>
    public bool Owns(FurnitureData furniture)
    {
        return furniture != null && _ownedFurniture.Contains(furniture);
    }

    // ──────────────────────────────────────────────
    // 公開 API — ボーナス算出
    // ──────────────────────────────────────────────

    /// <summary>全家具の接客満足度ボーナス合計を返す。</summary>
    public float GetTotalSatisfactionBonus()
    {
        float total = 0f;
        foreach (var f in _ownedFurniture)
            total += f.SatisfactionBonus;
        return total;
    }

    /// <summary>全家具の来客数ボーナス合計を返す。</summary>
    public int GetTotalCustomerBonus()
    {
        int total = 0;
        foreach (var f in _ownedFurniture)
            total += f.CustomerBonus;
        return total;
    }

    // ──────────────────────────────────────────────
    // 公開 API — セーブ/ロード
    // ──────────────────────────────────────────────

    /// <summary>所有家具の ID リストを返す（セーブ用）。</summary>
    public List<string> GetOwnedIDs()
    {
        var ids = new List<string>(_ownedFurniture.Count);
        foreach (var f in _ownedFurniture)
            ids.Add(f.Id);
        return ids;
    }

    /// <summary>ID リストから所有家具を復元する（ロード用）。</summary>
    public void RestoreOwned(List<string> ids)
    {
        _ownedFurniture.Clear();
        if (ids == null || ids.Count == 0) return;

        FurnitureData[] allFurniture = Resources.LoadAll<FurnitureData>("");
        var lookup = new Dictionary<string, FurnitureData>(allFurniture.Length);
        foreach (var f in allFurniture)
        {
            if (f != null && !string.IsNullOrEmpty(f.Id))
                lookup[f.Id] = f;
        }

        foreach (var id in ids)
        {
            if (lookup.TryGetValue(id, out var furniture))
                _ownedFurniture.Add(furniture);
            else
                Debug.LogWarning($"[HousingManager] 家具 ID '{id}' が見つかりません。スキップします。");
        }
    }

    // ──────────────────────────────────────────────
    // 公開 API — リセット
    // ──────────────────────────────────────────────

    /// <summary>所有家具をすべてクリアする。</summary>
    public void ClearAll()
    {
        _ownedFurniture.Clear();
    }
}
