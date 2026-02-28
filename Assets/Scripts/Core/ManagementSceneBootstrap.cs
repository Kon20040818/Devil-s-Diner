// ============================================================
// ManagementSceneBootstrap.cs
// ManagementScene のブートストラップ。シーンロード時にスクリプト間の
// 参照を自動で結線し、Inspector での手動アタッチを削減する。
// ============================================================
using System.Reflection;
using UnityEngine;

/// <summary>
/// ManagementScene 起動時に各コンポーネント間の参照を自動結線するブートストラップ。
/// シーン内の GameObject にアタッチして使用する。
/// </summary>
public sealed class ManagementSceneBootstrap : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        WireAll();
    }

    // ──────────────────────────────────────────────
    // 結線メイン
    // ──────────────────────────────────────────────

    private void WireAll()
    {
        // コンポーネント検索
        CookingUI cookingUI = FindFirstObjectByType<CookingUI>();
        CookingMinigame cookingMinigame = FindFirstObjectByType<CookingMinigame>();
        CustomerSpawner customerSpawner = FindFirstObjectByType<CustomerSpawner>();
        SeatManager seatManager = FindFirstObjectByType<SeatManager>();
        DinerManager dinerManager = FindFirstObjectByType<DinerManager>();

        // ── 1. CookingUI → CookingMinigame, CookingConfig ──
        if (cookingUI != null)
        {
            if (cookingMinigame != null)
            {
                TryWireField(cookingUI, "_cookingMinigame", cookingMinigame);

                // CookingConfig を CookingMinigame の _config フィールドから取得して共有
                FieldInfo configField = typeof(CookingMinigame).GetField(
                    "_config", BindingFlags.NonPublic | BindingFlags.Instance);
                CookingConfig config = configField?.GetValue(cookingMinigame) as CookingConfig;

                if (config != null)
                {
                    TryWireField(cookingUI, "_cookingConfig", config);
                }
                else
                {
                    Debug.LogWarning("[ManagementSceneBootstrap] CookingMinigame の _config が未設定です。" +
                                     " CookingUI の CookingConfig を結線できません。");
                }
            }
            else
            {
                Debug.LogWarning("[ManagementSceneBootstrap] CookingMinigame が見つかりません。");
            }
        }
        else
        {
            Debug.LogWarning("[ManagementSceneBootstrap] CookingUI が見つかりません。");
        }

        // ── 2. CustomerSpawner → SeatManager, DinerManager ──
        if (customerSpawner != null)
        {
            if (seatManager != null)
            {
                TryWireField(customerSpawner, "_seatManager", seatManager);
            }
            else
            {
                Debug.LogWarning("[ManagementSceneBootstrap] SeatManager が見つかりません。");
            }

            if (dinerManager != null)
            {
                TryWireField(customerSpawner, "_dinerManager", dinerManager);
            }
            else
            {
                Debug.LogWarning("[ManagementSceneBootstrap] DinerManager が見つかりません。");
            }
        }
        else
        {
            Debug.LogWarning("[ManagementSceneBootstrap] CustomerSpawner が見つかりません。");
        }

        // ── 3. DinerManager 存在確認 ──
        if (dinerManager != null)
        {
            Debug.Log("[ManagementSceneBootstrap] DinerManager を検出しました。");
        }
        else
        {
            Debug.LogWarning("[ManagementSceneBootstrap] DinerManager が見つかりません。");
        }

        Debug.Log("[ManagementSceneBootstrap] Auto-wired: CookingUI, CustomerSpawner");
    }

    // ──────────────────────────────────────────────
    // リフレクション結線ヘルパー
    // ──────────────────────────────────────────────

    /// <summary>
    /// リフレクションで対象オブジェクトのプライベートフィールドに値を設定する。
    /// 既に値が設定済み（非 null）の場合は上書きしない。
    /// </summary>
    /// <typeparam name="T">設定する値の型。</typeparam>
    /// <param name="target">フィールドを持つオブジェクト。</param>
    /// <param name="fieldName">プライベートフィールド名。</param>
    /// <param name="value">設定する値。</param>
    /// <returns>値を設定した場合は true、スキップまたは失敗した場合は false。</returns>
    private static bool TryWireField<T>(object target, string fieldName, T value) where T : class
    {
        if (target == null || value == null) return false;

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Debug.LogWarning($"[ManagementSceneBootstrap] {target.GetType().Name} にフィールド '{fieldName}' が見つかりません。");
            return false;
        }

        if (field.GetValue(target) != null) return false;

        field.SetValue(target, value);
        return true;
    }
}
