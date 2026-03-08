// ============================================================
// StaffBuffRoller.cs
// バトル終了時にスカウト済み悪魔のランダムバフを一括抽選する。
// BattleResultController から呼ばれる静的ユーティリティ。
// ============================================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スカウト成功した悪魔に対し、種族の PossibleBuffs から
/// 重み付き抽選でランダムバフを確定する。
/// バトル終了時に一括で実行し、結果を <see cref="RecruitedDemonData"/> に格納する。
/// </summary>
public static class StaffBuffRoller
{
    /// <summary>
    /// BattleManager.ScoutedEnemies を RecruitedDemonData リストに変換する。
    /// 種族マスターの検索は Resources.LoadAll で行う。
    /// </summary>
    public static List<RecruitedDemonData> RollAll(List<BattleManager.ScoutedEnemyRecord> scoutedEnemies)
    {
        var results = new List<RecruitedDemonData>();
        if (scoutedEnemies == null || scoutedEnemies.Count == 0) return results;

        // 種族マスター一覧をロード
        StaffRaceData[] allRaces = Resources.LoadAll<StaffRaceData>("");

        foreach (var enemy in scoutedEnemies)
        {
            StaffRaceData race = FindRace(enemy, allRaces);
            StaffBuffData[] rolledBuffs = race != null
                ? RollBuffs(race)
                : System.Array.Empty<StaffBuffData>();

            results.Add(new RecruitedDemonData
            {
                EnemyName = enemy.DisplayName,
                Stats = enemy.Stats,
                Race = race,
                RolledBuffs = rolledBuffs
            });

            if (race != null)
            {
                Debug.Log($"[StaffBuffRoller] {enemy.DisplayName} ({race.RaceName}): バフ {rolledBuffs.Length}個確定");
            }
            else
            {
                Debug.LogWarning($"[StaffBuffRoller] {enemy.DisplayName} に対応する種族マスターが見つかりません。デフォルト適用。");
            }
        }

        return results;
    }

    // ──────────────────────────────────────────────
    // 種族マッチング
    // ──────────────────────────────────────────────

    /// <summary>敵名や ID から対応する StaffRaceData を検索する。</summary>
    private static StaffRaceData FindRace(BattleManager.ScoutedEnemyRecord enemy, StaffRaceData[] allRaces)
    {
        if (allRaces == null || allRaces.Length == 0) return null;

        // EnemyData に直接紐付けされた種族を最優先
        if (enemy.EnemyData != null && enemy.EnemyData.StaffRace != null)
        {
            return enemy.EnemyData.StaffRace;
        }

        // Stats.Id で RaceID と照合
        if (enemy.Stats != null && !string.IsNullOrEmpty(enemy.Stats.Id))
        {
            foreach (var race in allRaces)
            {
                if (race.RaceID == enemy.Stats.Id) return race;
            }
        }

        // EnemyData.Id で照合
        if (enemy.EnemyData != null && !string.IsNullOrEmpty(enemy.EnemyData.Id))
        {
            foreach (var race in allRaces)
            {
                if (race.RaceID == enemy.EnemyData.Id) return race;
            }
        }

        // フォールバック: 最初の種族を返す（マスター未整備期間用）
        return allRaces.Length > 0 ? allRaces[0] : null;
    }

    // ──────────────────────────────────────────────
    // バフ抽選
    // ──────────────────────────────────────────────

    /// <summary>種族の候補プールから重み付きランダム抽選する。</summary>
    private static StaffBuffData[] RollBuffs(StaffRaceData race)
    {
        StaffBuffData[] pool = race.PossibleBuffs;
        if (pool == null || pool.Length == 0) return System.Array.Empty<StaffBuffData>();

        int count = Random.Range(race.MinBuffCount, race.MaxBuffCount + 1);
        count = Mathf.Min(count, pool.Length);

        var selected = new List<StaffBuffData>(count);
        var available = new List<StaffBuffData>(pool);

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            StaffBuffData picked = WeightedPick(available);
            if (picked != null)
            {
                selected.Add(picked);
                available.Remove(picked); // 重複を防ぐ
            }
        }

        return selected.ToArray();
    }

    /// <summary>重み付きランダム選択。Rarity が高いほど出にくい。</summary>
    private static StaffBuffData WeightedPick(List<StaffBuffData> candidates)
    {
        float totalWeight = 0f;
        foreach (var buff in candidates)
        {
            if (buff != null) totalWeight += buff.SelectionWeight;
        }

        if (totalWeight <= 0f) return candidates.Count > 0 ? candidates[0] : null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var buff in candidates)
        {
            if (buff == null) continue;
            cumulative += buff.SelectionWeight;
            if (roll <= cumulative) return buff;
        }

        return candidates[candidates.Count - 1];
    }
}
