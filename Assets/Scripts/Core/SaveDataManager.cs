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
        public int ChefLevel = 1;
        public int Reputation;
        public int CookingXP;

        /// <summary>所有家具の ID リスト。</summary>
        public List<string> OwnedFurniture;

        /// <summary>装備中の武器 ItemID。</summary>
        public string EquippedWeaponID;

        /// <summary>全アイテム共通エントリ。</summary>
        public List<ItemEntry> Items;

        /// <summary>常勤スタッフ。</summary>
        public List<StaffEntry> PermanentStaff;

        /// <summary>旧フォーマット互換用（読み込み専用）。</summary>
        public List<MaterialEntry> Materials;

        [Serializable]
        public class ItemEntry
        {
            public string ItemID;
            public int Amount;
            /// <summary>料理の品質。料理以外は null。</summary>
            public string Quality;
        }

        /// <summary>スタッフ保存用エントリ。</summary>
        [Serializable]
        public class StaffEntry
        {
            public string ID;
            public string SourceEnemyName;
            public string RaceID;
            public string[] BuffIDs;
            public int MoralePenalty;
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
            Reputation = gm.Reputation,
            Items      = new List<SaveData.ItemEntry>(),
            PermanentStaff = new List<SaveData.StaffEntry>()
        };

        // 汎用アイテムを ItemID ベースで保存
        foreach (var kvp in gm.Inventory.GetAllItems())
        {
            if (kvp.Key == null || string.IsNullOrEmpty(kvp.Key.ItemID)) continue;
            saveData.Items.Add(new SaveData.ItemEntry
            {
                ItemID = kvp.Key.ItemID,
                Amount = kvp.Value
            });
        }

        // 品質付き料理を保存
        foreach (var kvp in gm.Inventory.GetAllDishes())
        {
            if (kvp.Key.Data == null || string.IsNullOrEmpty(kvp.Key.Data.ItemID)) continue;
            saveData.Items.Add(new SaveData.ItemEntry
            {
                ItemID  = kvp.Key.Data.ItemID,
                Amount  = kvp.Value,
                Quality = kvp.Key.Quality.ToString()
            });
        }

        // 常勤スタッフを保存（臨時は翌朝消えるため保存しない）
        if (gm.Staff != null)
        {
            foreach (var staff in gm.Staff.PermanentStaff)
            {
                var buffIDs = new string[staff.RandomBuffs.Length];
                for (int i = 0; i < staff.RandomBuffs.Length; i++)
                {
                    buffIDs[i] = staff.RandomBuffs[i] != null ? staff.RandomBuffs[i].BuffID : "";
                }

                saveData.PermanentStaff.Add(new SaveData.StaffEntry
                {
                    ID = staff.ID,
                    SourceEnemyName = staff.SourceEnemyName,
                    RaceID = staff.Race != null ? staff.Race.RaceID : "",
                    BuffIDs = buffIDs,
                    MoralePenalty = staff.MoralePenalty
                });
            }
        }

        // シェフレベル・経験値を保存
        saveData.ChefLevel = gm.ChefLevel;
        saveData.CookingXP = gm.CookingXP;

        // 家具を保存
        if (gm.Housing != null)
        {
            saveData.OwnedFurniture = gm.Housing.GetOwnedIDs();
        }

        // 装備武器を保存
        saveData.EquippedWeaponID = gm.EquippedWeaponID;

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(FilePath, json);

        Debug.Log($"[SaveDataManager] セーブ完了 → {FilePath} ({saveData.Items.Count} アイテム, {saveData.PermanentStaff.Count} スタッフ)");
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
        gm.SetReputation(saveData.Reputation);
        gm.SetChefLevel(saveData.ChefLevel);
        gm.SetCookingXP(saveData.CookingXP);

        // ── 装備武器復元 ──
        gm.SetEquippedWeaponID(saveData.EquippedWeaponID);

        // ── 家具復元 ──
        if (gm.Housing != null && saveData.OwnedFurniture != null)
        {
            gm.Housing.RestoreOwned(saveData.OwnedFurniture);
        }

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
                if (!itemLookup.TryGetValue(entry.ItemID, out ItemData itemData))
                {
                    Debug.LogWarning($"[SaveDataManager] ItemID '{entry.ItemID}' に対応する ItemData が見つかりません。スキップします。");
                    continue;
                }

                // Quality フィールドが存在 & DishData なら料理専用ストアへ
                if (!string.IsNullOrEmpty(entry.Quality) && itemData is DishData dishData
                    && System.Enum.TryParse<DishQuality>(entry.Quality, out var quality))
                {
                    gm.Inventory.AddDish(new DishInstance(dishData, quality), entry.Amount);
                }
                else
                {
                    gm.Inventory.Add(itemData, entry.Amount);
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

        // ── スタッフ復元 ──
        if (saveData.PermanentStaff != null && saveData.PermanentStaff.Count > 0 && gm.Staff != null)
        {
            gm.Staff.ClearAll();

            // ルックアップ構築
            StaffRaceData[] allRaces = Resources.LoadAll<StaffRaceData>("");
            var raceLookup = new Dictionary<string, StaffRaceData>(allRaces.Length);
            foreach (var race in allRaces)
            {
                if (race != null && !string.IsNullOrEmpty(race.RaceID))
                    raceLookup[race.RaceID] = race;
            }

            StaffBuffData[] allBuffs = Resources.LoadAll<StaffBuffData>("");
            var buffLookup = new Dictionary<string, StaffBuffData>(allBuffs.Length);
            foreach (var buff in allBuffs)
            {
                if (buff != null && !string.IsNullOrEmpty(buff.BuffID))
                    buffLookup[buff.BuffID] = buff;
            }

            foreach (var entry in saveData.PermanentStaff)
            {
                StaffRaceData race = null;
                if (!string.IsNullOrEmpty(entry.RaceID))
                    raceLookup.TryGetValue(entry.RaceID, out race);

                var buffs = new List<StaffBuffData>();
                if (entry.BuffIDs != null)
                {
                    foreach (var buffID in entry.BuffIDs)
                    {
                        if (!string.IsNullOrEmpty(buffID) && buffLookup.TryGetValue(buffID, out var buff))
                            buffs.Add(buff);
                    }
                }

                var staff = new StaffInstance(entry.SourceEnemyName, race, buffs.ToArray(), StaffSlotType.Permanent);
                gm.Staff.TryHire(staff, StaffSlotType.Permanent);
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
