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
/// F1: ダミー素材を全種10個ずつ追加
/// F2: 所持金 +1000G
/// F4: 手動セーブ
/// F5: 手動ロード
/// </summary>
public sealed class DebugController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [SerializeField] private MaterialData[] _debugMaterials;
    [SerializeField] private int _materialAddAmount = 10;
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
            AddDebugMaterials();
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

    private void AddDebugMaterials()
    {
        MaterialData[] materials = _debugMaterials;

        if (materials == null || materials.Length == 0)
        {
            materials = Resources.LoadAll<MaterialData>("");
        }

        if (materials == null || materials.Length == 0)
        {
            Debug.LogWarning("[DebugController] 追加できる MaterialData が見つかりません。");
            return;
        }

        foreach (MaterialData mat in materials)
        {
            GameManager.Instance.Inventory.AddMaterial(mat, _materialAddAmount);
            Debug.Log($"[DebugController] {mat.name} x{_materialAddAmount} 追加");
        }

        Debug.Log($"[DebugController] デバッグ素材追加完了！ {materials.Length}種 x {_materialAddAmount}個");
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
