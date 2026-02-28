// ============================================================
// SeatNode.cs
// 客が着席する席のノード。椅子のGameObjectにアタッチする。
// ============================================================
using UnityEngine;

/// <summary>
/// 席ノード。客NPCが着席する位置と占有状態を管理する。
/// </summary>
public sealed class SeatNode : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private CustomerAI _occupyingCustomer;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>この席が占有されているかどうか。</summary>
    public bool IsOccupied => _occupyingCustomer != null;

    /// <summary>現在この席に座っている客。</summary>
    public CustomerAI OccupyingCustomer => _occupyingCustomer;

    /// <summary>座る位置（ワールド座標）。</summary>
    public Vector3 SitPosition => transform.position;

    /// <summary>座る回転。</summary>
    public Quaternion SitRotation => transform.rotation;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>席を占有する。</summary>
    public void Occupy(CustomerAI customer)
    {
        _occupyingCustomer = customer;
    }

    /// <summary>席を解放する。</summary>
    public void Release()
    {
        _occupyingCustomer = null;
    }
}
