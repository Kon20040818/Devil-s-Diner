// ============================================================
// SaveDataManager.cs
// GameManager と同一 GameObject にアタッチ。
// ゲーム進行データの JSON シリアライズ / デシリアライズを担当する。
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// セーブ / ロードを管理するコンポーネント。
/// <see cref="GameManager"/> と同じ GameObject に配置される。
/// </summary>
public sealed class SaveDataManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // セーブデータ構造
    // ──────────────────────────────────────────────

    /// <summary>JSON に書き出すゲーム進行データ。</summary>
    [Serializable]
    public class SaveData
    {
        public int CurrentDay;
        public int Gold;

        /// <summary>全アイテム共通エントリ。</summary>
        public List<ItemEntry> Items;

        /// <summary>旧フォーマット互換用（読み込み専用）。</summary>
        public List<MaterialEntry> Materials;

        [Serializable]
        public class ItemEntry
        {
            public string ItemID;
            public int Amount;
        }

        /// <summary>旧フォーマット互換用。</summary>
        [Serializable]
        public class MaterialEntry
        {
            public string Id;
            public int Amount;
        }
    }

    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const string SAVE_FILE_NAME = "save_data.json";

    // ──────────────────────────────────────────────
    // 内部ヘルパー
    // ──────────────────────────────────────────────

    /// <summary>セーブファイルのフルパスを返す。</summary>
    private static string FilePath
        => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

    // ──────────────────────────────────────────────
    // 公開 API — セーブ
    // ──────────────────────────────────────────────

    /// <summary>現在のゲーム進行データを JSON ファイルへ保存する。</summary>
    public void Save()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[SaveDataManager] GameManager.Instance が null のためセーブを中断しました。");
            return;
        }

        var saveData = new SaveData
        {
            CurrentDay = gm.CurrentDay,
            Gold       = gm.Gold,
            Items      = new List<SaveData.ItemEntry>()
        };

        // 全アイテムを ItemID ベースで保存
        foreach (var kvp in gm.Inventory.GetAllItems())
        {
            if (kvp.Key == null || string.IsNullOrEmpty(kvp.Key.ItemID)) continue;
            saveData.Items.Add(new SaveData.ItemEntry
            {
                ItemID = kvp.Key.ItemID,
                Amount = kvp.Value
            });
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(FilePath, json);

        Debug.Log($"[SaveDataManager] セーブ完了 → {FilePath} ({saveData.Items.Count} アイテム)");
    }

    // ──────────────────────────────────────────────
    // 公開 API — ロード
    // ──────────────────────────────────────────────

    /// <summary>JSON ファイルからゲーム進行データを復元する。</summary>
    public void Load()
    {
        if (!HasSaveData())
        {
            Debug.LogWarning("[SaveDataManager] セーブデータが見つかりません。");
            return;
        }

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[SaveDataManager] GameManager.Instance が null のためロードを中断しました。");
            return;
        }

        string json = File.ReadAllText(FilePath);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        if (saveData == null)
        {
            Debug.LogError("[SaveDataManager] セーブデータのデシリアライズに失敗しました。");
            return;
        }

        // ── 基本パラメータ復元 ──
        gm.SetCurrentDay(saveData.CurrentDay);
        gm.SetGold(saveData.Gold);

        // ── インベントリクリア ──
        gm.Inventory.ClearAll();

        // ── ItemData ルックアップ構築 ──
        ItemData[] allItems = Resources.LoadAll<ItemData>("");
        var itemLookup = new Dictionary<string, ItemData>(allItems.Length);
        foreach (ItemData item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.ItemID))
                itemLookup[item.ItemID] = item;
        }

        // ── 新フォーマット (Items) でロード ──
        if (saveData.Items != null && saveData.Items.Count > 0)
        {
            foreach (var entry in saveData.Items)
            {
                if (itemLookup.TryGetValue(entry.ItemID, out ItemData itemData))
                {
                    gm.Inventory.Add(itemData, entry.Amount);
                }
                else
                {
                    Debug.LogWarning($"[SaveDataManager] ItemID '{entry.ItemID}' に対応する ItemData が見つかりません。スキップします。");
                }
            }
        }
        // ── 旧フォーマット (Materials) との後方互換 ──
        else if (saveData.Materials != null && saveData.Materials.Count > 0)
        {
            Debug.Log("[SaveDataManager] 旧フォーマット (Materials) を検出。互換ロードを実行します。");

#pragma warning disable CS0612, CS0618
            MaterialData[] allMaterials = Resources.LoadAll<MaterialData>("");
            var matLookup = new Dictionary<string, MaterialData>(allMaterials.Length);
            foreach (MaterialData mat in allMaterials)
            {
                if (mat != null && !string.IsNullOrEmpty(mat.Id))
                    matLookup[mat.Id] = mat;
            }

            foreach (var entry in saveData.Materials)
            {
                if (matLookup.TryGetValue(entry.Id, out MaterialData matData))
                {
                    gm.Inventory.AddMaterial(matData, entry.Amount);
                }
                else
                {
                    Debug.LogWarning($"[SaveDataManager] 素材 ID '{entry.Id}' に対応する MaterialData が見つかりません。スキップします。");
                }
            }
#pragma warning restore CS0612, CS0618
        }

        Debug.Log("[SaveDataManager] ロード完了。");
    }

    // ──────────────────────────────────────────────
    // 公開 API — ユーティリティ
    // ──────────────────────────────────────────────

    /// <summary>セーブデータが存在するかを返す。</summary>
    public bool HasSaveData() => File.Exists(FilePath);

    /// <summary>セーブデータを削除する。</summary>
    public void DeleteSaveData()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
            Debug.Log("[SaveDataManager] セーブデータを削除しました。");
        }
    }
}
