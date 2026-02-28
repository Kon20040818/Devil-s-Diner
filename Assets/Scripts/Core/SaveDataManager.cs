// ============================================================
// SaveDataManager.cs
// GameManager と同一 GameObject にアタッチ。
// ゲーム進行データの JSON シリアライズ / デシリアライズを担当する。
// ============================================================
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
    [System.Serializable]
    public class SaveData
    {
        public int CurrentDay;
        public int Gold;
        public int ShopLevel;
        public List<MaterialEntry> Materials;
        public List<string> FurnitureIds;
        public List<string> UnlockedSkillIds;

        /// <summary>素材 1 エントリ分。</summary>
        [System.Serializable]
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

        // SaveData 構築
        var saveData = new SaveData
        {
            CurrentDay = gm.CurrentDay,
            Gold       = gm.Gold,
            ShopLevel  = gm.ShopLevel,
            Materials  = new List<SaveData.MaterialEntry>(),
            FurnitureIds    = new List<string>(),
            UnlockedSkillIds = new List<string>()
        };

        // 素材
        foreach (KeyValuePair<string, int> kvp in gm.Inventory.GetAllMaterials())
        {
            saveData.Materials.Add(new SaveData.MaterialEntry
            {
                Id     = kvp.Key,
                Amount = kvp.Value
            });
        }

        // 家具
        foreach (FurnitureData furniture in gm.Inventory.Furniture)
        {
            if (furniture != null)
            {
                saveData.FurnitureIds.Add(furniture.Id);
            }
        }

        // スキル
        SkillManager skillManager = FindFirstObjectByType<SkillManager>();
        if (skillManager != null)
        {
            saveData.UnlockedSkillIds = skillManager.GetUnlockedSkillIds();
        }

        // JSON 書き出し
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(FilePath, json);

        Debug.Log($"[SaveDataManager] セーブ完了 → {FilePath}");
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
        gm.SetShopLevel(saveData.ShopLevel);

        // ── インベントリクリア ──
        gm.Inventory.ClearAll();

        // ── 素材復元 ──
        if (saveData.Materials != null && saveData.Materials.Count > 0)
        {
            // Resources 内の全 MaterialData を ID で引けるようにする
            MaterialData[] allMaterials = Resources.LoadAll<MaterialData>("");
            var materialLookup = new Dictionary<string, MaterialData>(allMaterials.Length);
            foreach (MaterialData mat in allMaterials)
            {
                if (mat != null && !string.IsNullOrEmpty(mat.Id))
                {
                    materialLookup[mat.Id] = mat;
                }
            }

            foreach (SaveData.MaterialEntry entry in saveData.Materials)
            {
                if (materialLookup.TryGetValue(entry.Id, out MaterialData matData))
                {
                    gm.Inventory.AddMaterial(matData, entry.Amount);
                }
                else
                {
                    Debug.LogWarning($"[SaveDataManager] 素材 ID '{entry.Id}' に対応する MaterialData が見つかりません。スキップします。");
                }
            }
        }

        // ── 家具復元 ──
        if (saveData.FurnitureIds != null && saveData.FurnitureIds.Count > 0)
        {
            FurnitureData[] allFurniture = Resources.LoadAll<FurnitureData>("");
            var furnitureLookup = new Dictionary<string, FurnitureData>(allFurniture.Length);
            foreach (FurnitureData furn in allFurniture)
            {
                if (furn != null && !string.IsNullOrEmpty(furn.Id))
                {
                    furnitureLookup[furn.Id] = furn;
                }
            }

            foreach (string id in saveData.FurnitureIds)
            {
                if (furnitureLookup.TryGetValue(id, out FurnitureData furnData))
                {
                    gm.Inventory.AddFurniture(furnData);
                }
                else
                {
                    Debug.LogWarning($"[SaveDataManager] 家具 ID '{id}' に対応する FurnitureData が見つかりません。スキップします。");
                }
            }
        }

        // ── スキル復元 ──
        if (saveData.UnlockedSkillIds != null && saveData.UnlockedSkillIds.Count > 0)
        {
            SkillManager skillManager = FindFirstObjectByType<SkillManager>();
            if (skillManager != null)
            {
                skillManager.RestoreUnlockedSkills(saveData.UnlockedSkillIds);
            }
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
