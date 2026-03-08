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
}
