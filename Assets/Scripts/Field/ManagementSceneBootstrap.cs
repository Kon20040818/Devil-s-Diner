// ============================================================
// ManagementSceneBootstrap.cs
// ManagementScene（経営パート）のブートストラップ。
// ============================================================
using UnityEngine;

/// <summary>
/// ManagementScene 起動時にシステムを初期化するブートストラップ。
/// </summary>
public sealed class ManagementSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        EnsureGameManagerExists();

        // カーソル解放
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // CookingManager を配置
        CookingManager cookingMgr = FindFirstObjectByType<CookingManager>();
        if (cookingMgr == null)
        {
            var go = new GameObject("CookingManager");
            cookingMgr = go.AddComponent<CookingManager>();
        }

        // DinerService を配置
        DinerService dinerService = FindFirstObjectByType<DinerService>();
        if (dinerService == null)
        {
            var go = new GameObject("DinerService");
            dinerService = go.AddComponent<DinerService>();
        }

        // ManagementSceneUI 検索 & 初期化
        ManagementSceneUI managementUI = FindFirstObjectByType<ManagementSceneUI>();
        if (managementUI == null)
        {
            Debug.LogWarning("[ManagementSceneBootstrap] ManagementSceneUI が見つかりません。");
        }
        else
        {
            managementUI.Initialize(cookingMgr, dinerService);
        }

        Debug.Log("[ManagementSceneBootstrap] 経営シーン結線完了。");
    }

    /// <summary>
    /// 単独シーン再生時の開発用フォールバック。
    /// BootScene を経由せずに直接 Play した場合に GameManager を自動生成する。
    /// </summary>
    private static void EnsureGameManagerExists()
    {
        if (GameManager.Instance != null) return;
        var go = new GameObject("GameManager [Fallback]");
        go.AddComponent<GameManager>();
        Debug.LogWarning("[ManagementSceneBootstrap] GameManager フォールバック生成。通常は BootScene から起動してください。");
    }
}
