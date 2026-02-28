// ============================================================
// OrderQueue.cs
// 客の注文をキュー管理し、調理済み料理の提供を仲介する。
// ============================================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CustomerAI の注文と料理提供を仲介するオーダーキュー。
/// CustomerAI.OnCustomerOrdered → キュー追加 → 料理ストックから提供。
/// </summary>
public sealed class OrderQueue : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 内部データ
    // ──────────────────────────────────────────────

    private struct Order
    {
        public CustomerAI Customer;
    }

    private readonly Queue<Order> _orderQueue = new Queue<Order>();

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>未処理の注文数。</summary>
    public int PendingOrderCount => _orderQueue.Count;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 注文をキューに追加する。CustomerAI.OnCustomerOrdered から呼ばれる。
    /// </summary>
    public void EnqueueOrder(CustomerAI customer)
    {
        if (customer == null) return;
        _orderQueue.Enqueue(new Order { Customer = customer });
    }

    /// <summary>
    /// キューの先頭の注文を処理し、InventoryManager から料理を取り出して提供する。
    /// 料理ストックが空の場合は false を返す。
    /// </summary>
    public bool TryProcessNextOrder()
    {
        if (_orderQueue.Count == 0) return false;

        InventoryManager inventory = GameManager.Instance.Inventory;
        if (inventory.CookedDishCount == 0) return false;

        Order order = _orderQueue.Dequeue();

        if (order.Customer == null) return false;

        CookedDishData dish = inventory.ServeDish();
        if (dish == null) return false;

        order.Customer.ServeDish(dish);
        return true;
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Update()
    {
        // 毎フレーム、処理可能な注文があれば自動で提供を試行
        if (_orderQueue.Count > 0 && GameManager.Instance.Inventory.CookedDishCount > 0)
        {
            TryProcessNextOrder();
        }
    }
}
