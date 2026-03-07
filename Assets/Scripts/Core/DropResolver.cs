// ============================================================
// DropResolver.cs
// 敵撃破時のドロップ判定ロジック（静的ユーティリティ）。
// ドロップ成功時はインベントリに直接追加する。
// ============================================================
using UnityEngine;

/// <summary>
/// 敵撃破時のアイテムドロップを判定する静的クラス。
/// バトル終了時に呼び出される。
/// </summary>
public static class DropResolver
{
    /// <summary>
    /// ドロップ判定を行い、成功時はインベントリに追加する。
    /// </summary>
    /// <param name="enemyData">撃破した敵のデータ。</param>
    /// <param name="isCritical">トドメがクリティカルだったか。</param>
    public static void ResolveDrop(EnemyData enemyData, bool isCritical)
    {
        if (enemyData == null) return;

        ItemData dropItem;
        float dropRate;

        if (isCritical && enemyData.DropItemJust != null)
        {
            dropItem = enemyData.DropItemJust;
            dropRate = enemyData.DropRateJust;
        }
        else
        {
            dropItem = enemyData.DropItemNormal;
            dropRate = enemyData.DropRateNormal;
        }

        if (dropItem == null) return;

        // スキル効果によるドロップ率ボーナスを加算
        float effectiveDropRate = Mathf.Clamp01(dropRate + SkillEffectApplier.DropRateBonus);

        if (Random.value <= effectiveDropRate)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Inventory.Add(dropItem);
                Debug.Log($"[DropResolver] ドロップ成功: {dropItem.DisplayName}");
            }
        }
    }
}
