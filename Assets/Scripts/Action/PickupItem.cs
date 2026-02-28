// ============================================================
// PickupItem.cs
// ワールドに出現するドロップアイテムの物理オブジェクト。
// 敵撃破時に生成され、バウンドしながら落下し、
// プレイヤーが接触すると素材をインベントリに追加して消滅する。
// ============================================================
using UnityEngine;

/// <summary>
/// ドロップアイテムの物理表現。
/// スポーン時に上方＋ランダム水平方向の力が加わり、
/// プレイヤーとの接触で素材を回収して自動消滅する。
/// 物理衝突にはプリミティブの BoxCollider を使用し、
/// プレイヤー検出には追加の SphereCollider (Trigger) を使用する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class PickupItem : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float AUTO_DESTROY_TIME = 30f;
    private const float SPAWN_FORCE_MIN_Y = 3f;
    private const float SPAWN_FORCE_MAX_Y = 5f;
    private const float SPAWN_FORCE_HORIZONTAL = 1f;
    private const float PICKUP_TRIGGER_RADIUS = 0.8f;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private MaterialData _materialData;

    // ──────────────────────────────────────────────
    // 公開メソッド
    // ──────────────────────────────────────────────

    /// <summary>
    /// ドロップアイテムを初期化する。スポーン直後に呼び出すこと。
    /// </summary>
    /// <param name="data">このアイテムが表す素材データ。</param>
    public void Initialize(MaterialData data)
    {
        _materialData = data;
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        // 自動消滅タイマー
        Destroy(gameObject, AUTO_DESTROY_TIME);

        // プレイヤー検出用の Trigger SphereCollider を追加
        // （物理衝突にはプリミティブの BoxCollider を使用する）
        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = PICKUP_TRIGGER_RADIUS;

        // スポーン時の弾き飛ばし力
        if (TryGetComponent(out Rigidbody rb))
        {
            float forceY = Random.Range(SPAWN_FORCE_MIN_Y, SPAWN_FORCE_MAX_Y);
            float forceX = Random.Range(-SPAWN_FORCE_HORIZONTAL, SPAWN_FORCE_HORIZONTAL);
            float forceZ = Random.Range(-SPAWN_FORCE_HORIZONTAL, SPAWN_FORCE_HORIZONTAL);

            rb.AddForce(new Vector3(forceX, forceY, forceZ), ForceMode.Impulse);
        }
    }

    // ──────────────────────────────────────────────
    // トリガー検出
    // ──────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_materialData == null) return;

        GameManager.Instance.Inventory.AddMaterial(_materialData);
        Debug.Log($"[PickupItem] プレイヤーが素材を回収: {_materialData.MaterialName}");
        Destroy(gameObject);
    }
}
