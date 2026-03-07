// ============================================================
// DebugController.cs
// テストプレイ用のデバッグチート機能。
// GameManager と同一 GameObject に自動追加され、DontDestroyOnLoad で永続化。
// NOTE: プロダクションビルドでは無効化またはストリップすること。
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// デバッグ用チートコントローラー。
/// F1: テストアイテムを追加
/// F2: 所持金 +1000G
/// F4: 手動セーブ
/// F5: 手動ロード
/// </summary>
public sealed class DebugController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [SerializeField] private ItemData[] _debugItems;
    [SerializeField] private int _itemAddAmount = 10;
    [SerializeField] private int _goldAddAmount = 1000;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f1Key.wasPressedThisFrame)
        {
            AddDebugItems();
        }

        if (kb.f2Key.wasPressedThisFrame)
        {
            GameManager.Instance.AddGold(_goldAddAmount);
            Debug.Log($"[DebugController] 所持金 +{_goldAddAmount}G (現在: {GameManager.Instance.Gold}G)");
        }

        if (kb.f4Key.wasPressedThisFrame)
        {
            ManualSave();
        }

        if (kb.f5Key.wasPressedThisFrame)
        {
            ManualLoad();
        }
    }

    // ──────────────────────────────────────────────
    // 内部メソッド
    // ──────────────────────────────────────────────

    private void AddDebugItems()
    {
        ItemData[] items = _debugItems;

        if (items == null || items.Length == 0)
        {
            items = Resources.LoadAll<ItemData>("");
        }

        if (items == null || items.Length == 0)
        {
            Debug.LogWarning("[DebugController] 追加できる ItemData が見つかりません。");
            return;
        }

        foreach (ItemData item in items)
        {
            GameManager.Instance.Inventory.Add(item, _itemAddAmount);
            Debug.Log($"[DebugController] {item.DisplayName} x{_itemAddAmount} 追加");
        }

        Debug.Log($"[DebugController] デバッグアイテム追加完了！ {items.Length}種 x {_itemAddAmount}個");
    }

    private void ManualSave()
    {
        SaveDataManager saveManager = FindFirstObjectByType<SaveDataManager>();
        if (saveManager != null)
        {
            saveManager.Save();
            Debug.Log("[DebugController] F4: 手動セーブを実行しました。");
        }
        else
        {
            Debug.LogWarning("[DebugController] SaveDataManager が見つかりません。");
        }
    }

    private void ManualLoad()
    {
        SaveDataManager saveManager = FindFirstObjectByType<SaveDataManager>();
        if (saveManager != null)
        {
            if (saveManager.HasSaveData())
            {
                saveManager.Load();
                Debug.Log("[DebugController] F5: 手動ロードを実行しました。");
            }
            else
            {
                Debug.LogWarning("[DebugController] F5: セーブデータが存在しません。先に F4 でセーブしてください。");
            }
        }
        else
        {
            Debug.LogWarning("[DebugController] SaveDataManager が見つかりません。");
        }
    }
}
