// ============================================================
// DropResolver.cs
// 敵撃破時のドロップ判定ロジック（静的ユーティリティ）。
// ドロップ成功時はワールドに PickupItem を生成する。
// ============================================================
using UnityEngine;

/// <summary>
/// 敵撃破時の素材ドロップを判定する静的クラス。
/// EnemyController から呼び出される。
/// </summary>
public static class DropResolver
{
    /// <summary>
    /// ドロップ判定を行い、成功時はワールドに PickupItem を生成する。
    /// </summary>
    /// <param name="enemyData">撃破した敵のデータ。</param>
    /// <param name="wasJustInput">トドメがジャスト入力だったか。</param>
    /// <param name="spawnPosition">ドロップアイテムの生成位置。</param>
    public static void ResolveDrop(EnemyData enemyData, bool wasJustInput, Vector3 spawnPosition)
    {
        if (enemyData == null) return;

        MaterialData dropItem;
        float dropRate;

        if (wasJustInput && enemyData.DropItemJust != null)
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
            SpawnPickupItem(dropItem, spawnPosition);
        }
    }

    // ──────────────────────────────────────────────
    // 内部ヘルパー
    // ──────────────────────────────────────────────

    /// <summary>
    /// PickupItem をワールドに生成する。
    /// </summary>
    /// <param name="data">ドロップする素材データ。</param>
    /// <param name="position">生成基準位置。</param>
    private static void SpawnPickupItem(MaterialData data, Vector3 position)
    {
        // Cube プリミティブを生成
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"PickupItem_{data.MaterialName}";
        obj.transform.position = position + Vector3.up * 0.5f;
        obj.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        // 視認性のためシアン色に設定
        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0f, 1f, 1f, 1f);
        }

        // PickupItem コンポーネントを追加・初期化
        var pickupItem = obj.AddComponent<PickupItem>();
        pickupItem.Initialize(data);
    }
}
